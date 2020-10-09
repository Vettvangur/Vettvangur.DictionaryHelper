using DictionaryHelper.Models;
using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Services;

namespace DictionaryHelper
{
	public class Service
	{

		public static IEnumerable<DictionaryItem> GetAll()
		{
			return DictionaryCache._cache.Select(x => x.Value);
		}

		public static bool KeyExist(string key, string culture = null)
		{
			culture = culture ?? Thread.CurrentThread.CurrentCulture.Name;

			return DictionaryCache._cache.Any(x => x.Key == key + "-" + culture);
		}

		public static DictionaryItem GetDictionaryItem(string key, string culture = null)
		{
			culture = culture ?? Thread.CurrentThread.CurrentCulture.Name;

			if (KeyExist(key, culture))
			{
				return DictionaryCache._cache.FirstOrDefault(x => x.Key == key + "-" + culture).Value;
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

			var dict = GetDictionaryItem(key, culture);

			if (dict != null)
            {
				return dict;
			}

			if (create)
			{
				if (keys.Length > 0)
                {
					var item = CreateDictionaryTree(keys, defaultValue, culture);

					return item;
				} else
                {
					DictionaryItem parentItem = null;

					if (!string.IsNullOrEmpty(parentKey))
					{
						parentItem = GetDictionaryItem(parentKey, culture);
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

				var item = GetDictionaryItem(key, culture);

				if (item == null)
				{

					DictionaryItem parentItem = null;
					var _defaultValue = string.Empty;

					if (i > 0)
					{
						parentItem = GetDictionaryItem(keys[i-1], culture);
					}

					if (i == keys.Length - 1)
					{
						_defaultValue = defaultValue;
					}

					CreateDictionaryItem(key, _defaultValue, parentItem != null ? parentItem.Id : (Guid?)null, culture);
				}
			}

			return GetDictionaryItem(keys.Last(), culture);

		}

		private static DictionaryItem CreateDictionaryItem(string key, string defaultValue, Guid? parent, string culture)
		{
			var ls = ApplicationContext.Current.Services.LocalizationService;

			var languages = ls.GetAllLanguages();

			var language = languages.FirstOrDefault(x => x.IsoCode == culture);

			if (language != null)
			{
				var dict = ls.GetDictionaryItemByKey(key);

				if (dict == null)
                {
					dict = ls.CreateDictionaryItemWithIdentity(key, parent, defaultValue);

					foreach (var la in languages)
					{
						UpdateDictionaryItemCache(ls, dict, la, defaultValue);
					}

					ls.Save(dict);
				}

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

		private static readonly ILog Log =
				LogManager.GetLogger(
					MethodBase.GetCurrentMethod().DeclaringType
				);
	}
}
