using Microsoft.EntityFrameworkCore;
using SoftDeleteEFCore.Models;

namespace SoftDeleteEFCore.Data
{
    public class ApplicationDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Pessoa> Pessoas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pessoa>(builder =>
            {
                
            });
        }
    }
}
