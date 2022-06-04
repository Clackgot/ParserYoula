using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParserYoula.Data
{
    public class ExcelContext
    {
        public string ExcelFileName { get; } = "result.xlsx";

        private byte[] Generate(IEnumerable<Product> products)
        {
            ExcelPackage package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Результат");

            //sheet.Cells["A1"].Value = "Ссылка";
            //sheet.Cells["B1"].Value = "Название";
            //sheet.Cells["C1"].Value = "Описание";

            sheet.Cells[1, 1].Value = "Ссылка";
            sheet.Cells[1, 2].Value = "Название";
            sheet.Cells[1, 3].Value = "Описание";

            for (int i = 0; i < products.Count(); i++)
            {
                sheet.Cells[2 + i, 1].Value = products.ElementAt(i)?.ShortLinkYoula;
                sheet.Cells[2 + i, 2].Value = products.ElementAt(i)?.Name;
                sheet.Cells[2 + i, 3].Value = products.ElementAt(i)?.Description;
            }
            
            return package.GetAsByteArray();
        }

        public void Save(IEnumerable<Product> products)
        {
            File.WriteAllBytes(ExcelFileName, Generate(products));
        }
    }
}
