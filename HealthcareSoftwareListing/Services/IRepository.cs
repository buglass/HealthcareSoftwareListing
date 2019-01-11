using System;
using System.Collections.Generic;
using HealthcareSoftwareListing.Entities;
using HealthcareSoftwareListing.Helpers;

namespace HealthcareSoftwareListing.Services
{
	public interface IRepository
    {
		PagedList<Company> GetCompanies(CompaniesResourceParameters companiesResourceParameters);
		Company GetCompany(Guid companyId);
		IEnumerable<Company> GetCompanies(IEnumerable<Guid> companyIds);
		void AddCompany(Company company);
		void DeleteCompany(Company company);
		void UpdateCompany(Company company);
		IEnumerable<Product> GetProductsForCompany(Guid companyId);
		Product GetProduct(Guid productId);
		void AddProduct(Guid companyId, Product product);
		void UpdateProduct(Product product);
		void DeleteProduct(Product product);
		void DeleteCompanies();
    }
}
