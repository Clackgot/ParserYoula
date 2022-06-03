using Microsoft.EntityFrameworkCore;
using System.Data.Entity.Infrastructure;

namespace ParserYoula.Data
{
    public class DataBaseContext : DbContext
    {
        public string DbName { get; } = "database.db";
        public DbSet<Product> Products { get; set; } = null!;
        public DataBaseContext()
        {
            if (!File.Exists(DbName))
            {
                Database.EnsureDeleted();
                Database.EnsureCreated();
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseSqlite($"Filename={DbName}");
    }
}