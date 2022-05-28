using DustInTheWind.ConsoleTools.Controls.InputControls;
using DustInTheWind.ConsoleTools.Controls.Menus;
using DustInTheWind.ConsoleTools.Controls.Menus.MenuItems;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Parser
{

    internal class Program
    {
        static async Task Main(string[] args)
        {
            //Console.WriteLine(MainMenu.ShowMainMenu());



            //MenuItem item = new MenuItem();
            //item.Items.Add(new MenuItem() 
            //{ 
            //    Name = "Ссылка", 
            //    Run = (a) => { Console.WriteLine("Выбрана ссылка"); return null; },
            //});
            //item.Items.Add(new MenuItem() { Name = "Пункт"});
            //item.Items.Add(new MenuItem() { Name = "Показать список"});
            //item.Items.Add(new MenuItem() { Name = "Выход"});

            //item.Show();



            ScrollMenu scrollMenu = new ScrollMenu()
            {
                EraseAfterClose = true,
                Padding = new DustInTheWind.ConsoleTools.Controls.Thickness(0, -5, 0, 0),
            };

            scrollMenu.AddItems(new IMenuItem[]
            {
                    new LabelMenuItem()
                    {
                        Text = "Город по ссылке",
                        Command = new CityByLinkCommand(),
                    },
                    new LabelMenuItem()
                    {
                        Text = "Случайный город по ссылке",
                        Command = new RandomCityByLinkCommand(),
                    },
            });
            scrollMenu.Display();

        }
    }

    internal class RandomCityByLinkCommand : ICommand
    {
        public bool IsActive => true;

        public void Execute()
        {
            Console.Clear();
            try
            {
                Parser parser = new Parser();
                //string link = "https://youla.ru/pyatigorsk/zhivotnye/tovary?attributes[price][to]=10000&attributes[price][from]=1000";

                StringValue link = new StringValue("Ссылка:");
                link.Read();

                Int32Value citiesCount = new Int32Value("Количество городов:");
                citiesCount.Read();

                for (int i = 0; i < citiesCount.Value; i++)
                {
                    parser.RunWithRandomCity(link.Value).Wait();
                }
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

    internal class CityByLinkCommand : ICommand
    {
        public bool IsActive => true;

        public void Execute()
        {
            Console.Clear();
            try
            {
                Parser parser = new Parser();
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
