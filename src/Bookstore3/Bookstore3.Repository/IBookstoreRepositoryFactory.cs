using KpzRepository.Factory;

namespace Bookstore3.Repository;

public interface IBookstoreRepositoryFactory : IKpzRepositoryFactory
{
    IBookRepository GetBookRepository();
    IAppOptionRepository GetAppOptionRepository();
}
