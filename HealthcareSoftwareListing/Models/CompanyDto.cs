using System;

namespace HealthcareSoftwareListing.Models
{
	public class CompanyDto
    {
		public Guid Id { get; set; }
		public string Name { get; set; }
		public int Age { get; set; }
		public string Location { get; set; }
	}
}
