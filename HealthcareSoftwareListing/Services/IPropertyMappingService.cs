using System.Collections.Generic;

namespace HealthcareSoftwareListing.Services
{
	public interface IPropertyMappingService
    {
		Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>();
		bool ValidMappingExistsFor<TSource, TDestination>(string fields);
	}
}
