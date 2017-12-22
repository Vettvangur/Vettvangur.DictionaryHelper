using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
			LocalizationService.DeletingDictionaryItem += DeletingDictionaryItem;

			DictionaryCache.Fill();
		}


		private void SavedDictionaryItem(ILocalizationService sv, SaveEventArgs<IDictionaryItem> args)
		{

			foreach (var e in args.SavedEntities)
			{

				foreach (var t in e.Translations)
				{
					DictionaryCache.AddOrUpdate(e.ItemKey, t.Key, t.Value, t.Language.IsoCode);
				}

			}
		}

		private void DeletingDictionaryItem(ILocalizationService sv, DeleteEventArgs<IDictionaryItem> args)
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

		private static readonly ILog Log =
				LogManager.GetLogger(
					MethodBase.GetCurrentMethod().DeclaringType
				);
	}
}
