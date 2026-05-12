using ClosedXML.Excel;
using ManpowerMonitoringTool.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ManpowerMonitoringTool.Services;

public sealed class ExcelManpowerReader
{
    public IReadOnlyList<ManpowerEntry> Read(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();
        var headerRow = worksheet.FirstRowUsed() ?? throw new InvalidOperationException("The Excel file has no rows.");
        var headers = headerRow.CellsUsed()
            .ToDictionary(cell => NormalizeHeader(cell.GetString()), cell => cell.Address.ColumnNumber);

        var rows = new List<ManpowerEntry>();
        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            var unitName = GetString(row, headers, "unitname");
            var function = GetString(row, headers, "function");
            if (string.IsNullOrWhiteSpace(unitName) || string.IsNullOrWhiteSpace(function))
            {
                continue;
            }

            rows.Add(new ManpowerEntry
            {
                UnitCode = GetString(row, headers, "unitcode"),
                UnitName = unitName,
                CurrentYear = (int)GetDecimal(row, headers, "currentyear"),
                CurrentMonth = (int)GetDecimal(row, headers, "currentmonth"),
                Function = function,
                ActualMpCost = GetDecimal(row, headers, "actualmpcost"),
                ActualMpCostLeasing = GetDecimal(row, headers, "actualmpcostleasing"),
                TotalMpCost = GetDecimal(row, headers, "totalmpcost"),
                Section = GetString(row, headers, "section")
            });
        }

        return rows;
    }

    private static string GetString(IXLRow row, IReadOnlyDictionary<string, int> headers, string name)
    {
        return headers.TryGetValue(name, out var column)
            ? row.Cell(column).GetFormattedString().Trim()
            : string.Empty;
    }

    private static decimal GetDecimal(IXLRow row, IReadOnlyDictionary<string, int> headers, string name)
    {
        if (!headers.TryGetValue(name, out var column))
        {
            return 0;
        }

        var cell = row.Cell(column);
        if (cell.TryGetValue<decimal>(out var numericValue))
        {
            return numericValue;
        }

        var text = cell.GetFormattedString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        var cleaned = Regex.Replace(text, "[^0-9.,\\-]", string.Empty).Replace(",", string.Empty);
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private static string NormalizeHeader(string value)
    {
        return Regex.Replace(value, "[^a-zA-Z0-9]", string.Empty).ToLowerInvariant();
    }
}
