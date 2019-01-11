using System.ComponentModel.DataAnnotations;

namespace HealthcareSoftwareListing.Models
{
	public abstract class ProductForManipulationDto
	{
		[Required(ErrorMessage = "The product needs a name.")]
		[MaxLength(50, ErrorMessage = "The name shouldn't be longer than 50 characters.")]
		public virtual string Name { get; set; }
	}
}
