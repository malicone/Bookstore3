using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

    public static byte[] RotateImageBytes(byte[] imageBytes, double angle)
    {
        using var inputStream = new MemoryStream(imageBytes);
        var decoder = BitmapDecoder.Create(
            inputStream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];

        var rotated = new TransformedBitmap(frame, new RotateTransform(angle));
        rotated.Freeze();

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rotated));

        using var outputStream = new MemoryStream();
        encoder.Save(outputStream);
        return outputStream.ToArray();
    }

    public static byte[]? LoadCoverImage(byte[]? coverImage, Image image, UIElement noImagePanel)
    {
        var normalized = coverImage is { Length: > 0 } ? coverImage : null;

        if (normalized is not null)
        {
            image.Source = CreateBitmapImageFromBytes(normalized);
            image.Visibility = Visibility.Visible;
            noImagePanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            image.Source = null;
            image.Visibility = Visibility.Collapsed;
            noImagePanel.Visibility = Visibility.Visible;
        }

        return normalized;
    }

    private static BitmapImage CreateBitmapImageFromBytes(byte[] imageBytes)
    {
        using var stream = new MemoryStream(imageBytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = stream;
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
