using System.Globalization;

namespace EscolaSync.Converters;

/// <summary>
/// Converte int (0–100) para double (0.0–1.0) para o ProgressBar.
/// </summary>
public class IntToDoubleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i ? i / 100.0 : 0.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is double d ? (int)(d * 100) : 0;
}

/// <summary>
/// Converte string hexadecimal de cor (#RRGGBB) para Color.
/// </summary>
public class StringToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && Color.TryParse(hex, out var color))
            return color;
        return Colors.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
