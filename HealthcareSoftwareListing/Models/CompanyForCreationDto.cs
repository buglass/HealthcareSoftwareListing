using System;
using System.Collections.Generic;

namespace HealthcareSoftwareListing.Models
{
	public class CompanyForCreationDto
    {
		public string Name { get; set; }
		public DateTimeOffset StartDate { get; set; }
		public string Location { get; set; }
		public ICollection<ProductForCreationDto> Products { get; set; } = new List<ProductForCreationDto>();
	}
}
