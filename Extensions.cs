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
		public static string DictionaryValue(this UmbracoHelper helper, string key)
		{
			var content = helper.UmbracoContext.PublishedContentRequest;
			string culture = Thread.CurrentThread.CurrentCulture.Name;

			if (content != null)
			{
				culture = content.Culture.Name;
			}

			return Service.GetValueByKeyAndCulture(key, culture);
		}
	}
}
