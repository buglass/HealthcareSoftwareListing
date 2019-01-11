using System.Reflection;

namespace HealthcareSoftwareListing.Services
{
    public class TypeHelperService : ITypeHelperService
	{
		public bool TypeHasProperties<T>(string fields)
		{
			if (string.IsNullOrWhiteSpace(fields))
				return true;

			var fieldCollection = fields.Split(',');

			foreach (var field in fieldCollection)
			{
				var propertyName = field.Trim();

				PropertyInfo propertyInfo = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

				if (propertyInfo == null)
					return false;
			}

			return true;
		}
	}
}
