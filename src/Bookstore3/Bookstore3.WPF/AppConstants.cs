using System.Globalization;

namespace Bookstore3.WPF;

public static class AppConstants
{
    public const string AppSettingsFileName = "appsettings.json";
    public const string DefaultDateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public static string FormatDateTime(DateTime dateTime) =>
        dateTime.ToString(DefaultDateTimeFormat, CultureInfo.InvariantCulture);

    public static string FormatDateTime(DateTime? dateTime) =>
        dateTime.HasValue ? FormatDateTime(dateTime.Value) : string.Empty;
}
