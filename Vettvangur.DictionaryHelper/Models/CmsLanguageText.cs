using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Persistence;

namespace DictionaryHelper.Models
{
	[TableName("cmsLanguageText")]
	[PrimaryKey("pk", AutoIncrement = true)]
	public class CmsLanguageText
	{
		public int pk { get; set; }
		public int languageId { get; set; }
		public Guid UniqueId { get; set; }
		public string value { get; set; }
	}
}
