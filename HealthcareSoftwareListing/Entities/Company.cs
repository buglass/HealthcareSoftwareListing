using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthcareSoftwareListing.Entities
{
	public class Company
	{
		[Key]
		public Guid Id { get; set; }

		[Required]
		[MaxLength(50)]
		public string Name { get; set; }

		[MaxLength(50)]
		public string Location { get; set; } // v3

		[Required]
		public DateTimeOffset StartDate { get; set; }

		public ICollection<Product> Products { get; set; } = new List<Product>();

		public DateTimeOffset? DateOfDemise { get; set; } // v2
    }
}
