using System.Linq;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using CSharpTest.Net.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DictionaryHelper
{
    class DictionaryComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder
                .AddNotificationHandler<DictionaryItemSavedNotification, NotificationHandlers>()
                .AddNotificationHandler<DictionaryItemDeletingNotification, NotificationHandlers>()
                ;

            builder.Services
                .AddTransient<DictionaryCache>()
                .AddTransient<DictionaryService>()
                .AddTransient<DictionaryRepository>()
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
        readonly DictionaryService _dictionaryService;
        public NotificationHandlers(DictionaryCache dictionaryCache, DictionaryService dictionaryService)
        {
            _dictionaryCache = dictionaryCache;
            _dictionaryService = dictionaryService;
        }

        public void Handle(DictionaryItemDeletingNotification n)
        {
            foreach (var e in n.DeletedEntities)
            {
                foreach (var t in e.Translations)
                {
                    _dictionaryCache.Remove(e.ItemKey + "-" + t.Language.IsoCode);
                }

                var children = _dictionaryService.GetDictionaryItemDescendants(e.Key);

                if (children.Any())
                {
                    foreach (var c in children)
                    {
                        foreach (var t in c.Translations)
                        {
                            DictionaryCache.Remove(c.ItemKey + "-" + t.Language.IsoCode);
                        }
                    }
                }
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
