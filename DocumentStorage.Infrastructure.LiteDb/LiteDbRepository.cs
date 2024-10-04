using DocumentStorage.Domain;
using LiteDB;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using DocumentStorage.Domain.Attributes;

namespace DocumentStorage.Infrastructure.LiteDb;

/// <inheritdoc/>
/// <remarks>
/// Abstract base class for a LiteDB repository.
/// </remarks>
/// <typeparam name="TRecord">The type of records this repository will handle, constrained to classes implementing <see cref="IDsDbRecord"/>.</typeparam>
/// <typeparam name="TContext">The type of the LiteDB context, constrained to <see cref="LiteDbContext"/>.</typeparam>
public abstract class LiteDbRepository<TRecord, TContext> : IDsRepository<TRecord>
    where TRecord : class, IDsDbRecord
    where TContext : LiteDbContext
{
    /// <summary>
    /// The database context used by this repository.
    /// </summary>
    protected readonly TContext _context;

    /// <summary>
    /// The entity updater for managing record updates.
    /// </summary>
    protected readonly IDsEntityUpdater _entityUpdater = new EntityUpdater();

    /// <inheritdoc/>
    public event DsRecordEventHandler<TRecord> AfterRecordUpserted;

    /// <inheritdoc/>
    public event DsRecordEventHandler<TRecord> AfterRecordDeleted;

    /// <summary>
    /// The LiteDB collection that holds records of type <see cref="TRecord"/>.
    /// </summary>
    protected ILiteCollection<TRecord> Collection { get; }

    /// <summary>
    /// Initializes a new instance of the repository with the provided database context.
    /// </summary>
    /// <param name="context">The database context to use.</param>
    protected LiteDbRepository(TContext context)
    {
        _context = context;
        Collection = GetCollection();
    }

    /// <inheritdoc/>
    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        var record = await GetAsync(id);
        bool deleted = Collection.Delete(new BsonValue(id));
        AfterRecordDeleted?.Invoke(this, new DsRecordEventArgs<TRecord>(record));
        return deleted;
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<TRecord>> FindAsync(Expression<Func<TRecord, bool>> request) =>
        await Task.FromResult(Collection.Find(request.ModifyExpressionForEncryption(_context.EncryptionProvider)));

    /// <inheritdoc/>
    public virtual async Task<TRecord> GetAsync(Guid id) => await GetLiteDbRecordAsync(id).ConfigureAwait(false);

    /// <summary>
    /// Retrieves the LiteDB collection for the current record type.
    /// </summary>
    /// <returns>The LiteDB collection of TRecord.</returns>
    protected virtual ILiteCollection<TRecord> GetCollection() => _context.Database.GetCollection<TRecord>();

    /// <summary>
    /// Retrieves a LiteDB record by its unique identifier, decrypting it if necessary.
    /// </summary>
    /// <param name="id">The unique identifier of the record.</param>
    /// <returns>A task that returns the record if found, otherwise null.</returns>
    protected virtual async Task<TRecord> GetLiteDbRecordAsync(Guid id) => 
        await Task.FromResult((TRecord)Collection.FindOne(Query.EQ("_id", id)).DecryptEntity(_context.EncryptionProvider)).ConfigureAwait(false);

    /// <inheritdoc/>
    public virtual async Task InitializeAsync()
    {
        await Task.FromResult(Collection.LongCount()).ConfigureAwait(false);

        Type type = typeof(TRecord);

        foreach (var prop in type.GetProperties())
        {
            if (prop.GetCustomAttribute(typeof(NotMappedAttribute)) != null) continue;
            if (prop.GetCustomAttribute(typeof(DsCreateIndexAttribute)) == null) continue;

            Collection.EnsureIndex(prop.Name, $"LOWER($.{prop.Name})", false);
        }

        if (typeof(TRecord).GetCustomAttribute(typeof(DsIndexGuidsAttribute)) != null)
        {
            foreach (var prop in type.GetProperties().Where(p => p.PropertyType == typeof(Guid)))
            {
                if (prop.GetCustomAttribute(typeof(NotMappedAttribute)) != null) continue;

                Collection.EnsureIndex(prop.Name, $"LOWER($.{prop.Name})", false);
            }
        }

        if (typeof(IDsUniqueDbRecord).IsAssignableFrom(typeof(TRecord)))
        {
            Collection.EnsureIndex(nameof(IDsUniqueDbRecord.UniqueKey), $"LOWER($.{nameof(IDsUniqueDbRecord.UniqueKey)})", true);
        }
    }

    /// <inheritdoc/>
    public virtual bool IsDuplicate(IDsUniqueDbRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        BsonValue bsonKeyValue = new(record?.UniqueKey?.ToLower());

        TRecord liteDbRecord = Collection.FindOne(Query.EQ($"LOWER($.{nameof(record.UniqueKey)})", bsonKeyValue));
        TRecord existingRecord = liteDbRecord;

        IDsDbRecord recordToCheck = record;

        return existingRecord is IDsDbRecord existingRecordToCheck && existingRecordToCheck.Id != recordToCheck.Id;
    }

    /// <inheritdoc/>
    public virtual async Task UpsertAsync(TRecord record)
    {
        await Task.FromResult(0);

        if (record == null) throw new DsDbRecordValidationException($"Can't upsert a null {nameof(record)}");

        if (record is IDsDbRecord dbRecord)
        {
            if (dbRecord.CreatedDateTime == DateTimeOffset.MinValue)
            {
                dbRecord.CreatedDateTime = DateTimeOffset.UtcNow;
            }

            if (dbRecord.LastModifiedDateTime == DateTimeOffset.MinValue)
            {
                dbRecord.LastModifiedDateTime = DateTimeOffset.UtcNow;
            }
        }

        TRecord liteDbRecord = record;

        if (liteDbRecord is IDsUniqueDbRecord uniqueDbRecord)
        {
            if (IsDuplicate(uniqueDbRecord))
            {
                throw new DsUniqueDbRecordDuplicateException($"Conflict on {typeof(TRecord).Name}.{nameof(uniqueDbRecord.UniqueKey)} '{uniqueDbRecord.UniqueKey}'");
            }
        }

        IDsDbRecord dsDbRecord = liteDbRecord;

        dsDbRecord.LastModifiedDateTime = DateTimeOffset.UtcNow;

        List<ValidationResult> results = new();
        if (!DsRecordValidator.TryValidateObjectRecursive(dsDbRecord, results))
        {
            string message = string.Join("\r\n", results?.Select(r => r.ErrorMessage));

            throw new DsDbRecordValidationException(message);
        }

        Collection.Upsert((TRecord)liteDbRecord.EncryptEntity(_context.EncryptionProvider));

        if (record is IDsDbRecord finalRecord)
        {
            finalRecord.CreatedDateTime = dsDbRecord.CreatedDateTime;
            finalRecord.LastModifiedDateTime = dsDbRecord.LastModifiedDateTime;
            AfterRecordUpserted?.Invoke(this, new DsRecordEventArgs<TRecord>((TRecord)finalRecord));
        }
    }
}