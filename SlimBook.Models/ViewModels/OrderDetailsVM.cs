using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimBook.Models.ViewModels
{
	public class OrderDetailsVM
	{
		public OrderHeader OrderHeader { get; set; }
		public IEnumerable<OrderDetails> OrderDetails { get; set; }
		public bool IsInternalUser { get; set; }
	}
}