using System.Globalization;
using System.Windows.Media;

namespace Bookstore3.WPF;

public enum ExportFormat
{
    Excel,
    Csv,
    Pdf
}

public static class AppConstants
{
    public const string AppSettingsFileName = "appsettings.json";

    public const string DefaultDateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    public const string ExportFileNameDateTimeFormat = "yyyy-MM-dd'_-_'HH-mm-ss";
    public const string DefaultPriceFormat = "F2";

    public static string DefaultPriceDataFormatString =>
        string.Create(CultureInfo.InvariantCulture, $"{{0:{DefaultPriceFormat}}}");
    
    public const string GridCellBorderColor = "#E0E0E0";
    
    public const long NullRecordId = long.MinValue;
    public const long UndefinedRecordId = 0;

    public const double CoverImageWidthToHeightRatio = 0.6644295;

    public static SolidColorBrush GridCellBorderBrush { get; } = CreateFrozenBrush(GridCellBorderColor);

    private static SolidColorBrush CreateFrozenBrush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
        brush.Freeze();
        return brush;
    }
}
