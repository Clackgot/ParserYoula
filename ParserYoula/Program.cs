using System.Diagnostics;
using System.Linq;

namespace ParserYoula
{


    class Program
    {
        static void Main(string[] args)
        {
            Parser parser = new Parser();
            parser.Run().Wait();
        }
    }
}
