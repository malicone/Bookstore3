using Bookstore3.WPF.Utils;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Bookstore3.WPF.AppWindows;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        CopyrightTextBlock.Text = BuildCopyrightText();
        EmailHyperlink.NavigateUri = new Uri("mailto:admin@kpzrepository.com");
        WebsiteHyperlink.NavigateUri = new Uri("https://bookstore3.kpzrepository.com");

        Loaded += (_, _) => LoadAppIcon();
    }

    private static string BuildCopyrightText()
    {
        const int startYear = 2026;
        var currentYear = DateTime.Now.Year;
        var yearRange = currentYear > startYear
            ? $"{startYear} - {currentYear}"
            : startYear.ToString();

        return $"Copyright © {yearRange} Maxim Mihaluk";
    }

    private void LoadAppIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Bookstore3_icon.ico", UriKind.Absolute);
            var resourceStream = Application.GetResourceStream(uri)?.Stream;
            if (resourceStream is null)
                return;

            using (resourceStream)
            {
                var decoder = BitmapDecoder.Create(
                    resourceStream,
                    BitmapCreateOptions.None,
                    BitmapCacheOption.OnLoad);

                const double displayLogicalSize = 72;
                var dpi = VisualTreeHelper.GetDpi(this);
                var displayPixels = (int)Math.Round(displayLogicalSize * dpi.DpiScaleX);

                var displayFrame = SelectBestIconFrame(decoder, displayPixels);
                displayFrame.Freeze();
                AppIconImage.Source = displayFrame;
                AppIconImage.Width = displayLogicalSize;
                AppIconImage.Height = displayLogicalSize;

                var windowFrame = SelectBestIconFrame(decoder, (int)Math.Round(32 * dpi.DpiScaleX));
                windowFrame.Freeze();
                Icon = windowFrame;
            }
        }
        catch
        {
            // Window can open without icon if resource is unavailable.
        }
    }

    private static BitmapFrame SelectBestIconFrame(BitmapDecoder decoder, int preferredSize)
    {
        BitmapFrame? bestFrame = null;
        var bestScore = int.MaxValue;

        foreach (var frame in decoder.Frames)
        {
            var size = Math.Max(frame.PixelWidth, frame.PixelHeight);
            var score = size >= preferredSize
                ? size - preferredSize
                : preferredSize - size + 1_000_000;

            if (score < bestScore)
            {
                bestScore = score;
                bestFrame = frame;
            }
        }

        return bestFrame ?? decoder.Frames[0];
    }

    private void CloseButton_ClickHandler(object sender, RoutedEventArgs e) =>
        Close();

    private void EmailHyperlink_RequestNavigateHandler(object sender, RequestNavigateEventArgs e) =>
        OpenUri(e);

    private void WebsiteHyperlink_RequestNavigateHandler(object sender, RequestNavigateEventArgs e) =>
        OpenUri(e);

    private void PackageLink_RequestNavigateHandler(object sender, RequestNavigateEventArgs e) =>
        OpenUri(e);

    private static void OpenUri(RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppUtils.ShowErrorMessage($"Failed to open link:{Environment.NewLine}{ex.Message}", "About");
        }

        e.Handled = true;
    }
}
