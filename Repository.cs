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
            using (var db = Current.ScopeProvider.CreateScope(autoComplete: true).Database)
			{
				return db.Fetch<CmsDictionary>();
			}
		}

		public List<CmsLanguageText> GetAllText()
		{
            using (var db = Current.ScopeProvider.CreateScope(autoComplete: true).Database)
            {
				return db.Fetch<CmsLanguageText>();
			}
		}
	}
}
