// Not supported in Umbraco v17


//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Umbraco.Cms.Core;
//using Umbraco.Cms.Core.Models.ContentEditing;
//using Umbraco.Cms.Core.Trees;

//namespace DictionaryHelper;

//public class SearchTree : ISearchableTree
//{
//    // This is still the correct alias for the dictionary tree in v17
//    public string TreeAlias => Constants.Trees.Dictionary;

//    private readonly DictionaryService _dictionaryService;

//    public SearchTree(DictionaryService dictionaryService)
//    {
//        _dictionaryService = dictionaryService;
//    }

//    public Task<EntitySearchResults> SearchAsync(
//        string query,
//        int pageSize,
//        long pageIndex,
//        string? searchFrom = null)
//    {
//        var searchResults = new List<SearchResultEntity>();

//        if (!string.IsNullOrWhiteSpace(query) && query.Length > 2)
//        {
//            // Your own custom search of dictionary keys/values
//            var results = _dictionaryService.SearchByValueOrKey(query);

//            // Respect the requested page size / pageIndex instead of hard-coded Take(50)
//            var paged = results
//                .Skip((int)pageIndex * pageSize)
//                .Take(pageSize)
//                .ToList();

//            foreach (var result in paged)
//            {
//                var icon = "icon-document";

//                // Shorten the value for display
//                string displayValue = result.Value ?? string.Empty;
//                if (displayValue.Length > 10)
//                {
//                    displayValue = displayValue.Substring(0, 10) + "...";
//                }

//                var name = string.IsNullOrEmpty(result.Value)
//                    ? result.Key
//                    : $"{result.Key} ({displayValue})";

//                var item = new SearchResultEntity
//                {
//                    Name = name,
//                    Id = result.Id,
//                    Key = result.Id,
//                    Score = 1,
//                    Icon = icon
//                };

//                searchResults.Add(item);
//            }

//            // totalFound is the total count for paging metadata
//            var totalFound = results.Count;
//            return Task.FromResult(new EntitySearchResults(searchResults, totalFound));
//        }

//        // No results / too short query
//        return Task.FromResult(new EntitySearchResults(searchResults, 0));
//    }
//}
