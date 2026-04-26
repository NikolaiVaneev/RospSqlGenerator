using CommunityToolkit.Mvvm.ComponentModel;

namespace RospSqlGenerator.Models;

/// <summary>
/// Строка предпросмотра, отображаемая в таблице приложения.
/// </summary>
public sealed partial class RospPreviewRow : ObservableObject
{
    public RospPreviewRow(RospImportRow row)
    {
        SourceRowNumber = row.SourceRowNumber;
        Region = row.Region;
        Code = row.Code;
        Name = row.Name;
        Address = row.Address;
        FsspChiefsFullName = row.FsspChiefsFullName;
        FsspTelephoneNumber = row.FsspTelephoneNumber;
        FsspPhoneOfHelpService = row.FsspPhoneOfHelpService;
        FsspPhoneOfHelpService2 = row.FsspPhoneOfHelpService2;
        FsspWorkHours = row.FsspWorkHours;
    }

    [ObservableProperty]
    private bool isSelected = true;

    [ObservableProperty]
    private bool isDuplicate;

    [ObservableProperty]
    private bool hasConflict;

    [ObservableProperty]
    private string? validationError;

    public int SourceRowNumber { get; }

    public short Region { get; }

    public string Code { get; }

    public string Name { get; }

    public string? Address { get; }

    public string? FsspChiefsFullName { get; }

    public string? FsspTelephoneNumber { get; }

    public string? FsspPhoneOfHelpService { get; }

    public string? FsspPhoneOfHelpService2 { get; }

    public string? FsspWorkHours { get; }

    /// <summary>
    /// Возвращает нормализованную строку импорта без UI-состояния.
    /// </summary>
    public RospImportRow ToImportRow()
    {
        return new RospImportRow
        {
            SourceRowNumber = SourceRowNumber,
            Region = Region,
            Code = Code,
            Name = Name,
            Address = Address,
            FsspChiefsFullName = FsspChiefsFullName,
            FsspTelephoneNumber = FsspTelephoneNumber,
            FsspPhoneOfHelpService = FsspPhoneOfHelpService,
            FsspPhoneOfHelpService2 = FsspPhoneOfHelpService2,
            FsspWorkHours = FsspWorkHours
        };
    }
}