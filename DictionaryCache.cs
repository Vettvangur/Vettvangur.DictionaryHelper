using System;
using System.Collections.Concurrent;
using System.Linq;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using DictionaryItem = DictionaryHelper.Models.DictionaryItem;

namespace DictionaryHelper
{
    public static class DictionaryCache
    {
        public static ConcurrentDictionary<string, DictionaryItem> _cache = new ConcurrentDictionary<string, DictionaryItem>();
        private static ILocalizationService ls = Umbraco.Core.Composing.Current.Services.LocalizationService;

        public static void Fill()
        {
            var repo = new Repository();

            var allKeys = repo.GetAllKeys();
            var allTexts = repo.GetAllText();

            var allLanguages = ls.GetAllLanguages();

            foreach (var key in allKeys)
            {
                var texts = allTexts.Where(x => x.UniqueId == key.id);

                foreach (var text in texts)
                {
                    ILanguage language = text != null ? allLanguages.FirstOrDefault(x => x.Id == text.languageId) : null;

                    if (language != null)
                    {
                        var dictionary = new Models.DictionaryItem()
                        {
                            Id = key.id,
                            Key = key.key,
                            Value = text.value,
                            Culture = language.CultureInfo.Name
                        };

                        _cache.TryAdd(dictionary.Key + "-" + dictionary.Culture, dictionary);
                    }
                    else
                    {
                        var dictionary = new Models.DictionaryItem()
                        {
                            Id = key.id,
                            Key = key.key,
                            Value = "",
                            Culture = ""
                        };

                        _cache.TryAdd(dictionary.Key + "-" + dictionary.Culture, dictionary);
                    }
                }
            }

        }

        public static void AddOrUpdate(string key, Guid Id, string value, string culture = null)
        {
            if (culture == null)
            {
                var allLanguages = ls.GetAllLanguages();

                foreach (var language in allLanguages)
                {
                    AddOrUpdateItem(key, Id, value, language.CultureInfo.Name);
                }

            }
            else
            {
                AddOrUpdateItem(key, Id, value, culture);
            }


        }

        private static void AddOrUpdateItem(string key, Guid id, string value, string culture)
        {
            var dictionary = new DictionaryItem()
            {
                Id = id,
                Key = key,
                Value = value,
                Culture = culture
            };

            _cache.AddOrUpdate(dictionary.Key + "-" + dictionary.Culture, dictionary, (k, oldValue) => dictionary);
        }


        public static void Remove(string key)
        {
            _cache.TryRemove(key, out DictionaryItem item);
        }


    }
}
