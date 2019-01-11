using System.Collections.Generic;

namespace HealthcareSoftwareListing.Models
{
	public abstract class LinkedResourceBaseDto
	{
		public List<LinkDto> Links { get; set; } = new List<LinkDto>();
	}
}
