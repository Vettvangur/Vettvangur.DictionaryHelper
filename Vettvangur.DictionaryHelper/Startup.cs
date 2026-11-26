using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;

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
            .AddNotificationAsyncHandler<DictionaryItemDeletedNotification, NotificationHandlers>()
            .AddNotificationAsyncHandler<DictionaryItemMovedNotification, NotificationHandlers>()
            .AddNotificationAsyncHandler<DictionaryCacheRefresherNotification, NotificationHandlers>()
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
    INotificationAsyncHandler<DictionaryItemDeletedNotification>,
    INotificationAsyncHandler<DictionaryItemMovedNotification>,
    INotificationAsyncHandler<DictionaryCacheRefresherNotification>
{
    readonly DictionaryCache _dictionaryCache;
    readonly ILogger<NotificationHandlers> _logger;
    public NotificationHandlers(DictionaryCache dictionaryCache, ILogger<NotificationHandlers> logger)
    {
        _dictionaryCache = dictionaryCache;
        _logger = logger;
    }

    public Task HandleAsync(DictionaryItemDeletedNotification notification, CancellationToken cancellationToken)
    {
        // Once the item is really gone, so a delete that something cancels keeps its texts.
        // Umbraco deletes the descendants with it, and the cache finds them by their parents.
        try
        {
            foreach (var e in notification.DeletedEntities)
            {
                DictionaryCache.RemoveItemAndDescendants(e.Key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove dictionary from cache.");
        }

        return Task.CompletedTask;
    }

    public async Task HandleAsync(DictionaryItemSavedNotification notification, CancellationToken cancellationToken)
    {
        foreach (var e in notification.SavedEntities)
        {
            foreach (var t in e.Translations)
            {
                await _dictionaryCache.AddOrUpdateAsync(e.ItemKey, t.Key, t.Value, e.ParentId, t.LanguageIsoCode).ConfigureAwait(false);
            }

            // After a rename, the old name stops returning the text
            DictionaryCache.ForgetOtherNames(e.Key, e.ItemKey);
            DictionaryCache.RememberPk(e.Id, e.Key);
        }
    }

    public Task HandleAsync(DictionaryItemMovedNotification notification, CancellationToken cancellationToken)
    {
        // Only this server hears about a move: Umbraco sends the other servers nothing for it.
        try
        {
            foreach (var move in notification.MoveInfoCollection)
            {
                DictionaryCache.MoveItem(move.Entity.Key, move.Entity.ParentId ?? Guid.Empty);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move a dictionary item in the cache.");
        }

        return Task.CompletedTask;
    }

    public async Task HandleAsync(DictionaryCacheRefresherNotification notification, CancellationToken cancellationToken)
    {
        // Umbraco sends this to every server, this one included, when an item is saved or deleted,
        // so the other servers' caches follow along too.
        try
        {
            switch (notification.MessageType)
            {
                case MessageType.RefreshById when notification.MessageObject is int pk:
                    await _dictionaryCache.RefreshItemAsync(pk).ConfigureAwait(false);
                    break;

                case MessageType.RemoveById when notification.MessageObject is int pk:
                    DictionaryCache.RemoveItem(pk);
                    break;

                case MessageType.RefreshAll:
                    await _dictionaryCache.ReloadAllAsync(cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update the dictionary cache after a change.");
        }
    }
}
