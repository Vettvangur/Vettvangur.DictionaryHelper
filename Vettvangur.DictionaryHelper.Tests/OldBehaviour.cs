using System.Collections.Concurrent;
using DictionaryHelper;
using DictionaryHelper.Models;
using Umbraco.Cms.Core.Models;
using DictionaryItem = DictionaryHelper.Models.DictionaryItem;

namespace DictionaryHelper.Tests;

/// <summary>
/// The lookups as they were in 5.0.2, copied as they were, so the tests can check that the faster
/// versions give the same answers.
/// </summary>
internal static class OldBehaviour
{
    public static void Fill(
        ConcurrentDictionary<string, DictionaryItem> cache,
        List<CmsDictionary> allKeys,
        List<CmsLanguageText> allTexts,
        IEnumerable<ILanguage> allLanguages)
    {
        foreach (var key in allKeys)
        {
            var texts = allTexts.Where(x => x.UniqueId == key.id).ToList();

            if (texts.Any())
            {
                foreach (var text in texts)
                {
                    var language = allLanguages.FirstOrDefault(lang => lang.Id == text.languageId);
                    Add(cache, key, text?.value ?? "", language?.IsoCode ?? "");
                }
            }
            else
            {
                foreach (var language in allLanguages)
                {
                    Add(cache, key, "", language?.IsoCode ?? "");
                }
            }
        }
    }

    static void Add(ConcurrentDictionary<string, DictionaryItem> cache, CmsDictionary key, string value, string culture)
    {
        var dictionaryItem = new DictionaryItem
        {
            Id = key.id,
            Key = key.key,
            Value = value,
            Culture = culture,
            Parent = key.parent
        };

        cache.TryAdd($"{dictionaryItem.Key}-{dictionaryItem.Culture}", dictionaryItem);
    }

    /// <summary>GetByKeyAndCultureAsync's and GetDictionaryItemAsync's search of the cache.</summary>
    public static DictionaryItem? Find(string key, string culture, Guid parent) =>
        DictionaryCache._cache.Values.FirstOrDefault(x =>
            x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
            && x.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase)
            && x.Parent == parent);

    /// <summary>
    /// GetValueByKeyAndCultureAsync without a parent key and without creating anything: the
    /// last part of a dotted key, looked up at the root, or the default.
    /// </summary>
    public static string GetValue(string key, string culture, string? defaultValue)
    {
        if (key.Contains(".", StringComparison.InvariantCultureIgnoreCase))
        {
            key = key.Split('.').Last();
        }

        var existingItem = Find(key, culture, Guid.Empty);

        return existingItem != null ? existingItem.Value : defaultValue ?? "";
    }

    public static IEnumerable<DictionaryItem> GetChildren(string key, string culture)
    {
        var rootItem = DictionaryCache._cache.Values
            .FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
                                 && x.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase));

        if (rootItem == null)
            return Enumerable.Empty<DictionaryItem>();

        return DictionaryCache._cache.Values
            .Where(x => x.Parent == rootItem.Id && x.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<DictionaryItem> GetDescendants(string key, string culture)
    {
        var rootItem = DictionaryCache._cache.Values
            .FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
                                 && x.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase));

        if (rootItem == null)
            return Enumerable.Empty<DictionaryItem>();

        var descendants = new List<DictionaryItem>();
        var itemsToProcess = new Queue<DictionaryItem>();
        itemsToProcess.Enqueue(rootItem);

        while (itemsToProcess.Any())
        {
            var currentItem = itemsToProcess.Dequeue();
            descendants.Add(currentItem);

            var children = DictionaryCache._cache.Values
                .Where(x => x.Parent == currentItem.Id && x.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase));

            foreach (var child in children)
            {
                itemsToProcess.Enqueue(child);
            }
        }

        return descendants.Skip(1);
    }
}
