using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace RospSqlGenerator.Converters;

/// <summary>
/// Возвращает фон ячейки таблицы в зависимости от наличия конфликта по коду РОСП.
/// </summary>
public sealed class ConflictBackgroundBrushConverter : IValueConverter
{
    private static readonly IBrush ConflictBrush = new SolidColorBrush(Color.Parse("#FFFFE5E5"));
    private static readonly IBrush NormalBrush = Brushes.Transparent;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true
            ? ConflictBrush
            : NormalBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}