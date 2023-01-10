using DictionaryHelper.Models;
using Umbraco.Cms.Core.Services;

namespace DictionaryHelper
{
    public class DictionaryService
	{
		private static IEnumerable<Umbraco.Cms.Core.Models.ILanguage> _allLanguages = null;
        private readonly ILocalizationService _localizationService;

        public DictionaryService(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public IEnumerable<DictionaryItem> GetAll()
		{
			return DictionaryCache._cache.Select(x => x.Value);
		}

		public bool KeyExist(string key)
		{
			return DictionaryCache._cache.Any(x => x.Value.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
		}

		public DictionaryItem GetDictionaryItem(string key)
		{
			if (KeyExist(key))
			{
				return DictionaryCache._cache.FirstOrDefault(x => x.Value.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
			}

			return null;
		}

		public DictionaryItem GetByKeyAndCulture(string key, string culture, string defaultValue = null, string parentKey = null, bool create = false)
		{
			var keys = new string[] {};

			if (key.Contains("."))
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
				_allLanguages = _localizationService.GetAllLanguages();

				if (keys.Length > 0)
				{
					var item = CreateDictionaryTree(keys, defaultValue, culture);

					return item;
				} else
				{
					DictionaryItem parentItem = null;

					if (!string.IsNullOrEmpty(parentKey))
					{
						parentItem = GetDictionaryItem(parentKey);
					}

					var item = CreateDictionaryItem(key, defaultValue, parentItem != null ? parentItem.Id : (Guid?)null, culture);

					return item;
				}
			} else
			{
				return new DictionaryItem()
				{
					Id = Guid.Empty,
					Culture = culture,
					Key = key,
					Value = defaultValue
				};
			}
		}

		public string GetValueByKeyAndCulture(string key, string culture, string defaultValue = null, string parentKey = null, bool create = false)
		{
			var dict = GetByKeyAndCulture(key, culture, defaultValue, parentKey, create);

			if (dict != null)
			{
				return dict.Value;
			}

			return string.Empty;
		}

		private DictionaryItem CreateDictionaryTree(string[] keys, string defaultValue, string culture)
		{

			for (int i = 0; i < keys.Length; i++)
			{
				var key = keys[i];

				var item = GetDictionaryItem(key);

				if (item == null)
				{

					DictionaryItem parentItem = null;
					var _defaultValue = string.Empty;

					if (i > 0)
					{
						parentItem = GetDictionaryItem(keys[i-1]);
					}

					if (i == keys.Length - 1)
					{
						_defaultValue = defaultValue;
					}

					var dict = CreateDictionaryItem(key, _defaultValue, parentItem != null ? parentItem.Id : (Guid?)null, culture);

					if (i == keys.Length - 1)
					{
						return dict;
					}

				} else
				{
					if (i == keys.Length - 1)
					{
						return item;
					}
				}
			}

			return null;

		}

        private DictionaryItem CreateDictionaryItem(string key, string defaultValue, Guid? parent, string culture)
		{
			var language = _allLanguages.FirstOrDefault(x => x.IsoCode == culture);

			if (language != null)
			{
				var dict = _localizationService.CreateDictionaryItemWithIdentity(key, parent, defaultValue);

				foreach (var la in _allLanguages)
				{
					UpdateDictionaryItemCache(_localizationService, dict, la, defaultValue);
				}

                _localizationService.Save(dict);

				return new DictionaryItem()
				{
					Culture = culture,
					Id = dict.Key,
					Key = key,
					Value = defaultValue
				};
			}

			return null;
		}

		private void UpdateDictionaryItemCache(
            ILocalizationService ls,
            Umbraco.Cms.Core.Models.IDictionaryItem dict,
            Umbraco.Cms.Core.Models.ILanguage language, 
            string defaultValue)
		{
			ls.AddOrUpdateDictionaryValue(dict, language, defaultValue);
		}
	}
}
