using System.Collections.Generic;
using HealthcareSoftwareListing.Models;
using Microsoft.AspNetCore.Mvc;

namespace HealthcareSoftwareListing.Controllers
{
	[Route("api")]
	public class RootController : Controller
	{
		private readonly IUrlHelper _urlHelper;

		/// <summary>
		/// Controller to return general documentation on how to interact with the API to the consumer.
		/// </summary>
		public RootController(IUrlHelper urlHelper)
		{
			_urlHelper = urlHelper;
		}

		[HttpGet(Name = "GetRoot")]
		public IActionResult GetRoot([FromHeader(Name = "Accept")] string mediaType)
		{
			if (mediaType == "application/vnd.marvin.hateoas+json")
			{
				var links = new List<LinkDto>();

				links.Add(new LinkDto(
					href: _urlHelper.Link("GetRoot", new { }),
					rel: "self",
					method: "GET"));

				links.Add(new LinkDto(
					href: _urlHelper.Link("GetCompanies", new { }),
					rel: "companies",
					method: "GET"));

				links.Add(new LinkDto(
					href: _urlHelper.Link("CreateCompany", new { }),
					rel: "create_company",
					method: "POST"));

				return Ok(links);
			}
			else
			{
				return NoContent();
			}
		}
	}
}
