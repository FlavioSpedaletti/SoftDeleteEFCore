using Microsoft.EntityFrameworkCore;
using SoftDeleteEFCore.Models;

namespace SoftDeleteEFCore.Data
{
    public sealed class ApplicationDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Pessoa> Pessoas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pessoa>(builder =>
            {
                modelBuilder.Entity<Pessoa>().HasQueryFilter(r => !r.IsDeleted);
            });

            //só para sql server
            //modelBuilder.Entity<Pessoa>()
            //    .HasIndex(r => r.IsDeleted)
            //    .HasFilter("IsDeleted = 0");
        }
    }
}
