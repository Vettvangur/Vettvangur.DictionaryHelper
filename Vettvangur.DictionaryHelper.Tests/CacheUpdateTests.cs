using DictionaryHelper.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Xunit;
using UmbracoDictionaryItem = Umbraco.Cms.Core.Models.DictionaryItem;

namespace DictionaryHelper.Tests;

/// <summary>
/// What happens to the cache when an editor changes the dictionary on a live site: on this
/// server, and on the others, which only hear about it through Umbraco's cache refresh.
/// </summary>
public sealed class CacheUpdateTests : IDisposable
{
    readonly ILanguage _icelandic = Language(1, "is-IS");
    readonly ILanguage _english = Language(2, "en-US");
    readonly FakeRepository _repository = new();
    readonly DictionaryCache _cache;
    readonly NotificationHandlers _handlers;

    // A small tree: Global > Cart, Global > Checkout > Pay, and a root text Hello
    readonly CmsDictionary _global = Row(1, "Global", Guid.Empty);
    readonly CmsDictionary _cart;
    readonly CmsDictionary _checkout;
    readonly CmsDictionary _pay;
    readonly CmsDictionary _hello = Row(5, "Hello", Guid.Empty);

    public CacheUpdateTests()
    {
        Reset();

        var languageService = Substitute.For<ILanguageService>();
        languageService.GetAllAsync().Returns(Task.FromResult<IEnumerable<ILanguage>>(new[] { _icelandic, _english }));
        _cache = new DictionaryCache(languageService, _repository, NullLogger<DictionaryCache>.Instance);
        _handlers = new NotificationHandlers(_cache, NullLogger<NotificationHandlers>.Instance);

        _cart = Row(2, "Cart", _global.id);
        _checkout = Row(3, "Checkout", _global.id);
        _pay = Row(4, "Pay", _checkout.id);

        var keys = new[] { _global, _cart, _checkout, _pay, _hello };
        DictionaryCache.Populate(keys, keys.SelectMany(Texts), new[] { _icelandic, _english });
    }

    public void Dispose() => Reset();

    static void Reset()
    {
        DictionaryCache._cache.Clear();
        DictionaryCache._languages.Clear();
    }

    [Fact]
    public async Task A_rename_here_stops_the_old_name_returning_the_text()
    {
        var renamed = Item(_cart, "Basket", ("is-IS", "Karfa"));

        await _handlers.HandleAsync(new DictionaryItemSavedNotification(renamed, new EventMessages()), default);

        Assert.Equal("Karfa", DictionaryCache.Find("Basket", "is-IS")?.Value);
        Assert.Null(DictionaryCache.Find("Cart", "is-IS"));
        Assert.Null(DictionaryCache.Find("Cart", "en-US"));
    }

    [Fact]
    public async Task A_move_here_puts_the_item_under_its_new_parent()
    {
        var moved = Item(_cart, "Cart", ("is-IS", "Cart (is-IS)"));
        moved.ParentId = _checkout.id;

        await _handlers.HandleAsync(new DictionaryItemMovedNotification(new MoveEventInfo<IDictionaryItem>(moved, string.Empty, _checkout.pk, _checkout.id), new EventMessages()), default);

        Assert.NotNull(DictionaryCache.Find("Cart", "is-IS", _checkout.id));
        Assert.NotNull(DictionaryCache.Find("Cart", "en-US", _checkout.id));
        Assert.Null(DictionaryCache.Find("Cart", "is-IS", _global.id));
        Assert.Equal("Cart (is-IS)", await Service().GetValueByKeyAndCultureAsync("Global.Checkout.Cart", "is-IS", "x", create: true));
    }

    [Fact]
    public async Task A_delete_here_removes_the_item_and_everything_under_it()
    {
        await _handlers.HandleAsync(new DictionaryItemDeletedNotification(Item(_global, "Global"), new EventMessages()), default);

        foreach (var key in new[] { "Global", "Cart", "Checkout", "Pay" })
        {
            Assert.False(Service().KeyExist(key), key);
        }

        Assert.True(Service().KeyExist("Hello"));
    }

    [Fact]
    public async Task A_save_on_another_server_reloads_the_item_here()
    {
        // The other server renamed Cart to Basket and changed its Icelandic text
        _repository.Rows[_cart.pk] = Row(_cart.pk, "Basket", _global.id, _cart.id);
        _repository.Texts[_cart.id] = [Text(_cart.id, _icelandic, "Karfa"), Text(_cart.id, _english, "Basket")];

        await _handlers.HandleAsync(new DictionaryCacheRefresherNotification(_cart.pk, MessageType.RefreshById), default);

        Assert.Equal("Karfa", DictionaryCache.Find("Basket", "is-IS", _global.id)?.Value);
        Assert.Equal("Basket", DictionaryCache.Find("Basket", "en-US", _global.id)?.Value);
        Assert.Null(DictionaryCache.Find("Cart", "is-IS"));
    }

    [Fact]
    public async Task A_new_item_on_another_server_shows_up_here()
    {
        var thanks = Row(6, "Thanks", _global.id);
        _repository.Rows[thanks.pk] = thanks;
        _repository.Texts[thanks.id] = [Text(thanks.id, _icelandic, "Takk")];

        await _handlers.HandleAsync(new DictionaryCacheRefresherNotification(thanks.pk, MessageType.RefreshById), default);

        Assert.Equal("Takk", await Service().GetValueByKeyAndCultureAsync("Global.Thanks", "is-IS", "x", create: true));
    }

    [Fact]
    public async Task A_delete_on_another_server_removes_the_item_and_everything_under_it_here()
    {
        await _handlers.HandleAsync(new DictionaryCacheRefresherNotification(_checkout.pk, MessageType.RemoveById), default);

        Assert.False(Service().KeyExist("Checkout"));
        Assert.False(Service().KeyExist("Pay"));
        Assert.True(Service().KeyExist("Cart"));
    }

    [Fact]
    public async Task An_item_already_gone_when_its_save_arrives_is_removed()
    {
        // The repository has no row for it any more
        await _handlers.HandleAsync(new DictionaryCacheRefresherNotification(_hello.pk, MessageType.RefreshById), default);

        Assert.False(Service().KeyExist("Hello"));
    }

    [Fact]
    public void Reloading_everything_replaces_changes_and_removes_what_is_gone()
    {
        var thanks = Row(6, "Thanks", Guid.Empty);
        var keys = new[] { _global, _cart, thanks };
        var texts = keys.SelectMany(Texts).Select(x => x.UniqueId == _cart.id && x.languageId == 1 ? Text(_cart.id, _icelandic, "Karfan") : x).ToList();

        DictionaryCache.Reload(keys, texts, new[] { _icelandic, _english });

        Assert.Equal("Karfan", DictionaryCache.Find("Cart", "is-IS")?.Value);
        Assert.NotNull(DictionaryCache.Find("Thanks", "en-US"));
        Assert.False(Service().KeyExist("Checkout"));
        Assert.False(Service().KeyExist("Hello"));
    }

    static DictionaryService Service() => new(Substitute.For<IDictionaryItemService>(), Substitute.For<ILanguageService>());

    static ILanguage Language(int id, string isoCode)
    {
        var language = Substitute.For<ILanguage>();
        language.Id.Returns(id);
        language.IsoCode.Returns(isoCode);
        return language;
    }

    static CmsDictionary Row(int pk, string key, Guid parent, Guid? id = null) =>
        new() { pk = pk, id = id ?? Guid.NewGuid(), key = key, parent = parent };

    IEnumerable<CmsLanguageText> Texts(CmsDictionary row) =>
        [Text(row.id, _icelandic, $"{row.key} (is-IS)"), Text(row.id, _english, $"{row.key} (en-US)")];

    static CmsLanguageText Text(Guid id, ILanguage language, string value) =>
        new() { UniqueId = id, languageId = language.Id, value = value };

    UmbracoDictionaryItem Item(CmsDictionary row, string key, params (string Culture, string Value)[] texts)
    {
        var item = new UmbracoDictionaryItem(row.parent == Guid.Empty ? null : row.parent, key) { Id = row.pk, Key = row.id };
        item.Translations = texts
            .Select(t => (IDictionaryTranslation)new DictionaryTranslation(t.Culture == "is-IS" ? _icelandic : _english, t.Value, row.id))
            .ToList();
        return item;
    }

    /// <summary>The database as the other server left it.</summary>
    sealed class FakeRepository : DictionaryRepository
    {
        public FakeRepository() : base(null!) { }

        public Dictionary<int, CmsDictionary> Rows { get; } = new();
        public Dictionary<Guid, List<CmsLanguageText>> Texts { get; } = new();

        internal override Task<CmsDictionary?> GetKeyAsync(int pk) =>
            Task.FromResult(Rows.TryGetValue(pk, out var row) ? row : null);

        internal override Task<List<CmsLanguageText>> GetTextsAsync(Guid id) =>
            Task.FromResult(Texts.TryGetValue(id, out var texts) ? texts : new List<CmsLanguageText>());
    }
}
