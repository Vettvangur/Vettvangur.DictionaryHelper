using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictionaryHelper.Models
{
	public class DictionaryItem
	{
		public string Key { get; set; }
		public string Value { get; set; }
		public string Culture { get; set; }
		public Guid Id { get; set; }
	}
}
