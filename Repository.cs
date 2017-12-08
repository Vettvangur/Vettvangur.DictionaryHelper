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
		public IEnumerable<CmsDictionary> GetAllKeys()
		{
			using (var db = ApplicationContext.Current.DatabaseContext.Database)
			{
				return db.Query<CmsDictionary>("SELECT * FROM cmsDictionary");
			}
		}

		public IEnumerable<CmsLanguageText> GetAllText()
		{
			using (var db = ApplicationContext.Current.DatabaseContext.Database)
			{
				return db.Query<CmsLanguageText>("SELECT * FROM cmsLanguageText");
			}
		}
	}
}
