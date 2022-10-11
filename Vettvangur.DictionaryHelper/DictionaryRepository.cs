using DictionaryHelper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Cms.Infrastructure.Scoping;

namespace DictionaryHelper
{
	class DictionaryRepository
	{
        readonly IScopeProvider _scopeProvider;

        public DictionaryRepository(IScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public List<CmsDictionary> GetAllKeys()
		{
			using (var scope = _scopeProvider.CreateScope())
			{
				var data = scope.Database.Fetch<CmsDictionary>();
				scope.Complete();

				return data;
			}
		}

		public List<CmsLanguageText> GetAllText()
		{
			using (var scope = _scopeProvider.CreateScope())
			{
				var data = scope.Database.Fetch<CmsLanguageText>();
				scope.Complete();

				return data;
			}
		}
	}
}
