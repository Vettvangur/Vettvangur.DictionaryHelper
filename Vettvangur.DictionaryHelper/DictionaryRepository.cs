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

    public async Task<List<CmsDictionary>> GetAllKeysAsync(CancellationToken ct)
    {
        using (var scope = _scopeProvider.CreateScope())
        {
            var data = await scope.Database.FetchAsync<CmsDictionary>(ct).ConfigureAwait(false);
            scope.Complete();

            return data;
        }
    }

    public async Task<List<CmsLanguageText>> GetAllTextAsync(CancellationToken ct)
    {
        using (var scope = _scopeProvider.CreateScope())
        {
            var data = await scope.Database.FetchAsync<CmsLanguageText>(ct).ConfigureAwait(false);
            scope.Complete();

            return data;
        }
    }
}
