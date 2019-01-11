using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;

namespace HealthcareSoftwareListing.Helpers
{
	public static class IEnumerableExtensions
	{
		public static IEnumerable<ExpandoObject> ShapeData<TSource>(
			this IEnumerable<TSource> source,
			string fields)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			var propertyInfoList = new List<PropertyInfo>();
			if (string.IsNullOrWhiteSpace(fields))
			{
				propertyInfoList.AddRange(typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance));
			}
			else
			{
				var fieldCollection = fields.Split(',');

				foreach (var field in fieldCollection)
				{
					var propertyName = field.Trim();

					PropertyInfo propertyInfo = typeof(TSource).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

					if (propertyInfo == null)
						throw new Exception($"Property {propertyName} wasn't found on {typeof(TSource)}");

					propertyInfoList.Add(propertyInfo);
				}
			}

			var expandoObjects = new List<ExpandoObject>();
			foreach (TSource sourceObject in source)
			{
				var expandoObject = new ExpandoObject();

				foreach (var propertyInfo in propertyInfoList)
				{
					var propertyValue = propertyInfo.GetValue(sourceObject);
					((IDictionary<string, object>)expandoObject).Add(propertyInfo.Name, propertyValue);
				}

				expandoObjects.Add(expandoObject);
			}

			return expandoObjects;
		}
	}
}
