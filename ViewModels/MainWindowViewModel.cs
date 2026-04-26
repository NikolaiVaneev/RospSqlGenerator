using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RospSqlGenerator.Abstractions;
using RospSqlGenerator.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RospSqlGenerator.ViewModels;

/// <summary>
/// Основная модель представления приложения генерации SQL-скрипта обновления справочника РОСП/ОСП.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IRospWorkbookReader _workbookReader;
    private readonly IRospSqlGenerator _sqlGenerator;
    private readonly IFileDialogService _fileDialogService;
    private readonly ObservableCollection<RospPreviewRow> _filteredRows = new();

    private bool _isUpdatingSelectAllState;

    public MainWindowViewModel(
        IRospWorkbookReader workbookReader,
        IRospSqlGenerator sqlGenerator,
        IFileDialogService fileDialogService)
    {
        _workbookReader = workbookReader;
        _sqlGenerator = sqlGenerator;
        _fileDialogService = fileDialogService;

        FilteredRows = new ReadOnlyObservableCollection<RospPreviewRow>(_filteredRows);
    }

    /// <summary>
    /// Строки предпросмотра, загруженные из xlsx-файла.
    /// </summary>
    public ObservableCollection<RospPreviewRow> Rows { get; } = new();

    public ReadOnlyObservableCollection<RospPreviewRow> FilteredRows { get; }

    /// <summary>
    /// Сообщения импорта: ошибки и предупреждения.
    /// </summary>
    public ObservableCollection<string> Messages { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool isBusy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool canSave;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool hasImportErrors;

    [ObservableProperty]
    private bool? selectAllState = false;

    [ObservableProperty]
    private bool showOnlyConflicts;

    [ObservableProperty]
    private int totalCount;

    [ObservableProperty]
    private int selectedCount;

    [ObservableProperty]
    private int duplicateGroupCount;

    [ObservableProperty]
    private int conflictCount;

    [ObservableProperty]
    private string status = "Файл не загружен.";

    [ObservableProperty]
    private string? loadedFilePath;

    public bool IsNotBusy => !IsBusy;

    private bool CanLoad() => !IsBusy;

    private bool CanSaveSql() => CanSave && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanLoad))]
    private async Task LoadAsync()
    {
        var filePath = await _fileDialogService.PickXlsxFileAsync();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Status = "Загрузка отменена.";
            return;
        }

        try
        {
            IsBusy = true;
            Status = "Чтение xlsx-файла...";

            ClearCurrentData();

            var importResult = await _workbookReader.ReadAsync(filePath);

            LoadedFilePath = filePath;
            HasImportErrors = importResult.HasErrors;

            foreach (var error in importResult.Errors)
            {
                Messages.Add($"Ошибка: {error}");
            }

            foreach (var warning in importResult.Warnings)
            {
                Messages.Add($"Предупреждение: {warning}");
            }

            foreach (var importRow in importResult.Rows)
            {
                var previewRow = new RospPreviewRow(importRow);
                previewRow.PropertyChanged += OnPreviewRowPropertyChanged;
                Rows.Add(previewRow);
            }

            RecalculateState();

            Status = importResult.HasErrors
                ? $"Файл загружен с ошибками. Строк: {Rows.Count}."
                : $"Файл загружен. Строк: {Rows.Count}.";
        }
        catch (Exception exception)
        {
            ClearCurrentData();

            HasImportErrors = true;
            Messages.Add($"Ошибка: {exception.Message}");
            Status = "Не удалось загрузить файл.";

            RecalculateState();
        }
        finally
        {
            IsBusy = false;
            RecalculateState();
        }
    }

    /// <summary>
    /// Формирует и сохраняет SQL-скрипт по выбранным строкам.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSaveSql))]
    private async Task SaveAsync()
    {
        if (!CanSave)
        {
            return;
        }

        var filePath = await _fileDialogService.PickSqlSaveFileAsync(BuildSuggestedSqlFileName());

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Status = "Сохранение отменено.";
            return;
        }

        try
        {
            IsBusy = true;
            Status = "Формирование SQL-скрипта...";

            var selectedRows = Rows
                .Where(row => row.IsSelected)
                .Select(row => row.ToImportRow())
                .ToArray();

            var sql = _sqlGenerator.Generate(selectedRows);

            await File.WriteAllTextAsync(
                filePath,
                sql,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            Status = $"SQL-скрипт сохранен: {filePath}";
        }
        catch (Exception exception)
        {
            Messages.Add($"Ошибка сохранения: {exception.Message}");
            Status = "Не удалось сохранить SQL-скрипт.";
        }
        finally
        {
            IsBusy = false;
            RecalculateState();
        }
    }

    partial void OnSelectAllStateChanged(bool? value)
    {
        if (_isUpdatingSelectAllState || !value.HasValue)
        {
            return;
        }

        SetAllSelected(value.Value);
    }

    partial void OnShowOnlyConflictsChanged(bool value)
    {
        RefreshFilteredRows();
    }

    private void SetAllSelected(bool isSelected)
    {
        foreach (var row in Rows)
        {
            row.IsSelected = isSelected;
        }

        RecalculateState();
    }

    private void OnPreviewRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RospPreviewRow.IsSelected))
        {
            RecalculateState();
        }
    }

    private void RecalculateState()
    {
        foreach (var row in Rows)
        {
            row.IsDuplicate = false;
            row.HasConflict = false;
            row.ValidationError = null;
        }

        var duplicateGroups = Rows
            .GroupBy(row => row.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .ToArray();

        foreach (var group in duplicateGroups)
        {
            var selectedInGroup = group.Count(row => row.IsSelected);
            var hasConflict = selectedInGroup != 1;

            foreach (var row in group)
            {
                row.IsDuplicate = true;
                row.HasConflict = hasConflict;

                if (hasConflict)
                {
                    row.ValidationError = selectedInGroup == 0
                        ? $"Для кода {row.Code} не выбрана ни одна строка."
                        : $"Для кода {row.Code} выбрано строк: {selectedInGroup}. Нужно выбрать одну.";
                }
            }
        }

        TotalCount = Rows.Count;
        SelectedCount = Rows.Count(row => row.IsSelected);
        DuplicateGroupCount = duplicateGroups.Length;
        ConflictCount = duplicateGroups.Count(group => group.Count(row => row.IsSelected) != 1);

        UpdateSelectAllState();
        RefreshFilteredRows();

        CanSave =
            !IsBusy &&
            !HasImportErrors &&
            Rows.Count > 0 &&
            SelectedCount > 0 &&
            ConflictCount == 0 &&
            Rows.Where(row => row.IsSelected)
                .GroupBy(row => row.Code, StringComparer.Ordinal)
                .All(group => group.Count() == 1);

        LoadCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
    }

    private void RefreshFilteredRows()
    {
        var sourceRows = ShowOnlyConflicts
            ? Rows.Where(row => row.IsDuplicate).ToArray()
            : Rows.ToArray();

        if (_filteredRows.Count == sourceRows.Length &&
            _filteredRows.SequenceEqual(sourceRows))
        {
            return;
        }

        _filteredRows.Clear();

        foreach (var row in sourceRows)
        {
            _filteredRows.Add(row);
        }
    }

    private void UpdateSelectAllState()
    {
        _isUpdatingSelectAllState = true;

        try
        {
            SelectAllState = ResolveSelectAllState();
        }
        finally
        {
            _isUpdatingSelectAllState = false;
        }
    }

    private bool? ResolveSelectAllState()
    {
        if (Rows.Count == 0)
        {
            return false;
        }

        var selectedRowsCount = Rows.Count(row => row.IsSelected);

        if (selectedRowsCount == 0)
        {
            return false;
        }

        if (selectedRowsCount == Rows.Count)
        {
            return true;
        }

        return null;
    }

    private void ClearCurrentData()
    {
        foreach (var row in Rows)
        {
            row.PropertyChanged -= OnPreviewRowPropertyChanged;
        }

        Rows.Clear();
        _filteredRows.Clear();
        Messages.Clear();

        LoadedFilePath = null;
        HasImportErrors = false;
        Status = "Файл не загружен.";
    }

    private static string BuildSuggestedSqlFileName()
    {
        return $"update_rosps_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.sql";
    }
}