using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static Fix.Program.Parser.Filter;
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

            public static class Filter
            {
                public class FilterParams : JsonEntity
                {
                    public int MinRatingCount { get; set; } = 0;
                    public int MaxRatingCount { get; set; } = 2;

                    public List<string> Blackwords { get; set; } = new List<string>();

                    public bool withShops { get; set; }
                }
                public class FilterResult : JsonEntity
                {
                    public bool IsShop { get; set; }
                    public bool HasBlackwords { get; set; }
                    public bool IsRaitingValid { get; set; }
                    public bool IsValid() => !IsShop && !HasBlackwords && IsRaitingValid;
                }

                public static FilterResult Check(Product product, FilterParams filterParams = null)
                {
                    if(filterParams == null)
                        filterParams = new FilterParams();
                    FilterResult filterResult = new FilterResult();
                    filterResult.IsRaitingValid = product.Owner.rating_mark_cnt < filterParams.MaxRatingCount &&
                                                product.Owner.rating_mark_cnt > filterParams.MinRatingCount;
                    bool hasBlackwords = false;
                    foreach (var blackWord in filterParams.Blackwords)
                    {
                        hasBlackwords = product.Name.ToLowerInvariant().Contains(blackWord.ToLower()) ||
                                         product.Description.ToLowerInvariant().Contains(blackWord.ToLower());
                        if (hasBlackwords) break;
                    }
                    filterResult.HasBlackwords = hasBlackwords;

                    if (!filterParams.withShops) { filterResult.IsShop = product.Owner.store != null; }
                    else
                    {
                        filterResult.IsShop = false;
                    }
                    return filterResult;
                }
            }
            public FilterParams filterParams { get; set; } = new FilterParams();

            public Parser()
            {
                if(!File.Exists("filter.json"))
                {
                    filterParams.Blackwords = new List<string>() { "example1", "example2"};
                    var filterJson = JsonConvert.SerializeObject(filterParams);
                    File.WriteAllText("filter.json", filterJson);
                }
                else
                {
                    var json = File.ReadAllText("filter.json");
                    filterParams = JsonConvert.DeserializeObject<FilterParams>(json);
                }
            }

            public async Task Run()
            {
                string link = "https://youla.ru/pyatigorsk/zhivotnye";
                var products = GetAllProducts(new SearchParams(link));
                int count = 0;


                await foreach (var product in products)
                {
                    if (Check(product, filterParams).IsValid())
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        ValidProducts.Add(product);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    Console.WriteLine(product.Name);
                    Console.ResetColor();
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
