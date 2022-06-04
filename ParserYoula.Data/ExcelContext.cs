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

            sheet.Cells[1, 1].Value = "qwe";


            return package.GetAsByteArray();
        }

        public void Save(IEnumerable<Product> products)
        {
            File.WriteAllBytes(ExcelFileName, Generate(products));
        }
    }
}
