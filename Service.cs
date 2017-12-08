using DictionaryHelper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictionaryHelper
{
	public static class Service
	{

		public static IEnumerable<DictionaryItem> GetAll()
		{
			return DictionaryCache._cache.Select(x => x.Value);
		}

		public static DictionaryItem GetByKeyAndCulture(string key, string culture)
		{

			if (DictionaryCache._cache.Any(x => x.Value.Key == key && x.Value.Culture == culture)) {

				var dict = DictionaryCache._cache.FirstOrDefault(x => x.Value.Key == key && x.Value.Culture == culture);

				return dict.Value;
			}

			return null;
		}

		public static string GetValueByKeyAndCulture(string key, string culture)
		{
			var dict = GetByKeyAndCulture(key, culture);

			if (dict != null)
			{
				return dict.Value;
			}

			return string.Empty;
		}
	}
}
