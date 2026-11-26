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

    /// <summary>
    /// One dictionary item by its database id (cmsDictionary.pk, the id Umbraco sends to every
    /// server when an item changes), or null when it no longer exists.
    /// </summary>
    internal virtual async Task<CmsDictionary?> GetKeyAsync(int pk)
    {
        using (var scope = _scopeProvider.CreateScope())
        {
            var data = await scope.Database.FetchAsync<CmsDictionary>("SELECT * FROM cmsDictionary WHERE pk = @0", new object[] { pk }).ConfigureAwait(false);
            scope.Complete();

            return data.FirstOrDefault();
        }
    }

    /// <summary>The texts of one dictionary item, in all languages.</summary>
    internal virtual async Task<List<CmsLanguageText>> GetTextsAsync(Guid id)
    {
        using (var scope = _scopeProvider.CreateScope())
        {
            var data = await scope.Database.FetchAsync<CmsLanguageText>("SELECT * FROM cmsLanguageText WHERE UniqueId = @0", new object[] { id }).ConfigureAwait(false);
            scope.Complete();

            return data;
        }
    }
}
