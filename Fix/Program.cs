using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Fix.Program.Parser.Filter;
using static Fix.Yola;

namespace Fix
{
    internal class Program
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }


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
            private List<(Product, FilterResult)> InvalidProducts = new List<(Product, FilterResult)>();

            public static class Filter
            {
                public class FilterParams : JsonEntity
                {
                    public int MinRatingCount { get; set; } = 0;
                    public int MaxRatingCount { get; set; } = 2;

                    public List<string> BlackwordsTitle { get; set; } = new List<string>();
                    public List<string> BlackwordsDescription { get; set; } = new List<string>();

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
                    filterResult.IsRaitingValid = product.Owner.rating_mark_cnt <= filterParams.MaxRatingCount &&
                                                product.Owner.rating_mark_cnt >= filterParams.MinRatingCount;
                    bool hasBlackwords = false;
                    foreach (var blackWord in filterParams.BlackwordsTitle)
                    {
                        hasBlackwords = product.Name.ToLowerInvariant().Contains(blackWord.ToLower());
                        if (hasBlackwords) break;
                    }
                    foreach (var blackWord in filterParams.BlackwordsDescription)
                    {
                        hasBlackwords = product.Description.ToLowerInvariant().Contains(blackWord.ToLower());
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
                    filterParams.BlackwordsDescription = new List<string>() { "Слово_в_описании", "Фраза в описании"};
                    filterParams.BlackwordsTitle = new List<string>() { "Слово_в_названии", "Фраза в названии"};
                    var filterJson = JsonConvert.SerializeObject(filterParams);
                    File.WriteAllText("filter.json", filterJson);
                }
                else
                {
                    var json = File.ReadAllText("filter.json");
                    filterParams = JsonConvert.DeserializeObject<FilterParams>(json);
                }
            }

            private void SaveToExcel()
            {
                var package = new ExcelPackage();

                var valid = package.Workbook.Worksheets.Add("Валид");
                valid.Cells[1, 1].Value = "Ссылка";
                valid.Cells[1, 2].Value = "Название";
                valid.Cells[1, 3].Value = "Дата публикации";
                valid.Cells[1, 4].Value = "Любые звонки";
                valid.Cells[1, 5].Value = "Системные звонки";
                valid.Cells[1, 6].Value = "P2P звонки";

                int row = 2;
                int col = 1;

                foreach (var product in ValidProducts)
                {
                    valid.Cells[row, col].Hyperlink = new Uri($"https://youla.ru/p{product.IdString}");
                    valid.Cells[row, col + 1].Value = product.Name;
                    valid.Cells[row, col + 2].Value = UnixTimeStampToDateTime((double)product.DatePublished).ToString("dd.MM.yyyy");

                    if(product.Owner.settings.CallSettings.any_call_enabled)
                    {
                        valid.Cells[row, col + 3].Value = "Доступны";
                        valid.Cells[row, col + 3].Style.Font.Color.SetColor(System.Drawing.Color.Green);
                    }
                    else
                    {
                        valid.Cells[row, col + 3].Value = "Недоступны";
                        valid.Cells[row, col + 3].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    }

                    if (product.Owner.settings.CallSettings.system_call_enabled)
                    {
                        valid.Cells[row, col + 4].Value = "Доступны";
                        valid.Cells[row, col + 4].Style.Font.Color.SetColor(System.Drawing.Color.Green);
                    }
                    else
                    {
                        valid.Cells[row, col + 4].Value = "Недоступны";
                        valid.Cells[row, col + 4].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    }


                    if (product.Owner.settings.CallSettings.p2p_call_enabled)
                    {
                        valid.Cells[row, col + 5].Value = "Доступны";
                        valid.Cells[row, col + 5].Style.Font.Color.SetColor(System.Drawing.Color.Green);
                    }
                    else
                    {
                        valid.Cells[row, col + 5].Value = "Недоступны";
                        valid.Cells[row, col + 5].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    }
                    row++;
                }

                valid.Protection.IsProtected = false;


                var invalid = package.Workbook.Worksheets.Add("Невалид");
                invalid.Cells[1, 1].Value = "Ссылка";
                invalid.Cells[1, 2].Value = "Название";
                invalid.Cells[1, 3].Value = "Дата публикации";
                invalid.Cells[1, 4].Value = "Это магазин";
                invalid.Cells[1, 5].Value = "Отзывов < 3";
                invalid.Cells[1, 6].Value = "Есть слова из блеклиста";
                invalid.Cells[1, 7].Value = "Уже в базе";

                row = 2;
                col = 1;

                foreach (var product in InvalidProducts)
                {
                    invalid.Cells[row, col].Hyperlink = new Uri($"https://youla.ru/p{product.Item1.IdString}");
                    invalid.Cells[row, col + 1].Value = product.Item1.Name;
                    invalid.Cells[row, col + 2].Value = UnixTimeStampToDateTime((double)product.Item1.DatePublished).ToString("dd.MM.yyyy");
                    invalid.Cells[row, col + 3].Value = product.Item2.IsShop;

                    if (product.Item2.IsShop)
                    {
                        invalid.Cells[row, col + 3].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    }
                    else
                    {
                        invalid.Cells[row, col + 3].Style.Font.Color.SetColor(System.Drawing.Color.Green);
                    }
                    
                    invalid.Cells[row, col + 4].Value = product.Item2.IsRaitingValid;
                    if (product.Item2.IsRaitingValid)
                    {
                        invalid.Cells[row, col + 4].Style.Font.Color.SetColor(System.Drawing.Color.Green);
                    }
                    else
                    {
                        invalid.Cells[row, col + 4].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    }


                    invalid.Cells[row, col + 5].Value = product.Item2.HasBlackwords;
                    if (product.Item2.HasBlackwords)
                    {
                        invalid.Cells[row, col + 5].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    }
                    else
                    {
                        invalid.Cells[row, col + 5].Style.Font.Color.SetColor(System.Drawing.Color.Green);
                    }

                    invalid.Cells[row, col + 6].Value = product.Item2.IsExsist;
                    if (product.Item2.IsExsist)
                    {
                        invalid.Cells[row, col + 6].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    }
                    else
                    {
                        invalid.Cells[row, col + 6].Style.Font.Color.SetColor(System.Drawing.Color.Green);
                    }


                    row++;
                }

                valid.Protection.IsProtected = false;



                var excel = package.GetAsByteArray();



                File.WriteAllBytes("result.xlsx", excel);
            }

            public async Task JoinDatabases()
            {
                List<Product> products = new List<Product>() {
                new Product(){IdString = ""},
                };
                await context.AddRangeAsync(
                    new Product { IdString = "590ccd6027a9ab0f82455b53" },
                    new Product { IdString = "59966c52b5fc2d1b354cabe8" },
                    new Product { IdString = "5a159dfd132ca5303506e7b8" },
                    new Product { IdString = "5ab9458785e9d25659200b42" },
                    new Product { IdString = "5ac4b7f5bd36c034ee31d471" },
                    new Product { IdString = "5b0a799385e9d25e1f4cb5a0" },
                    new Product { IdString = "5b0cf9f79e94ba27d2212ef2" },
                    new Product { IdString = "5b73c339938000762f7ff9df" },
                    new Product { IdString = "5b857aec0fff8160a569c3ba" },
                    new Product { IdString = "5bb33efad67750b4c24fe352" },
                    new Product { IdString = "5beabb442756ba1d473a0912" },
                    new Product { IdString = "5bf94d11aaab2847e52237e2" },
                    new Product { IdString = "5c35d0ac074b3e78cd346c55" },
                    new Product { IdString = "5c850a04f235024d9d25abef" },
                    new Product { IdString = "5c9fc4c10fff81d7062aa793" },
                    new Product { IdString = "5cb554df132ca58a1956ac12" },
                    new Product { IdString = "5cd9081122a449b09d6553ad" },
                    new Product { IdString = "5cf8121f074b3e04f30ea962" },
                    new Product { IdString = "5d0b628227a9abd0d67e6f29" },
                    new Product { IdString = "5d109bbbf235023b4f7305e0" },
                    new Product { IdString = "5d46ebeda0995d87fa4377c2" },
                    new Product { IdString = "5d5b900b93800017cd1b8c02" },
                    new Product { IdString = "5d7e60022756ba66966af5e2" },
                    new Product { IdString = "5dad75e7eef141a0e31de57f" },
                    new Product { IdString = "5dadd4d9a09cd5b66a6f2c86" },
                    new Product { IdString = "5db7b4ba9e94ba1d927f31de" },
                    new Product { IdString = "5e399f29f695768c0b67ae72" },
                    new Product { IdString = "5e57ae02b5fc2d9cd8403823" },
                    new Product { IdString = "5e66b45a226e484e8f453cf3" },
                    new Product { IdString = "5e6c9211aaab28783764f41a" },
                    new Product { IdString = "5e7cbd9c62e1c608165f7f4a" },
                    new Product { IdString = "5ea46a692138bb56f31bd4d3" },
                    new Product { IdString = "5ead5abcb5fc2daecf138ae8" },
                    new Product { IdString = "5eb15f6995c8a40bbb650c33" },
                    new Product { IdString = "5eb666089549bd707011a7c9" },
                    new Product { IdString = "5ec4fc5379823c0e970fde25" },
                    new Product { IdString = "5f19b95712cb176ad56cbd73" },
                    new Product { IdString = "5f2b0088efbf5104f8174fe8" },
                    new Product { IdString = "5f4184e8cbb85875721a9c37" },
                    new Product { IdString = "5f426668700a4900df384aa9" },
                    new Product { IdString = "5f4544abab507d2bf8265a43" },
                    new Product { IdString = "5f4df629ae93091d9c0ff2e6" },
                    new Product { IdString = "5f532ae3c980a00dd44d1fc2" },
                    new Product { IdString = "5f59e9fe34eb4c5f983e66f5" },
                    new Product { IdString = "5f7202cf216817614f689333" },
                    new Product { IdString = "5f786f032d32a15325279a2e" },
                    new Product { IdString = "5f9bd9ba913b9652de382b83" },
                    new Product { IdString = "5fab964fd73c6055ce460ca3" },
                    new Product { IdString = "5fbe2fa14951971fd86122bd" },
                    new Product { IdString = "5fc4fba5025f1f49c5681e58" },
                    new Product { IdString = "5fc9e36d5f2d750a601cd41a" },
                    new Product { IdString = "5fd2278c9385fa535521e2e6" },
                    new Product { IdString = "5fd5bf453d4ece09071e18ab" },
                    new Product { IdString = "5fd5f3bb11522c36610b3a03" },
                    new Product { IdString = "5fdb68e7ab4e7f314808c1b0" },
                    new Product { IdString = "5fdf106d2c1fae3e2e37012d" },
                    new Product { IdString = "5fdf2763763dfc06ae2d76fd" },
                    new Product { IdString = "5ff4354627c4cd574458ea53" },
                    new Product { IdString = "5ff60c0b4adeb754bc74b749" },
                    new Product { IdString = "5ff6c3cb3fa74523083c676b" },
                    new Product { IdString = "5ffed0d200f6c0252c64e9cb" },
                    new Product { IdString = "6002d49c1942287c943c64c2" },
                    new Product { IdString = "6012a5735d12d40d8e1c3866" },
                    new Product { IdString = "60238411b5f23e6a36530e16" },
                    new Product { IdString = "602b683bc1ba340e9a71e523" },
                    new Product { IdString = "603b444a1a55b93dd82189c3" },
                    new Product { IdString = "603f60e4cfa3db237174dcad" },
                    new Product { IdString = "60476ab2a451d16b9e2676a3" },
                    new Product { IdString = "605f4e05fddf4712457b1974" },
                    new Product { IdString = "6062bb1572a75e09172bb30a" },
                    new Product { IdString = "60770555b706862c066582e5" },
                    new Product { IdString = "607840fe656c2c5e204ff4f3" },
                    new Product { IdString = "60850b725cde247b685ab673" },
                    new Product { IdString = "609cc7ff19023177f7668d5d" },
                    new Product { IdString = "60a32b9a76ecbd1697181c7c" },
                    new Product { IdString = "60a4e67bd590cd5692010b33" },
                    new Product { IdString = "60a73ef2661d8a11222b6133" },
                    new Product { IdString = "60b450f356d2574f7979c113" },
                    new Product { IdString = "60b8d27938abe563321e420f" },
                    new Product { IdString = "60d9c4eefe755e53762be19a" },
                    new Product { IdString = "60dbe9bb01b0665b9f376443" },
                    new Product { IdString = "60e31df732480659fc488db1" },
                    new Product { IdString = "60f7125fdf08a9383714a077" },
                    new Product { IdString = "60f726e6d87fc919243d6553" },
                    new Product { IdString = "60feb2bb1ce5b444941fa0d5" },
                    new Product { IdString = "610636e9d62287147f1dc153" },
                    new Product { IdString = "6113cc82b4e1a850b92e022b" },
                    new Product { IdString = "6113d230c7e8147f66319553" },
                    new Product { IdString = "6113f8ff70f895093d7358c9" },
                    new Product { IdString = "6114f205f5de597d875b56c3" },
                    new Product { IdString = "6120ba17069b7a222f229403" },
                    new Product { IdString = "61216195f98d15556c3f6273" },
                    new Product { IdString = "6124df408b09650b400cea8c" },
                    new Product { IdString = "61272695dbe1426f340e29f3" },
                    new Product { IdString = "61285b0308007a6ba869c110" },
                    new Product { IdString = "6138bf1138db3c1eed190269" },
                    new Product { IdString = "613a2433c2aea46d4c4f315e" },
                    new Product { IdString = "613b1a9b9077d77a26192f83" },
                    new Product { IdString = "613de31117d6fe462853be2d" },
                    new Product { IdString = "6140871dce7659153e066964" },
                    new Product { IdString = "6141a3485e9e4a5dfb19bbc3" },
                    new Product { IdString = "6141b0fbe91a600c134f6530" },
                    new Product { IdString = "614313b2390c204df409493a" },
                    new Product { IdString = "61458dee1c206a3263020b71" },
                    new Product { IdString = "6145aebabde93f741b0d8a3b" },
                    new Product { IdString = "6147210237c69264f86fbef0" },
                    new Product { IdString = "614a0ce6ebce9975e813b4da" },
                    new Product { IdString = "614b6c7360466857e31763df" },
                    new Product { IdString = "6155ceb1a22cd60c7236c713" },
                    new Product { IdString = "6156da0be4418d701a34f0c0" },
                    new Product { IdString = "61599582cd349b785e72703c" },
                    new Product { IdString = "6162ebcd7cd4104cea621c40" },
                    new Product { IdString = "6163603086cc76663a2995cd" },
                    new Product { IdString = "6165a80229916c1f1f6154fe" },
                    new Product { IdString = "6165a97d075dd234eb539570" },
                    new Product { IdString = "6166f49cc1d06041ee783760" },
                    new Product { IdString = "6167ccaafa014a651279c624" },
                    new Product { IdString = "616860431ba2b9771c6a87a7" },
                    new Product { IdString = "616aa65fcc003726f84ccfb4" },
                    new Product { IdString = "616eabaddfe0d70fa5636cd8" },
                    new Product { IdString = "616facbbb1d3a915d85cb1a3" },
                    new Product { IdString = "61751846ea8ed278384c5063" },
                    new Product { IdString = "617926680e1c2b18f24a2d07" },
                    new Product { IdString = "617add45334a66007409465f" },
                    new Product { IdString = "617bf9fe43f4ed64fe7a6893" },
                    new Product { IdString = "617d09a56c5997352c26da11" },
                    new Product { IdString = "617e2c3efece1324062b1643" },
                    new Product { IdString = "6188d2d3fe6bab536d5618fa" },
                    new Product { IdString = "6188e9838bd61d3348218c4a" },
                    new Product { IdString = "618d4211d91f80431726ee6b" },
                    new Product { IdString = "618fb1f01c462b4a7e42b249" },
                    new Product { IdString = "61913180c4aca063ae101fc9" },
                    new Product { IdString = "6193f80c96e4cb129a65b81d" },
                    new Product { IdString = "6199b5e6b5476839310a2918" },
                    new Product { IdString = "619cc1e5b6a42556023915fe" },
                    new Product { IdString = "619e629f1b64990abd6dc5f3" },
                    new Product { IdString = "619e918cc518f503f26a57c9" },
                    new Product { IdString = "61a2c0afccea53325453511e" },
                    new Product { IdString = "61a34f4eb5bd613c8f6f5b13" },
                    new Product { IdString = "61a5d7cfb2dd9e7ab7621b8d" },
                    new Product { IdString = "61ace0ee66a87a5e1e45f578" },
                    new Product { IdString = "61b220d5e1fe5151a41bea7d" },
                    new Product { IdString = "61b24af18e89ab144f4f22b6" },
                    new Product { IdString = "61b3104fdfa6033afd3a27df" },
                    new Product { IdString = "61b5dbf4ac1ce4415140b147" },
                    new Product { IdString = "61b708b910bf9d721050cb3a" },
                    new Product { IdString = "61b8c2c6f454f454cc716a55" },
                    new Product { IdString = "61bb47f7700f0572417762fc" },
                    new Product { IdString = "61bc874544257175763d5b96" },
                    new Product { IdString = "61bef90573076338a928a783" },
                    new Product { IdString = "61bf2438896587131814a23b" },
                    new Product { IdString = "61c00858c4d08a58af4dbb4b" },
                    new Product { IdString = "61c0b83cb1b57465f35032a5" },
                    new Product { IdString = "61c754bbf0947a20e63be928" },
                    new Product { IdString = "61c9929d88eed439a179a5bb" },
                    new Product { IdString = "61cd382208b97109be1187d9" },
                    new Product { IdString = "61cd4092f3a0b90ae55537dc" },
                    new Product { IdString = "61cd55bd5d8483308428e689" },
                    new Product { IdString = "61d456eff6847d7517205909" },
                    new Product { IdString = "61dd462b9ff91677d456c061" },
                    new Product { IdString = "61e6400c65d5856ebc518be3" },
                    new Product { IdString = "61e7d02e94b3f67e531e5b1b" },
                    new Product { IdString = "61ec007ff68f9e779b7b9173" },
                    new Product { IdString = "61ed4984023627585b080243" },
                    new Product { IdString = "61eec95234d4893f5b6c5fdd" },
                    new Product { IdString = "61f25d9dc665e130e05f01c4" },
                    new Product { IdString = "61f96f4ed8bfaf6cb5383b0a" },
                    new Product { IdString = "62003d31f323f96ca37c46f3" },
                    new Product { IdString = "6200a802923a6b164b3a43c3" },
                    new Product { IdString = "620372a4afaa81532b0d3d0a" },
                    new Product { IdString = "62079cffd55237429f531c6a" },
                    new Product { IdString = "6208d178b8f81f663b4871b0" },
                    new Product { IdString = "620a079e3f0b4e35fc5401b3" },
                    new Product { IdString = "620a570e25110b1e3d01b1ac" },
                    new Product { IdString = "621118779bc63f64243aa030" },
                    new Product { IdString = "62126742d824d514e7521741" },
                    new Product { IdString = "6214c27ff54a577b0e64cabb" },
                    new Product { IdString = "62171b3086860a68a243c81a" },
                    new Product { IdString = "6219d31833f51619f369699a" },
                    new Product { IdString = "621cd0fd67404870b67220dd" },
                    new Product { IdString = "621ceb2cd44fb6246c16ddc3" },
                    new Product { IdString = "6225ec06a445a7527341f4b3" },
                    new Product { IdString = "6228bd6c5864944cab6794a3" },
                    new Product { IdString = "622b2ea7024f715c910da383" },
                    new Product { IdString = "622b437e63b7673f153cfae3" },
                    new Product { IdString = "622b6b0a1484fa4ea85e5885" },
                    new Product { IdString = "622e14dad864c979d65a0c1e" },
                    new Product { IdString = "6230515e60c8801828753a43" },
                    new Product { IdString = "6232a2c04f2a7c77823b1cad" },
                    new Product { IdString = "62369afff399f1278a0ed69a" },
                    new Product { IdString = "623a6397a41dd44a73268b33" },
                    new Product { IdString = "623b4dda1ee8a66c48002b6e" },
                    new Product { IdString = "623bf28e3126931ceb2d163c" },
                    new Product { IdString = "623c99b6b890a76dc77ff87a" },
                    new Product { IdString = "623d537de89faa1e403dd31f" },
                    new Product { IdString = "623ff6011181f51bf30dfa92" },
                    new Product { IdString = "624007e2b106455feb2cead7" },
                    new Product { IdString = "624095f1988522184430bf9b" },
                    new Product { IdString = "624160cf78c52d6fa10173ee" },
                    new Product { IdString = "62418263c1dec837d52653e2" },
                    new Product { IdString = "624194e0935d7b1b6f552d33" },
                    new Product { IdString = "6243370e1fc6ee41856c37d3" },
                    new Product { IdString = "6243fc6f36dcd9148d02f95f" },
                    new Product { IdString = "6246eb268eefaf6f2273cd5b" },
                    new Product { IdString = "6248348b9b472405e53eba91" },
                    new Product { IdString = "62492a83dd511d13e2369b73" },
                    new Product { IdString = "62495a2589a70b2ba5738a8e" },
                    new Product { IdString = "6249f772c8bf09556e071d2d" },
                    new Product { IdString = "624a9c49b9011f7d3e6d7cb3" },
                    new Product { IdString = "624aac5f025c6f62b74f30d7" },
                    new Product { IdString = "624ab31572006c11df7514bb" },
                    new Product { IdString = "624ac5a34f6a5a11f61625e0" },
                    new Product { IdString = "624b07ba02b56425cc5a47b5" },
                    new Product { IdString = "624b2c35d880f3755a62e360" },
                    new Product { IdString = "624b4efaff6a5f3cfb0e6d53" },
                    new Product { IdString = "624c02e6b0ec7a32f569f4e9" },
                    new Product { IdString = "624c79343e12a63e22378393" },
                    new Product { IdString = "624d449107175454b765f263" },
                    new Product { IdString = "624d53961cb9681a3d51b893" },
                    new Product { IdString = "624d930fdfe5fa66f875fd34" },
                    new Product { IdString = "624e4e00d2255a7819484166" },
                    new Product { IdString = "624ea300dcd54965a2264323" },
                    new Product { IdString = "624ec81c850a5738340192e6" },
                    new Product { IdString = "625235c3cf511455e50ec4d0" },
                    new Product { IdString = "625310f1cc2c6c5253757288" },
                    new Product { IdString = "62542ca476d4a07cdb6a7d14" },
                    new Product { IdString = "62543111467f5d04c33b18ad" },
                    new Product { IdString = "62547858af3121547751a813" },
                    new Product { IdString = "62554e813acb9c15856c15cb" },
                    new Product { IdString = "62556d57226ee56af4087030" },
                    new Product { IdString = "6256532ca7bc397cdd6f398f" },
                    new Product { IdString = "6256cd3cba573d77d80036f3" },
                    new Product { IdString = "6256fabcde3f803fa1146098" },
                    new Product { IdString = "62579035a10d4a75b030b415" },
                    new Product { IdString = "62583851be27a427322d3117" },
                    new Product { IdString = "62591861e6bf9a4b4f7c2923" },
                    new Product { IdString = "625930773f8584504b4b4523" },
                    new Product { IdString = "625948eb5cb45156a76c7f41" },
                    new Product { IdString = "6259e7044dfb2c65a84148b9" },
                    new Product { IdString = "625a509886c966648204b25e" },
                    new Product { IdString = "625a72429b64b5661a144cad" },
                    new Product { IdString = "625a9228d39bd907471c873e" },
                    new Product { IdString = "625aa92544572f05651f9003" },
                    new Product { IdString = "625ad2d45e99c41a903bd8c7" },
                    new Product { IdString = "625b07326b3732409d7e8cf2" },
                    new Product { IdString = "625be48c72424870df27ab96" },
                    new Product { IdString = "625d42adddfa4d5bca5af684" },
                    new Product { IdString = "625d546294d9d135d126b727" },
                    new Product { IdString = "625e57ee853e9374ee75cd88" },
                    new Product { IdString = "625e5b451c79ea329a6edb7f" },
                    new Product { IdString = "625e9dbaf3015e63ed1efa0d" },
                    new Product { IdString = "625ea87e0b632a79a25c2dbc" },
                    new Product { IdString = "625ebb3068e4bb17652f1cb4" },
                    new Product { IdString = "625ec84d4bdf3069153525e7" },
                    new Product { IdString = "625edb464db3657498328a76" },
                    new Product { IdString = "625fb4d8bd32330a68496b32" },
                    new Product { IdString = "6260023feb00d82dd134f331" },
                    new Product { IdString = "62603211fe68015c0b221f0d" },
                    new Product { IdString = "62604df5ca636150056ee2ff" },
                    new Product { IdString = "62617934a268884a1c7b108d" },
                    new Product { IdString = "626229baa2fa7670432aea6a" },
                    new Product { IdString = "62626dafd3f7ee0c113771cd" },
                    new Product { IdString = "62628afc8437146869386d5c" },
                    new Product { IdString = "6262b621457b43703b0d5483" },
                    new Product { IdString = "6263e6a187ef570c2610248d" },
                    new Product { IdString = "62661297dc88cd7ac32846ec" },
                    new Product { IdString = "626673def16bb4365321916f" },
                    new Product { IdString = "62678ef7b5ce8b0827050083" },
                    new Product { IdString = "626922d96c14003c3f6a3be3" },
                    new Product { IdString = "6269259161a901765b646b62" },
                    new Product { IdString = "626abb3509dea54f6a3bfe47" },
                    new Product { IdString = "626bc8592e7d04164773bc8c" },
                    new Product { IdString = "626bcec708a23318824952f7" },
                    new Product { IdString = "626c04233ac75d0fa76ade47" },
                    new Product { IdString = "626cedd8e81f032ce838f787" },
                    new Product { IdString = "626cef74fb89b85ca9190f0a" },
                    new Product { IdString = "626d1e5d6f78e0673e2701de" },
                    new Product { IdString = "626de5e1a80d820b584c413a" },
                    new Product { IdString = "626f8749c329fe0fb9549bbf" },
                    new Product { IdString = "626f9468e8a975437449eded" },
                    new Product { IdString = "626fd906297c702087094660" }
                    );


                List<User> users = new List<User>() {
                    new User { idString = "569e006aececd4760b78b9c5"},
                    new User { idString = "56a5afb9ececd4636f78b9c5"},
                    new User { idString = "56e6213fececd4385093fcbc"},
                    new User { idString = "56f183ad6a11467663c40517"},
                    new User { idString = "56f406eaa9a3fef141845f4e"},
                    new User { idString = "56fd54ff6a11464340c40517"},
                    new User { idString = "570d49566ec863217445f40b"},
                    new User { idString = "575c3b2114d2f5cc5f0208e1"},
                    new User { idString = "575c7fb8d53f3dbf1c77c404"},
                    new User { idString = "57951fe096ad84d60747fed1"},
                    new User { idString = "579c47bad53f3dc71505c568"},
                    new User { idString = "579cd602d53f3d851705c566"},
                    new User { idString = "57b731eb96ad84d642e17ff1"},
                    new User { idString = "57c2da3114d2f53247b287f1"},
                    new User { idString = "57c3e3e61c4031cba5e9f3ad"},
                    new User { idString = "57c63b8f14d2f53f6bb287f1"},
                    new User { idString = "57c9af30d53f3d99472dbe17"},
                    new User { idString = "57d4521fd53f3d4a0afaf4e1"},
                    new User { idString = "57ded4afc5c2e63413fe30bc"},
                    new User { idString = "57e0bc2e8ae74bb725319b1d"},
                    new User { idString = "57ebfb7f1c4031986b2726bf"},
                    new User { idString = "57fe1d0d96ad84501b7b8688"},
                    new User { idString = "580f244586302edd451992f4"},
                    new User { idString = "582d1e07c97904d014a5a9c7"},
                    new User { idString = "5833fd8b9a64a207171b0b8f"},
                    new User { idString = "5854d9b604559f1b74a62297"},
                    new User { idString = "5858c682e931f23088d712cf"},
                    new User { idString = "587f9196e931f2fb97f3ba25"},
                    new User { idString = "588def17d9f65a443facb813"},
                    new User { idString = "5898238414d2f57f57fcbb90"},
                    new User { idString = "58998106d53f3db2755d798a"},
                    new User { idString = "589d79fb86302eb13363b5ad"},
                    new User { idString = "58a2a74b0cc3da728f3342f3"},
                    new User { idString = "58a942fbe57ad483132d06ca"},
                    new User { idString = "58a9fa64c5c2e6c44d7e3c42"},
                    new User { idString = "58c7623bcd3022191c1f886e"},
                    new User { idString = "58c936ed28c4aa9140e8ca03"},
                    new User { idString = "58cab272c97904e578244ed5"},
                    new User { idString = "58cd3d5fcd3022e27985302c"},
                    new User { idString = "58e393a9132ca5aa7071faf2"},
                    new User { idString = "58eb986dc3bdd2246273da24"},
                    new User { idString = "58ec2d4a02a558239f14e802"},
                    new User { idString = "58ec6fd5c3bdd20568010aa2"},
                    new User { idString = "58f86dec9e94ba0d7d76ed72"},
                    new User { idString = "5916a15ab261ff51e9657f18"},
                    new User { idString = "5918a1bd27a9ab5556735705"},
                    new User { idString = "5919408cd677509f0c3fdc12"},
                    new User { idString = "591a859bd67750928408f2e7"},
                    new User { idString = "59217e55f094f303c975a023"},
                    new User { idString = "592558b1f8efdc332a1d1c75"},
                    new User { idString = "592d16f9f235023dd96bd1f3"},
                    new User { idString = "593588362756ba3e94423178"},
                    new User { idString = "594bcf64132ca53e98724cc3"},
                    new User { idString = "5961033e62e1c61683436b72"},
                    new User { idString = "59677fd1e7696a9e7155c409"},
                    new User { idString = "596cfeeecf20458ccf7b88c5"},
                    new User { idString = "596e182a85e9d2104e7d2792"},
                    new User { idString = "5972160aeef1414f2a645676"},
                    new User { idString = "5980886a62e1c688b65b3b56"},
                    new User { idString = "5997de3dbedcc5885c64dd72"},
                    new User { idString = "59a8d7ab66fb07cf7e5526a3"},
                    new User { idString = "59ae61182c593e33793516d3"},
                    new User { idString = "59aeb991c6ab9e27a066fd33"},
                    new User { idString = "59b178806c86cb1f6e5c7d73"},
                    new User { idString = "59c7cd0f22a449cb0259df26"},
                    new User { idString = "59c9ee96c3bdd22de979bab4"},
                    new User { idString = "59ca9325f094f325e5576ff6"},
                    new User { idString = "59e1c454ec985587982b3412"},
                    new User { idString = "59e5e2cb6c86cb132c1bda62"},
                    new User { idString = "59e8552e938000771f553d52"},
                    new User { idString = "59e981a2cf204580004ca894"},
                    new User { idString = "59f6fcf56c86cb0c0a19bce4"},
                    new User { idString = "59f765282c593e821b02e652"},
                    new User { idString = "59fad2002756ba52d94e8593"},
                    new User { idString = "59fdafa4f695767e2616b123"},
                    new User { idString = "5a11a69bec98556087323f35"},
                    new User { idString = "5a139b262aecd61d7e483342"},
                    new User { idString = "5a1b83bed138b3115c5e8c84"},
                    new User { idString = "5a1d7156f8efdc935107e5c2"},
                    new User { idString = "5a252809821a990a1d1a0a02"},
                    new User { idString = "5a488eb7d138b3109d05d893"},
                    new User { idString = "5a4f479ff094f381de58a666"},
                    new User { idString = "5a50ecc185e9d29c417c90b3"},
                    new User { idString = "5a51b4c12756ba0e4112d7b3"},
                    new User { idString = "5a5f0f4462e1c61643388aab"},
                    new User { idString = "5a79f27c0fff81b2bd0ebad2"},
                    new User { idString = "5a7cfc2cf235027da7212532"},
                    new User { idString = "5a817091f8efdc46b501f912"},
                    new User { idString = "5a9191c4821a99d3da0066b2"},
                    new User { idString = "5aa27a62821a99570150ddcf"},
                    new User { idString = "5aa50e09226e48d03f4627a2"},
                    new User { idString = "5aa7a3259e94ba9d617ed97f"},
                    new User { idString = "5aa9691066fb07368c4d6b52"},
                    new User { idString = "5ab243cae7696a40571451af"},
                    new User { idString = "5ab5b7130fff81b7dd4c0213"},
                    new User { idString = "5ab7879d132ca516692c8504"},
                    new User { idString = "5ab89e13c6ab9e8ffd10a184"},
                    new User { idString = "5ac4b37eb5fc2d73ac4144b7"},
                    new User { idString = "5ac5ace2f094f3801e540c43"},
                    new User { idString = "5ac9659deef141cee53c7be3"},
                    new User { idString = "5acc426ff20263a1d019ec95"},
                    new User { idString = "5aeed8acf695765606660419"},
                    new User { idString = "5b056ce322a4494cc8142004"},
                    new User { idString = "5b0daa0666fb077529179e43"},
                    new User { idString = "5b1122e727a9ab0bff34f812"},
                    new User { idString = "5b1c1a9ec6ab9e0d60144dc1"},
                    new User { idString = "5b29f89027a9abb355177b17"},
                    new User { idString = "5b2cc6fd9e94ba43c366f89c"},
                    new User { idString = "5b4d9add80e08e69a0644dc9"},
                    new User { idString = "5b507b472aecd6d06761aaf8"},
                    new User { idString = "5b5824bcf8efdc45781efdb2"},
                    new User { idString = "5b73dfa52aecd62e5564aa32"},
                    new User { idString = "5b7f91bd2aecd69255038037"},
                    new User { idString = "5b8fbe38c3bdd23a5f23e122"},
                    new User { idString = "5b96b2062aecd695e101eb06"},
                    new User { idString = "5b9dde36b261ff3c4227ebe7"},
                    new User { idString = "5b9e3e369380005734705872"},
                    new User { idString = "5bad01a86c86cb929d0a5f0b"},
                    new User { idString = "5bb7a2dec887e03e4222cf82"},
                    new User { idString = "5bb92600dbdf0f088273a062"},
                    new User { idString = "5bbcc4baf2026326267c1e52"},
                    new User { idString = "5bbceb53d138b37eb337b987"},
                    new User { idString = "5bbf6169b5fc2d4c31400f33"},
                    new User { idString = "5be3e92e80e08ecb7a1f83d6"},
                    new User { idString = "5be5b35ef567955b63131172"},
                    new User { idString = "5be7f6ee2aecd68ddf4a10ac"},
                    new User { idString = "5bf3d4e0dbdf0f1bf01ba5a3"},
                    new User { idString = "5bf677f122a449912b6a9d5b"},
                    new User { idString = "5bf6d81fc3bdd2250a67ac17"},
                    new User { idString = "5bfe7b2ccf689a1a592ba6c2"},
                    new User { idString = "5c039c76a09cd593ca61b6a5"},
                    new User { idString = "5c1619b2d138b3104627e7aa"},
                    new User { idString = "5c18e99af20263814333c219"},
                    new User { idString = "5c359f59f202634ec41246d6"},
                    new User { idString = "5c377c5565bcf1093a49aa26"},
                    new User { idString = "5c38b627b5fc2d0a4e151003"},
                    new User { idString = "5c418e6802a5584884167cf2"},
                    new User { idString = "5c42edbcf2026357ef15fa82"},
                    new User { idString = "5c60732f85e9d248211bd265"},
                    new User { idString = "5c666b8f80e08e711d28a822"},
                    new User { idString = "5c66eb62b5fc2da6675f3c7e"},
                    new User { idString = "5c6816102756ba309906a400"},
                    new User { idString = "5c6fafea85e9d22f970d6ae3"},
                    new User { idString = "5c70ce6fe7d7ceb3573e84b2"},
                    new User { idString = "5c72354d2c593e35b67e44d4"},
                    new User { idString = "5c7517aabedcc5578f10a785"},
                    new User { idString = "5c77b9fab261ffa2ef725622"},
                    new User { idString = "5c91ee836c4b44aebc29d490"},
                    new User { idString = "5ca4ad32b261ffbb123e1a28"},
                    new User { idString = "5cc513cb226e48d1f3309bb2"},
                    new User { idString = "5cd29453eef141a39054c2e4"},
                    new User { idString = "5ce690e102a55851ca0192ac"},
                    new User { idString = "5cfcfa012138bb0d9226535f"},
                    new User { idString = "5cfff6702c593e41d3424bf1"},
                    new User { idString = "5d1063b3132ca51eb04c26e5"},
                    new User { idString = "5d11df32df53408ff4748211"},
                    new User { idString = "5d1b74abec98558fcf2b2060"},
                    new User { idString = "5d3b3329132ca597405fd7e4"},
                    new User { idString = "5d49a90da066062c651ac47d"},
                    new User { idString = "5d4d14c0a380b6b94a7ffafe"},
                    new User { idString = "5d5aaecc5eaa9ed4c30195b9"},
                    new User { idString = "5d611dcdf202631eea23ff80"},
                    new User { idString = "5d6908fb80e08ecd86556c77"},
                    new User { idString = "5d6a28f63f53c4897f17e482"},
                    new User { idString = "5d70d149df5340425d4728d2"},
                    new User { idString = "5d7f304ecf20451eb80111a2"},
                    new User { idString = "5d85f117cf204543790d8b82"},
                    new User { idString = "5d9f3f53b5fc2d43d32b6289"},
                    new User { idString = "5dd8a91ac15ae37b3f377dbe"},
                    new User { idString = "5e20723d9e94ba747b5d45f0"},
                    new User { idString = "5e243b11132ca582672092b1"},
                    new User { idString = "5e2a6d0985e9d261be25ba62"},
                    new User { idString = "5e38575e76bdc2c0891a9685"},
                    new User { idString = "5e39812f62e1c6546341b5f2"},
                    new User { idString = "5e3d2493e7d7cea16e199e16"},
                    new User { idString = "5e49435727a9abcb3c1c3fc3"},
                    new User { idString = "5e53c93476bdc2d0093173d3"},
                    new User { idString = "5e54ff7876bdc26b515c16d4"},
                    new User { idString = "5e563a3ae7d7ce2a5d1a7093"},
                    new User { idString = "5e67d148c15ae3d70a186023"},
                    new User { idString = "5e7b49c62138bbb5cb7b43a1"},
                    new User { idString = "5e821a2e13a31e3880560b33"},
                    new User { idString = "5e8f577ac15ae3159e462e62"},
                    new User { idString = "5e91f39a6c4b44a042736fc5"},
                    new User { idString = "5e9c2d4fe7d7ce35020feb03"},
                    new User { idString = "5ea12ab2f235020c7971f8e2"},
                    new User { idString = "5ea28aa027a9ab14de5567a2"},
                    new User { idString = "5ea7f4e72c593e7a6502bc03"},
                    new User { idString = "5eb65ad64a1ca7169c07dcf2"},
                    new User { idString = "5ebb9901a8b03c3aed0d4ff3"},
                    new User { idString = "5ec5b63564bbbb4688747163"},
                    new User { idString = "5ec76a818d7f1a700b138423"},
                    new User { idString = "5ed2191be4cf933eb552d5c3"},
                    new User { idString = "5ed7b27c5d46016084237d1d"},
                    new User { idString = "5ede52168878bb56711031aa"},
                    new User { idString = "5ee1023d059f5769966e1bc1"},
                    new User { idString = "5eea45c170ab6a43ee5c4094"},
                    new User { idString = "5f00dfa2b249043f4126ee69"},
                    new User { idString = "5f12b2f313f99079a92b1b27"},
                    new User { idString = "5f24f5649419464200692023"},
                    new User { idString = "5f26d093135c7a6342194e16"},
                    new User { idString = "5f300c11fe0f9b24e06a64a4"},
                    new User { idString = "5f50aeb0e51c92722b3bf252"},
                    new User { idString = "5f58d36626472138d85f58c3"},
                    new User { idString = "5f71fb6867e21235401dd243"},
                    new User { idString = "5f81fe1001d5274c99792073"},
                    new User { idString = "5f8c3f97166e134881177799"},
                    new User { idString = "5f9bf31b7f786b7e5503f254"},
                    new User { idString = "5f9d840e7d411461fa73ff68"},
                    new User { idString = "5fa41809ed059b38fe3585e3"},
                    new User { idString = "5fc639c8acc3320d9a0fc6be"},
                    new User { idString = "5fca8325bf35c72bbe02c694"},
                    new User { idString = "5fe8db45991df92fd373f624"},
                    new User { idString = "5fef87282bb3b161b8757813"},
                    new User { idString = "5ff023ec88da6551dc7c5941"},
                    new User { idString = "6003caa4cd2941390339cda5"},
                    new User { idString = "600c4bc41d498051003c4ab7"},
                    new User { idString = "601bf67dc4bd27432879a826"},
                    new User { idString = "602f903c1c6794730630c04a"},
                    new User { idString = "603504a0e198ce50910a0d29"},
                    new User { idString = "60489ac9b14be907ab1096d7"},
                    new User { idString = "6055e7722cc92a14687da794"},
                    new User { idString = "6067576e55210c19e352685a"},
                    new User { idString = "608be38a9a076a20e617465f"},
                    new User { idString = "6092b84436dd062a1070aee2"},
                    new User { idString = "609a56fb6875cd4d89065e15"},
                    new User { idString = "60a4c9c6f689d426144f4d4b"},
                    new User { idString = "60ad078f62476008200e428d"},
                    new User { idString = "60b684a5ca9d81548a57ce6b"},
                    new User { idString = "60b74ea3f7149d3201668e34"},
                    new User { idString = "60b841d918b89e029051df37"},
                    new User { idString = "60b88001ba0ffe4ccb1cd005"},
                    new User { idString = "60c01f71952cd4315c70c6a3"},
                    new User { idString = "60c0766613095c12c57631cd"},
                    new User { idString = "60c08bb4b5dccb471b402154"},
                    new User { idString = "60c502220418a97460123e34"},
                    new User { idString = "60c9a3c3499ea73d9159561f"},
                    new User { idString = "60d8725b8c7cde0cc6020585"},
                    new User { idString = "60db3834e58358556c0a50b2"},
                    new User { idString = "60e6024038910b768146bfe6"},
                    new User { idString = "60edd728cae0040d350c155a"},
                    new User { idString = "60f10881c9f7b155a769e3bb"},
                    new User { idString = "60fd74df623489591469c896"},
                    new User { idString = "60fe7ec18282a45ce7657f94"},
                    new User { idString = "60fec12677f108669812359f"},
                    new User { idString = "61125dcdabb202193e72d008"},
                    new User { idString = "611752ca31940a7ae37f2c27"},
                    new User { idString = "611de915b446c9595d2ccf28"},
                    new User { idString = "612c999ec411c252c73b6d12"},
                    new User { idString = "6132ede78e416d42f217e003"},
                    new User { idString = "613dd9d468000e6a95795a5d"},
                    new User { idString = "613e18231b5eb96a17699a82"},
                    new User { idString = "61484efd8bbcc01bae309f7f"},
                    new User { idString = "6151db944930fe750606cf8c"},
                    new User { idString = "61582f5254d8b335273a9023"},
                    new User { idString = "616140f074297f51437ecd53"},
                    new User { idString = "616aa487dcd6165c92718ffd"},
                    new User { idString = "6175184489ff6546441b422a"},
                    new User { idString = "6183e29757597c3b837551ed"},
                    new User { idString = "618d1c37ca92bf040c5e64dd"},
                    new User { idString = "61aba75f55acfb0fa469f27f"},
                    new User { idString = "61bb76e5ef1d9d64f84b299c"},
                    new User { idString = "61c00748d31a867c287ddbda"},
                    new User { idString = "61c5b55315ce4d725042cdf0"},
                    new User { idString = "61c98ea243a5e61c53117be3"},
                    new User { idString = "61d5dbd520326a1e9519ccec"},
                    new User { idString = "61fd1028037a937aec1f8f77"},
                    new User { idString = "620155ede6413a1540682c4b"},
                    new User { idString = "621b8cee8abd924792198c6e"},
                    new User { idString = "621f68ddc7554d619a3325d7"},
                    new User { idString = "622863f936b29c1537291efc"},
                    new User { idString = "622cf4b4cd2b5a71b47defb6"},
                    new User { idString = "622e01a8b79a02713336ff84"},
                    new User { idString = "622e13e58d2f0a1dee5e854f"},
                    new User { idString = "624edb6d1d94466c1823d500"},
                    new User { idString = "6251fd12833754338a599d36"},
                    new User { idString = "625bac0f87b0157e2426d802"},
                    new User { idString = "625bdcdb74a8483770055623"},
                    new User { idString = "626001a7820ea232d15f42bd"},
                    new User { idString = "626673dc0f022d42dd7fff69"},
                    new User { idString = "626ced2cf3bd0c0cad5101f4"}
                };
                await context.AddRangeAsync(products);
                await context.AddRangeAsync(users);
                await context.SaveChangesAsync();
            }


            public async Task Run()
            {

                //string link = "https://youla.ru/pyatigorsk/zhivotnye/tovary?attributes[price][to]=10000&attributes[price][from]=9000";
                Console.WriteLine("Ссылка:");
                string link = Console.ReadLine();
                SearchParams searchParams = new SearchParams(link);
                IEnumerable<Product> products = await GetAllProducts(searchParams);
                IEnumerable<Product> disctinctProducts = products.GroupBy(x => x.Owner.idString).Select(y => y.First());
                var dublicates = products.Except(disctinctProducts);
                Console.ForegroundColor = ConsoleColor.Green;
                
                if(dublicates.Count() > 0)
                {
                    Console.WriteLine("Удалены:");

                    foreach (var product in dublicates)
                    {
                        Console.WriteLine(product.Name);
                    }
                    Console.WriteLine();
                    Console.WriteLine("-----------------------------------------------------");
                }
                else
                {
                    Console.WriteLine("Дублей не обнаружено");
                }
                

                Console.ResetColor();
                int count = 0;
                Console.WriteLine("Результаты:");
                foreach (Product product in disctinctProducts)
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
                        InvalidProducts.Add((product, checkResult));
                    }
                    //Console.WriteLine($"https://youla.ru/p{product.IdString} Отзывов[{filterParams.MinRatingCount}-{filterParams.MaxRatingCount}]:{checkResult.IsRaitingValid} Есть слова из блеклиста:{checkResult.HasBlackwords} Магазин:{checkResult.IsShop}");
                    Console.WriteLine($"https://youla.ru/p{product.IdString}");
                    Console.ResetColor();
                    count++;
                }

                ValidProducts.Sort(new ProductNewer());

                SaveToExcel();
                
                await context.SaveChangesAsync();
            }
        }


        public class ProductNewer : Comparer<Product>
        {
            public override int Compare([AllowNull] Product x, [AllowNull] Product y)
            {
                if(x.DatePublished < y.DatePublished)
                {
                    return 1;
                }
                else if(x.DatePublished > y.DatePublished)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
        }



        static async Task Main(string[] args)
        {

            try
            {
                Parser parser = new Parser();
                //await parser.JoinDatabases();
                await parser.Run();
            }
            catch (Exception e)
            {
                File.WriteAllText("log.txt", e.ToString());
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(e.Message);
                Console.ResetColor();
                Console.ReadKey();
            }

        }
    }
}
