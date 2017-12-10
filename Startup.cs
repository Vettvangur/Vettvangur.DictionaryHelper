using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace DictionaryHelper
{
    class Startup : IApplicationEventHandler
	{
		public void OnApplicationInitialized(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
		{

		}

		public void OnApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
		{

		}
		public void OnApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
		{
			LocalizationService.SavedDictionaryItem += SavedDictionaryItem;
			LocalizationService.DeletedDictionaryItem += DeletedDictionaryItem;

			DictionaryCache.Fill();
		}


		private void SavedDictionaryItem(ILocalizationService sv, SaveEventArgs<IDictionaryItem> args)
		{

			foreach (var e in args.SavedEntities)
			{

				foreach (var t in e.Translations)
				{
					DictionaryCache.AddOrUpdate(e.ItemKey, t.Value, t.Language.CultureName);
				}

			}
		}

		private void DeletedDictionaryItem(ILocalizationService sv, DeleteEventArgs<IDictionaryItem> args)
		{

			foreach (var e in args.DeletedEntities)
			{
				DictionaryCache.Remove(e.ItemKey);
			}
		}
	}
}
