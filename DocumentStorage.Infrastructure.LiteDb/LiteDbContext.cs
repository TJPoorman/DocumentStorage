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
    private readonly LiteRepository _repository;
    
    /// <summary>
    /// The encryption provider used for encrypting and decrypting entity properties.
    /// </summary>
    public readonly IEncryptionProvider EncryptionProvider;

    /// <summary>
    /// The database used for the LiteDbContext
    /// </summary>
    public ILiteDatabase Database => _repository.Database;

    /// <summary>
    /// Initializes a new instance of the LiteDbContext with the specified repository directory and encryption provider.
    /// </summary>
    /// <param name="options">The <see cref="LiteDbContextOptions"/> to use to configure the repository.</param>
    /// <param name="encryptionProvider">The encryption provider for securing data.</param>
    protected LiteDbContext(LiteDbContextOptions options, IEncryptionProvider encryptionProvider)
    {
        _options = options;
        _repository = LiteDbRepositoryUtil.CreateFromContextOptions(GetType().FullName ?? "database", _options);
        EncryptionProvider = encryptionProvider;
    }

    /// <summary>
    /// Ensures that the LiteDB backing file is created.
    /// </summary>
    public void EnsureCreated()
    {
        _ = _repository.Database;    // Will throw argument null exception if connection string isn't valid.
    }

    /// <summary>
    /// Ensures that the LiteDB backing file is deleted.
    /// Deletes the physical database file.
    /// </summary>
    public void EnsureDeleted()
    {
        string typeName = GetType().FullName ?? "database";
        FileInfo fInfo = new(_options.Filename);
        if (!fInfo.Extension.Equals(".db", StringComparison.InvariantCultureIgnoreCase)) fInfo = new FileInfo(Path.Combine(_options.Filename, $"{typeName}.db"));
        if (fInfo.Exists) fInfo.Delete();
    }

    /// <summary>
    /// Gets a LiteDB set of type <typeparamref name="T"/>. The set is associated with the repository.
    /// </summary>
    /// <typeparam name="T">The type of records managed by the LiteDB set.</typeparam>
    /// <returns>An ILiteCollection of type <typeparamref name="T"/>.</returns>
    public ILiteCollection<T> Set<T>() where T : IDsRecord => Database.GetCollection<T>();

    /// <inheritdoc/>
    public void Dispose()
    {
        Database?.Dispose();
        GC.SuppressFinalize(this);
    }
}