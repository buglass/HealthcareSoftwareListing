using System;
using System.Collections.Generic;
using System.Linq;
using HealthcareSoftwareListing.Entities;
using HealthcareSoftwareListing.Helpers;
using HealthcareSoftwareListing.Models;

namespace HealthcareSoftwareListing.Services
{
	public class EntityFrameworkRepository : IRepository
	{
		private RepositoryContext _repositoryContext;
		private readonly IPropertyMappingService _propertyMappingService;

		public EntityFrameworkRepository(RepositoryContext repositoryContext, IPropertyMappingService propertyMappingService)
		{
			_repositoryContext = repositoryContext;
			_propertyMappingService = propertyMappingService;
		}

		public void AddCompany(Company company)
		{
			company.Id = Guid.NewGuid();
			_repositoryContext.Companies.Add(company);

			foreach (var product in company.Products)
			{
				product.Id = Guid.NewGuid();
			}

			TrySave();
		}

		public void AddProduct(Guid companyId, Product product)
		{
			var company = GetCompany(companyId);
			if (company != null)
			{
				if (product.Id == Guid.Empty)
				{
					product.Id = Guid.NewGuid(); // Upserting so generate new id.
				}
				company.Products.Add(product);
			}
			TrySave();
		}

		public void DeleteCompany(Company company)
		{
			_repositoryContext.Companies.Remove(company);
			TrySave();
		}

		public void DeleteProduct(Product product)
		{
			_repositoryContext.Products.Remove(product);
		}

		public PagedList<Company> GetCompanies(CompaniesResourceParameters companiesResourceParameters)
		{
			var mappingDictionary = _propertyMappingService.GetPropertyMapping<CompanyDto, Company>();

			// Apply sort to collection using a custom ApplySort extension on IQueryable.
			var collection = _repositoryContext.Companies.ApplySort(
				companiesResourceParameters.OrderBy,
				mappingDictionary);

			// Filtering
			if (!string.IsNullOrEmpty(companiesResourceParameters.Name))
			{
				string nameFilter = companiesResourceParameters.Name.Trim().ToLowerInvariant();
				collection = collection.Where(company => company.Name.ToLowerInvariant() == nameFilter);
			}

			// Filtering on location (v3)
			if (!string.IsNullOrEmpty(companiesResourceParameters.Location))
			{
				string locationFilter = companiesResourceParameters.Location.Trim().ToLowerInvariant();
				collection = collection.Where(company => company.Location.ToLowerInvariant() == locationFilter);
			}

			// Searching (simple hard-coded implementation). Could use Lucene?
			if (!string.IsNullOrEmpty(companiesResourceParameters.SearchQuery))
			{
				string searchQuery = companiesResourceParameters.SearchQuery.Trim().ToLowerInvariant();
				collection = collection.Where(a => a.Name.ToLowerInvariant().Contains(searchQuery));
			}

			// Paging
			return PagedList<Company>.Create(
				source: collection,
				pageNumber: companiesResourceParameters.PageNumber,
				pageSize: companiesResourceParameters.PageSize);
		}

		public IEnumerable<Company> GetCompanies(IEnumerable<Guid> companyIds)
		{
			return _repositoryContext.Companies.Where(company => companyIds.Contains(company.Id));
		}

		public Company GetCompany(Guid companyId)
		{
			return _repositoryContext.Companies.SingleOrDefault(company => company.Id == companyId);
		}

		public Product GetProduct(Guid productId)
		{
			return _repositoryContext.Products.SingleOrDefault(product => product.Id == productId);
		}

		public IEnumerable<Product> GetProductsForCompany(Guid companyId)
		{
			return _repositoryContext.Products.Where(product => product.CompanyId == companyId);
		}

		public void UpdateCompany(Company company)
		{
			TrySave();
		}

		public void UpdateProduct(Product product)
		{
			TrySave();
		}

		private void TrySave()
		{
			if (_repositoryContext.SaveChanges() < 0)
				throw new Exception();
		}

		public void DeleteCompanies()
		{
			_repositoryContext.Companies.RemoveRange(_repositoryContext.Companies);
			TrySave();
		}
	}
}
