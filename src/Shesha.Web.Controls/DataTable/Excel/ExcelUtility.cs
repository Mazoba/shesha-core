using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Shesha.Extensions;
using Shesha.Utilities;

namespace Shesha.Web.DataTable.Excel
{
    public delegate Task WriteRowsDelegate(OpenXmlWriter xmlWriter);

    public static class ExcelUtility
    {
        #region Constants
        public const string DefaultSheetName = "Sheet1";
        #endregion

        #region Private methods

        /// <summary>
        /// Set the name of the spreadsheet. 
        /// </summary>
        private static void SetSheetName(string excelSpreadSheetName, SpreadsheetDocument spreadSheet)
        {
            excelSpreadSheetName ??= DefaultSheetName;
            var ss = spreadSheet.WorkbookPart.Workbook.Descendants<Sheet>().FirstOrDefault(s => s.Name == DefaultSheetName);
            if (ss != null)
                ss.Name = excelSpreadSheetName;
        }

        /// <summary>
        /// Set the style sheet
        /// </summary>
        private static void SetStyleSheet(SpreadsheetDocument spreadSheet)
        {
            var styleSheet = spreadSheet.WorkbookPart.WorkbookStylesPart.Stylesheet;
            styleSheet.Fonts.Append(new Font(
                new FontSize { Val = 11 }, 
                new Color { Rgb = "FFFFFF" }, 
                new Bold(),
                new FontName { Val = "Calibri" }));

            styleSheet.Fills.AppendChild(new Fill
            {
                PatternFill = new PatternFill
                {
                    PatternType = PatternValues.Solid,
                    ForegroundColor = new ForegroundColor() { Rgb = "4f6228" },
                    BackgroundColor = new BackgroundColor() { Indexed = 64 },
                }
            });

            // cell format list
            styleSheet.CellFormats = new CellFormats();
            // empty one for index 0, seems to be required
            styleSheet.CellFormats.AppendChild(new CellFormat());
            // cell format with vertical alignment = top and WrapText = true
            styleSheet.CellFormats.AppendChild(new CellFormat { FormatId = 1, FontId = 0, BorderId = 0, FillId = 0, ApplyFill = false, ApplyBorder = false, ApplyFont = false }).AppendChild(new Alignment { Vertical = VerticalAlignmentValues.Top, WrapText = true });
            styleSheet.CellFormats.Count = 2;

            spreadSheet.WorkbookPart.WorkbookStylesPart.Stylesheet.Save();
        }

        /// <summary>
        /// Add cell width styles. 
        /// </summary>
        /// <param name="minCol">Minimum column index.</param>
        /// <param name="maxCol">Maximum column index.</param>
        /// <param name="maxWidth">Maximum column width.</param>
        /// <param name="spreadSheet">Spread sheet.</param>
        /// <param name="workSheetPart">Work sheet.</param>
        private static void AddCellWidthStyles(uint minCol, uint maxCol, int maxWidth, SpreadsheetDocument spreadSheet, WorksheetPart workSheetPart)
        {
            var cols = new DocumentFormat.OpenXml.Spreadsheet.Columns(new Column { CustomWidth = true, Min = minCol, Max = maxCol, Width = maxWidth, BestFit = false });
            workSheetPart.Worksheet.InsertBefore(cols, workSheetPart.Worksheet.GetFirstChild<SheetData>());
        }

        private static readonly Dictionary<int, string> LettersCache = new Dictionary<int, string>();

        private static string ReplaceSpecialCharacters(string value)
        {
            return value.Replace("’", "'")
                .Replace("“", "\"")
                .Replace("”", "\"")
                .Replace("–", "-")
                .Replace("…", "...");
        }

        private static void SetHeaderStyle(SpreadsheetDocument spreadSheet, Cell cell)
        {
            Stylesheet styleSheet = spreadSheet.WorkbookPart.WorkbookStylesPart.Stylesheet;
            cell.SetAttribute(new OpenXmlAttribute("", "s", "", "1"));
            OpenXmlAttribute cellStyleAttribute = cell.GetAttribute("s", "");
            CellFormats cellFormats = spreadSheet.WorkbookPart.WorkbookStylesPart.Stylesheet.CellFormats;

            // pick the first cell format.
            CellFormat cellFormat = (CellFormat)cellFormats.ElementAt(0);

            CellFormat cf = new CellFormat(cellFormat.OuterXml);
            cf.FontId = styleSheet.Fonts.Count;
            cf.FillId = styleSheet.Fills.Count;

            cellFormats.AppendChild(cf);

            cell.SetAttribute(cellStyleAttribute);

            cell.StyleIndex = styleSheet.CellFormats.Count;
        }

        #endregion

        private static async Task<MemoryStream> DataToExcelStreamAsync(WriteRowsDelegate writeRows, IList<String> headers, string sheetName, List<int> columnWidths = null)
        {
            var xmlStream = ReportingHelper.GetResourceStream("Shesha.Web.DataTable.Excel.template.xlsx", typeof(ExcelUtility).Assembly);

            using (var document = SpreadsheetDocument.Open(xmlStream, true))
            {
                var workbookPart = document.WorkbookPart;
                var worksheetPart = workbookPart.WorksheetParts.First();
                var originalSheetId = workbookPart.GetIdOfPart(worksheetPart);

                var replacementPart = workbookPart.AddNewPart<WorksheetPart>();
                var replacementPartId = workbookPart.GetIdOfPart(replacementPart);

                // Configure the spreadsheet
                SetSheetName(sheetName, document);
                SetStyleSheet(document);

                // Fit to page
                var sp = new SheetProperties(new PageSetupProperties());

                var ws = worksheetPart.Worksheet;
                ws.SheetProperties = sp;

                // Set the FitToPage property to true
                ws.SheetProperties.PageSetupProperties.FitToPage = BooleanValue.FromBoolean(true);

                var pgOr = new PageSetup
                {
                    // Page size A4 landscape
                    PaperSize = 9,
                    Orientation = OrientationValues.Landscape,
                    // Scale to fit to page width
                    FitToWidth = 1,
                    FitToHeight = 0
                };
                ws.AppendChild(pgOr);

                var maxWidth = 0;

                if (columnWidths != null)
                {
                    var idx = 1;
                    var columns =
                        columnWidths
                        .Select(
                            w => new Column
                            {
                                CustomWidth = true,
                                Min = Convert.ToUInt32(idx),
                                Max = Convert.ToUInt32(idx++),
                                Width = w,
                                BestFit = false
                            })
                        .ToList();
                    var cols = new DocumentFormat.OpenXml.Spreadsheet.Columns(columns);
                    worksheetPart.Worksheet.InsertBefore(cols, worksheetPart.Worksheet.GetFirstChild<SheetData>());
                }
                else
                {
                    maxWidth = headers.Select(h => h.Length).Max();
                    AddCellWidthStyles(Convert.ToUInt32(1), Convert.ToUInt32(headers.Count), maxWidth, document, worksheetPart);
                }

                worksheetPart.Worksheet.Save();
                document.WorkbookPart.Workbook.Save();

                using (var xmlReader = OpenXmlReader.Create(worksheetPart))
                {
                    using (var xmlWriter = OpenXmlWriter.Create(replacementPart))
                    {
                        while (xmlReader.Read())
                        {
                            if (xmlReader.ElementType == typeof(SheetData))
                            {
                                if (xmlReader.IsEndElement)
                                    continue;
                                xmlWriter.WriteStartElement(new SheetData());

                                var headerCell = new Cell(new CellValue());
                                headerCell.DataType = new EnumValue<CellValues>(CellValues.String);

                                // write headers
                                xmlWriter.WriteStartElement(new Row());
                                SetHeaderStyle(document, headerCell);
                                foreach (var header in headers)
                                {
                                    headerCell.CellValue.Text = header;
                                    xmlWriter.WriteElement(headerCell);
                                }
                                xmlWriter.WriteEndElement();

                                await writeRows.Invoke(xmlWriter);

                                xmlWriter.WriteEndElement();
                            }
                            else
                            {
                                if (xmlReader.IsStartElement)
                                {
                                    xmlWriter.WriteStartElement(xmlReader);
                                }
                                else if (xmlReader.IsEndElement)
                                {
                                    xmlWriter.WriteEndElement();
                                }
                            }
                        }
                    }
                }

                var sheet = workbookPart.Workbook.Descendants<Sheet>().First(s => s.Id.Value.Equals(originalSheetId));

                sheet.Id.Value = replacementPartId;
                workbookPart.DeletePart(worksheetPart);
            }

            return xmlStream;
        }

        /// <summary>
        /// Iterate through the list of entities and output the results in an Excel file as a stream
        /// </summary>
        public static async Task<MemoryStream> ReadToExcelStreamAsync<TRow, TId>(this IList<TRow> list, IList<DataTableColumn> columns, string sheetName = DefaultSheetName, bool includeId = false) where TRow: class, IEntity<TId>
        {
            var headers = columns
                .Where(t => t.IsExportable /*&& (t.AuthorizationRules == null || t.AuthorizationRules.IsAuthorized())*/)
                .Select(t => ReplaceSpecialCharacters(t.Caption))
                .ToList();
            if (includeId)
                headers.Insert(0, "ID");

            var result = await DataToExcelStreamAsync(async writer =>
            {
                var r = new Row();
                var c = new Cell(new CellValue());

                foreach (var row in list)
                {
                    writer.WriteStartElement(r);

                    if (includeId && row.IsEntity())
                    {
                        c.FillCellValue(row.GetId().ToString(), typeof(string));
                        writer.WriteElement(c);
                    }

                    foreach (var column in columns)
                    {
                        if (column.IsExportable
                            /*&& (columns[colNum].AuthorizationRules == null || columns[colNum].AuthorizationRules.IsAuthorized())*/)
                        {
                            var strValue = (await column.CellContentAsync<TRow, TId>(row, true))?.ToString() ?? "";

                            if (column.StripHtml && !string.IsNullOrWhiteSpace(strValue))
                                strValue = strValue.StripHtml();

                            strValue = ReplaceSpecialCharacters(strValue);

                            c.FillCellValue(strValue, typeof(string));
                            writer.WriteElement(c);
                        }
                    }

                    writer.WriteEndElement();
                }
            }, headers, sheetName);

            return result;
        }

        private static void FillCellValue(this Cell cell, string value, Type type)
        {
            cell.CellValue.Text = ReplaceSpecialCharacters(ReplaceSpecialCharacters(value ?? ""));

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    cell.DataType = new EnumValue<CellValues>(CellValues.Boolean);
                    break;
                case TypeCode.DateTime:
                    cell.DataType = new EnumValue<CellValues>(CellValues.Date);
                    break;
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                    break;
                default:
                    cell.DataType = new EnumValue<CellValues>(CellValues.String);
                    break;
            }
        }
    }
}
