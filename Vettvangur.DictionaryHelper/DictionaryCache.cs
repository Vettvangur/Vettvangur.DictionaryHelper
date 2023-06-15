using System.Collections.Concurrent;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Services;
using static System.Net.Mime.MediaTypeNames;
using DictionaryItem = DictionaryHelper.Models.DictionaryItem;

namespace DictionaryHelper
{
    public class DictionaryCache
	{
        public static ConcurrentDictionary<string, DictionaryItem> _cache = new ConcurrentDictionary<string, DictionaryItem>();
        public static ConcurrentDictionary<string, ILanguage> _languages = new ConcurrentDictionary<string, ILanguage>();

        private readonly ILocalizationService _localizationService;
        private readonly DictionaryRepository _repository;
        public DictionaryCache(ILocalizationService localizationService, DictionaryRepository repository)
        {
            _localizationService = localizationService;
            _repository = repository;
        }

        public void Fill()
		{
			var allKeys = _repository.GetAllKeys();
			var allTexts = _repository.GetAllText();

			var allLanguages = _localizationService.GetAllLanguages();

            foreach (var lang in allLanguages)
            {
                _languages[lang.CultureInfo.Name] = lang;
            }

            foreach (var key in allKeys)
            {
                var texts = allTexts.Where(x => x.UniqueId == key.id);

                if (texts.Any())
                {
                    foreach (var text in texts)
                    {
                        ILanguage? language = text != null ? allLanguages.FirstOrDefault(x => x.Id == text.languageId) : null;

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
                else
                {
                    foreach (var lang in allLanguages)
                    {
                        var dictionary = new Models.DictionaryItem()
                        {
                            Id = key.id,
                            Key = key.key,
                            Value = "",
                            Culture = lang.CultureInfo.Name
                        };

                        _cache.TryAdd(dictionary.Key + "-" + dictionary.Culture, dictionary);
                    }
                }
            }
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
