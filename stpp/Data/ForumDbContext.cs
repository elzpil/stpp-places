using Microsoft.EntityFrameworkCore;
using stpp.Data.Entities;

namespace stpp.Data
{
    public class ForumDbContext : DbContext
    {
        private readonly IConfiguration _configuration;
        public DbSet<Country> Countries { get; set; }

        public DbSet<City> Cities { get; set; }

        public DbSet<Place> Places { get; set; }

        public ForumDbContext(IConfiguration configuration)
        {
            _configuration  = configuration;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_configuration.GetConnectionString("PostgreSQL"));
        }
    }
}
