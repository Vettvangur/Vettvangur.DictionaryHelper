using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Trees;

namespace DictionaryHelper;

public class SearchTree : ISearchableTree
{
    public string TreeAlias => Umbraco.Cms.Core.Constants.Trees.Dictionary;

    private readonly DictionaryService _dictionaryService;
    public SearchTree(DictionaryService dictionaryService)
    {
        _dictionaryService = dictionaryService;
    }

    public Task<EntitySearchResults> SearchAsync(string query, int pageSize, long pageIndex, string? searchFrom = null)
    {
        long totalFound = 0;
        var searchResults = new List<SearchResultEntity?>();
        if (!string.IsNullOrEmpty(query) && query.Length > 2)
        {
            var results = _dictionaryService.SearchByValueOrKey(query);

            foreach (var result in results.Take(50))
            {
                var icon = "icon-document";

                var name = result.Key + (!string.IsNullOrEmpty(result.Value) ? " (" + (result.Value.Length > 10 ? result.Value.Substring(0,10) + "..." : result.Value) + ")" : "");
                
                var item = new SearchResultEntity()
                {
                    Name = name,
                    Id = result.Id,
                    Key = result.Id,
                    Score = 1,
                    Icon = icon
                };


                searchResults.Add(item);
            }

        }

        return Task.FromResult(new EntitySearchResults(searchResults, totalFound));
    }
}
