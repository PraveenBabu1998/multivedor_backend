using elemechWisetrack.Models;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;

public class ExcelReader
{
    public static List<string> ReadColumnB(IFormFile file)
    {
        List<string> columnB = new List<string>();

        using (var stream = new MemoryStream())
        {
            file.CopyTo(stream);

            using (var package = new ExcelPackage(stream))
            {
                var sheet = package.Workbook.Worksheets[0];
                int rows = sheet.Dimension.Rows;

                for (int i = 2; i <= rows; i++)
                {
                    columnB.Add(sheet.Cells[i, 2].Text);
                }
            }
        }

        return columnB;
    }

    public static List<string> ReadColumnG(IFormFile file)
    {
        List<string> columnG = new List<string>();

        using (var stream = new MemoryStream())
        {
            file.CopyTo(stream);

            using (var package = new ExcelPackage(stream))
            {
                var sheet = package.Workbook.Worksheets[0];

                int rows = sheet.Dimension.End.Row;

                for (int i = 2; i <= rows; i++) // start from row 2 (skip header)
                {
                    var value = sheet.Cells[i, 7].Text; // Column E

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        columnG.Add(value);
                    }
                }
            }
        }

        return columnG;
    }

    //public static List<string> ReadColumnV(IFormFile file)
    //{
    //    List<string> columnG = new List<string>();

    //    using (var stream = new MemoryStream())
    //    {
    //        file.CopyTo(stream);

    //        using (var package = new ExcelPackage(stream))
    //        {
    //            var sheet = package.Workbook.Worksheets[0];

    //            int rows = sheet.Dimension.End.Row;

    //            for (int i = 2; i <= rows; i++) // start from row 2 (skip header)
    //            {
    //                var value = sheet.Cells[i, 23].Text; // Column E

    //                if (!string.IsNullOrWhiteSpace(value))
    //                {
    //                    columnG.Add(value);
    //                }
    //            }
    //        }
    //    }

    //    return columnG;
    //}

    public static List<ExcelProductRow> ReadExcelData(IFormFile file)
    {
        List<ExcelProductRow> data = new List<ExcelProductRow>();

        using (var stream = new MemoryStream())
        {
            file.CopyTo(stream);

            using (var package = new ExcelPackage(stream))
            {
                var sheet = package.Workbook.Worksheets[0];
                int rows = sheet.Dimension.End.Row;

                for (int i = 2; i <= rows; i++) // skip header
                {
                    var row = new ExcelProductRow
                    {
                        B = sheet.Cells[i,2].Text,
                        E = sheet.Cells[i, 5].Text,
                        G = sheet.Cells[i, 7].Text,
                        H = sheet.Cells[i, 8].Text,
                        I = sheet.Cells[i, 9].Text,
                        M = sheet.Cells[i, 13].Text,
                        P = sheet.Cells[i, 16].Text,
                        Q = sheet.Cells[i, 17].Text,
                        T = sheet.Cells[i, 20].Text,
                        U = sheet.Cells[i, 21].Text,
                        V = sheet.Cells[i, 22].Text,
                        W = sheet.Cells[i, 23].Text,
                        X = sheet.Cells[i, 24].Text,
                        Y = sheet.Cells[i, 25].Text,
                        AA = sheet.Cells[i, 27].Text
                    };

                    data.Add(row);
                }
            }
        }

        return data;
    }

}