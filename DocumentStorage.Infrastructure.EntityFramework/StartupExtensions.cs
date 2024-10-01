using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DocumentStorage.Infrastructure.EntityFramework;

public static class StartupExtensions
{
    /// <summary>
    /// Adds DocumentStorage Entity Framework services and configuration to the application's dependency injection container.
    /// </summary>
    /// <typeparam name="TContext">The context class to register, which must implement <see cref="EntityFrameworkDbContext"/></typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> instance to which the DocumentStorage is added.</param>
    /// <param name="optionsAction">An optional action to configure the <see cref="DbContextOptionsBuilder"/>.</param>
    /// <param name="autoMapRepositories">A flag to enable automatic repository mapping (default is true).</param>
    /// <param name="encryptionProvider">An optional custom encryption provider for managing encryption.</param>
    /// <param name="contextLifetime">The service lifetime for the <typeparamref name="TContext"/> (default is Scoped).</param>
    /// <param name="optionsLifetime">The service lifetime for the <see cref="DbContextOptions"/> (default is Scoped).</param>
    /// <returns>The updated <see cref="IHostApplicationBuilder"/> instance.</returns>
    /// <remarks>
    /// The method performs the following tasks:
    /// 1. Registers the <see cref="IEncryptionProvider"/> if provided; else the <see cref="DefaultEncryptionProvider"/> as a singleton to manage encryption functionality.
    /// 2. Registers the specified <typeparamref name="TContext"/> EntityFrameworkDbContext with optional configurations.
    /// 3. If <paramref name="autoMapRepositories"/> is true, it scans the loaded assemblies for classes that inherit from <see cref="EntityFrameworkRepository{TEntity, TKey}"/>
    ///    and registers them with their corresponding interfaces in the dependency injection container, then initializes the registered repositories.
    /// </remarks>
    public static IHostApplicationBuilder AddDocumentStorage<TContext>(
        this IHostApplicationBuilder builder,
        Action<DbContextOptionsBuilder>? optionsAction = null,
        bool autoMapRepositories = true,
        IEncryptionProvider? encryptionProvider = null,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
        ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
        where TContext : EntityFrameworkDbContext
    {
        ILogger logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<TContext>>();

        encryptionProvider ??= new DefaultEncryptionProvider();
        builder.Services.Add(new ServiceDescriptor(typeof(IEncryptionProvider), encryptionProvider.GetType(), contextLifetime));

        builder.Services.AddDbContext<TContext, TContext>(optionsAction, contextLifetime, optionsLifetime);

        if (autoMapRepositories)
        {
            List<Type> repositoryInterfaces = new();

            IEnumerable<Type>? repositories = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a is not null && a.FullName is not null && !a.FullName.StartsWith("System") && !a.FullName.StartsWith("Microsoft"))
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract &&
                    Enumerable
                        .Range(0, 1)
                        .Select(_ =>
                        {
                            Type? currentType = t;
                            while (currentType != null && currentType != typeof(object))
                            {
                                if (currentType.IsGenericType)
                                {
                                    currentType = currentType.GetGenericTypeDefinition();
                                }
                                if (currentType == typeof(EntityFrameworkRepository<,>))
                                {
                                    return true;
                                }
                                currentType = currentType.BaseType;
                            }
                            return false;
                        })
                        .First());

            foreach (Type? repository in repositories)
            {
                if (repository is null) continue;

                var interfaces = repository.GetInterfaces();
                var topInterfaces = interfaces.Except(interfaces.SelectMany(a => a.GetInterfaces())).Where(a => !a.IsGenericType);
                if (topInterfaces.Count() > 1)
                {
                    logger.LogWarning("Could not auto-map repository {Repository}", repository.Name);
                    continue;
                }
                if (!topInterfaces.Any())
                {
                    builder.Services.Add(new ServiceDescriptor(repository, repository, contextLifetime));
                    repositoryInterfaces.Add(repository);
                }
                else
                {
                    builder.Services.Add(new ServiceDescriptor(topInterfaces.First(), repository, contextLifetime));
                    repositoryInterfaces.Add(topInterfaces.First());
                }
            }

            using var provider = builder.Services.BuildServiceProvider();
            foreach (var repository in repositoryInterfaces)
            {
                var service = provider.GetRequiredService(repository);

                MethodInfo? initializeMethod = service.GetType().GetMethod("InitializeAsync");
                var result = initializeMethod?.Invoke(service, null);
                if (result is Task task) task.Wait();
            }
        }

        return builder;
    }

    /// <summary>
    /// Adds DocumentStorage Entity Framework services and configuration to the application's dependency injection container.
    /// </summary>
    /// <typeparam name="TContext">The context class to register, which must implement <see cref="EntityFrameworkDbContext"/></typeparam>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> instance to which the DocumentStorage is added.</param>
    /// <param name="optionsAction">An optional action that configures the <see cref="DbContextOptionsBuilder"/> and allows access to the <see cref="IServiceProvider"/>.</param>
    /// <param name="autoMapRepositories">A flag to enable automatic repository mapping (default is true).</param>
    /// <param name="encryptionProvider">An optional custom encryption provider for managing encryption.</param>
    /// <param name="contextLifetime">The service lifetime for the <typeparamref name="TContext"/> (default is Scoped).</param>
    /// <param name="optionsLifetime">The service lifetime for the <see cref="DbContextOptions"/> (default is Scoped).</param>
    /// <returns>The updated <see cref="IHostApplicationBuilder"/> instance.</returns>
    /// <remarks>
    /// The method performs the following tasks:
    /// 1. Registers the <see cref="IEncryptionProvider"/> if provided; else the <see cref="DefaultEncryptionProvider"/> as a singleton to manage encryption functionality.
    /// 2. Registers the specified <typeparamref name="TContext"/> EntityFrameworkDbContext with optional configurations.
    /// 3. If <paramref name="autoMapRepositories"/> is true, it scans the loaded assemblies for classes that inherit from <see cref="EntityFrameworkRepository{TEntity, TKey}"/>
    ///    and registers them with their corresponding interfaces in the dependency injection container, then initializes the registered repositories.
    /// </remarks>
    public static IHostApplicationBuilder AddDocumentStorage<TContext>(
        this IHostApplicationBuilder builder,
        Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction,
        bool autoMapRepositories = true,
        IEncryptionProvider? encryptionProvider = null,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
        ServiceLifetime optionsLifetime = ServiceLifetime.Scoped)
        where TContext : EntityFrameworkDbContext
    {
        ILogger logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<TContext>>();

        encryptionProvider ??= new DefaultEncryptionProvider();
        builder.Services.Add(new ServiceDescriptor(typeof(IEncryptionProvider), encryptionProvider.GetType(), contextLifetime));

        builder.Services.AddDbContext<TContext, TContext>(optionsAction, contextLifetime, optionsLifetime);

        if (autoMapRepositories)
        {
            List<Type> repositoryInterfaces = new();

            IEnumerable<Type>? repositories = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a is not null && a.FullName is not null && !a.FullName.StartsWith("System") && !a.FullName.StartsWith("Microsoft"))
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract &&
                    Enumerable
                        .Range(0, 1)
                        .Select(_ =>
                        {
                            Type? currentType = t;
                            while (currentType != null && currentType != typeof(object))
                            {
                                if (currentType.IsGenericType)
                                {
                                    currentType = currentType.GetGenericTypeDefinition();
                                }
                                if (currentType == typeof(EntityFrameworkRepository<,>))
                                {
                                    return true;
                                }
                                currentType = currentType.BaseType;
                            }
                            return false;
                        })
                        .First());

            foreach (Type? repository in repositories)
            {
                if (repository is null) continue;

                var interfaces = repository.GetInterfaces();
                var topInterfaces = interfaces.Except(interfaces.SelectMany(a => a.GetInterfaces())).Where(a => !a.IsGenericType);
                if (topInterfaces.Count() > 1)
                {
                    logger.LogWarning("Could not auto-map repository {Repository}", repository.Name);
                    continue;
                }
                if (!topInterfaces.Any())
                {
                    builder.Services.Add(new ServiceDescriptor(repository, repository, contextLifetime));
                    repositoryInterfaces.Add(repository);
                }
                else
                {
                    builder.Services.Add(new ServiceDescriptor(topInterfaces.First(), repository, contextLifetime));
                    repositoryInterfaces.Add(topInterfaces.First());
                }
            }

            using var provider = builder.Services.BuildServiceProvider();
            foreach (var repository in repositoryInterfaces)
            {
                var service = provider.GetRequiredService(repository);

                MethodInfo? initializeMethod = service.GetType().GetMethod("InitializeAsync");
                var result = initializeMethod?.Invoke(service, null);
                if (result is Task task) task.Wait();
            }
        }

        return builder;
    }
}