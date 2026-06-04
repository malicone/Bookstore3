using Bookstore3.Model;
using KpzRepository.Repository;

namespace Bookstore3.Repository;

public interface IBookRepository : IKpzRepository<long, book>
{
    /// <summary>
    /// Returns a list of books with only 'small' fields like id, title, price and omits the 'big' fields like description, image, etc.
    /// This is useful for displaying a list of books (no memory overhead).
    /// </summary>
    /// <returns>List of lightweight book objects.</returns>
    IEnumerable<book_ex> GetAllBooksLightweight();

    /// <summary>
    /// Asynchronously returns a list of books with only 'small' fields like id, title, price and omits the 'big' fields like description, image, etc.
    /// This is useful for displaying a list of books (no memory overhead).
    /// </summary>
    /// <returns>List of lightweight book objects.</returns>
    Task<IEnumerable<book_ex>> GetAllBooksLightweightAsync();

    /// <summary>
    /// Searches for books by title or author.
    /// Returns a list of books with only 'small' fields like id, title, price and omits the 'big' fields like description, image, etc.
    /// This is useful for displaying a list of books (no memory overhead).
    /// </summary>
    /// <param name="searchText">The text to search for.</param>
    /// <returns>List of lightweight book objects that match the search criteria.</returns>
    IEnumerable<book_ex> SearchBooksLightweight(string searchText);

    /// <summary>
    /// Asynchronously searches for books by title or author.
    /// Returns a list of books with only 'small' fields like id, title, price and omits the 'big' fields like description, image, etc.
    /// This is useful for displaying a list of books (no memory overhead).
    /// </summary>
    /// <param name="searchText">The text to search for.</param>
    /// <returns>List of lightweight book objects that match the search criteria.</returns>
    Task<IEnumerable<book_ex>> SearchBooksLightweightAsync(string searchText);

    int CountBooksReferencingLookup(LookupBookReferenceKind referenceKind, long lookupId);

    int ClearBookLookupReferences(LookupBookReferenceKind referenceKind, long lookupId, long undefinedRecordId);
}
