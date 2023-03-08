using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Umbraco.Cms.Web.Common;

namespace DictionaryHelper
{
    public static class Extensions
	{
        static public DictionaryService svc { set; get; }
        /// <summary>
        /// Returns the dictionary value for the key specified
        /// </summary>
        /// <param name="key">Dictionary key</param>
        /// <param name="defaultValue">Optional: If dictionary item is not found and has empty value then default value will be returned</param>
        /// <param name="parentKey">Optional: Key will be created as a child of the parent key.</param>
        /// <param name="create">Optional: If key does not exist it will be created. Requires default value.</param>
        /// <returns>Dictionary string value</returns>
        public static string DictionaryValue(this UmbracoHelper helper, string key, string defaultValue = null, string parentKey = null, bool create = false)
		{
			string culture = Thread.CurrentThread.CurrentCulture.Name;

			var value = svc?.GetValueByKeyAndCulture(key, culture, defaultValue, parentKey, create);

            return value;
		}
	}
}
