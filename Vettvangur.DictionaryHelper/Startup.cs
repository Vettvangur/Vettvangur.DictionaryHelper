using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace DictionaryHelper;

class DictionaryComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services
            .AddTransient<DictionaryCache>()
            .AddTransient<DictionaryService>()
            .AddTransient<DictionaryRepository>()
            ;

        //builder.SearchableTrees().Add<SearchTree>();

        builder
            .AddNotificationAsyncHandler<DictionaryItemSavedNotification, NotificationHandlers>()
            .AddNotificationAsyncHandler<DictionaryItemDeletingNotification, NotificationHandlers>()
        ;

        builder.Components().Append<Startup>();
    }
}

class Startup : IAsyncComponent
{
    readonly DictionaryCache _dictionaryCache;
    readonly IServiceProvider _factory;

    public Startup(DictionaryCache dictionaryCache, IServiceProvider factory)
    {
        _dictionaryCache = dictionaryCache;
        _factory = factory;
    }

    public async Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        await _dictionaryCache.FillAsync(cancellationToken).ConfigureAwait(false);
        Configuration.Resolver = _factory;
    }

    public Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

class NotificationHandlers :
    INotificationAsyncHandler<DictionaryItemSavedNotification>,
    INotificationAsyncHandler<DictionaryItemDeletingNotification>
{
    readonly DictionaryCache _dictionaryCache;
    readonly IDictionaryItemService _dictionaryItemService;
    readonly ILogger<DictionaryItemDeletingNotification> _logger;
    public NotificationHandlers(DictionaryCache dictionaryCache, ILogger<DictionaryItemDeletingNotification> logger, IDictionaryItemService dictionaryItemService)
    {
        _dictionaryCache = dictionaryCache;
        _logger = logger;
        _dictionaryItemService = dictionaryItemService;
    }

    public async Task HandleAsync(DictionaryItemDeletingNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var e in notification.DeletedEntities)
            {
                foreach (var t in e.Translations)
                {
                    _dictionaryCache.Remove(e.ItemKey + "-" + t.LanguageIsoCode);
                }

                var children = await _dictionaryItemService.GetDescendantsAsync(e.Key).ConfigureAwait(false);

                if (children.Any())
                {
                    foreach (var c in children)
                    {
                        foreach (var t in c.Translations)
                        {
                            _dictionaryCache.Remove(c.ItemKey + "-" + t.LanguageIsoCode);
                        }
                    }
                }
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove dictionary from cache.");
        }

    }
    public async Task HandleAsync(DictionaryItemSavedNotification notification, CancellationToken cancellationToken)
    {
        foreach (var e in notification.SavedEntities)
        {
            foreach (var t in e.Translations)
            {
                await _dictionaryCache.AddOrUpdateAsync(e.ItemKey, t.Key, t.Value, e.ParentId, t.LanguageIsoCode).ConfigureAwait(false);
            }
        }
    }
}
