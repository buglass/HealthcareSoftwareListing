namespace HealthcareSoftwareListing.Helpers
{
	public class CompaniesResourceParameters
    {
		const int maxPageSize = 10;

		public int PageNumber { get; set; } = 1;

		private int _pageSize = 10;

		public int PageSize
		{
			get { return _pageSize; }
			set { _pageSize = value > maxPageSize ? maxPageSize : value; }
		}

		public string Name { get; set; }

		public string SearchQuery { get; set; }

		public string OrderBy { get; set; } = "Name";

		public string Fields { get; set; }

		public string Location { get; set; } // v3
	}
}
