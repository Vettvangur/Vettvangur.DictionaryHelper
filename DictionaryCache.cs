using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using DictionaryHelper.Models;
using Umbraco.Core;

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

			var ds = ApplicationContext.Current.Services.LocalizationService;

			var allLanguages = ds.GetAllLanguages();

			foreach (var text in allTexts)
			{
				var key = allKeys.FirstOrDefault(x => x.id == text.UniqueId);
				var language = allLanguages.FirstOrDefault(x => x.Id == text.languageId);

				if (key != null && language != null)
				{
					var dictionary = new DictionaryItem()
					{
						Key = key.key,
						Value = text.value,
						Culture = language.CultureName
					};

					_cache.TryAdd(dictionary.Key + "-" + dictionary.Culture, dictionary);
				}

			}

		}

		public static void AddOrUpdate(string key, string culture, string value)
		{
			var dictionary = new DictionaryItem()
			{
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
