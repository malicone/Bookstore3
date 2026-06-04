namespace Bookstore3.WPF.AITools;

public interface IAiBookMetadataService
{
    Task<IReadOnlyList<BookMetadataResult>> FetchMetadataAsync(
        string title,
        string? author,
        int? edition,
        CancellationToken cancellationToken);
}
