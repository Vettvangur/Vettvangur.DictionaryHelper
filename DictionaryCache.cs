using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using DictionaryHelper.Models;
using Umbraco.Core;
using log4net;
using System.Reflection;

namespace DictionaryHelper
{
	public static class DictionaryCache
	{
		public static ConcurrentDictionary<string, DictionaryItem> _cache = new ConcurrentDictionary<string, DictionaryItem>();
		public static void Fill()
		{
			var repo = new Repository();

			var allKeys = repo.GetAllKeys();
			var allTexts = repo.GetAllText();

			var ls = ApplicationContext.Current.Services.LocalizationService;

			var allLanguages = ls.GetAllLanguages();

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

		public static void AddOrUpdate(string key, Guid Id, string value, string culture = null)
		{
			if (culture == null)
			{
				var ls = ApplicationContext.Current.Services.LocalizationService;

				var allLanguages = ls.GetAllLanguages();

				foreach (var language in allLanguages)
				{
					AddOrUpdateItem(key, Id, value, language.CultureInfo.Name);
				}

			} else
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

		private static readonly ILog Log =
				LogManager.GetLogger(
					MethodBase.GetCurrentMethod().DeclaringType
				);


	}
}
