using System.Collections.Concurrent;
using NSubstitute;
using Umbraco.Cms.Core.Services;
using Xunit;
using DictionaryItem = DictionaryHelper.Models.DictionaryItem;

// The cache is static, so tests that fill it must not run at the same time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace DictionaryHelper.Tests;

public sealed class LookupTests : IDisposable
{
    static readonly string[] AskedCultures = [.. TestDictionary.Cultures, "IS-is", "en-us", "de-DE", ""];

    readonly IDictionaryItemService _dictionaryItemService = Substitute.For<IDictionaryItemService>();
    readonly DictionaryService _service;

    public LookupTests()
    {
        Reset();
        _service = new DictionaryService(_dictionaryItemService, Substitute.For<ILanguageService>());
    }

    public void Dispose() => Reset();

    static void Reset()
    {
        DictionaryCache._cache.Clear();
        DictionaryCache._languages.Clear();
    }

    [Fact]
    public void The_cache_ignores_case_so_lookups_go_straight_to_their_entry()
    {
        // If .NET handed back a wrapped comparer, every miss would quietly read the whole cache.
        Assert.Same(StringComparer.OrdinalIgnoreCase, DictionaryCache._cache.Comparer);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Filling_the_cache_gives_the_same_items_as_before(int seed)
    {
        var data = new TestDictionary(seed);
        var before = new ConcurrentDictionary<string, DictionaryItem>();
        OldBehaviour.Fill(before, data.Keys, data.Texts, data.Languages);

        DictionaryCache.Populate(data.Keys, data.Texts, data.Languages);

        Assert.Equal(before.Count, DictionaryCache._cache.Count);

        foreach (var (key, item) in before)
        {
            Assert.True(DictionaryCache._cache.TryGetValue(key, out var now), key);
            Assert.Equal((item.Id, item.Key, item.Value, item.Culture, item.Parent), (now.Id, now.Key, now.Value, now.Culture, now.Parent));
        }

        Assert.Equal(TestDictionary.Cultures.OrderBy(x => x), DictionaryCache._languages.Keys.OrderBy(x => x));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Finding_an_item_gives_the_same_item_as_before(int seed)
    {
        var data = new TestDictionary(seed);
        DictionaryCache.Populate(data.Keys, data.Texts, data.Languages);
        var random = new Random(seed);

        for (var i = 0; i < 5000; i++)
        {
            var known = data.Keys[random.Next(data.Keys.Count)];
            var key = random.Next(10) == 0 ? "Missing" + i : TestDictionary.RandomCase(known.key, random);
            var culture = AskedCultures[random.Next(AskedCultures.Length)];

            // Its own parent, the root, or some other folder
            var parent = random.Next(3) switch
            {
                0 => known.parent,
                1 => Guid.Empty,
                _ => data.Keys[random.Next(data.Keys.Count)].id,
            };

            Assert.Same(OldBehaviour.Find(key, culture, parent), DictionaryCache.Find(key, culture, parent));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Reading_a_text_gives_the_same_text_as_before(int seed)
    {
        var data = new TestDictionary(seed);
        DictionaryCache.Populate(data.Keys, data.Texts, data.Languages);
        var random = new Random(seed);

        for (var i = 0; i < 3000; i++)
        {
            var known = data.Keys[random.Next(data.Keys.Count)];
            var key = random.Next(4) switch
            {
                0 => known.key,
                1 => $"Folder0.{known.key}",
                2 => $"Some.Path.{known.key}",
                _ => "Missing" + i,
            };
            key = TestDictionary.RandomCase(key, random);
            var culture = AskedCultures[random.Next(AskedCultures.Length)];

            var expected = OldBehaviour.GetValue(key, culture, "fallback");
            var actual = await _service.GetValueByKeyAndCultureAsync(key, culture, "fallback");

            Assert.Equal(expected, actual);
        }

        // Nothing was read from or written to Umbraco: every answer came from memory
        Assert.Empty(_dictionaryItemService.ReceivedCalls());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Children_and_descendants_are_the_same_as_before(int seed)
    {
        var data = new TestDictionary(seed);
        DictionaryCache.Populate(data.Keys, data.Texts, data.Languages);

        foreach (var key in data.Keys.Select(x => x.key).Append("Missing"))
        {
            foreach (var culture in AskedCultures)
            {
                Assert.Equal(
                    OldBehaviour.GetChildren(key, culture).Select(x => (x.Id, x.Culture)),
                    _service.GetChildren(key, culture).Select(x => (x.Id, x.Culture)));

                Assert.Equal(
                    OldBehaviour.GetDescendants(key, culture).Select(x => (x.Id, x.Culture)),
                    _service.GetDescendants(key, culture).Select(x => (x.Id, x.Culture)));
            }
        }
    }

    [Fact]
    public void A_dash_in_a_key_does_not_match_a_different_key_and_culture()
    {
        // "a-b" in culture "c" and "a" in culture "b-c" share the cache entry name "a-b-c"
        var item = new DictionaryItem { Id = Guid.NewGuid(), Key = "a-b", Culture = "c", Value = "x" };
        DictionaryCache._cache["a-b-c"] = item;

        Assert.Null(DictionaryCache.Find("a", "b-c", Guid.Empty));
        Assert.Null(OldBehaviour.Find("a", "b-c", Guid.Empty));
        Assert.Same(item, DictionaryCache.Find("A-B", "C", Guid.Empty));
    }

    [Fact]
    public async Task A_cache_swapped_in_from_outside_still_finds_texts_in_any_case()
    {
        // Sites' tests replace the cache with a plain dictionary of their own
        var original = DictionaryCache._cache;

        try
        {
            DictionaryCache._cache = new ConcurrentDictionary<string, DictionaryItem>();
            var item = new DictionaryItem { Id = Guid.NewGuid(), Key = "Cart", Culture = "is-IS", Value = "Karfa" };
            DictionaryCache._cache["Cart-is-IS"] = item;

            Assert.Same(item, DictionaryCache.Find("cART", "IS-is", Guid.Empty));
            Assert.Equal("Karfa", await _service.GetValueByKeyAndCultureAsync("CART", "is-is"));
            Assert.True(_service.KeyExist("cart"));
        }
        finally
        {
            DictionaryCache._cache = original;
        }
    }

    [Fact]
    public async Task Saved_and_deleted_texts_show_up_straight_away()
    {
        var cache = new DictionaryCache(Substitute.For<ILanguageService>(), null!, null!);
        var id = Guid.NewGuid();

        await cache.AddOrUpdateAsync("Greeting", id, "Halló", null, "is-IS");
        Assert.Equal("Halló", await _service.GetValueByKeyAndCultureAsync("greeting", "is-IS"));

        await cache.AddOrUpdateAsync("Greeting", id, "Góðan dag", null, "is-IS");
        Assert.Equal("Góðan dag", await _service.GetValueByKeyAndCultureAsync("greeting", "is-IS"));

        cache.Remove("Greeting-is-IS");
        Assert.Equal("fallback", await _service.GetValueByKeyAndCultureAsync("greeting", "is-IS", "fallback"));
    }

    [Fact]
    public async Task A_dotted_key_that_exists_is_read_without_touching_Umbraco()
    {
        var cache = new DictionaryCache(Substitute.For<ILanguageService>(), null!, null!);
        var global = Guid.NewGuid();
        await cache.AddOrUpdateAsync("Global", global, "", null, "is-IS");
        await cache.AddOrUpdateAsync("Cart", Guid.NewGuid(), "Karfa", global, "is-IS");

        var value = await _service.GetValueByKeyAndCultureAsync("Global.Cart", "is-IS", "Karfan", create: true);

        Assert.Equal("Karfa", value);
        Assert.Empty(_dictionaryItemService.ReceivedCalls());
    }
}
