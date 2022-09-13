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

            products = products.ToList().OrderByDescending(product => product.CreateDate).ToList();
            ExcelPackage package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Результат");
            
            //sheet.Cells["A1"].Value = "Ссылка";
            //sheet.Cells["B1"].Value = "Название";
            //sheet.Cells["C1"].Value = "Описание";
            sheet.Cells[1, 1].Value = "Ссылка";
            sheet.Cells[1, 2].Value = "Название";
            sheet.Cells[1, 3].Value = "Создано";
            sheet.Rows.Height = 15;
            sheet.DefaultColWidth = 80;
            var firstCol = sheet.Columns.FirstOrDefault();
            var lastCol = sheet.Columns.LastOrDefault();
            if(firstCol != null) firstCol.Width = 41.0f;
            if(lastCol != null) lastCol.Width = 12.0f;
            for (int i = 0; i < products.Count(); i++)
            {
                sheet.Cells[2 + i, 1].Value = products.ElementAt(i)?.ShortLinkYoula;
                sheet.Cells[2 + i, 2].Value = products.ElementAt(i)?.Name;

                double.TryParse(products.ElementAt(i)?.CreateDate, out double createDate);

                sheet.Cells[2 + i, 3].Value = UnixTimeStampToDateTime(createDate)?.ToString("dd.MM.yyyy");
            }
            
            return package.GetAsByteArray();
        }

        private static DateTime? UnixTimeStampToDateTime(double? unixTimeStamp)
        {
            if (unixTimeStamp == null) return null;
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds((double)unixTimeStamp).ToLocalTime();
            return dateTime;
        }

        public void Save(IEnumerable<Product> products)
        {
            File.WriteAllBytes(ExcelFileName, Generate(products));
        }
    }
}
