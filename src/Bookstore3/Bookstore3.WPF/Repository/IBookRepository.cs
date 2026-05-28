using Bookstore3.WPF.Model;
using KpzRepository.Repository;

namespace Bookstore3.WPF.Repository;

public interface IBookRepository : IKpzRepository<long, book>
{
    IEnumerable<book_ex> GetAllBooksLightweight();
    Task<IEnumerable<book_ex>> GetAllBooksLightweightAsync();
}
