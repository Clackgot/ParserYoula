using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
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
                if(!File.Exists(DbName))
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




        public class Parser
        {
            private static DataBaseContext context = new DataBaseContext();

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
                    public bool IsExsist { get; set; }
                    public bool IsShop { get; set; }
                    public bool HasBlackwords { get; set; }
                    public bool IsRaitingValid { get; set; }
                    public bool IsValid() => !IsShop && !HasBlackwords && IsRaitingValid && !IsExsist;
                }

                public static async Task<FilterResult> Check(Product product, FilterParams filterParams = null)
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

                    //context.Products.Select

                    var existedProduct = await context.Products.FirstOrDefaultAsync(p => p.IdString == product.IdString);
                    var existedOwner = await context.Owners.FirstOrDefaultAsync(p => p.Id == product.Owner.Id);
                    filterResult.IsExsist = existedProduct != null || existedOwner != null;
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

            private static void SaveToExcel(List<Product> products)
            {
                var package = new ExcelPackage();

                var sheet = package.Workbook.Worksheets.Add("Результат");
                sheet.Cells[1, 1].Value = "Ссылка";
                sheet.Cells[1, 2].Value = "Название";
                sheet.Cells[1, 3].Value = "Дата публикации";

                int row = 2;
                int col = 1;

                foreach (var product in products)
                {
                    sheet.Cells[row, col].Value = $"https://youla.ru/p{product.IdString}";
                    sheet.Cells[row, col + 1].Value = product.Name;
                    sheet.Cells[row, col + 2].Value = product.DatePublished;
                    row++;
                }

                sheet.Protection.IsProtected = false;
                var excel = package.GetAsByteArray();
                File.WriteAllBytes("result.xlsx", excel);
            }


            public async Task Run()
            {
                string link = "https://youla.ru/pyatigorsk/zhivotnye/tovary?attributes[price][to]=10000&attributes[price][from]=9000";
                SearchParams searchParams = new SearchParams(link);
                IAsyncEnumerable<Product> products = GetAllProducts(searchParams);
                int count = 0;      

                await foreach (Product product in products)
                {
                    await context.AddAsync(product);
                    FilterResult checkResult = await Check(product, filterParams);
                    if (checkResult.IsValid())
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
                SaveToExcel(ValidProducts);
                
                await context.SaveChangesAsync();
            }
        }

        static async Task Main(string[] args)
        {

            Parser parser = new Parser();
            await parser.Run();

        }
    }
}
