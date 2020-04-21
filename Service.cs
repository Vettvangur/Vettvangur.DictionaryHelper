using DictionaryHelper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Services;

namespace DictionaryHelper
{
	public static class Service
	{
		private static IEnumerable<Umbraco.Core.Models.ILanguage> _allLanguages = null;
        private static ILocalizationService ls = Umbraco.Core.Composing.Current.Services.LocalizationService;

        public static IEnumerable<DictionaryItem> GetAll()
		{
			return DictionaryCache._cache.Select(x => x.Value);
		}

		public static bool KeyExist(string key)
		{
			return DictionaryCache._cache.Any(x => x.Value.Key == key);
		}

		public static DictionaryItem GetDictionaryItem(string key)
		{
			if (KeyExist(key))
			{
				return DictionaryCache._cache.FirstOrDefault(x => x.Value.Key == key).Value;
			}

			return null;
		}

		public static DictionaryItem GetByKeyAndCulture(string key, string culture, string defaultValue = null, string parentKey = null, bool create = false)
		{
			var keys = new string[] {};

			if (key.Contains("."))
			{
				keys = key.Split('.');

				key = keys.Last();
			}

			if (DictionaryCache._cache.Any(x => x.Value.Key == key && x.Value.Culture == culture)) {

				var dict = DictionaryCache._cache.FirstOrDefault(x => x.Value.Key == key && x.Value.Culture == culture);

				if (!string.IsNullOrEmpty(dict.Value.Value))
				{
					return dict.Value;
				}

			}

			if (create)
			{
				_allLanguages = ls.GetAllLanguages();

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

		public static string GetValueByKeyAndCulture(string key, string culture, string defaultValue = null, string parentKey = null, bool create = false)
		{
			var dict = GetByKeyAndCulture(key, culture, defaultValue, parentKey, create);

			if (dict != null)
			{
				return dict.Value;
			}

			return string.Empty;
		}

		private static DictionaryItem CreateDictionaryTree(string[] keys, string defaultValue, string culture)
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

        private static DictionaryItem CreateDictionaryItem(string key, string defaultValue, Guid? parent, string culture)
		{
			var language = _allLanguages.FirstOrDefault(x => x.IsoCode == culture);

			if (language != null)
			{
				var dict = ls.CreateDictionaryItemWithIdentity(key, parent, defaultValue);

				foreach (var la in _allLanguages)
				{
					UpdateDictionaryItemCache(ls, dict, la, defaultValue);
				}

				ls.Save(dict);

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

		private static void UpdateDictionaryItemCache(ILocalizationService ls, Umbraco.Core.Models.IDictionaryItem dict, Umbraco.Core.Models.ILanguage language, string defaultValue)
		{
			ls.AddOrUpdateDictionaryValue(dict, language, defaultValue);
		}

	}
}
