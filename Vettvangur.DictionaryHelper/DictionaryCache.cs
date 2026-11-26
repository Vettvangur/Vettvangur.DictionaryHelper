using DictionaryHelper.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using DictionaryItem = DictionaryHelper.Models.DictionaryItem;

namespace DictionaryHelper;

public class DictionaryCache
{
    // Keyed "{Key}-{Culture}". Case-insensitive, because every lookup compares key and culture
    // case-insensitively: that lets a lookup go straight to its entry instead of reading them all.
    public static ConcurrentDictionary<string, DictionaryItem> _cache = new ConcurrentDictionary<string, DictionaryItem>(StringComparer.OrdinalIgnoreCase);
    public static ConcurrentDictionary<string, ILanguage> _languages = new ConcurrentDictionary<string, ILanguage>();

    // Each item's database id (cmsDictionary.pk). Umbraco tells every server about a removal by
    // that id only, after the item is gone, so this is how a removal finds the item it is about.
    private static readonly ConcurrentDictionary<int, Guid> _idsByPk = new ConcurrentDictionary<int, Guid>();

    private readonly ILanguageService _languageService;
    private readonly DictionaryRepository _repository;
    private readonly ILogger<DictionaryCache> _logger;
    public DictionaryCache(
        ILanguageService languageService,
        DictionaryRepository repository,
        ILogger<DictionaryCache> logger)
    {
        _languageService = languageService;
        _repository = repository;
        _logger = logger;
    }

    public async Task FillAsync(CancellationToken ct = default)
    {
        try
        {
            var allKeys = await _repository.GetAllKeysAsync(ct).ConfigureAwait(false);
            var allTexts = await _repository.GetAllTextAsync(ct).ConfigureAwait(false);

            var allLanguages = await _languageService.GetAllAsync().ConfigureAwait(false);

            if (allLanguages == null || !allLanguages.Any())
            {
                return;
            }

            Populate(allKeys, allTexts, allLanguages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fill dictionary cache.");
        }

    }

    /// <summary>
    /// Fills the cache from the dictionary tables in one pass: the texts are grouped by key first,
    /// so each key finds its own texts directly instead of searching every text.
    /// </summary>
    internal static void Populate(IEnumerable<CmsDictionary> allKeys, IEnumerable<CmsLanguageText> allTexts, IEnumerable<ILanguage> allLanguages)
        => Populate(_cache, allKeys, allTexts, allLanguages);

    static void Populate(ConcurrentDictionary<string, DictionaryItem> target, IEnumerable<CmsDictionary> allKeys, IEnumerable<CmsLanguageText> allTexts, IEnumerable<ILanguage> allLanguages)
    {
        var languages = allLanguages.ToList();

        foreach (var lang in languages)
        {
            _languages[lang.IsoCode] = lang;
        }

        // The texts of each key, in the order the database returned them
        var textsByKey = allTexts.ToLookup(x => x.UniqueId);

        // The first language with each id, as a search through the list would find
        var languagesById = new Dictionary<int, ILanguage>();
        foreach (var lang in languages)
        {
            languagesById.TryAdd(lang.Id, lang);
        }

        foreach (var key in allKeys)
        {
            _idsByPk[key.pk] = key.id;
            var hasTexts = false;

            foreach (var text in textsByKey[key.id])
            {
                hasTexts = true;

                // Find language based on text's languageId
                languagesById.TryGetValue(text.languageId, out var language);
                AddToCache(target, key, text?.value ?? "", language?.IsoCode ?? "");
            }

            if (!hasTexts)
            {
                // No texts for the key; add empty values for all languages
                foreach (var language in languages)
                {
                    AddToCache(target, key, "", language?.IsoCode ?? "");
                }
            }
        }
    }

    static void AddToCache(ConcurrentDictionary<string, DictionaryItem> target, CmsDictionary key, string value, string culture)
    {
        var dictionaryItem = new DictionaryItem
        {
            Id = key.id,
            Key = key.key,
            Value = value,
            Culture = culture,
            Parent = key.parent
        };

        target.TryAdd($"{dictionaryItem.Key}-{dictionaryItem.Culture}", dictionaryItem);
    }

    public async Task AddOrUpdateAsync(string key, Guid Id, string value, Guid? parent, string culture = null)
    {
        if (culture == null)
        {
            var allLanguages = await _languageService.GetAllAsync().ConfigureAwait(false);

            foreach (var language in allLanguages)
            {
                AddOrUpdateItem(key, Id, value, parent, language.IsoCode);
            }
        }
        else
        {
            AddOrUpdateItem(key, Id, value, parent, culture);
        }
    }

    private void AddOrUpdateItem(string key, Guid id, string value, Guid? parent, string culture)
    {
        var dictionary = new DictionaryItem()
        {
            Id = id,
            Key = key,
            Value = value,
            Culture = culture,
            Parent = parent.HasValue ? parent.Value : Guid.Empty
        };

        _cache.AddOrUpdate(dictionary.Key + "-" + dictionary.Culture, dictionary, (k, oldValue) => dictionary);
    }

    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// Reloads one item from the database after it was saved on this server or another one, so a
    /// changed text or a new name shows up on every server.
    /// </summary>
    internal async Task RefreshItemAsync(int pk)
    {
        var row = await _repository.GetKeyAsync(pk).ConfigureAwait(false);

        if (row == null)
        {
            // Removed again in the meantime
            RemoveItem(pk);
            return;
        }

        var texts = await _repository.GetTextsAsync(row.id).ConfigureAwait(false);
        var languages = await _languageService.GetAllAsync().ConfigureAwait(false);

        ReplaceItem(row, texts, languages);
    }

    /// <summary>
    /// Puts one item in the cache the way filling it would, over what was there, and drops the
    /// entries of a name the item no longer has.
    /// </summary>
    internal static void ReplaceItem(CmsDictionary row, IEnumerable<CmsLanguageText> texts, IEnumerable<ILanguage> languages)
    {
        var fresh = new ConcurrentDictionary<string, DictionaryItem>(StringComparer.OrdinalIgnoreCase);
        Populate(fresh, new[] { row }, texts, languages);

        foreach (var entry in fresh)
        {
            _cache[entry.Key] = entry.Value;
        }

        ForgetOtherNames(row.id, row.key);
    }

    /// <summary>
    /// After an item is renamed, removes the entries still under its old name, so the old name
    /// stops returning its text.
    /// </summary>
    internal static void ForgetOtherNames(Guid id, string key)
    {
        foreach (var entry in _cache)
        {
            if (entry.Value.Id == id && !entry.Value.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                _cache.TryRemove(entry);
            }
        }
    }

    /// <summary>Records an item's database id, for a removal that comes later.</summary>
    internal static void RememberPk(int pk, Guid id)
    {
        if (pk > 0)
        {
            _idsByPk[pk] = id;
        }
    }

    /// <summary>Removes an item that was deleted, by its database id, with everything under it.</summary>
    internal static void RemoveItem(int pk)
    {
        if (_idsByPk.TryRemove(pk, out var id))
        {
            RemoveItemAndDescendants(id);
        }
    }

    /// <summary>
    /// Removes an item and everything under it in every language. Umbraco deletes the
    /// descendants along with the item without telling anyone about them.
    /// </summary>
    internal static void RemoveItemAndDescendants(Guid id)
    {
        var ids = new HashSet<Guid> { id };
        bool foundMore;

        do
        {
            foundMore = false;

            foreach (var entry in _cache)
            {
                if (ids.Contains(entry.Value.Parent) && ids.Add(entry.Value.Id))
                {
                    foundMore = true;
                }
            }
        }
        while (foundMore);

        foreach (var entry in _cache)
        {
            if (ids.Contains(entry.Value.Id))
            {
                _cache.TryRemove(entry);
            }
        }
    }

    /// <summary>Moves an item to another parent (Guid.Empty for the root) in every language.</summary>
    internal static void MoveItem(Guid id, Guid parent)
    {
        foreach (var entry in _cache)
        {
            var item = entry.Value;

            if (item.Id == id && item.Parent != parent)
            {
                // A new object, so a lookup running at the same time never sees half a change
                _cache.TryUpdate(entry.Key, new DictionaryItem
                {
                    Id = item.Id,
                    Key = item.Key,
                    Value = item.Value,
                    Culture = item.Culture,
                    Parent = parent
                }, item);
            }
        }
    }

    /// <summary>
    /// Reloads everything, when Umbraco asks every server to refresh all dictionary items: entries
    /// that are new or changed are replaced, and entries that are gone are removed.
    /// </summary>
    internal async Task ReloadAllAsync(CancellationToken ct = default)
    {
        var allKeys = await _repository.GetAllKeysAsync(ct).ConfigureAwait(false);
        var allTexts = await _repository.GetAllTextAsync(ct).ConfigureAwait(false);
        var allLanguages = await _languageService.GetAllAsync().ConfigureAwait(false);

        if (allLanguages == null || !allLanguages.Any())
        {
            return;
        }

        Reload(allKeys, allTexts, allLanguages);
    }

    /// <summary>
    /// Brings the cache in line with the whole dictionary without emptying it first, so lookups
    /// keep finding their texts while it happens.
    /// </summary>
    internal static void Reload(IEnumerable<CmsDictionary> allKeys, IEnumerable<CmsLanguageText> allTexts, IEnumerable<ILanguage> allLanguages)
    {
        var fresh = new ConcurrentDictionary<string, DictionaryItem>(StringComparer.OrdinalIgnoreCase);
        Populate(fresh, allKeys, allTexts, allLanguages);

        foreach (var entry in fresh)
        {
            _cache[entry.Key] = entry.Value;
        }

        foreach (var entry in _cache)
        {
            if (!fresh.ContainsKey(entry.Key))
            {
                _cache.TryRemove(entry);
            }
        }
    }

    /// <summary>
    /// The cached item with this key in this culture, or null. Matches key and culture
    /// case-insensitively. With a parent, the item must also sit directly under that parent
    /// (Guid.Empty for the root); without one, it can sit anywhere in the tree.
    /// </summary>
    internal static DictionaryItem? Find(string key, string culture, Guid? parent = null)
    {
        // Read the field once: code outside this class may swap in a dictionary of its own (tests do).
        var cache = _cache;

        // The entry's key is built from the item's own key and culture, so this is the only entry
        // that can match. Checking its fields as well keeps a key that contains a dash from
        // matching the wrong split ("a-b" + "c" against "a" + "b-c").
        if (cache.TryGetValue(key + "-" + culture, out var item) && Matches(item, key, culture, parent))
        {
            return item;
        }

        if (ReferenceEquals(cache.Comparer, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        // A swapped-in dictionary that compares keys case-sensitively can hold entries the direct
        // lookup misses, so read through it as every lookup did before.
        foreach (var entry in cache)
        {
            if (Matches(entry.Value, key, culture, parent))
            {
                return entry.Value;
            }
        }

        return null;
    }

    private static bool Matches(DictionaryItem item, string key, string culture, Guid? parent) =>
        item.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
        && item.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase)
        && (parent == null || item.Parent == parent.Value);
}
