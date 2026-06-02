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
    public string? annotation { get; set; }
    public string? coverImageUrl { get; set; }
}
