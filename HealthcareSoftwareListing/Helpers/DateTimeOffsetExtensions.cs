using System;

namespace Library.API.Helpers
{
	public static class DateTimeOffsetExtensions
	{
		public static int GetAge(this DateTimeOffset dateTimeOffset)
		{
			var currentDate = DateTime.UtcNow;
			int age = currentDate.Year - dateTimeOffset.Year;

			if (currentDate < dateTimeOffset.AddYears(age))
			{
				age--;
			}

			return age;
		}

		public static int GetAge(this DateTimeOffset dateTimeOffset, DateTimeOffset? dateOfDemise)
		{
			var dateToCalculateTo = dateOfDemise.HasValue ? dateOfDemise.Value : DateTime.UtcNow;

			int age = dateToCalculateTo.Year - dateTimeOffset.Year;

			if (dateToCalculateTo < dateTimeOffset.AddYears(age))
			{
				age--;
			}

			return age;
		}
	}
}
