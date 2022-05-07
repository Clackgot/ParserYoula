using System;
using System.Threading.Tasks;

namespace Fix
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var products = Yola.GetAllProducts(new Yola.SearchParams() { City="sochi"});
            int count = 0;

            await foreach (var product in products)
            {
                Console.WriteLine(product);
                Console.ReadKey();
                count++;
            }
            Console.WriteLine(count);

            //var user = await Yola.GetUserByIdAsync("5a03237180e08e05465886a4");
            //Console.WriteLine(user);
        }
    }
}
