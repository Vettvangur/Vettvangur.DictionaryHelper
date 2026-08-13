using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using DictionaryItem = DictionaryHelper.Models.DictionaryItem;

namespace DictionaryHelper;

public class DictionaryService
{
    private readonly IDictionaryItemService _dictionaryItemService;
    private readonly ILanguageService _languageService;
    public DictionaryService(
        IDictionaryItemService dictionaryItemService,
        ILanguageService languageService
        )
    {
        _dictionaryItemService = dictionaryItemService;
        _languageService = languageService;
    }

    public IEnumerable<DictionaryItem> GetAll()
    {
        return DictionaryCache._cache.Select(x => x.Value);
    }

    public bool KeyExist(string key)
    {
        return DictionaryCache._cache.Any(x => x.Value.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    public DictionaryItem? GetDictionaryItem(string key)
    {
        try
        {
            return DictionaryCache._cache.FirstOrDefault(x => x.Value.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
        }
        catch
        {
            return null;
        }
    }
    public IEnumerable<DictionaryItem> GetChildren(string key, string culture)
    {
        // Look up the root item by key and culture
        var rootItem = DictionaryCache._cache.Values
            .FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
                                 && x.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase));

        if (rootItem == null)
            return Enumerable.Empty<DictionaryItem>();

        // Find and return only the direct children of the root item that match the culture
        var children = DictionaryCache._cache.Values
            .Where(x => x.Parent == rootItem.Id && x.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase));

        return children;
    }

    public IEnumerable<DictionaryItem> GetDescendants(string key, string culture)
    {
        // Look up the root item by key and culture
        var rootItem = DictionaryCache._cache.Values
            .FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
                                 && x.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase));

        if (rootItem == null)
            return Enumerable.Empty<DictionaryItem>();

        // Use a queue to perform a breadth-first search for descendants
        var descendants = new List<DictionaryItem>();
        var itemsToProcess = new Queue<DictionaryItem>();
        itemsToProcess.Enqueue(rootItem);

        while (itemsToProcess.Any())
        {
            var currentItem = itemsToProcess.Dequeue();
            descendants.Add(currentItem);

            // Find all direct children of the current item that match the culture
            var children = DictionaryCache._cache.Values
                .Where(x => x.Parent == currentItem.Id && x.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase));

            // Add children to the queue for further processing
            foreach (var child in children)
            {
                itemsToProcess.Enqueue(child);
            }
        }

        // Exclude the root item if only descendants are desired
        return descendants.Skip(1);
    }

    public IEnumerable<DictionaryItem> SearchByValueOrKey(string value)
    {
        return DictionaryCache._cache.Where(x => x.Value.Value.InvariantContains(value) || x.Key.InvariantContains(value)).Select(x => x.Value);
    }

    public async Task<DictionaryItem?> GetByKeyAndCultureAsync(string key, string culture, string? defaultValue = null, string? parentKey = null, bool create = false)
    {
        var keys = new string[] { };

        if (key.Contains(".", StringComparison.InvariantCultureIgnoreCase))
        {
            keys = key.Split('.');

            key = keys.Last();
        }

        DictionaryItem? parentItem = null;

        if (keys.Length == 0 && !string.IsNullOrEmpty(parentKey))
        {
            parentItem = await GetDictionaryItemAsync(parentKey, null, culture).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Dictionary parent item '{parentKey}' was not found.");
        }

        var existingItem = DictionaryCache._cache.Values.FirstOrDefault(x =>
            x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
            && x.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase)
            && x.Parent == (parentItem?.Id ?? Guid.Empty));

        if (existingItem != null)
        {
            if (!string.IsNullOrEmpty(existingItem.Value) || !create)
            {
                return existingItem;
            }

            return await SetDefaultValueAsync(existingItem, defaultValue, culture).ConfigureAwait(false);
        }

        if (create)
        {
            if (keys.Length > 0)
            {
                var item = await CreateDictionaryTreeAsync(keys, defaultValue, culture).ConfigureAwait(false);

                return item;
            }
            else
            {
                var item = await GetDictionaryItemAsync(key, parentItem?.Id, culture).ConfigureAwait(false);

                if (item != null)
                {
                    return string.IsNullOrEmpty(item.Value)
                        ? await SetDefaultValueAsync(item, defaultValue, culture).ConfigureAwait(false)
                        : item;
                }

                item = await CreateDictionaryItemAsync(key, defaultValue, parentItem?.Id, culture).ConfigureAwait(false);

                return item;
            }
        }
        else
        {
            return new DictionaryItem()
            {
                Id = Guid.Empty,
                Culture = culture,
                Key = key,
                Value = defaultValue ?? ""
            };
        }
    }

    public async Task<string> GetValueByKeyAndCultureAsync(string key, string culture, string? defaultValue = null, string? parentKey = null, bool create = false)
    {
        var dict = await GetByKeyAndCultureAsync(key, culture, defaultValue, parentKey, create).ConfigureAwait(false);

        if (dict != null)
        {
            return dict.Value;
        }

        return string.Empty;
    }

    private async Task<DictionaryItem?> CreateDictionaryTreeAsync(string[] keys, string? defaultValue, string culture)
    {
        Guid? parent = null;
        DictionaryItem? item = null;

        for (int i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            var isFinalKey = i == keys.Length - 1;
            item = await GetDictionaryItemAsync(key, parent, culture, allowDifferentParent: isFinalKey).ConfigureAwait(false);

            if (item == null)
            {
                item = await CreateDictionaryItemAsync(key, isFinalKey ? defaultValue : string.Empty, parent, culture).ConfigureAwait(false);
            }

            if (item == null)
            {
                return null;
            }

            if (isFinalKey)
            {
                return string.IsNullOrEmpty(item.Value)
                    ? await SetDefaultValueAsync(item, defaultValue, culture).ConfigureAwait(false)
                    : item;
            }

            parent = item.Id;
        }

        return null;

    }

    private async Task<DictionaryItem?> GetDictionaryItemAsync(string key, Guid? parent, string culture, bool allowDifferentParent = false)
    {
        var parentId = parent ?? Guid.Empty;
        var cachedItem = DictionaryCache._cache.Values.FirstOrDefault(x =>
            x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
            && x.Parent == parentId
            && x.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase));

        if (cachedItem != null)
        {
            return cachedItem;
        }

        var item = await _dictionaryItemService.GetAsync(key).ConfigureAwait(false);

        if (item == null)
        {
            return null;
        }

        if (item.ParentId != parent && !allowDifferentParent)
        {
            throw new InvalidOperationException($"Dictionary item '{key}' exists under a different parent.");
        }

        var translation = item.Translations.FirstOrDefault(x => x.LanguageIsoCode.Equals(culture, StringComparison.OrdinalIgnoreCase));

        return new DictionaryItem
        {
            Id = item.Key,
            Key = item.ItemKey,
            Parent = item.ParentId ?? Guid.Empty,
            Culture = culture,
            Value = translation?.Value ?? string.Empty
        };
    }

    private async Task<DictionaryItem> SetDefaultValueAsync(DictionaryItem item, string? defaultValue, string culture)
    {
        if (string.IsNullOrEmpty(defaultValue))
        {
            return item;
        }

        var dictionaryItem = await _dictionaryItemService.GetAsync(item.Id).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Dictionary item '{item.Key}' was not found.");
        var translation = dictionaryItem.Translations.FirstOrDefault(x => x.LanguageIsoCode.Equals(culture, StringComparison.OrdinalIgnoreCase));

        if (translation == null)
        {
            DictionaryCache._languages.TryGetValue(culture, out ILanguage? language);
            language ??= (await _languageService.GetAllAsync().ConfigureAwait(false))
                .FirstOrDefault(x => x.IsoCode.Equals(culture, StringComparison.OrdinalIgnoreCase));

            if (language == null)
            {
                throw new InvalidOperationException($"Dictionary language '{culture}' was not found.");
            }

            dictionaryItem.Translations = dictionaryItem.Translations
                .Append(new DictionaryTranslation(language, defaultValue))
                .ToList();
        }
        else
        {
            translation.Value = defaultValue;
        }

        var update = await _dictionaryItemService.UpdateAsync(dictionaryItem, Guid.Empty).ConfigureAwait(false);

        if (!update.Success)
        {
            throw new Exception($"Failed to update dictionary item. Key: {item.Key} Status: {update.Status}", update.Exception);
        }

        item.Value = defaultValue;
        return item;
    }

    private async Task<DictionaryItem?> CreateDictionaryItemAsync(string key, string? defaultValue, Guid? parent, string culture)
    {
        DictionaryCache._languages.TryGetValue(culture, out ILanguage? language);

        if (language != null)
        {
            var dictItem = new Umbraco.Cms.Core.Models.DictionaryItem(parent, key);

            var translations = new List<DictionaryTranslation>();

            foreach (var la in DictionaryCache._languages)
            {
                translations.Add(new DictionaryTranslation(
                    la.Value,
                    la.Key.Equals(culture, StringComparison.OrdinalIgnoreCase) ? defaultValue ?? "" : ""));
            }

            dictItem.Translations = translations;

            var dict = await _dictionaryItemService.CreateAsync(dictItem, Guid.Empty).ConfigureAwait(false);

            if (dict.Success)
            {
                var result = dict.Result;

                return new DictionaryItem()
                {
                    Culture = culture,
                    Id = result.Key,
                    Key = key,
                    Value = defaultValue ?? "",
                    Parent = parent ?? Guid.Empty
                };
            }

            throw new Exception($"Failed to create dictionary item. Key: {key} Status: {dict.Status.ToString()}", dict.Exception);

        }

        return null;
    }
}
