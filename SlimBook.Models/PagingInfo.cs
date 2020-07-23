using System;

namespace SlimBook.Models
{
	public class PagingInfo
	{
		public int TotalItem { get; set; }
		public int ItemsPerPage { get; set; }
		public int CurrentPage { get; set; }
		public string UrlParam { get; set; }

		public int TotalPage => (int)Math.Ceiling((decimal)TotalItem / ItemsPerPage);
	}
}