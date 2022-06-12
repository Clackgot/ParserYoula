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
            sheet.Cells[1, 4].Value = "Обновлено";
            sheet.Cells[1, 5].Value = "Описание";
            sheet.Rows.Height = 15;
            sheet.DefaultColWidth = 30;
            for (int i = 0; i < products.Count(); i++)
            {

                sheet.Cells[2 + i, 1].Value = products.ElementAt(i)?.ShortLinkYoula;
                sheet.Cells[2 + i, 2].Value = products.ElementAt(i)?.Name;



                double.TryParse(products.ElementAt(i)?.CreateDate, out double createDate);
                double.TryParse(products.ElementAt(i)?.UpdateDate, out double updateDate);

                sheet.Cells[2 + i, 3].Value = UnixTimeStampToDateTime(createDate)?.ToString("dd.MM.yyyy");
                sheet.Cells[2 + i, 4].Value = UnixTimeStampToDateTime(updateDate)?.ToString("dd.MM.yyyy");
                sheet.Cells[2 + i, 5].Value = products.ElementAt(i)?.Description;
                //sheet.Cells[2 + i, 5].Style.WrapText = false;
                sheet.Cells[2 + i, 5].Style.ShrinkToFit = true;
                sheet.Cells[2 + i, 5].Style.Indent = 0;
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
