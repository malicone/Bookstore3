namespace Bookstore3.WPF.AITools;

public interface IAiBookMetadataService
{
    Task<BookMetadataResult> FetchMetadataAsync(
        string title,
        string? author,
        CancellationToken cancellationToken);
}
