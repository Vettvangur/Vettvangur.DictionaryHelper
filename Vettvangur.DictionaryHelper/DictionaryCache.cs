using DictionaryHelper.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using DictionaryItem = DictionaryHelper.Models.DictionaryItem;

namespace DictionaryHelper;

public class DictionaryCache
{
    public static ConcurrentDictionary<string, DictionaryItem> _cache = new ConcurrentDictionary<string, DictionaryItem>();
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

            foreach (var lang in allLanguages)
            {
                _languages[lang.CultureName] = lang;
            }

            // Iterate through each key and populate _cache
            foreach (var key in allKeys)
            {
                // Get texts associated with the current key
                var texts = allTexts.Where(x => x.UniqueId == key.id).ToList();

                if (texts.Any())
                {
                    foreach (var text in texts)
                    {
                        // Find language based on text's languageId
                        var language = allLanguages.FirstOrDefault(lang => lang.Id == text.languageId);
                        AddToCache(key, text?.value ?? "", language?.CultureName ?? "");
                    }
                }
                else
                {
                    // No texts for the key; add empty values for all languages
                    foreach (var language in allLanguages)
                    {
                        AddToCache(key, "", language?.CultureName ?? "");
                    }
                }
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fill dictionary cache.");
        }

    }
    void AddToCache(CmsDictionary key, string value, string culture)
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
                AddOrUpdateItem(key, Id, value, parent, language.CultureName);
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
}
