using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Fix
{
    internal class Program
    {
        public partial class DataBaseContext : DbContext
        {
            public string DbName { get; } = "database.db";

            public DbSet<Product> Products { get; set; } = null!;
            public DbSet<User> Owners { get; set; }
            public DbSet<Location> Locations { get; set; }
            public DbSet<Image> Images { get; set; }



            public DataBaseContext()
            {
                Database.EnsureDeleted();
                Database.EnsureCreated();
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


        static async Task Main(string[] args)
        {
            DataBaseContext context = new DataBaseContext();
            var products1 = Yola.GetAllProducts(new Yola.SearchParams() { City = "sochi" });
            var products2 = await Yola.GetProducts(new Yola.SearchParams() { Limit = 10, Page = 0});
            int count = 0;

            foreach (var product in products2)
            {
                await context.AddAsync(product);
                count++;
            }
            Console.WriteLine(count);


            await context.SaveChangesAsync();

            //var user = await Yola.GetUserByIdAsync("5a03237180e08e05465886a4");
            //Console.WriteLine(user);
        }
    }
}
