using System.Threading;
using System.Threading.Tasks;

namespace RospSqlGenerator.Abstractions;

using RospSqlGenerator.Models;

/// <summary>
/// Читает xlsx-файл справочника РОСП/ОСП и возвращает нормализованные строки.
/// </summary>
public interface IRospWorkbookReader
{
    Task<RospImportResult> ReadAsync(string filePath, CancellationToken cancellationToken = default);
}