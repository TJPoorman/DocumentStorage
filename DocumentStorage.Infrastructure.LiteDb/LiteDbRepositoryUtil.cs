using LiteDB;
using System;
using System.IO;
using System.Reflection;

namespace DocumentStorage.Infrastructure.LiteDb;

/// <summary>
/// Utility class that provides methods for creating LiteDB repositories from file paths.
/// </summary>
public static class LiteDbRepositoryUtil
{
    /// <summary>
    /// Creates a LiteRepository from <see cref="LiteDbContextOptions"/> using the assembly name as the database file name.
    /// </summary>
    /// <param name="assembly">The assembly whose name will be used as the database file name.</param>
    /// <param name="options">The LiteDbContextOptions to use to configure the repository.</param>
    /// <returns>A LiteRepository object connected to the specified file path.</returns>
    public static LiteRepository CreateFromContextOptions(Assembly assembly, LiteDbContextOptions options) => CreateFromContextOptions(assembly.GetName().Name, options);

    /// <summary>
    /// Creates a LiteRepository from <see cref="LiteDbContextOptions"/> using the full name of the specified type as the database file name.
    /// </summary>
    /// <param name="serviceType">The type whose full name will be used as the database file name.</param>
    /// <param name="options">The LiteDbContextOptions to use to configure the repository.</param>
    /// <returns>A LiteRepository object connected to the specified file path.</returns>
    public static LiteRepository CreateFromContextOptions(Type serviceType, LiteDbContextOptions options) => CreateFromContextOptions(serviceType.FullName, options);

    /// <summary>
    /// Creates a LiteRepository from <see cref="LiteDbContextOptions"/> using the specified service name as the database file name.
    /// </summary>
    /// <param name="serviceName">The name to be used for the database file.</param>
    /// <param name="options">The LiteDbContextOptions to use to configure the repository.</param>
    /// <returns>A LiteRepository object connected to the specified file path.</returns>
    public static LiteRepository CreateFromContextOptions(string serviceName, LiteDbContextOptions options)
    {
        try
        {
            FileInfo fInfo = new(options.Filename);
            if (!fInfo.Extension.Equals(".db", StringComparison.InvariantCultureIgnoreCase)) fInfo = new FileInfo(Path.Combine(options.Filename, $"{serviceName}.db"));
            if (!fInfo.Directory.Exists) fInfo.Directory.Create();

            return new LiteRepository(new ConnectionString()
            {
                Connection = options.Connection,
                Filename = fInfo.FullName,
                Password = options.Password,
                InitialSize = options.InitialSize,
                ReadOnly = options.ReadOnly,
                Upgrade = options.Upgrade,
                AutoRebuild = options.AutoRebuild,
                Collation = options.Collation,
            }, options.BsonMapper);
        }
        catch (Exception exception)
        {
            throw new Exception($"{nameof(LiteRepository)}.{nameof(CreateFromFilePath)} failed.  See inner exception for details.", exception);
        }
    }

    /// <summary>
    /// Creates a LiteRepository from a file path using the assembly name as the database file name.
    /// </summary>
    /// <param name="assembly">The assembly whose name will be used as the database file name.</param>
    /// <param name="filePath">The file path where the database will be stored.</param>
    /// <returns>A LiteRepository object connected to the specified file path.</returns>
    public static LiteRepository CreateFromFilePath(Assembly assembly, string filePath) => CreateFromFilePath(assembly.GetName().Name, filePath);

    /// <summary>
    /// Creates a LiteRepository from a file path using the full name of the specified type as the database file name.
    /// </summary>
    /// <param name="serviceType">The type whose full name will be used as the database file name.</param>
    /// <param name="filePath">The file path where the database will be stored.</param>
    /// <returns>A LiteRepository object connected to the specified file path.</returns>
    public static LiteRepository CreateFromFilePath(Type serviceType, string filePath) => CreateFromFilePath(serviceType.FullName, filePath);

    /// <summary>
    /// Creates a LiteRepository from a file path using the specified service name as the database file name.
    /// </summary>
    /// <param name="serviceName">The name to be used for the database file.</param>
    /// <param name="filePath">The file path where the database will be stored.</param>
    /// <returns>A LiteRepository object connected to the specified file path.</returns>
    public static LiteRepository CreateFromFilePath(string serviceName, string filePath)
    {
        try
        {
            FileInfo fInfo = new(filePath);
            if (!fInfo.Extension.Equals(".db", StringComparison.InvariantCultureIgnoreCase)) fInfo = new FileInfo(Path.Combine(filePath, $"{serviceName}.db"));
            if (!fInfo.Directory.Exists) fInfo.Directory.Create();

            return new LiteRepository(new ConnectionString() { Connection = ConnectionType.Shared, Filename = fInfo.FullName });
        }
        catch (Exception exception)
        {
            throw new Exception($"{nameof(LiteRepository)}.{nameof(CreateFromFilePath)} failed.  See inner exception for details.", exception);
        }
    }
}