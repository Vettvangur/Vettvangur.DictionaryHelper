using DictionaryHelper.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using DictionaryItem = DictionaryHelper.Models.DictionaryItem;

namespace DictionaryHelper
{
    public class DictionaryCache
	{
        public static ConcurrentDictionary<string, DictionaryItem> _cache = new ConcurrentDictionary<string, DictionaryItem>();
        public static ConcurrentDictionary<string, ILanguage> _languages = new ConcurrentDictionary<string, ILanguage>();

        private readonly ILocalizationService _localizationService;
        private readonly DictionaryRepository _repository;
        private readonly ILogger<DictionaryCache> _logger;
        public DictionaryCache(ILocalizationService localizationService, DictionaryRepository repository, ILogger<DictionaryCache> logger)
        {
            _localizationService = localizationService;
            _repository = repository;
            _logger = logger;
        }

        public void Fill()
		{
            try
            {
                var allKeys = _repository.GetAllKeys();
                var allTexts = _repository.GetAllText();
                var allLanguages = _localizationService.GetAllLanguages();

                if (allLanguages == null || !allLanguages.Any())
                {
                    return;
                }

                foreach (var lang in allLanguages)
                {
                    _languages[lang.CultureInfo.Name] = lang;
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
                            AddToCache(key, text?.value ?? "", language?.CultureInfo.Name ?? "");
                        }
                    }
                    else
                    {
                        // No texts for the key; add empty values for all languages
                        foreach (var language in allLanguages)
                        {
                            AddToCache(key, "", language.CultureInfo.Name);
                        }
                    }
                }

            } catch(Exception ex)
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

        public void AddOrUpdate(string key, Guid Id, string value, string culture = null)
		{
			if (culture == null)
			{
				var allLanguages = _localizationService.GetAllLanguages();

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

		private void AddOrUpdateItem(string key, Guid id, string value, string culture)
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

		public void Remove(string key)
		{
			_cache.TryRemove(key, out DictionaryItem item);
		}
	}
}
