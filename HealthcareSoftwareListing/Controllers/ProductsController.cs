using System;
using System.Collections.Generic;
using System.Linq;
using HealthcareSoftwareListing.Entities;
using HealthcareSoftwareListing.Models;
using HealthcareSoftwareListing.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HealthcareSoftwareListing.Controllers
{
	[Route("api/companies/{companyid}/products")]
    public class ProductsController : Controller
    {
		private readonly IRepository _repository;
		private readonly IUrlHelper _urlHelper;
		private readonly ILogger<ProductsController> _logger; // TODO - Implement more complete logging

		public ProductsController(IRepository repository, IUrlHelper urlHelper, ILogger<ProductsController> logger)
		{
			_repository = repository;
			_urlHelper = urlHelper;
			_logger = logger;
		}

		[HttpGet(Name = "GetProductsForCompany")]
		public IActionResult GetProductsForCompany(Guid companyId)
		{
			return
				_repository.GetCompany(companyId) != null
				?
				Ok(
					CreateLinks(
						new LinkedCollectionResourceWrapperDto<ProductDto>(
							AutoMapper.Mapper.Map<IEnumerable<ProductDto>>(
								_repository.GetProductsForCompany(companyId))
							.Select(
								mappedProduct => CreateLinks(mappedProduct)))))
				:
				(IActionResult)NotFound();
		}

		private ProductDto CreateLinks(ProductDto product)
		{
			product.Links.Add(
				new LinkDto(
					href: _urlHelper.Link("GetProductForCompany", new { id = product.Id }),
					rel: "self",
					method: "GET"));

			product.Links.Add(
				new LinkDto(
					href: _urlHelper.Link("DeleteProductForCompany", new { id = product.Id }),
					rel: "delete_product",
					method: "DELETE"));

			product.Links.Add(
				new LinkDto(
					href: _urlHelper.Link("UpdateProductForCompany", new { id = product.Id }),
					rel: "update_product",
					method: "PUT"));

			product.Links.Add(
				new LinkDto(
					href: _urlHelper.Link("PatchProductForCompany", new { id = product.Id }),
					rel: "partially_update_product",
					method: "PATCH"));

			return product;
		}

		private LinkedCollectionResourceWrapperDto<ProductDto> CreateLinks(
			LinkedCollectionResourceWrapperDto<ProductDto> productsWrapper)
		{
			productsWrapper.Links.Add(
				new LinkDto(
					href: _urlHelper.Link("GetProductsForCompany", new { }),
					rel: "self",
					method: "GET"));

			return productsWrapper;
		}

		[HttpGet("{id}", Name = "GetProductForCompany")]
		public IActionResult GetProductForCompany(Guid companyId, Guid id)
		{
			// TODO - Ensure sufficient HATEOAS support
			// TODO - Remove duplicate database calls

			return
				_repository.GetCompany(companyId) != null && _repository.GetProduct(id) != null
				?
				Ok(CreateLinks(AutoMapper.Mapper.Map<ProductDto>(_repository.GetProduct(id))))
				:
				(IActionResult)NotFound();
		}

		[HttpPost(Name = "CreateProductForCompany")]
		public IActionResult CreateProductForCompany(
			Guid companyId,
			[FromBody] ProductForCreationDto product)
		{
			if (product == null)
				return BadRequest();

			Company company = _repository.GetCompany(companyId);

			if (company == null)
				return NotFound();

			// The whole validation approach used relies on; models, data annotations, and rules in a way which
			// leads to duplication of code and merging of concerns. JeremySkinner's FluentValidation is worth a look.

			if (product.Name == company.Name)
				ModelState.AddModelError(nameof(ProductForCreationDto), "Please enter a proper name for the product.");

			if (!ModelState.IsValid)
				return new UnprocessableEntityObjectResult(ModelState); // Custom ObjectResult for 422

			var productEntity = AutoMapper.Mapper.Map<Product>(product);

			_repository.AddProduct(companyId, productEntity); // Needs try / catch

			return CreatedAtRoute(
				routeName: "GetProductForCompany",
				routeValues: new { authorId = companyId, id = productEntity.Id },
				value: CreateLinks(AutoMapper.Mapper.Map<ProductDto>(productEntity)));
		}

		[HttpDelete("{id}", Name = "DeleteProductForCompany")]
		public IActionResult DeleteProductForCompany(Guid companyId, Guid id)
		{
			return
				_repository.GetCompany(companyId) != null && _repository.GetProduct(id) != null
				?
				DeleteProductForCompany(_repository.GetProduct(id))
				:
				NotFound();
		}

		private IActionResult DeleteProductForCompany(Product product)
		{
			_repository.DeleteProduct(product); // Needs try / catch

			_logger.LogInformation(
				eventId: 100,
				message: $"Product {product.Name} was deleted.");

			return NoContent();
		}

		// <summary>
		/// PUT using upserting
		/// </summary>
		[HttpPut("{id}", Name = "UpdateProductForCompany")]
		public IActionResult UpdateProductForCompany(
			Guid companyId,
			Guid id,
			[FromBody] ProductForUpdateDto productUpdates)
		{
			if (productUpdates == null)
				return BadRequest();

			Company company = _repository.GetCompany(companyId);

			if (company == null)
				return NotFound();

			// The whole validation approach used relies on; models, data annotations, and rules in a way which
			// leads to duplication of code and merging of concerns. JeremySkinner's FluentValidation is worth a look.

			if (productUpdates.Name == company.Name)
				ModelState.AddModelError(nameof(ProductForCreationDto), "Please enter a proper name for the product.");

			if (!ModelState.IsValid)
				return new UnprocessableEntityObjectResult(ModelState); // Custom ObjectResult for 422

			if (_repository.GetProduct(id) == null)
			{
				// Upserting implementation.

				var productToAdd = AutoMapper.Mapper.Map<Product>(productUpdates);
				productToAdd.Id = id;

				_repository.AddProduct(companyId, productToAdd); // Needs try / catch

				return CreatedAtRoute(
					routeName: "GetProductForCompany",
					routeValues: new { authorId = companyId, id = id },
					value: AutoMapper.Mapper.Map<ProductDto>(productToAdd));
			}

			Product savedProduct = _repository.GetProduct(id);

			AutoMapper.Mapper.Map(productUpdates, savedProduct);

			_repository.UpdateProduct(savedProduct); // Needs try / catch

			return NoContent();
		}

		[HttpPatch("{id}", Name = "PatchProductForCompany")]
		public IActionResult PatchProductForCompany(
			Guid companyId,
			Guid id,
			[FromBody] JsonPatchDocument<ProductForUpdateDto> productPatchDocument)
		{
			if (productPatchDocument == null)
				return BadRequest();

			Company savedCompany = _repository.GetCompany(companyId);

			if (savedCompany == null)
				return NotFound();

			Product savedProduct = _repository.GetProduct(id);

			if (savedProduct == null)
				return NotFound();

			var productForUpdateDto = AutoMapper.Mapper.Map<ProductForUpdateDto>(savedProduct);
			productPatchDocument.ApplyTo(productForUpdateDto, ModelState);

			if (productForUpdateDto.Name == savedCompany.Name)
				ModelState.AddModelError(nameof(ProductForUpdateDto), "Please enter a proper name for the product.");

			TryValidateModel(productForUpdateDto);

			if (!ModelState.IsValid)
				return new UnprocessableEntityObjectResult(ModelState);

			AutoMapper.Mapper.Map(productForUpdateDto, savedProduct);

			_repository.UpdateProduct(savedProduct); // needs try / catch

			return NoContent();
		}

		[HttpOptions]
		public IActionResult GetProductsOptions()
		{
			Response.Headers.Add("Allow", "GET,OPTIONS,POST");
			return Ok();
		}
	}
}