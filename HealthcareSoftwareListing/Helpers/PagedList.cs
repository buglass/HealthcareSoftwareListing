﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace HealthcareSoftwareListing.Helpers
{
	public class PagedList<T> : List<T>
	{
		public PagedList(List<T> items, int count, int pageNumber, int pageSize)
		{
			TotalCount = count;
			PageSize = pageSize;
			CurrentPage = pageNumber;
			TotalPages = (int)Math.Ceiling(count / (double)pageSize);
			AddRange(items);
		}

		public static PagedList<T> Create(IQueryable<T> source, int pageNumber, int pageSize)
		{
			var count = source.Count();
			var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize);
			return new PagedList<T>(items.ToList(), count, pageNumber, pageSize);
		}

		public int CurrentPage { get; private set; }

		public int TotalPages { get; private set; }

		public int PageSize { get; private set; }

		public int TotalCount { get; private set; }

		public bool HasPreviousPage
		{
			get { return CurrentPage > 1; }
		}

		public bool HasNextPage
		{
			get { return CurrentPage < TotalPages; }
		}
	}
}
