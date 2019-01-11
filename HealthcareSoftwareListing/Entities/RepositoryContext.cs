using Microsoft.EntityFrameworkCore;

namespace HealthcareSoftwareListing.Entities
{
    public class RepositoryContext : DbContext
	{
		public RepositoryContext(DbContextOptions<RepositoryContext> options)
		   : base(options)
		{
			Database.Migrate();
		}

		public DbSet<Company> Companies { get; set; }

		public DbSet<Product> Products { get; set; }
	}
}
