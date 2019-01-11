using System.ComponentModel.DataAnnotations;

namespace HealthcareSoftwareListing.Models
{
	public class ProductForUpdateDto : ProductForManipulationDto
	{
		[Required(ErrorMessage = "You must enter a name.")]
		public override string Name { get => base.Name; set => base.Name = value; }
	}
}
