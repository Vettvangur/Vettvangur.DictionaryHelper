using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Web;

namespace DictionaryHelper
{
	public static class Extensions
	{
		public static string DictionaryValue(this UmbracoHelper helper, string key)
		{
			return Service.GetValueByKeyAndCulture(key, helper.UmbracoContext.PublishedContentRequest.Culture.DisplayName);
		}
	}
}
