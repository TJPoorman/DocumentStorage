using DocumentStorage.Domain;
using DocumentStorage.Domain.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DocumentStorage.Infrastructure.EntityFramework;

/// <inheritdoc/>
/// <remarks>
/// Abstract base class for an Entity Framework repository.
/// </remarks>
/// <typeparam name="TRecord">The type of records this repository will handle, constrained to classes implementing <see cref="IDsDbRecord"/>.</typeparam>
/// <typeparam name="TContext">The type of the Entity Framework database context, constrained to <see cref="EntityFrameworkDbContext"/>.</typeparam>
public abstract class EntityFrameworkRepository<TRecord, TContext> : IEntityFrameworkRepository<TRecord>
    where TRecord : class, IDsDbRecord
    where TContext : EntityFrameworkDbContext
{
    /// <summary>
    /// The database context used by this repository.
    /// </summary>
    protected readonly TContext _context;

    /// <summary>
    /// The entity updater for managing record updates.
    /// </summary>
    protected readonly IDsEntityUpdater _entityUpdater;

    /// <summary>
    /// Represents the current database transaction.
    /// </summary>
    protected EntityFrameworkTransaction _transaction;

    /// <summary>
    /// Lazy-loading property for the root record's table name.
    /// </summary>
    protected Lazy<string> RootRecordTableName => new(GetRootRecordTableName, true);

    /// <inheritdoc/>
    public event DsRecordEventHandler<TRecord> AfterRecordUpserted;

    /// <inheritdoc/>
    public event DsRecordEventHandler<TRecord> AfterRecordDeleted;

    /// <summary>
    /// Initializes a new instance of the repository with the provided database context.
    /// </summary>
    /// <param name="context">The database context to use.</param>
    protected EntityFrameworkRepository(TContext context)
    {
        _context = context;
        _entityUpdater = new EntityUpdater<TContext>(context);
    }

    /// <summary>
    /// Adds a new record to the context.
    /// </summary>
    /// <param name="record">The record to add.</param>
    protected virtual void Add(TRecord record) => _context.Set<TRecord>().Add(record);

    /// <inheritdoc/>
    public async Task<IDisposable> BeginTransactionAsync()
    {
        if (_transaction?.Disposed ?? false) _transaction = null;

        _transaction ??= new EntityFrameworkTransaction();

        return await Task.FromResult(_transaction).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task CommitTransactionAsync(IDisposable transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        if (transaction is not EntityFrameworkTransaction efTransaction) throw new ArgumentException("Invalid transaction type", nameof(transaction));
        if (efTransaction.Disposed) throw new ObjectDisposedException(nameof(transaction));

        if (_transaction?.Disposed ?? true) throw new InvalidOperationException($"No open {nameof(EntityFrameworkTransaction)}");
        if (_transaction.Id != efTransaction.Id) throw new InvalidOperationException("The provided transaction to commit is invalid in the current database context.");

        try
        {
            await SaveChangesAsync().ConfigureAwait(false);
        }
        finally
        {
            _transaction?.Dispose();
        }
    }

    /// <inheritdoc/>
    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        if (await GetAsync(id, false).ConfigureAwait(false) is TRecord record)
        {
            _entityUpdater.Remove(record);

            if (_transaction?.Disposed ?? true) await SaveChangesAsync().ConfigureAwait(false);

            AfterRecordDeleted?.Invoke(this, new DsRecordEventArgs<TRecord>(record));

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<TRecord>> FindAsync(Expression<Func<TRecord, bool>> request) => await FindAsync(request, true);

    /// <summary>
    /// Asynchronously finds records based on the provided expression filter.
    /// </summary>
    /// <param name="request">An expression used to filter the records.</param>
    /// <param name="includeChildren">Whether to include child entities in the request.</param>
    /// <returns>A <see cref="Task{TResult}"/> that returns an <see cref="IEnumerable{TRecord}"/> of matching records.</returns>
    protected virtual async Task<IEnumerable<TRecord>> FindAsync(Expression<Func<TRecord, bool>> request, bool includeChildren) => includeChildren
        ? await Task.FromResult(IncludeChildren(GetQueryable(true)).Where(request.ModifyExpressionForEncryption(_context.EncryptionProvider)))
        : await Task.FromResult(GetQueryable(true).Where(request.ModifyExpressionForEncryption(_context.EncryptionProvider)));

    /// <inheritdoc/>
    public virtual async Task<TRecord> GetAsync(Guid id) => await GetAsync(id, true).ConfigureAwait(false);

    /// <summary>
    /// Retrieves a record by its identifier with an option for tracking changes.
    /// </summary>
    /// <param name="id">The identifier of the record to retrieve.</param>
    /// <param name="noTracking">Whether to retrieve the record without tracking changes.</param>
    /// <returns>A task representing the asynchronous operation, returning the found record or null if not found.</returns>
    protected virtual async Task<TRecord> GetAsync(Guid id, bool noTracking)
    {
        IQueryable<TRecord> query = GetQueryable(noTracking).Where<IDsDbRecord>(r => r.Id == id).OfType<TRecord>();
        query = IncludeChildren(query);
        TRecord entityRecord = await query.FirstOrDefaultAsync().ConfigureAwait(false);

        if (entityRecord == null) return null;

        await PopulateReferenceEntities(entityRecord);

        return entityRecord;
    }

    /// <summary>
    /// Gets a queryable collection of records with an option for tracking changes.
    /// </summary>
    /// <param name="noTracking">Whether to retrieve records without tracking changes.</param>
    /// <returns>A queryable collection of records.</returns>
    protected virtual IQueryable<TRecord> GetQueryable(bool noTracking)
    {
        IQueryable<TRecord> query = _context.Set<TRecord>();

        return noTracking ? query.AsNoTracking() : query;
    }

    /// <summary>
    /// Retrieves the root record's table name from the database context model.
    /// </summary>
    /// <returns>The name of the root record's table.</returns>
    protected virtual string GetRootRecordTableName()
    {
        IEntityType entityType = _context.Model.FindEntityType(typeof(TRecord)) ?? throw new InvalidOperationException($"Entity type {typeof(TRecord).Name} not found in the DbContext.");
        var tableName = entityType.GetTableName();
        return tableName;
    }

    /// <inheritdoc/>
    public abstract IQueryable<TRecord> IncludeChildren(IQueryable<TRecord> query);

    /// <inheritdoc/>
    public virtual async Task InitializeAsync() => await _context.Database.EnsureCreatedAsync().ConfigureAwait(false);

    /// <inheritdoc/>
    public virtual bool IsDuplicate(IDsUniqueDbRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        TRecord existingRecord = _context.Set<TRecord>()
            .FromSqlRaw($"SELECT TOP 1 * FROM [{RootRecordTableName.Value}] WHERE [{nameof(record.UniqueKey)}] = {{0}}", record.UniqueKey)
            .AsNoTracking()
            .FirstOrDefault();

        IDsDbRecord recordToCheck = record;

        return existingRecord is IDsDbRecord existingRecordToCheck && existingRecordToCheck.Id != recordToCheck.Id;
    }

    /// <summary>
    /// Saves changes to the database asynchronously, handling concurrency and validation exceptions.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, returning the number of affected rows.</returns>
    protected virtual async Task<int> SaveChangesAsync()
    {
        try
        {
            return await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException concurrencyException)
        {
            throw new DsDbUpdateConcurrencyException(concurrencyException.Message, concurrencyException);
        }
        catch (DbUpdateException updateException)
        {
            StringBuilder sb = new();
            foreach (var entry in updateException.Entries)
            {
                var entityType = entry.Entity.GetType().Name;
                sb.AppendLine($"Entity type: {entityType}");

                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(entry.Entity);
                if (!Validator.TryValidateObject(entry.Entity, validationContext, validationResults, true))
                {
                    foreach (var validationResult in validationResults)
                    {
                        sb.AppendLine($"Validation Error: {validationResult.ErrorMessage}");
                    }
                }
            }

            throw new DsDbRecordValidationException(sb.ToString(), updateException);
        }
    }

    /// <inheritdoc/>
    public virtual async Task UpsertAsync(TRecord record)
    {
        if (record == null) throw new DsDbRecordValidationException($"Can't upsert a null {nameof(record)}");

        if (record is IDsDbRecord dbRecord)
        {
            if (dbRecord.CreatedDateTime == DateTimeOffset.MinValue)
            {
                dbRecord.CreatedDateTime = DateTimeOffset.Now;
            }

            if (dbRecord.LastModifiedDateTime == DateTimeOffset.MinValue)
            {
                dbRecord.LastModifiedDateTime = DateTimeOffset.Now;
            }
        }

        TRecord entityRecord = record;

        if (entityRecord is IDsUniqueDbRecord uniqueDbRecord && IsDuplicate(uniqueDbRecord))
            throw new DsUniqueDbRecordDuplicateException($"Conflict on {typeof(TRecord).Name}.{nameof(uniqueDbRecord.UniqueKey)} '{uniqueDbRecord.UniqueKey}'");

        IDsDbRecord dsDbRecord = entityRecord;

        DateTimeOffset originalLastModified = dsDbRecord.LastModifiedDateTime;

        dsDbRecord.LastModifiedDateTime = DateTimeOffset.Now;

        TRecord existingRecord = await GetAsync(dsDbRecord.Id, false).ConfigureAwait(false);

        if (existingRecord == null)
        {
            Add(entityRecord);
        }
        else
        {
            if (originalLastModified != existingRecord.LastModifiedDateTime)
                throw new DsDbUpdateConcurrencyException("Optimistic concurrency check failed.  Record has been modified.  Please refresh before making changes.");

            _entityUpdater.Update(entityRecord, existingRecord);
        }

        if (_transaction?.Disposed ?? true) await SaveChangesAsync().ConfigureAwait(false);

        AfterRecordUpserted?.Invoke(this, new DsRecordEventArgs<TRecord>(entityRecord));
    }

    private IQueryable GetQueryable(Type entityType, bool noTracking) =>
        noTracking ? GetQueryableWithNoTracking(_context, entityType) : GetQueryableByType(_context, entityType);

    private static IQueryable GetQueryableByType(DbContext context, Type entityType)
    {
        var method = typeof(DbContext).GetMethod(nameof(DbContext.Set), Array.Empty<Type>()).MakeGenericMethod(entityType);
        return method.Invoke(context, null) as IQueryable;
    }

    private static IQueryable GetQueryableWithNoTracking(DbContext context, Type entityType)
    {
        var dbSet = typeof(DbContext)
            .GetMethod(nameof(DbContext.Set), Array.Empty<Type>())
            .MakeGenericMethod(entityType)
            .Invoke(context, null);

        var asNoTrackingMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethod(nameof(EntityFrameworkQueryableExtensions.AsNoTracking))
            .MakeGenericMethod(entityType);

        return asNoTrackingMethod.Invoke(null, new[] { dbSet }) as IQueryable;
    }

    private async Task PopulateReferenceEntities(TRecord record)
    {
        var referenceIdentifiers = typeof(TRecord).GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(DsReferenceAttribute)));

        foreach (var property in referenceIdentifiers)
        {
            if (property.PropertyType != typeof(Guid) && property.PropertyType != typeof(Guid?)) continue;

            var attribute = (DsReferenceAttribute)property.GetCustomAttribute(typeof(DsReferenceAttribute));
            var targetProperty = typeof(TRecord).GetProperty(attribute.ReferenceProperty);
            if (targetProperty is null) continue;

            Guid? lookupId = (Guid?)property.GetValue(record);
            if (lookupId is null) continue;

            var query = GetQueryable(targetProperty.PropertyType, true).ToQueryable(targetProperty.PropertyType).WhereDynamic("Id", lookupId);

            var lookupValue = await query.FirstOrDefaultAsync(targetProperty.PropertyType).ConfigureAwait(false);

            targetProperty.SetValue(record, lookupValue);
        }
    }
}