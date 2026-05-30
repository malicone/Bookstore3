using Bookstore3.Model;
using Dapper;
using KpzRepository.Repository;
using System.Data;

namespace Bookstore3.Repository;

public class BookRepository : KpzRepository<long, book>, IBookRepository
{
    public BookRepository(IDbConnection connection) : base(connection)
    {

    }

    public virtual IEnumerable<book_ex> GetAllBooksLightweight()
    {
        if(OpenConnection())
        {
            string sql = BuiltSelectAllBooksLightweightQuery();
            return Connection!.Query<book_ex>(sql);
        }
        return Enumerable.Empty<book_ex>();
    }

    public virtual async Task<IEnumerable<book_ex>> GetAllBooksLightweightAsync()
    {
        if (OpenConnection())
        {
            string sql = BuiltSelectAllBooksLightweightQuery();
            return await Connection!.QueryAsync<book_ex>(sql);
        }
        return await Task.FromResult(Enumerable.Empty<book_ex>());
    }

    private string BuiltSelectAllBooksLightweightQuery()
    {
        return
@"SELECT 
    b.id, 
    b.crt_date_time,
    b.author,
    b.title,
    b.publisher_id,
    b.page_count,
    b.publish_year,
    b.edition,
    b.format,
    b.isbn,
    b.price,
    b.date_when_get,
    b.wrapper,
    b.language_id,
    b.group_id,
    b.shop_id,
    b.city_id,
    b.has_digit_copy,
    b.book_file,
    p.name AS publisher_name, 
    l.name AS language_name, 
    c.name AS city_name, 
    s.name AS shop_name, 
    g.name AS group_name                 
FROM books b
LEFT JOIN publishers p ON b.publisher_id = p.id
LEFT JOIN languages l ON b.language_id = l.id
LEFT JOIN cities c ON b.city_id = c.id
LEFT JOIN shops s ON b.shop_id = s.id
LEFT JOIN groups g ON b.group_id = g.id
ORDER BY b.id";
    }
}