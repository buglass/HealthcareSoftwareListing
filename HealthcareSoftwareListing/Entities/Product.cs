using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareSoftwareListing.Entities
{
	public class Product
    {
		[Key]
		public Guid Id { get; set; }

		[Required]
		[MaxLength(50)]
		public string Name { get; set; }

		[ForeignKey("CompanyId")]
		public Company Company { get; set; } // EF implementation

		public Guid CompanyId { get; set; }  // EF implementation
	}
}
