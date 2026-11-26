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
            var hasTexts = false;

            foreach (var text in textsByKey[key.id])
            {
                hasTexts = true;

                // Find language based on text's languageId
                languagesById.TryGetValue(text.languageId, out var language);
                AddToCache(key, text?.value ?? "", language?.IsoCode ?? "");
            }

            if (!hasTexts)
            {
                // No texts for the key; add empty values for all languages
                foreach (var language in languages)
                {
                    AddToCache(key, "", language?.IsoCode ?? "");
                }
            }
        }
    }

    static void AddToCache(CmsDictionary key, string value, string culture)
    {
        var dictionaryItem = new DictionaryItem
        {
            Id = key.id,
            Key = key.key,
            Value = value,
            Culture = culture,
            Parent = key.parent
        };

        _cache.TryAdd($"{dictionaryItem.Key}-{dictionaryItem.Culture}", dictionaryItem);
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
