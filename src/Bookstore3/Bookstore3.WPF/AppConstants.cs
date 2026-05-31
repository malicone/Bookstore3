using System.Windows.Media;

namespace Bookstore3.WPF;

public static class AppConstants
{
    public const string AppSettingsFileName = "appsettings.json";
    public const string DefaultDateTimeFormat = "yyyy-MM-dd HH:mm:ss";
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
