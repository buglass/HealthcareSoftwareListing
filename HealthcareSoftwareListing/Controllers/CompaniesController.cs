using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using HealthcareSoftwareListing.Entities;
using HealthcareSoftwareListing.Helpers;
using HealthcareSoftwareListing.Models;
using HealthcareSoftwareListing.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace HealthcareSoftwareListing.Controllers
{
	[Route("api/companies")] // hard-coded so refactoring doesn't affect outer-facing contract
	public class CompaniesController : Controller
	{
		private readonly IRepository _repository;
		private readonly IPropertyMappingService _propertyMappingService;
		private readonly ITypeHelperService _typeHelperService;
		private readonly IUrlHelper _urlHelper;
		// TODO - support logging

		public CompaniesController(
			IRepository repository,
			IPropertyMappingService propertyMappingService,
			ITypeHelperService typeHelperService,
			IUrlHelper urlHelper)
		{
			_repository = repository;
			_propertyMappingService = propertyMappingService;
			_typeHelperService = typeHelperService;
			_urlHelper = urlHelper;
		}

		[HttpGet(Name = "GetCompanies")]
		[HttpHead] // Provide response headers without payload to allow consumer test calls
		public IActionResult GetCompanies(
			CompaniesResourceParameters companiesResourceParameters,
			[FromHeader(Name = "Accept")] string mediaType) // Support custom media-type to support HATEOAS
		{
			if (!_propertyMappingService.ValidMappingExistsFor<CompanyDto, Company>(companiesResourceParameters.OrderBy))
				return BadRequest();

			if (!_typeHelperService.TypeHasProperties<CompanyDto>(companiesResourceParameters.Fields))
				return BadRequest();

			PagedList<Company> companies = _repository.GetCompanies(companiesResourceParameters);

			var companyDtos = AutoMapper.Mapper.Map<IEnumerable<CompanyDto>>(companies);

			if (mediaType == "application/vnd.marvin.hateoas+json")
			{
				var paginationMetadata = new
				{
					totalCount = companies.TotalCount,
					pageSize = companies.PageSize,
					currentPage = companies.CurrentPage,
					totalPages = companies.TotalPages
				};

				Response.Headers.Add(
					key: "X-Pagination",
					value: JsonConvert.SerializeObject(paginationMetadata));

				IEnumerable<LinkDto> linkDtos = CreateLinks(
					companiesResourceParameters: companiesResourceParameters,
					hasNextPage: companies.HasNextPage,
					hasPreviousPage: companies.HasPreviousPage);

				IEnumerable<ExpandoObject> dataShapedCompanyDtos = companyDtos.ShapeData(companiesResourceParameters.Fields);

				var dataShapedCompanyDtosWithLinks = dataShapedCompanyDtos.Select(
					companyDto =>
					{
						var companyDtoAsDictionary = companyDto as IDictionary<string, object>;

						var companyLinks = CreateLinks(
							id: (Guid)companyDtoAsDictionary["Id"],
							fields: companiesResourceParameters.Fields);

						companyDtoAsDictionary.Add("links", companyLinks);

						return companyDtoAsDictionary;
					});

				var linkedCollectionResource = new
				{
					value = dataShapedCompanyDtosWithLinks,
					links = linkDtos
				};

				return Ok(linkedCollectionResource);
			}
			else
			{
				var previousPageLink = companies.HasPreviousPage
					? CreateResourceUri(companiesResourceParameters, ResourceUriType.PreviousPage)
					: null;

				var nextPageLink = companies.HasNextPage
					? CreateResourceUri(companiesResourceParameters, ResourceUriType.NextPage)
					: null;

				var paginationMetadata = new
				{
					totalCount = companies.TotalCount,
					pageSize = companies.PageSize,
					currentPage = companies.CurrentPage,
					totalPages = companies.TotalPages,
					previousPageLink = previousPageLink,
					nextPageLink = nextPageLink
				};

				Response.Headers.Add(
					key: "X-Pagination",
					value: JsonConvert.SerializeObject(paginationMetadata));

				return Ok(companyDtos.ShapeData(companiesResourceParameters.Fields));
			}
		}

		private IEnumerable<LinkDto> CreateLinks(Guid id, string fields)
		{
			var links = new List<LinkDto>();

			if (string.IsNullOrWhiteSpace(fields))
			{
				links.Add(new LinkDto(
					href: _urlHelper.Link("GetCompany", new { id = id }),
					rel: "self",
					method: "GET"));
			}
			else
			{
				links.Add(new LinkDto(
					href: _urlHelper.Link("GetCompany", new { id = id, fields = fields }),
					rel: "self",
					method: "GET"));
			}

			links.Add(new LinkDto(
				href: _urlHelper.Link("DeleteCompany", new { id = id }),
				rel: "delete_company",
				method: "DELETE"));

			links.Add(new LinkDto(
				href: _urlHelper.Link("CreateProductForCompany", new { companyId = id }),
				rel: "create_product_for_company",
				method: "POST"));

			links.Add(new LinkDto(
				href: _urlHelper.Link("GetProductsForCompany", new { companyId = id }),
				rel: "products",
				method: "GET"));

			return links;
		}

		private IEnumerable<LinkDto> CreateLinks(
			CompaniesResourceParameters companiesResourceParameters,
			bool hasNextPage,
			bool hasPreviousPage)
		{
			var links = new List<LinkDto>();

			links.Add(
				new LinkDto(
					href: CreateResourceUri(companiesResourceParameters, ResourceUriType.Current),
					rel: "self",
					method: "GET"));

			if (hasNextPage)
				links.Add(
					new LinkDto(
						href: CreateResourceUri(companiesResourceParameters, ResourceUriType.Current),
						rel: "nextPage",
						method: "GET"));

			if (hasPreviousPage)
				links.Add(
					new LinkDto(
						href: CreateResourceUri(companiesResourceParameters, ResourceUriType.Current),
						rel: "previousPage",
						method: "GET"));

			return links;
		}

		private string CreateResourceUri(CompaniesResourceParameters companiesResourceParameters, ResourceUriType resourceUriType)
		{
			switch (resourceUriType)
			{
				case ResourceUriType.PreviousPage:
					return _urlHelper.Link(
						routeName:
							"GetCompanies",
						values:
							new
							{
								fields = companiesResourceParameters.Fields,
								orderBy = companiesResourceParameters.OrderBy,
								searchQuery = companiesResourceParameters.SearchQuery,
								location = companiesResourceParameters.Location,
								pageNumber = companiesResourceParameters.PageNumber - 1,
								pageSize = companiesResourceParameters.PageSize
							});
				case ResourceUriType.NextPage:
					return _urlHelper.Link(
						routeName:
							"GetCompanies",
						values:
							new
							{
								fields = companiesResourceParameters.Fields,
								orderBy = companiesResourceParameters.OrderBy,
								searchQuery = companiesResourceParameters.SearchQuery,
								location = companiesResourceParameters.Location,
								pageNumber = companiesResourceParameters.PageNumber + 1,
								pageSize = companiesResourceParameters.PageSize
							});
				case ResourceUriType.Current:
				default:
					return _urlHelper.Link(
						routeName:
							"GetCompanies",
						values:
							new
							{
								fields = companiesResourceParameters.Fields,
								orderBy = companiesResourceParameters.OrderBy,
								searchQuery = companiesResourceParameters.SearchQuery,
								location = companiesResourceParameters.Location,
								pageNumber = companiesResourceParameters.PageNumber,
								pageSize = companiesResourceParameters.PageSize
							});
			}
		}

		[HttpGet("{id}", Name = "GetCompany")]
		public IActionResult GetCompany(Guid id, [FromQuery] string fields)
		{
			if (!_typeHelperService.TypeHasProperties<CompanyDto>(fields))
				return BadRequest();

			if (_repository.GetCompany(id) == null)
				return NotFound();

			var responseBody =
				((IDictionary<string, object>)
				(AutoMapper.Mapper.Map<CompanyDto>(_repository.GetCompany(id))
				.ShapeData(fields)));

			responseBody.Add("links", CreateLinks(id, fields));

			return Ok(responseBody);
		}

		[HttpPost(Name = "CreateCompany")]
		[RequestHeaderMatchesMediaType("Content-type",
			new[] { "application/vnd.marvin.company.full+json",
			"application/vnd.marvin.company.full+xml"})] // Versioning support with custom attribute
		public IActionResult CreateCompany(
			[FromBody] CompanyForCreationDto company)
		{
			// Automatically handles some basic validation with DTO serialization
			if (company == null)
				return BadRequest();

			// TODO - Add some validation

			var companyEntity = AutoMapper.Mapper.Map<Company>(company);

			_repository.AddCompany(companyEntity); // Needs try catch

			// Support for dynamic HATEOAS on POST
			var companyToReturn = AutoMapper.Mapper.Map<CompanyDto>(companyEntity);

			IEnumerable<LinkDto> links = CreateLinks(
				id: companyToReturn.Id,
				fields: null); // TODO - overload function so null isn't sent when data shaping isn't required

			var linkedResourceToReturn = (IDictionary<string, object>)companyToReturn.ShapeData(null);
			linkedResourceToReturn.Add("links", links);

			return CreatedAtRoute(
				routeName: "GetCompany",
				routeValues: new { id = linkedResourceToReturn["Id"] },
				value: linkedResourceToReturn);
		}

		[HttpPost(Name = "CreateCompanyWithDateOfDemise")]
		[RequestHeaderMatchesMediaType("Content-type", new[] { "application/vnd.marvin.companywithdateofdemise.full+json" })]
		public IActionResult CreateCompanyWithDateOfDemise(
			[FromBody] DemisedCompanyForCreationDto company)
		{
			// Automatically handles some basic validation with DTO serialization
			if (company == null)
				return BadRequest();

			// TODO - Add some validation

			var companyEntity = AutoMapper.Mapper.Map<Company>(company);

			_repository.AddCompany(companyEntity); // Needs try catch

			// Support for dynamic HATEOAS on POST
			var companyToReturn = AutoMapper.Mapper.Map<CompanyDto>(companyEntity);

			IEnumerable<LinkDto> links = CreateLinks(
				id: companyToReturn.Id,
				fields: null); // TODO - overload function so null isn't sent when data shaping isn't required

			var linkedResourceToReturn = (IDictionary<string, object>)companyToReturn.ShapeData(null);
			linkedResourceToReturn.Add("links", links);

			return CreatedAtRoute(
				routeName: "GetCompany",
				routeValues: new { id = linkedResourceToReturn["Id"] },
				value: linkedResourceToReturn);
		}

		[HttpPost(Name = "CreateCompanyWithLocation")]
		[RequestHeaderMatchesMediaType("Content-type", new[] { "application/vnd.marvin.companywithlocation.full+json" })]
		public IActionResult CreateCompanyWithDateOfDemise(
			[FromBody] CompanyWithLocationForCreationDto company)
		{
			// Automatically handles some basic validation with DTO serialization
			if (company == null)
				return BadRequest();

			// TODO - Add some validation

			var companyEntity = AutoMapper.Mapper.Map<Company>(company);

			_repository.AddCompany(companyEntity); // Needs try catch

			// Support for dynamic HATEOAS on POST
			var companyToReturn = AutoMapper.Mapper.Map<CompanyDto>(companyEntity);

			IEnumerable<LinkDto> links = CreateLinks(
				id: companyToReturn.Id,
				fields: null); // TODO - overload function so null isn't sent when data shaping isn't required

			var linkedResourceToReturn = (IDictionary<string, object>)companyToReturn.ShapeData(null);
			linkedResourceToReturn.Add("links", links);

			return CreatedAtRoute(
				routeName: "GetCompany",
				routeValues: new { id = linkedResourceToReturn["Id"] },
				value: linkedResourceToReturn);
		}

		[HttpPost("{id}")]
		public IActionResult BlockAuthorCreation(Guid id)
		{
			// Need to handle an attempted post with a guid.
			// With an existing guid will just be treated automatically as 404.
			// With a non-existing guid the same will happen but should strictly be a 409 conflict.
			// Create a post call which matches the URI of the get call then explicitly handle the incoming request.

			return
				_repository.GetCompany(id) == null
				?
				new StatusCodeResult(StatusCodes.Status409Conflict)
				:
				(IActionResult)NotFound();
		}

		[HttpDelete("{id}", Name = "DeleteCompany")]
		public IActionResult DeleteCompany(Guid id)
		{
			return
				_repository.GetCompany(id) == null
				?
				DeleteCompany(_repository.GetCompany(id))
				:
				NotFound();
		}

		private IActionResult DeleteCompany(Company company)
		{
			_repository.DeleteCompany(company); // Needs try / catch

			return NoContent();
		}

		[HttpPost(Name = "CreateDemisedCompany")]
		[RequestHeaderMatchesMediaType("Content-type",
			new[] { "application/vnd.marvin.demisedcompany.full+json",
			"application/vnd.marvin.demisedcompany.full+xml"})] // Support for XML for this call
		public IActionResult CreateDemisedCompany(
			[FromBody] DemisedCompanyForCreationDto company)
		{
			if (company == null)
				return BadRequest();

			var companyEntity = AutoMapper.Mapper.Map<Company>(company);

			_repository.AddCompany(companyEntity); // Need try / catch

			var companyToReturn = AutoMapper.Mapper.Map<CompanyDto>(companyEntity);
			var links = CreateLinks(companyToReturn.Id, null); // Needs overload to avoid the null
			var linkedResourceToReturn = (IDictionary<string, object>)companyToReturn.ShapeData(null);
			linkedResourceToReturn.Add("links", links);

			return CreatedAtRoute(
				routeName: "GetCompany",
				routeValues: new { id = linkedResourceToReturn["Id"] },
				value: linkedResourceToReturn);
		}

		[HttpDelete(Name = "DeleteCompanies")]
		public IActionResult DeleteCompanies()
		{
			// Included as useful for demo purposes but deletes everything from root!
			_repository.DeleteCompanies(); // Needs try / catch
			return NoContent();
		}

		[HttpPut("{id}", Name = "UpdateCompany")]
		public IActionResult UpdateCompany(
			Guid id,
			[FromBody] CompanyForUpdateDto companyUpdates)
		{
			// Update using PUT for demonstration purposes. No upserting and no patching.

			if (companyUpdates == null)
				return BadRequest();

			Company savedCompany = _repository.GetCompany(id);

			if (savedCompany == null)
				return NotFound();

			AutoMapper.Mapper.Map(companyUpdates, savedCompany);

			_repository.UpdateCompany(savedCompany); // needs try / catch

			return NoContent();
		}

		[HttpPatch("{id}", Name = "PatchCompany")]
		public IActionResult PatchCompany(
			Guid id,
			[FromBody] JsonPatchDocument<CompanyForUpdateDto> companyPatchDocument)
		{
			// Update using PATCH but still no upserting support.

			if (companyPatchDocument == null)
				return BadRequest();

			Company savedCompany = _repository.GetCompany(id);

			if (savedCompany == null)
				return NotFound();

			var companyForUpdateDto = AutoMapper.Mapper.Map<CompanyForUpdateDto>(savedCompany);
			companyPatchDocument.ApplyTo(companyForUpdateDto, ModelState);

			TryValidateModel(companyForUpdateDto);

			if (!ModelState.IsValid)
				return new UnprocessableEntityObjectResult(ModelState);

			AutoMapper.Mapper.Map(companyForUpdateDto, savedCompany);

			_repository.UpdateCompany(savedCompany); // needs try / catch

			return NoContent();
		}

		[HttpOptions]
		public IActionResult GetCompaniesOptions()
		{
			Response.Headers.Add("Allow", "GET,OPTIONS,POST,PUT");
			return Ok();
		}

		[HttpPost(Name = "CreateCompany")]
		[RequestHeaderMatchesMediaType("Content-type", new[] { "application/vnd.marvin.companywithlocation.full+json", "text/csv" })]
		public IActionResult CreateCompanyWithUnsupportedMediaType(
			[FromBody] CompanyForCreationDto company)
		{
			// Hacky - needs work! The custom media type / attribute combination isn't working well for this.
			return new UnsupportedMediaTypeResult();
		}
	}
}
