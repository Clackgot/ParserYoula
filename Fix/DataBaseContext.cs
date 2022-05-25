using Microsoft.EntityFrameworkCore;
using System.IO;

namespace Parser
{
    public class DataBaseContext : DbContext
    {
        public string DbName { get; } = "database.db";

        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<User> Owners { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Image> Images { get; set; }



        public DataBaseContext()
        {
            if (!File.Exists(DbName))
            {
                Database.EnsureDeleted();
                Database.EnsureCreated();
            }

            //if (!File.Exists(DbName))
            //    Database.Migrate();
            //Database.Migrate();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Filename={DbName}");
        }
    }
}
