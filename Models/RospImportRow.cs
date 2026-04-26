namespace RospSqlGenerator.Models;

/// <summary>
/// Нормализованная строка справочника РОСП/ОСП, подготовленная для предпросмотра и генерации SQL.
/// </summary>
public sealed record RospImportRow
{
    /// <summary>
    /// Номер строки в исходном xlsx-файле.
    /// </summary>
    public required int SourceRowNumber { get; init; }

    /// <summary>
    /// Код региона из исходного справочника.
    /// </summary>
    public required short Region { get; init; }

    /// <summary>
    /// Нормализованный код РОСП/ОСП.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Название РОСП/ОСП.
    /// </summary>
    public required string Name { get; init; }

    public string? Address { get; init; }

    public string? FsspChiefsFullName { get; init; }

    public string? FsspTelephoneNumber { get; init; }

    public string? FsspPhoneOfHelpService { get; init; }

    public string? FsspPhoneOfHelpService2 { get; init; }

    public string? FsspWorkHours { get; init; }
}