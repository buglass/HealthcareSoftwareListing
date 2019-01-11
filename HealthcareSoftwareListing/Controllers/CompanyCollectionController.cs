using System;
using System.Collections.Generic;
using System.Linq;
using HealthcareSoftwareListing.Entities;
using HealthcareSoftwareListing.Helpers;
using HealthcareSoftwareListing.Models;
using HealthcareSoftwareListing.Services;
using Microsoft.AspNetCore.Mvc;

namespace HealthcareSoftwareListing.Controllers
{
	[Route("api/companycollections")]
	public class CompanyCollectionController : Controller
    {
		private readonly IRepository _repository;

		public CompanyCollectionController(IRepository repository)
		{
			_repository = repository;
		}

		[HttpPost]
		public IActionResult CreateCompanyCollection([FromBody] IEnumerable<CompanyForCreationDto> companyCollection)
		{
			if (companyCollection == null)
				return BadRequest();

			var companyEntities = AutoMapper.Mapper.Map<IEnumerable<Company>>(companyCollection); // mapper confirm configuration would help error handling

			// Surround iteration in try / catch
			foreach (var companyEntity in companyEntities)
			{
				_repository.AddCompany(companyEntity);
			}

			return CreatedAtRoute(
				routeName:
					"GetCompanyCollection",
				routeValues:
					new { ids = string.Join(",", AutoMapper.Mapper.Map<IEnumerable<CompanyDto>>(companyEntities).Select(companyDto => companyDto.Id)) },
				value:
					AutoMapper.Mapper.Map<IEnumerable<CompanyDto>>(companyEntities));

			/* A composite key works much the same but the KVPs allow more complex data than just a 1-2-1 mapping.
			 * So for example you could match on an id and a name.
			 * Would need a route template with two keys which map to two parameters in the action signature.
			 * 
			 * http://localhost:6058/api/companycollections/(key1=value1,key2=value2)
			 * 
			 * Not something that's required here but something that would be good to introduce somewhere.
			 */
		}

		[HttpGet("({ids})", Name = "GetCompanyCollection")]
		public IActionResult GetCompanyCollection(
			[ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids) // Use custom array model binder to bind array
		{
			if (ids == null)
				return BadRequest();

			return
				_repository.GetCompanies(ids).Count() == ids.Count()
				?
				Ok(AutoMapper.Mapper.Map<IEnumerable<CompanyDto>>(_repository.GetCompanies(ids)))
				:
				(IActionResult)NotFound();
		}
	}
}
