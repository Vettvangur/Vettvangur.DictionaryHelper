using System.ComponentModel;
using System.Linq;
using Umbraco.Web;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Composing;
using Umbraco.Core.Services.Implement;

namespace DictionaryHelper
{
    public class MyStartup : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Components().Append<Startup>();
        }
    }

    public class Startup : Umbraco.Core.Composing.IComponent
    {
        public void Initialize()
        {
            LocalizationService.SavedDictionaryItem += DictionaryItem_Saved;
            LocalizationService.DeletingDictionaryItem += DictionaryItem_Deleted;

            DictionaryCache.Fill();
        }

        // terminate: runs once when Umbraco stops
        public void Terminate()
        { }

        private void DictionaryItem_Saved(ILocalizationService sv, SaveEventArgs<IDictionaryItem> args)
        {

            foreach (var e in args.SavedEntities)
            {

                foreach (var t in e.Translations)
                {
                    DictionaryCache.AddOrUpdate(e.ItemKey, t.Key, t.Value, t.Language.IsoCode);
                }

            }
        }

        private void DictionaryItem_Deleted(ILocalizationService sv, DeleteEventArgs<IDictionaryItem> args)
        {
            foreach (var e in args.DeletedEntities)
            {

                foreach (var t in e.Translations)
                {
                    DictionaryCache.Remove(e.ItemKey + "-" + t.Language.IsoCode);
                }

                var children = sv.GetDictionaryItemDescendants(e.Key);

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
    }

}
