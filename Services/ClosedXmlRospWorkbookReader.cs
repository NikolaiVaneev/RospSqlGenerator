using ClosedXML.Excel;
using RospSqlGenerator.Abstractions;
using RospSqlGenerator.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RospSqlGenerator.Services;

/// <summary>
/// Читает xlsx-файл справочника РОСП/ОСП через ClosedXML.
/// </summary>
public sealed class ClosedXmlRospWorkbookReader : IRospWorkbookReader
{
    private const string PreferredWorksheetName = "Лист2";

    private static readonly IReadOnlyDictionary<string, string[]> ColumnAliases = new Dictionary<string, string[]>
    {
        [ColumnKeys.RegionCode] =
        [
            "region code",
            "region"
        ],
        [ColumnKeys.AgencyCode] =
        [
            "code of the territorial agency",
            "agency code",
            "code"
        ],
        [ColumnKeys.Name] =
        [
            "name of the territorial agency",
            "name"
        ],
        [ColumnKeys.Address] =
        [
            "postal address",
            "address"
        ],
        [ColumnKeys.ChiefFullName] =
        [
            "chief's full name",
            "chief full name",
            "chiefs full name"
        ],
        [ColumnKeys.TelephoneNumber] =
        [
            "telephone number",
            "phone",
            "telephone"
        ],
        [ColumnKeys.PhoneOfHelpService] =
        [
            "phone of help service"
        ],
        [ColumnKeys.PhoneOfHelpService2] =
        [
            "phone of help service 2",
            "phone of help service2"
        ],
        [ColumnKeys.WorkHours] =
        [
            "working hours of the territorial agency",
            "working hours",
            "work hours"
        ]
    };

    /// <inheritdoc />
    public Task<RospImportResult> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Путь к xlsx-файлу не заполнен.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Xlsx-файл не найден.", filePath);
        }

        return Task.Run(() => ReadInternal(filePath, cancellationToken), cancellationToken);
    }

    private static RospImportResult ReadInternal(string filePath, CancellationToken cancellationToken)
    {
        var rows = new List<RospImportRow>();
        var errors = new List<string>();
        var warnings = new List<string>();

        using var workbook = new XLWorkbook(filePath);

        var worksheet = workbook.Worksheets.FirstOrDefault(x =>
                            string.Equals(x.Name, PreferredWorksheetName, StringComparison.OrdinalIgnoreCase))
                        ?? workbook.Worksheets.FirstOrDefault();

        if (worksheet is null)
        {
            errors.Add("В xlsx-файле не найдено ни одного листа.");
            return new RospImportResult(rows, errors, warnings);
        }

        var usedRange = worksheet.RangeUsed();

        if (usedRange is null)
        {
            errors.Add($"Лист \"{worksheet.Name}\" не содержит данных.");
            return new RospImportResult(rows, errors, warnings);
        }

        var headerRow = usedRange.FirstRowUsed();
        var headerMap = BuildHeaderMap(headerRow);

        var requiredColumns = new[]
        {
            ColumnKeys.RegionCode,
            ColumnKeys.AgencyCode,
            ColumnKeys.Name
        };

        foreach (var requiredColumn in requiredColumns)
        {
            if (!TryFindColumn(headerMap, requiredColumn, out _))
            {
                errors.Add($"В xlsx-файле не найдена обязательная колонка \"{requiredColumn}\".");
            }
        }

        if (errors.Count > 0)
        {
            return new RospImportResult(rows, errors, warnings);
        }

        AddOptionalColumnWarning(headerMap, ColumnKeys.Address, warnings);
        AddOptionalColumnWarning(headerMap, ColumnKeys.ChiefFullName, warnings);
        AddOptionalColumnWarning(headerMap, ColumnKeys.TelephoneNumber, warnings);
        AddOptionalColumnWarning(headerMap, ColumnKeys.PhoneOfHelpService, warnings);
        AddOptionalColumnWarning(headerMap, ColumnKeys.PhoneOfHelpService2, warnings);
        AddOptionalColumnWarning(headerMap, ColumnKeys.WorkHours, warnings);

        var firstDataRowNumber = headerRow.RowNumber() + 1;
        var lastDataRowNumber = usedRange.LastRowUsed().RowNumber();

        for (var rowNumber = firstDataRowNumber; rowNumber <= lastDataRowNumber; rowNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = worksheet.Row(rowNumber);

            if (IsEmptyDataRow(row, headerMap))
            {
                continue;
            }

            try
            {
                var regionCodeRaw = GetRequiredCellText(row, headerMap, ColumnKeys.RegionCode);
                var agencyCodeRaw = GetRequiredCellText(row, headerMap, ColumnKeys.AgencyCode);
                var name = GetRequiredCellText(row, headerMap, ColumnKeys.Name);

                if (!TryParseShort(regionCodeRaw, out var regionCode))
                {
                    errors.Add($"Строка {rowNumber}: код региона \"{regionCodeRaw}\" не является числом.");
                    continue;
                }

                var code = RospCodeNormalizer.Normalize(regionCode, agencyCodeRaw);

                rows.Add(new RospImportRow
                {
                    SourceRowNumber = rowNumber,
                    Region = regionCode,
                    Code = code,
                    Name = name,
                    Address = GetOptionalCellText(row, headerMap, ColumnKeys.Address),
                    FsspChiefsFullName = GetOptionalCellText(row, headerMap, ColumnKeys.ChiefFullName),
                    FsspTelephoneNumber = GetOptionalCellText(row, headerMap, ColumnKeys.TelephoneNumber),
                    FsspPhoneOfHelpService = GetOptionalCellText(row, headerMap, ColumnKeys.PhoneOfHelpService),
                    FsspPhoneOfHelpService2 = GetOptionalCellText(row, headerMap, ColumnKeys.PhoneOfHelpService2),
                    FsspWorkHours = GetOptionalCellText(row, headerMap, ColumnKeys.WorkHours)
                });
            }
            catch (Exception exception)
            {
                errors.Add($"Строка {rowNumber}: {exception.Message}");
            }
        }

        return new RospImportResult(rows, errors, warnings);
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLRangeRow headerRow)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.CellsUsed())
        {
            var header = NormalizeHeader(ReadCellText(cell));

            if (string.IsNullOrWhiteSpace(header))
            {
                continue;
            }

            result.TryAdd(header, cell.Address.ColumnNumber);
        }

        return result;
    }

    private static bool TryFindColumn(
        IReadOnlyDictionary<string, int> headerMap,
        string columnKey,
        out int columnNumber)
    {
        columnNumber = 0;

        if (!ColumnAliases.TryGetValue(columnKey, out var aliases))
        {
            return false;
        }

        foreach (var alias in aliases)
        {
            var normalizedAlias = NormalizeHeader(alias);

            if (headerMap.TryGetValue(normalizedAlias, out columnNumber))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetRequiredCellText(
        IXLRow row,
        IReadOnlyDictionary<string, int> headerMap,
        string columnKey)
    {
        if (!TryFindColumn(headerMap, columnKey, out var columnNumber))
        {
            throw new InvalidOperationException($"Не найдена обязательная колонка \"{columnKey}\".");
        }

        var value = NormalizeRequiredText(ReadCellText(row.Cell(columnNumber)));

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Не заполнено обязательное поле \"{columnKey}\".");
        }

        return value;
    }

    private static string? GetOptionalCellText(
        IXLRow row,
        IReadOnlyDictionary<string, int> headerMap,
        string columnKey)
    {
        if (!TryFindColumn(headerMap, columnKey, out var columnNumber))
        {
            return null;
        }

        return NormalizeOptionalText(ReadCellText(row.Cell(columnNumber)));
    }

    private static bool IsEmptyDataRow(IXLRow row, IReadOnlyDictionary<string, int> headerMap)
    {
        foreach (var columnKey in ColumnAliases.Keys)
        {
            if (!TryFindColumn(headerMap, columnKey, out var columnNumber))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(ReadCellText(row.Cell(columnNumber))))
            {
                return false;
            }
        }

        return true;
    }

    private static void AddOptionalColumnWarning(
        IReadOnlyDictionary<string, int> headerMap,
        string columnKey,
        ICollection<string> warnings)
    {
        if (!TryFindColumn(headerMap, columnKey, out _))
        {
            warnings.Add($"В xlsx-файле не найдена необязательная колонка \"{columnKey}\".");
        }
    }

    private static string ReadCellText(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        if (cell.DataType == XLDataType.Number)
        {
            var number = cell.GetDouble();

            if (Math.Abs(number % 1) < 0.000001)
            {
                return Convert.ToInt64(number).ToString(CultureInfo.InvariantCulture);
            }

            return number.ToString(CultureInfo.InvariantCulture);
        }

        return cell.GetFormattedString();
    }

    private static string NormalizeRequiredText(string value)
    {
        return NormalizeText(value) ?? string.Empty;
    }

    private static string? NormalizeOptionalText(string value)
    {
        var normalized = NormalizeText(value);

        if (string.Equals(normalized, "16", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(normalized, "null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return normalized;
    }

    private static string? NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value
            .Replace('\u00A0', ' ')
            .Replace('\u200B', ' ')
            .Trim();

        normalized = Regex.Replace(normalized, @"\s+", " ");

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }

    private static string NormalizeHeader(string value)
    {
        return NormalizeText(value)?
            .Trim()
            .ToLowerInvariant()
            .Replace("’", "'")
            .Replace("`", "'")
            ?? string.Empty;
    }

    private static bool TryParseShort(string value, out short result)
    {
        result = 0;

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        if (parsed < short.MinValue || parsed > short.MaxValue)
        {
            return false;
        }

        result = (short)parsed;
        return true;
    }

    private static class ColumnKeys
    {
        public const string RegionCode = "region code";
        public const string AgencyCode = "code of the territorial agency";
        public const string Name = "name of the territorial agency";
        public const string Address = "postal address";
        public const string ChiefFullName = "chief's full name";
        public const string TelephoneNumber = "telephone number";
        public const string PhoneOfHelpService = "phone of help service";
        public const string PhoneOfHelpService2 = "phone of help service 2";
        public const string WorkHours = "working hours of the territorial agency";
    }
}