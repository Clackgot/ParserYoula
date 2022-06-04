using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserYoula.Data
{
    internal class ExcelContext
    {
        public string ExcelFileName { get; } = "result.xlsx";

        //public byte[] Generate(List<Product> products)

        public ExcelContext()
        {
            ExcelPackage excelPackage = new ExcelPackage();
        }
    }
}
