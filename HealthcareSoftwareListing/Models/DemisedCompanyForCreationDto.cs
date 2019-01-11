using System;

namespace HealthcareSoftwareListing.Models
{
	public class DemisedCompanyForCreationDto : CompanyForCreationDto
    {
		public DateTimeOffset? DateOfDemise { get; set; }
    }
}
