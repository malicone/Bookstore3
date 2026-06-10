using Bookstore3.WPF.AITools;
using System.ComponentModel;
using System.IO;
using System.Globalization;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Bookstore3.WPF.AppWindows;

internal sealed class BookMetadataPickerItem : INotifyPropertyChanged
{
    public const double CoverWidth = 52;
    public const double CoverHeight = 78;

    public BookMetadataPickerItem(BookMetadataResult metadata)
    {
        Metadata = metadata;
        ExtendedDescription = BuildExtendedDescription(metadata);
    }

    public BookMetadataResult Metadata { get; }

    public string DisplayText => Metadata.DisplayText;

    public string ExtendedDescription { get; }

    public string? SourceUrl =>
        string.IsNullOrWhiteSpace(Metadata.sourceUrl) ? null : Metadata.sourceUrl.Trim();

    public bool HasSourceUrl => string.IsNullOrWhiteSpace(SourceUrl) == false;

    public ImageSource? CoverImage
    {
        get => _coverImage;
        private set
        {
            if (ReferenceEquals(_coverImage, value))
                return;

            _coverImage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasCoverImage));
        }
    }

    public bool HasCoverImage => CoverImage is not null;

    public async Task LoadCoverImageAsync(HttpClient httpClient, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Metadata.coverImageUrl))
            return;

        if (Uri.TryCreate(Metadata.coverImageUrl, UriKind.Absolute, out var uri) == false ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return;

        try
        {
            using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.IsSuccessStatusCode == false)
                return;

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.Length == 0)
                return;

            CoverImage = CreateCoverThumbnail(bytes);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Keep placeholder when cover download fails.
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private ImageSource? _coverImage;

    private static ImageSource CreateCoverThumbnail(byte[] imageBytes)
    {
        using var stream = new MemoryStream(imageBytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = stream;
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = (int)CoverWidth;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static string BuildExtendedDescription(BookMetadataResult metadata)
    {
        var lines = new List<string>();

        var details = new List<string>();
        AppendDetail(details, "ISBN", metadata.isbn);
        AppendDetail(details, "Edition", metadata.edition);
        AppendDetail(details, "Pages", metadata.pageCount);
        AppendDetail(details, "Year", metadata.publishYear);
        AppendDetail(details, "Format", metadata.format);
        if (details.Count > 0)
            lines.Add(string.Join(" · ", details));

        var publishing = new List<string>();
        AppendDetail(publishing, "Publisher", metadata.publisher);
        AppendDetail(publishing, "City", metadata.city);
        AppendDetail(publishing, "Language", metadata.language);
        AppendDetail(publishing, "Group", metadata.@group);
        if (publishing.Count > 0)
            lines.Add(string.Join(" · ", publishing));

        if (metadata.price.HasValue)
            lines.Add($"Price: {metadata.price.Value.ToString(CultureInfo.InvariantCulture)}");

        if (metadata.wrapper.HasValue)
            lines.Add(metadata.wrapper.Value ? "Hardcover" : "Paperback");

        if (string.IsNullOrWhiteSpace(metadata.annotation) == false)
        {
            if (lines.Count > 0)
                lines.Add(string.Empty);
            lines.Add(metadata.annotation.Trim());
        }

        return lines.Count > 0 ? string.Join(Environment.NewLine, lines) : "No additional details.";
    }

    private static void AppendDetail(List<string> target, string label, object? value)
    {
        if (value is null)
            return;

        var text = value switch
        {
            string s when string.IsNullOrWhiteSpace(s) => null,
            string s => s.Trim(),
            _ => value.ToString()
        };

        if (string.IsNullOrWhiteSpace(text))
            return;

        target.Add($"{label}: {text}");
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
