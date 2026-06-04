using System.Text;
using System.Text.Json.Serialization;

namespace Bookstore3.WPF.AITools;

public sealed class BookMetadataResult
{
    public string? title { get; set; }
    public string? author { get; set; }
    public string? isbn { get; set; }
    public int? pageCount { get; set; }
    public int? edition { get; set; }
    public string? format { get; set; }
    public int? publishYear { get; set; }
    public double? price { get; set; }
    public string? @group { get; set; }
    public string? language { get; set; }
    public string? publisher { get; set; }
    public string? city { get; set; }
    public bool? wrapper { get; set; }
    public string? annotation { get; set; }
    public string? coverImageUrl { get; set; }

    [JsonPropertyName("cover_url")]
    public string? cover_url { set { if (string.IsNullOrWhiteSpace(coverImageUrl)) coverImageUrl = value; } }

    [JsonPropertyName("cover_image_url")]
    public string? cover_image_url { set { if (string.IsNullOrWhiteSpace(coverImageUrl)) coverImageUrl = value; } }

    [JsonPropertyName("imageUrl")]
    public string? imageUrl { set { if (string.IsNullOrWhiteSpace(coverImageUrl)) coverImageUrl = value; } }

    public string DisplayText
    {
        get
        {
            var label = new StringBuilder();
            if (string.IsNullOrWhiteSpace(title) == false)
                label.Append(title.Trim());
            if (string.IsNullOrWhiteSpace(author) == false)
                label.Append(label.Length > 0 ? $" — {author.Trim()}" : author.Trim());

            var details = new List<string>();
            if (string.IsNullOrWhiteSpace(isbn) == false)
                details.Add($"ISBN {isbn.Trim()}");
            if (edition.HasValue)
                details.Add($"ed. {edition.Value}");
            if (publishYear.HasValue)
                details.Add(publishYear.Value.ToString());
            if (string.IsNullOrWhiteSpace(format) == false)
                details.Add(format.Trim());

            if (details.Count > 0)
            {
                if (label.Length > 0)
                    label.Append(' ');
                label.Append('(');
                label.Append(string.Join(", ", details));
                label.Append(')');
            }

            return label.Length > 0 ? label.ToString() : "(Unknown book)";
        }
    }
}