using DocumentStorage.Domain;
using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DocumentStorage.Infrastructure.LiteDb;

/// <summary>
/// An abstract base class for LiteDB repositories that integrates encryption capabilities.
/// Provides functionality for initializing, accessing, and managing LiteDB sets 
/// as well as encryption support through an encryption provider.
/// </summary>
public abstract class LiteDbContext : IDisposable
{
    private readonly LiteDbContextOptions _options;
    private readonly ConcurrentBag<LiteRepository> _liteRepositories = new();

    /// <summary>
    /// The encryption provider used for encrypting and decrypting entity properties.
    /// </summary>
    public readonly IEncryptionProvider EncryptionProvider;

    /// <summary>
    /// Initializes a new instance of the LiteDbContext with the specified repository directory and encryption provider.
    /// </summary>
    /// <param name="options">The <see cref="LiteDbContextOptions"/> to use to configure the repository.</param>
    /// <param name="encryptionProvider">The encryption provider for securing data.</param>
    protected LiteDbContext(LiteDbContextOptions options, IEncryptionProvider encryptionProvider)
    {
        _options = options;
        EncryptionProvider = encryptionProvider;
        EnsureCreated();
    }

    /// <summary>
    /// Ensures that all LiteDB sets defined in the context are created by initializing
    /// repositories for each set and associating them with the corresponding properties.
    /// </summary>
    public void EnsureCreated()
    {
        PropertyInfo[] properties = GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(LiteSet<>))
            .ToArray();
        foreach (var property in properties)
        {
            string typeName = $"{GetType().FullName}[{property.PropertyType.GenericTypeArguments[0].FullName}]";
            var repository = LiteDbRepositoryUtil.CreateFromContextOptions(typeName, _options);
            var val = (dynamic)CreateGenericInstance(typeof(LiteSet<>), property.PropertyType.GenericTypeArguments[0]);
            val.LiteRepository = repository;
            property.SetValue(this, val);
            _liteRepositories.Add(repository);
        }
    }

    /// <summary>
    /// Ensures that all LiteDB sets and their associated repository files are deleted.
    /// Deletes the physical database files corresponding to each set.
    /// </summary>
    public void EnsureDeleted()
    {
        PropertyInfo[] properties = GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(LiteSet<>))
            .ToArray();
        foreach (var property in properties)
        {
            string typeName = $"{GetType().FullName}[{property.PropertyType.GenericTypeArguments[0].FullName}]";
            FileInfo fInfo = new(_options.Filename);
            if (!fInfo.Extension.Equals(".db", StringComparison.InvariantCultureIgnoreCase)) fInfo = new FileInfo(Path.Combine(_options.Filename, $"{typeName}.db"));
            if (fInfo.Exists) fInfo.Delete();
        }
    }

    /// <summary>
    /// Gets a LiteDB set of type <typeparamref name="T"/>. The set is associated with the repository.
    /// </summary>
    /// <typeparam name="T">The type of records managed by the LiteDB set.</typeparam>
    /// <returns>The LiteDB set of type <typeparamref name="T"/>.</returns>
    public LiteSet<T> Set<T>() where T : IDsRecord
    {
        var setProp = GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(p => p.PropertyType == typeof(LiteSet<T>))
            .GetValue(this);
        return (LiteSet<T>)setProp;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        do
        {
            if (_liteRepositories.TryTake(out LiteRepository repo)) repo?.Dispose();
        } while (!_liteRepositories.IsEmpty);
        GC.SuppressFinalize(this);
    }

    private static object CreateGenericInstance(Type genericType, Type typeArgument) => Activator.CreateInstance(genericType.MakeGenericType(typeArgument));

    private static string GetDatabaseFileNameFromLiteRepository(LiteRepository repository)
    {
        FieldInfo dbField = typeof(LiteRepository).GetField("_db", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo engineField = typeof(LiteDatabase).GetField("_engine", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo settingsField = typeof(SharedEngine).GetField("_settings", BindingFlags.NonPublic | BindingFlags.Instance);
        if (dbField is null) return default;
        if (engineField is null) return default;
        if (settingsField is null) return default;
        if (dbField.GetValue(repository) is not LiteDatabase liteDatabase) return default;
        if (engineField.GetValue(liteDatabase) is not SharedEngine sharedEngine) return default;
        if (settingsField.GetValue(sharedEngine) is not EngineSettings engineSettings) return default;

        return engineSettings?.Filename ?? default;
    }
}