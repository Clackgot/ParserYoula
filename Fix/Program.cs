using System;
using System.IO;
using System.Threading.Tasks;

namespace Fix
{

    internal class Program
    {
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
