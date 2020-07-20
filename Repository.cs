using DictionaryHelper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace DictionaryHelper
{
	class Repository
	{
		public List<CmsDictionary> GetAllKeys()
		{
			using (var scope = Current.ScopeProvider.CreateScope())
			{
				var data = scope.Database.Fetch<CmsDictionary>();
				scope.Complete();

				return data;
			}
		}

		public List<CmsLanguageText> GetAllText()
		{
			using (var scope = Current.ScopeProvider.CreateScope())
			{
				var data = scope.Database.Fetch<CmsLanguageText>();
				scope.Complete();

				return data;
			}
		}
	}
}
