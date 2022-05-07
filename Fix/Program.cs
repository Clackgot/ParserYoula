using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Fix.Yola;

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




        public class Parser
        {
            private DataBaseContext context = new DataBaseContext();

            private List<Product> ValidProducts = new List<Product>();

            public List<string> BlackWords = new List<string>();

            public bool IsValid(Product product)
            {
                if (product != null)
                {
                    bool isRatingLowerThen3 = product.Owner.rating_mark_cnt < 3;
                    bool haveBlackwords = false;
                    foreach (var blackWord in BlackWords)
                    {
                        haveBlackwords = product.Description.Contains(blackWord);
                        if (haveBlackwords) break;
                    }
                    bool isShop = product.Owner.store != null;
                    Console.WriteLine(isShop);
                    Console.WriteLine($"{isRatingLowerThen3} {haveBlackwords} {isShop}");
                    return isRatingLowerThen3 && !haveBlackwords && !isShop;
                }
                else
                {
                    return false;
                }
            }

            public Parser()
            {
            }

            public async Task Run()
            {
                string link = "https://youla.ru/pyatigorsk/zhivotnye/tovary?attributes[price][to]=1000000&attributes[price][from]=900000";
                //var products1 = GetAllProducts(new SearchParams(link));
                var products2 = await GetProducts(new SearchParams(link));
                int count = 0;

                foreach (var product in products2)
                {
                    //await context.AddAsync(product);
                    //IsValid(product);
                    Console.WriteLine(product.Name);
                    count++;
                }

                //await context.SaveChangesAsync();

                //var user = await Yola.GetUserByIdAsync("5a03237180e08e05465886a4");
                //Console.WriteLine(user);
            }
        }

        static async Task Main(string[] args)
        {

            Parser parser = new Parser();
            await parser.Run();

        }
    }
}
