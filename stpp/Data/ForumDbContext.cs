using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using stpp.Auth.Model;
using stpp.Data.Entities;
using System.Reflection.Emit;

namespace stpp.Data
{
    public class ForumDbContext : IdentityDbContext<ForumRestUser>
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
