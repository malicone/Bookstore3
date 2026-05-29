using Bookstore3.Model;
using KpzRepository.Repository;

namespace Bookstore3.Repository;

public interface IBookRepository : IKpzRepository<long, book>
{
    IEnumerable<book_ex> GetAllBooksLightweight();
    Task<IEnumerable<book_ex>> GetAllBooksLightweightAsync();
}
