using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using RospSqlGenerator.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RospSqlGenerator.Services;

/// <summary>
/// Сервис выбора файлов через стандартные диалоги Avalonia.
/// </summary>
public sealed class AvaloniaFileDialogService : IFileDialogService
{
    private static readonly FilePickerFileType XlsxFileType = new("Excel (*.xlsx)")
    {
        Patterns = ["*.xlsx"],
        MimeTypes = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
    };

    private static readonly FilePickerFileType SqlFileType = new("SQL (*.sql)")
    {
        Patterns = ["*.sql"],
        MimeTypes = ["application/sql", "text/plain"]
    };

    private readonly IClassicDesktopStyleApplicationLifetime _desktop;

    public AvaloniaFileDialogService(IClassicDesktopStyleApplicationLifetime desktop)
    {
        _desktop = desktop;
    }

    /// <inheritdoc />
    public async Task<string?> PickXlsxFileAsync(CancellationToken cancellationToken = default)
    {
        var topLevel = ResolveTopLevel();

        if (topLevel is null)
        {
            return null;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите xlsx-файл справочника РОСП",
            AllowMultiple = false,
            FileTypeFilter = [XlsxFileType]
        });

        cancellationToken.ThrowIfCancellationRequested();

        return files.FirstOrDefault()?.Path.LocalPath;
    }

    /// <inheritdoc />
    public async Task<string?> PickSqlSaveFileAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        var topLevel = ResolveTopLevel();

        if (topLevel is null)
        {
            return null;
        }

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить SQL-скрипт",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "sql",
            FileTypeChoices = [SqlFileType]
        });

        cancellationToken.ThrowIfCancellationRequested();

        return file?.Path.LocalPath;
    }

    private TopLevel? ResolveTopLevel()
    {
        return _desktop.MainWindow;
    }
}