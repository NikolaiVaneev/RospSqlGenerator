using System.Collections.Generic;

namespace RospSqlGenerator.Models;

/// <summary>
/// Результат чтения и нормализации xlsx-файла.
/// </summary>
public sealed class RospImportResult
{
    public RospImportResult(
        IReadOnlyCollection<RospImportRow> rows,
        IReadOnlyCollection<string> errors,
        IReadOnlyCollection<string> warnings)
    {
        Rows = rows;
        Errors = errors;
        Warnings = warnings;
    }

    public IReadOnlyCollection<RospImportRow> Rows { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public IReadOnlyCollection<string> Warnings { get; }

    public bool HasErrors => Errors.Count > 0;
}