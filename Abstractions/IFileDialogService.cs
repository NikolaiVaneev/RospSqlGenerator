using System.Threading;
using System.Threading.Tasks;

namespace RospSqlGenerator.Abstractions;

/// <summary>
/// Сервис выбора файлов через пользовательский интерфейс.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Выбирает xlsx-файл справочника.
    /// </summary>
    Task<string?> PickXlsxFileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Выбирает путь для сохранения sql-файла.
    /// </summary>
    Task<string?> PickSqlSaveFileAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default);
}