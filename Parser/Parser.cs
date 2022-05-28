using DustInTheWind.ConsoleTools.Controls;
using DustInTheWind.ConsoleTools.Controls.Menus;
using DustInTheWind.ConsoleTools.Controls.Menus.MenuItems;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Parser.Parser.Filter;
using static Parser.Yola;

namespace Parser
{
    public class Parser
    {
        public async Task SaveResults()
        {
            SaveToExcel();
            await context.SaveChangesAsync();
        }
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
                public bool IsArchived { get; set; }
                public bool IsSold { get; set; }
                public bool IsBlocked { get; set; }
                public bool IsExpiring { get; set; }
                public bool IsValid() =>
                    !IsShop &&
                    !HasBlackwords &&
                    IsRaitingValid &&
                    !IsExsist &&
                    !IsArchived &&
                    !IsSold &&
                    !IsBlocked &&
                    !IsExpiring;
            }

            public static async Task<FilterResult> Check(Product product, FilterParams filterParams = null)
            {
                if (filterParams == null)
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



                filterResult.IsArchived = product.IsArchived;
                filterResult.IsSold = product.IsSold;
                filterResult.IsBlocked = product.IsBlocked;
                filterResult.IsExpiring = product.IsExpiring;



                return filterResult;
            }
        }
        public FilterParams filterParams { get; set; } = new FilterParams();

        public Parser()
        {
            if (!File.Exists("filter.json"))
            {
                filterParams.BlackwordsDescription = new List<string>() { "Слово_в_описании", "Фраза в описании" };
                filterParams.BlackwordsTitle = new List<string>() { "Слово_в_названии", "Фраза в названии" };
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
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var package = new ExcelPackage();

            var valid = package.Workbook.Worksheets.Add("Валид");
            valid.Cells[1, 1].Value = "Ссылка";
            valid.Cells[1, 2].Value = "Название";
            valid.Cells[1, 3].Value = "Дата публикации";
            valid.Cells[1, 4].Value = "Любые звонки";
            valid.Cells[1, 5].Value = "Системные звонки";
            valid.Cells[1, 6].Value = "P2P звонки";
            valid.Cells[1, 7].Value = "Номер";

            int row = 2;
            int col = 1;

            foreach (var product in ValidProducts)
            {
                valid.Cells[row, col].Hyperlink = new Uri($"https://youla.ru/p{product.IdString}");
                valid.Cells[row, col + 1].Value = product.Name;
                valid.Cells[row, col + 2].Value = Tools.UnixTimeStampToDateTime((double)product.DatePublished).ToString("dd.MM.yyyy");



                if (product.Owner.settings.CallSettings?.any_call_enabled != null)
                {
                    if ((bool)product.Owner.settings.CallSettings.any_call_enabled)
                    {
                        valid.Cells[row, col + 3].Value = "Доступны";
                        valid.Cells[row, col + 3].Style.Font.Color.SetColor(System.Drawing.Color.Green);
                    }
                    else
                    {
                        valid.Cells[row, col + 3].Value = "Недоступны";
                        valid.Cells[row, col + 3].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    }
                }
                else
                {
                    valid.Cells[row, col + 3].Value = "Неизвестно";
                    valid.Cells[row, col + 3].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                }

                if (product.Owner.settings.CallSettings?.system_call_enabled != null)
                {
                    if ((bool)product.Owner.settings.CallSettings.system_call_enabled)
                    {
                        valid.Cells[row, col + 4].Value = "Доступны";
                        valid.Cells[row, col + 4].Style.Font.Color.SetColor(System.Drawing.Color.Green);
                    }
                    else
                    {
                        valid.Cells[row, col + 4].Value = "Недоступны";
                        valid.Cells[row, col + 4].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    }
                }
                else
                {
                    valid.Cells[row, col + 4].Value = "Неизвестно";
                    valid.Cells[row, col + 4].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                }
                if (product.Owner.settings.CallSettings?.p2p_call_enabled != null)
                {
                    if ((bool)product.Owner.settings.CallSettings.p2p_call_enabled)
                    {
                        valid.Cells[row, col + 5].Value = "Доступны";
                        valid.Cells[row, col + 5].Style.Font.Color.SetColor(System.Drawing.Color.Green);
                    }
                    else
                    {
                        valid.Cells[row, col + 5].Value = "Недоступны";
                        valid.Cells[row, col + 5].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    }
                }
                else
                {
                    valid.Cells[row, col + 5].Value = "Неизвестно";
                    valid.Cells[row, col + 5].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                }
                if (!string.IsNullOrWhiteSpace(product.Owner.display_phone_num))
                {
                    valid.Cells[row, col + 6].Value = product.Owner.display_phone_num;
                    valid.Cells[row, col + 6].Style.Font.Color.SetColor(System.Drawing.Color.Green);
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
            invalid.Cells[1, 8].Value = "В архиве";
            invalid.Cells[1, 9].Value = "Продано";
            invalid.Cells[1, 10].Value = "Просрочено";
            invalid.Cells[1, 11].Value = "Заблокированно";

            row = 2;
            col = 1;

            foreach (var product in InvalidProducts)
            {
                invalid.Cells[row, col].Hyperlink = new Uri($"https://youla.ru/p{product.Item1.IdString}");
                invalid.Cells[row, col + 1].Value = product.Item1.Name;
                invalid.Cells[row, col + 2].Value = Tools.UnixTimeStampToDateTime((double)product.Item1.DatePublished).ToString("dd.MM.yyyy");
                invalid.Cells[row, col + 3].Value = product.Item2.IsShop;
                invalid.Cells[row, col + 4].Value = product.Item2.IsRaitingValid;
                invalid.Cells[row, col + 5].Value = product.Item2.HasBlackwords;
                invalid.Cells[row, col + 6].Value = product.Item2.IsExsist;
                invalid.Cells[row, col + 7].Value = product.Item2.IsArchived;
                invalid.Cells[row, col + 8].Value = product.Item2.IsSold;
                invalid.Cells[row, col + 9].Value = product.Item2.IsExpiring;
                invalid.Cells[row, col + 10].Value = product.Item2.IsBlocked;


                invalid.Cells[row, col + 3].Style.Font.Color.SetColor(product.Item2.IsShop ? System.Drawing.Color.Red : System.Drawing.Color.Green);
                invalid.Cells[row, col + 4].Style.Font.Color.SetColor(product.Item2.IsRaitingValid ? System.Drawing.Color.Red : System.Drawing.Color.Green);
                invalid.Cells[row, col + 5].Style.Font.Color.SetColor(product.Item2.HasBlackwords ? System.Drawing.Color.Red : System.Drawing.Color.Green);
                invalid.Cells[row, col + 6].Style.Font.Color.SetColor(product.Item2.IsExsist ? System.Drawing.Color.Red : System.Drawing.Color.Green);
                invalid.Cells[row, col + 7].Style.Font.Color.SetColor(product.Item2.IsArchived ? System.Drawing.Color.Red : System.Drawing.Color.Green);
                invalid.Cells[row, col + 8].Style.Font.Color.SetColor(product.Item2.IsSold ? System.Drawing.Color.Red : System.Drawing.Color.Green);
                invalid.Cells[row, col + 9].Style.Font.Color.SetColor(product.Item2.IsExpiring ? System.Drawing.Color.Red : System.Drawing.Color.Green);
                invalid.Cells[row, col + 10].Style.Font.Color.SetColor(product.Item2.IsBlocked ? System.Drawing.Color.Red : System.Drawing.Color.Green);
                row++;
            }

            valid.Protection.IsProtected = false;



            var excel = package.GetAsByteArray();



            File.WriteAllBytes("result.xlsx", excel);
        }


        public async Task DisplayMenu()
        {
            throw new NotImplementedException();
        }

        public async Task RunWithRandomCity(string link)
        {
            Random r = new Random();
            SearchParams searchParams = new SearchParams(link);
            var cities = await GetAllCities();
            //var randomCity = cities.ElementAt(r.Next(1, cities.Count() - 1));
            City randomCity = cities.OrderBy(x => r.Next()).Take(1).FirstOrDefault();
            searchParams.CityId = randomCity.Id;
            searchParams.CitySlug = randomCity.Slug;

            await GetProductsBySearchParams(searchParams);
        }

        public async Task GetProductsBySearchParams(SearchParams searchParams)
        {
            
            var cities = await GetAllCities();
            var currentCity = cities.FirstOrDefault(c => c.Slug == searchParams.CitySlug);
            Console.WriteLine($"Город: {currentCity.Name}");

            IEnumerable<Product> products = await GetAllProducts(searchParams);//Все объяления
            IEnumerable<Product> disctinctProducts = products.GroupBy(x => x.Owner.idString).Select(y => y.First());//Удаление объявлений от того же продавца
            var dublicates = products.Except(disctinctProducts);
            Console.ForegroundColor = ConsoleColor.Green;

            if (dublicates.Count() > 0)
            {
                Console.WriteLine("Удалены дубли:");

                foreach (var product in dublicates)
                {
                    Console.WriteLine(product.Name);
                }
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

        }

        public async Task RunWithCityFromLink(string link)
        {
            SearchParams searchParams = new SearchParams(link);
            await GetProductsBySearchParams(searchParams);
        }
    }

}
