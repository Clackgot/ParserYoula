using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Parser
{
    public class ProductNewer : Comparer<Product>
    {
        public override int Compare([AllowNull] Product x, [AllowNull] Product y)
        {
            if (x.DatePublished < y.DatePublished)
            {
                return 1;
            }
            else if (x.DatePublished > y.DatePublished)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }
}
