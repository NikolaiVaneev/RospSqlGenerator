using RospSqlGenerator.Models;
using RospSqlGenerator.Services;

namespace RospSqlGenerator.Tests;

public sealed class PostgreSqlRospSqlGeneratorTests
{
    private readonly PostgreSqlRospSqlGenerator _generator = new();

    [Fact]
    public void Generate_BuildsUpsertScriptForSelectedRows()
    {
        var rows = new[]
        {
            new RospImportRow
            {
                SourceRowNumber = 2,
                Region = 77,
                Code = "77001",
                Name = "ОСП О'Брайена",
                Address = null,
                FsspChiefsFullName = "Иванов И.И.",
                FsspTelephoneNumber = "123",
                FsspPhoneOfHelpService = null,
                FsspPhoneOfHelpService2 = null,
                FsspWorkHours = "9:00-18:00"
            },
            new RospImportRow
            {
                SourceRowNumber = 1,
                Region = 1,
                Code = "01007",
                Name = "Первый ОСП",
                Address = "Адрес",
                FsspChiefsFullName = null,
                FsspTelephoneNumber = null,
                FsspPhoneOfHelpService = "456",
                FsspPhoneOfHelpService2 = null,
                FsspWorkHours = null
            }
        };

        var sql = _generator.Generate(rows);

        Assert.StartsWith("BEGIN;", sql);
        Assert.Contains("CREATE TEMP TABLE \"TempRosps\"", sql);
        Assert.Contains("ON CONFLICT (\"Code\")", sql);
        Assert.Contains("'ОСП О''Брайена'", sql);
        Assert.Contains("'77001', 'ОСП О''Брайена', null", sql);
        Assert.True(sql.IndexOf("'01007'", StringComparison.Ordinal) < sql.IndexOf("'77001'", StringComparison.Ordinal));
        Assert.EndsWith("COMMIT;\r\n", sql);
    }

    [Fact]
    public void Generate_ThrowsWhenRowsAreEmpty()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => _generator.Generate(Array.Empty<RospImportRow>()));

        Assert.Equal("Нет строк для генерации SQL.", exception.Message);
    }

    [Fact]
    public void Generate_ThrowsWhenSelectedRowsContainDuplicateCodes()
    {
        var rows = new[]
        {
            CreateRow(sourceRowNumber: 1, code: "77001"),
            CreateRow(sourceRowNumber: 2, code: "77001")
        };

        var exception = Assert.Throws<InvalidOperationException>(() => _generator.Generate(rows));

        Assert.Contains("В выбранных строках есть дубли кодов: 77001.", exception.Message);
    }

    private static RospImportRow CreateRow(int sourceRowNumber, string code)
    {
        return new RospImportRow
        {
            SourceRowNumber = sourceRowNumber,
            Region = 77,
            Code = code,
            Name = $"ОСП {sourceRowNumber}"
        };
    }
}
