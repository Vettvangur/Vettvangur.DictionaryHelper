using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Persistence;

namespace DictionaryHelper.Models
{
	[TableName("cmsDictionary")]
	[PrimaryKey("pk", AutoIncrement = true)]
	public class CmsDictionary
	{
		public int pk { get; set; }
		public Guid id { get; set; }
		public Guid parent { get; set; }
		public string key { get; set; }
	}
}
