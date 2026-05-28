using KpzRepository.Factory;

namespace Bookstore3.WPF.Repository;

public interface IBookstoreRepositoryFactory : IKpzRepositoryFactory
{
    IBookRepository GetBookRepository();
}
