using DictionaryHelper.Models;
using Umbraco.Cms.Infrastructure.Scoping;

namespace DictionaryHelper;

public class DictionaryRepository
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
