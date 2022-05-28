using DustInTheWind.ConsoleTools.Controls.InputControls;
using DustInTheWind.ConsoleTools.Controls.Menus;
using DustInTheWind.ConsoleTools.Controls.Menus.MenuItems;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Parser
{

    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.Clear();
            ScrollMenu scrollMenu = new ScrollMenu()
            {
                EraseAfterClose = false,
                Margin = new DustInTheWind.ConsoleTools.Controls.Thickness(0, 1, 0, 0),
                HorizontalAlignment = DustInTheWind.ConsoleTools.Controls.HorizontalAlignment.Left,
                CursorVisibility = false,
                AllowWrapAround = true,
            };

            scrollMenu.AddItems(new IMenuItem[]
            {
                    new LabelMenuItem()
                    {
                        Text = "Город по ссылке",
                        Command = new CityByLinkCommand(),
                        PaddingRight = Console.WindowWidth - 16
                    },
                    new LabelMenuItem()
                    {
                        Text = "Случайный город по ссылке",
                        PaddingRight = Console.WindowWidth - 26,
                        Command = new RandomCityByLinkCommand(),
                    },
                    new LabelMenuItem()
                    {
                        Text = "Случайный город по ссылке(без крупных городов)",
                        PaddingRight = Console.WindowWidth - 47,
                        Command = new RandomCityByLinkFilterTopScoreCommand(),
                    },
                    new LabelMenuItem()
                    {
                        Text = "Случайный город по ссылке(фильтр по кол-ву товаров)",
                        PaddingRight = Console.WindowWidth - 52,
                        Command = new RandomCityByLinkFilterProductCountLessThen(),
                    },
            }); ;
            scrollMenu.Display();


        }
    }

    internal class RandomCityByLinkFilterProductCountLessThen : ICommand
    {
        private readonly Parser parser = new Parser();
        public RandomCityByLinkFilterProductCountLessThen()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            parser.SaveResults().Wait();
        }

        public bool IsActive => true;

        public void Execute()
        {
            Console.Clear();
            while (true)
            {

                try
                {

                    //string link = "https://youla.ru/pyatigorsk/zhivotnye/tovary?attributes[price][to]=10000&attributes[price][from]=1000";

                    Console.WriteLine("Ссылка:");
                    string link = Console.ReadLine();
                    int citiesCount = 0;
                    int productsCount = 0;
                    while (true)
                    {
                        Console.WriteLine("Городов:");
                        if (!int.TryParse(Console.ReadLine(), out citiesCount)) continue;
                        if (citiesCount < 1) continue;
                        break;
                    }
                    while (true)
                    {
                        Console.WriteLine("Продуктов меньше чем[10000..n]:");
                        if (!int.TryParse(Console.ReadLine(), out productsCount)) continue;
                        if (productsCount < 10000) continue;
                        break;
                    }


                    for (int i = 0; i < citiesCount; i++)
                    {
                        parser.RunWithRandomCityFilterProductCountLessThen(link, productsCount).Wait();
                    }
                    parser.SaveResults().Wait();
                    return;

                }
                catch (Exception e)
                {
                    File.WriteAllText("log.txt", e.ToString());
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine(e);
                    Console.ResetColor();
                    parser.SaveResults().Wait();
                    Console.ReadKey();
                    return;
                }
            }
        }
    }

    internal class RandomCityByLinkFilterTopScoreCommand : ICommand
    {
        private readonly Parser parser = new Parser();
        public RandomCityByLinkFilterTopScoreCommand()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            parser.SaveResults().Wait();
        }

        public bool IsActive => true;

        public void Execute()
        {
            Console.Clear();
            while (true)
            {

                try
                {

                    //string link = "https://youla.ru/pyatigorsk/zhivotnye/tovary?attributes[price][to]=10000&attributes[price][from]=1000";

                    Console.WriteLine("Ссылка:");
                    string link = Console.ReadLine();

                    Console.WriteLine("Городов:");
                    if (!int.TryParse(Console.ReadLine(), out int citiesCount)) continue;
                    if (citiesCount < 1) continue;



                    for (int i = 0; i < citiesCount; i++)
                    {
                        parser.RunWithRandomCityFilterTopScore(link).Wait();
                    }
                    parser.SaveResults().Wait();
                    return;

                }
                catch (Exception e)
                {
                    File.WriteAllText("log.txt", e.ToString());
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine(e);
                    Console.ResetColor();
                    parser.SaveResults().Wait();
                    Console.ReadKey();
                    return;
                }
            }
        }
    }

    internal class RandomCityByLinkCommand : ICommand
    {
        private readonly Parser parser = new Parser();
        public RandomCityByLinkCommand()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            parser.SaveResults().Wait();
        }

        public bool IsActive => true;

        public void Execute()
        {
            Console.Clear();
            while (true)
            {
                
                try
                {

                    //string link = "https://youla.ru/pyatigorsk/zhivotnye/tovary?attributes[price][to]=10000&attributes[price][from]=1000";

                    Console.WriteLine("Ссылка:");
                    string link = Console.ReadLine();

                    Console.WriteLine("Городов:");
                    if (!int.TryParse(Console.ReadLine(), out int citiesCount)) continue;
                    if (citiesCount < 1) continue;
                    


                    for (int i = 0; i < citiesCount; i++)
                    {
                        parser.RunWithRandomCity(link).Wait();
                    }
                    parser.SaveResults().Wait();
                    return;

                }
                catch (Exception e)
                {
                    File.WriteAllText("log.txt", e.ToString());
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine(e);
                    Console.ResetColor();
                    parser.SaveResults().Wait();
                    Console.ReadKey();
                    return;
                }
            }
        }
    }

    internal class CityByLinkCommand : ICommand
    {
        private readonly Parser parser = new Parser();
        public CityByLinkCommand()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            parser.SaveResults().Wait();
        }

        public bool IsActive => true;

        public void Execute()
        {
            Console.Clear();
            try
            {
                //string link = "https://youla.ru/pyatigorsk/zhivotnye/tovary?attributes[price][to]=10000&attributes[price][from]=1000";
                StringValue link = new StringValue("Ссылка");
                link.Read();

                parser.RunWithCityFromLink(link.Value).Wait();
                parser.SaveResults().Wait();


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
