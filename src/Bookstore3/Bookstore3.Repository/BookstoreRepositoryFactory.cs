using KpzRepository.Sqlite.Factory;

namespace Bookstore3.Repository;

public class BookstoreRepositoryFactory : KpzRepositorySqliteFactory, IBookstoreRepositoryFactory
{
    public BookstoreRepositoryFactory(string connectionString) : base(connectionString)
    {

    }

    public IBookRepository GetBookRepository()
    {
        return new BookRepository(GetNewConnection(ConnectionString));
    }

    public IAppOptionRepository GetAppOptionRepository()
    {
        return new AppOptionRepository(GetNewConnection(ConnectionString));
    }
}
