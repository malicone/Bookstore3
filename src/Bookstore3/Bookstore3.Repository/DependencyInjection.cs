using Microsoft.Extensions.DependencyInjection;

namespace Bookstore3.Repository;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Bookstore repository factory in the DI container.
    /// </summary>
    public static IServiceCollection AddBookstoreRepositoryFactory(this IServiceCollection services, string? connectionString)
    {
        if(string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException(nameof(connectionString));

        var repoFactoryDescriptor = new ServiceDescriptor(
            typeof(IBookstoreRepositoryFactory),
            provider => new BookstoreRepositoryFactory(connectionString),
            ServiceLifetime.Transient);

        services.Add(repoFactoryDescriptor);

        return services;
    }
}