using System.Globalization;
using System.Windows;

namespace Bookstore3.WPF;

public static class AppUtils
{
    public static string FormatDateTime(DateTime dateTime) =>
        dateTime.ToString(AppConstants.DefaultDateTimeFormat, CultureInfo.InvariantCulture);

    public static string FormatDateTime(DateTime? dateTime) =>
        dateTime.HasValue ? FormatDateTime(dateTime.Value) : string.Empty;

    public static void ShowErrorMessage(string message, string title = "Error")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
    public static void ShowInfoMessage(string message, string title = "Information")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
