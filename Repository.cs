using DictionaryHelper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;

namespace DictionaryHelper
{
	class Repository
	{
		public List<CmsDictionary> GetAllKeys()
		{
			using (var db = ApplicationContext.Current.DatabaseContext.Database)
			{
				return db.Fetch<CmsDictionary>("SELECT * FROM cmsDictionary");
			}
		}

		public List<CmsLanguageText> GetAllText()
		{
			using (var db = ApplicationContext.Current.DatabaseContext.Database)
			{
				return db.Fetch<CmsLanguageText>("SELECT * FROM cmsLanguageText");
			}
		}
	}
}
