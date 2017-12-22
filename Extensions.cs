using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Umbraco.Web;

namespace DictionaryHelper
{
	public static class Extensions
	{
		/// <summary>
		/// Returns the dictionary value for the key specified
		/// </summary>
		/// <param name="key">Dictionary key</param>
		/// <param name="defaultValue">Optional: If dictionary item is not found og has empty value then default value will be returned</param>
		/// <param name="create">Optional: If key does not exist it will be created. Required default value.</param>
		public static string DictionaryValue(this UmbracoHelper helper, string key, string defaultValue = null, bool create = false)
		{
			var content = helper.UmbracoContext.PublishedContentRequest;
			string culture = Thread.CurrentThread.CurrentCulture.Name;

			if (content != null)
			{
				culture = content.Culture.Name;
			}

			var value = Service.GetValueByKeyAndCulture(key, culture, defaultValue, create);

			return value;
		}
	}
}
