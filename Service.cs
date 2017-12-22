using DictionaryHelper.Models;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;

namespace DictionaryHelper
{
	public static class Service
	{

		public static IEnumerable<DictionaryItem> GetAll()
		{
			return DictionaryCache._cache.Select(x => x.Value);
		}

		public static bool KeyExist(string key)
		{
			return DictionaryCache._cache.Any(x => x.Value.Key == key);
		}

		public static DictionaryItem GetByKeyAndCulture(string key, string culture, string defaultValue = null, bool create = false)
		{

			if (DictionaryCache._cache.Any(x => x.Value.Key == key && x.Value.Culture == culture)) {

				var dict = DictionaryCache._cache.FirstOrDefault(x => x.Value.Key == key && x.Value.Culture == culture);

				if (!string.IsNullOrEmpty(dict.Value.Value))
				{
					return dict.Value;
				}

			}

			if (!string.IsNullOrEmpty(defaultValue))
			{
				if (create)
				{
					var ls = ApplicationContext.Current.Services.LocalizationService;

					var languages = ls.GetAllLanguages();

					var language = languages.FirstOrDefault(x => x.IsoCode == culture);

					if (language != null)
					{
						var dict = ls.CreateDictionaryItemWithIdentity(key, null, defaultValue);

						ls.AddOrUpdateDictionaryValue(dict, language, defaultValue);

						DictionaryCache.AddOrUpdate(key, culture, defaultValue);

						ls.Save(dict);
					}

				}

				return new DictionaryItem()
				{
					Culture = culture,
					Key = key,
					Value = defaultValue
				};
			}

			return null;
		}

		public static string GetValueByKeyAndCulture(string key, string culture, string defaultValue = null, bool create = false)
		{
			var dict = GetByKeyAndCulture(key, culture, defaultValue, create);

			if (dict != null)
			{
				return dict.Value;
			}

			return string.Empty;
		}
	}
}
