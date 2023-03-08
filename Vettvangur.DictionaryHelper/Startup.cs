using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace DictionaryHelper
{
    class DictionaryComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services
                .AddTransient<DictionaryCache>()
                .AddTransient<DictionaryService>()
                .AddTransient<DictionaryRepository>()
                ;

            builder
                .AddNotificationHandler<DictionaryItemSavedNotification, NotificationHandlers>()
                .AddNotificationHandler<DictionaryItemDeletingNotification, NotificationHandlers>()
            ;

            builder.Components().Append<Startup>();
        }
    }

    class Startup : IComponent
    {
        readonly DictionaryCache _dictionaryCache;
        readonly IServiceProvider _factory;

        public Startup(DictionaryCache dictionaryCache, IServiceProvider factory)
        {
            _dictionaryCache = dictionaryCache;
            _factory = factory;

            Extensions.svc = _factory.GetService<DictionaryService>();
        }

        public void Initialize()
        {
            _dictionaryCache.Fill();
            Configuration.Resolver = _factory;
        }

        public void Terminate() { }
    }

    class NotificationHandlers : 
        INotificationHandler<DictionaryItemSavedNotification>,
        INotificationHandler<DictionaryItemDeletingNotification>
    {
        readonly DictionaryCache _dictionaryCache;
        readonly ILocalizationService _localizationService;
        readonly ILogger<DictionaryItemDeletingNotification> _logger;
        public NotificationHandlers(DictionaryCache dictionaryCache, ILogger<DictionaryItemDeletingNotification> logger, ILocalizationService localizationService)
        {
            _dictionaryCache = dictionaryCache;
            _logger = logger;
            _localizationService = localizationService;
        }

        public void Handle(DictionaryItemDeletingNotification n)
        {
            try
            {
                foreach (var e in n.DeletedEntities)
                {
                    foreach (var t in e.Translations)
                    {
                        _dictionaryCache.Remove(e.ItemKey + "-" + t.Language.IsoCode);
                    }

                    var children = _localizationService.GetDictionaryItemDescendants(e.Key);

                    if (children.Any())
                    {
                        foreach (var c in children)
                        {
                            foreach (var t in c.Translations)
                            {
                                _dictionaryCache.Remove(c.ItemKey + "-" + t.Language.IsoCode);
                            }
                        }
                    }
                }

            } catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to remove dictionary from cache.");
            }


        }
        public void Handle(DictionaryItemSavedNotification n)
        {
            foreach (var e in n.SavedEntities)
            {
                foreach (var t in e.Translations)
                {
                    _dictionaryCache.AddOrUpdate(e.ItemKey, t.Key, t.Value, t.Language.IsoCode);
                }
            }
        }
    }
}
