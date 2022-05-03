using System.Diagnostics;
using System.Linq;

namespace ParserYoula
{
    public struct SearchAttributes
    {
        public string categorySlug { get; set; }
        public string subcategorySlug { get; set; }
        public string locationId { get; set; }

        public int? priceFrom { get; set; }
        public int? priceTo { get; set; }
        public override string ToString()
        {
            return $"{categorySlug} {subcategorySlug} {locationId} {priceFrom} - {priceTo}";
        }

    }
    public struct Product
    {
        public string Id { get; set; }
        public string OwnerId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? Price { get; set; }
        public int? MarksCount { get; set; }

        public override string ToString()
        {
            return $"{Id}\t {OwnerId}\t {Name}\t {Price / 100} руб.\t {MarksCount}";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Parser parser = new Parser();
            parser.Run().Wait();
        }
    }
}
