using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using DictionaryItem = DictionaryHelper.Models.DictionaryItem;

namespace DictionaryHelper;

public class DictionaryService
{
    private readonly IDictionaryItemService _dictionaryItemService;
    public DictionaryService(
        IDictionaryItemService dictionaryItemService
        )
    {
        _dictionaryItemService = dictionaryItemService;
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

        if (DictionaryCache._cache.Any(x => x.Value.Key.Equals(key, StringComparison.OrdinalIgnoreCase) && x.Value.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase)))
        {

            var dict = DictionaryCache._cache.FirstOrDefault(x => x.Value.Key.Equals(key, StringComparison.OrdinalIgnoreCase) && x.Value.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(dict.Value.Value))
            {
                return dict.Value;
            }
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
                DictionaryItem? parentItem = null;

                if (!string.IsNullOrEmpty(parentKey))
                {
                    parentItem = GetDictionaryItem(parentKey);
                }

                var item = await CreateDictionaryItemAsync(key, defaultValue, parentItem != null ? parentItem.Id : (Guid?)null, culture).ConfigureAwait(false);

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

    private async Task <DictionaryItem?> CreateDictionaryTreeAsync(string[] keys, string? defaultValue, string culture)
    {

        for (int i = 0; i < keys.Length; i++)
        {
            var key = keys[i];

            var item = GetDictionaryItem(key);

            if (item == null)
            {

                DictionaryItem? parentItem = null;
                var _defaultValue = string.Empty;

                if (i > 0)
                {
                    parentItem = GetDictionaryItem(keys[i - 1]);
                }

                if (i == keys.Length - 1)
                {
                    _defaultValue = defaultValue;
                }

                var dict = await CreateDictionaryItemAsync(key, _defaultValue, parentItem != null ? parentItem.Id : (Guid?)null, culture).ConfigureAwait(false);

                if (i == keys.Length - 1)
                {
                    return dict;
                }

            }
            else
            {
                if (i == keys.Length - 1)
                {
                    return item;
                }
            }
        }

        return null;

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
                translations.Add(new DictionaryTranslation(la.Value, defaultValue ?? ""));
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
                    Value = defaultValue ?? ""
                };
            }

            throw new Exception($"Failed to create dictionary item. Key: {key} Status: {dict.Status.ToString()}", dict.Exception);

        }

        return null;
    }
}
