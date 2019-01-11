using System;

namespace HealthcareSoftwareListing.Models
{
	public class ProductDto : LinkedResourceBaseDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; }
	}
}
