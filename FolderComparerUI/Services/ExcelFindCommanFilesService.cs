using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FolderComparerUI.Services
{
    /// <inheritdoc cref="IExcelFindCommonFilesService"/>
    public class ExcelFindCommanFilesService : IExcelFindCommonFilesService
    {
        /// <inheritdoc/>
        public int FindCommonFiles(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Excel file path must not be empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Excel file not found:\n{filePath}", filePath);

            // Detect whether another process has the file open before trying to write it
            if (IsFileLocked(filePath))
                throw new IOException(
                    $"The file is currently open in another application (e.g. Excel).\n" +
                    $"Close the file and try again.\n\n{filePath}");

            using var doc = SpreadsheetDocument.Open(filePath, isEditable: true);

            var workbookPart = doc.WorkbookPart
                ?? throw new InvalidOperationException("The workbook part is missing — the file may be corrupt.");

            var firstSheet = workbookPart.Workbook.Sheets?
                .Elements<Sheet>().FirstOrDefault()
                ?? throw new InvalidOperationException("The workbook contains no worksheets.");

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(firstSheet.Id!)
                ?? throw new InvalidOperationException("Could not load the first worksheet.");

            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()
                ?? throw new InvalidOperationException("The worksheet contains no data.");

            var rows = sheetData.Elements<Row>().ToList();

            // Skip header row (RowIndex == 1 or simply the first row)
            var dataRows = rows.Where(r => r.RowIndex?.Value > 1).ToList();

            // Build column-A set from data rows (header excluded)
            var columnASet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in dataRows)
            {
                string val = GetCellValue(doc, GetCell(row, "A"));
                if (!string.IsNullOrWhiteSpace(val))
                    columnASet.Add(val.Trim());
            }

            // Find values in column B that also appear in column A (data rows only)
            var commonFiles = new List<string>();
            foreach (var row in dataRows)
            {
                string val = GetCellValue(doc, GetCell(row, "B"));
                if (!string.IsNullOrWhiteSpace(val) && columnASet.Contains(val.Trim()))
                    commonFiles.Add(val.Trim());
            }

            commonFiles = commonFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // Write common files to column D on data rows
            int writeIdx = 0;
            foreach (var row in dataRows)
            {
                var dCell = GetCell(row, "D");

                if (writeIdx < commonFiles.Count)
                {
                    // Write this common-file entry
                    if (dCell != null)
                    {
                        dCell.CellValue = new CellValue(commonFiles[writeIdx]);
                        dCell.DataType  = CellValues.String;
                    }
                    else
                    {
                        var newCell = new Cell
                        {
                            CellReference = $"D{row.RowIndex}",
                            DataType      = CellValues.String,
                            CellValue     = new CellValue(commonFiles[writeIdx])
                        };
                        InsertCellInOrder(row, newCell);
                    }
                    writeIdx++;
                }
                else
                {
                    // Past the end of commonFiles — clear any stale D cell from a previous run
                    if (dCell != null)
                    {
                        dCell.Remove();
                    }
                }
            }

            worksheetPart.Worksheet.Save();
            return commonFiles.Count;
        }

        // ── helpers ────────────────────────────────────────────────────────────

        private static bool IsFileLocked(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open,
                    FileAccess.ReadWrite, FileShare.None);
                return false;
            }
            catch (IOException)  { return true; }
            catch               { return false; }
        }

        private static Cell? GetCell(Row row, string columnLetter) =>
            row.Elements<Cell>()
               .FirstOrDefault(c => c.CellReference?.Value
                   ?.StartsWith(columnLetter, StringComparison.OrdinalIgnoreCase) == true);

        private static string GetCellValue(SpreadsheetDocument doc, Cell? cell)
        {
            if (cell == null) return "";

            string raw = cell.InnerText ?? "";

            if (cell.DataType?.Value == CellValues.SharedString)
            {
                var sst = doc.WorkbookPart?.SharedStringTablePart?.SharedStringTable;
                if (sst == null) return raw; // guard: no shared-string table

                if (int.TryParse(raw, out int idx))
                    return sst.ElementAt(idx)?.InnerText ?? raw;
            }

            return raw;
        }

        /// <summary>
        /// Inserts a cell into the row in column-reference order so Open XML
        /// structural rules are satisfied (cells must be in order within a row).
        /// </summary>
        private static void InsertCellInOrder(Row row, Cell newCell)
        {
            Cell? refCell = row.Elements<Cell>()
                .FirstOrDefault(c => string.Compare(
                    c.CellReference?.Value, newCell.CellReference?.Value,
                    StringComparison.OrdinalIgnoreCase) > 0);

            if (refCell != null)
                row.InsertBefore(newCell, refCell);
            else
                row.Append(newCell);
        }
    }
}
