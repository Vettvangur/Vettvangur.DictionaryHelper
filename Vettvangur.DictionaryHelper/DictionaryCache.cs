using DictionaryHelper.Models;
using System.Collections.Concurrent;
using Umbraco.Cms.Core.Services;

namespace DictionaryHelper
{
    class DictionaryCache
	{
		public static ConcurrentDictionary<string, DictionaryItem> _cache = new ConcurrentDictionary<string, DictionaryItem>();

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

			foreach (var text in allTexts)
			{
				var key = allKeys.FirstOrDefault(x => x.id == text.UniqueId);
				var language = allLanguages.FirstOrDefault(x => x.Id == text.languageId);

				if (key != null && language != null)
				{
					var dictionary = new DictionaryItem()
					{
						Id = key.id,
						Key = key.key,
						Value = text.value,
						Culture = language.CultureInfo.Name
					};

					_cache.TryAdd(dictionary.Key + "-" + dictionary.Culture, dictionary);
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
