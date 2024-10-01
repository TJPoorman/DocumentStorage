using DocumentStorage.Domain;
using DocumentStorage.Domain.Attributes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DocumentStorage.Infrastructure.EntityFramework;

/// <summary>
/// An abstract base class for Entity Framework DbContexts that integrates encryption capabilities.
/// This class provides a mechanism for encrypting and decrypting entity properties automatically 
/// during database operations, as well as configuring database indices for entities.
/// </summary>
public abstract class EntityFrameworkDbContext : DbContext
{
    /// <summary>
    /// The encryption provider used for encrypting and decrypting entity properties.
    /// </summary>
    public readonly IEncryptionProvider EncryptionProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFrameworkDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to configure the DbContext.</param>
    /// <param name="encryptionProvider">The encryption provider used for entity property encryption.</param>
    protected EntityFrameworkDbContext(DbContextOptions options, IEncryptionProvider encryptionProvider) : base(options) => EncryptionProvider = encryptionProvider;

    /// <summary>
    /// Configures the DbContext to use encryption and decryption interceptors for entity operations.
    /// </summary>
    /// <param name="optionsBuilder">The builder to configure the DbContext options.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new EncryptInterceptor(EncryptionProvider), new DecryptInterceptor(EncryptionProvider));
        base.OnConfiguring(optionsBuilder);
    }

    /// <summary>
    /// Configures the model for the database by creating indices for entities and their properties
    /// based on specific attributes and interfaces.
    /// Removes foreign key delete behaviour as this is handled with the <see cref="IDsEntityUpdater"/>.
    /// </summary>
    /// <param name="modelBuilder">The model builder for configuring the entity model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dbSets = GetType()
            .GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(a => a.PropertyType.IsGenericType && a.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));
        var createDsIndexMethod = GetType().GetTypeInfo().GetRuntimeMethods().Single(m => m.Name == nameof(CreateDsIndexes));
        var createUniqueIndexMethod = GetType().GetTypeInfo().GetRuntimeMethods().Single(m => m.Name == nameof(CreateUniqueIndex));
        var createGuidIndexMethod = GetType().GetTypeInfo().GetRuntimeMethods().Single(m => m.Name == nameof(CreateGuidIndex));
        var args = new object[] { modelBuilder };
        foreach (var dbSet in dbSets)
        {
            var baseType = dbSet.PropertyType.GetGenericArguments().First();

            createDsIndexMethod.MakeGenericMethod(baseType).Invoke(this, args);

            if (baseType.GetInterface(nameof(IDsUniqueDbRecord)) != null)
            {
                createUniqueIndexMethod.MakeGenericMethod(baseType).Invoke(this, args);
            }
            if (baseType.GetCustomAttribute(typeof(DsIndexGuidsAttribute)) != null)
            {
                createGuidIndexMethod.MakeGenericMethod(baseType).Invoke(this, args);
            }
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var keys = entityType.GetForeignKeys().Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade);
            foreach (var key in keys)
            {
                key.DeleteBehavior = DeleteBehavior.ClientCascade;
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Creates a unique index on the specified entity's unique key property.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity to create the index for.</typeparam>
    /// <param name="modelBuilder">The model builder for configuring the entity model.</param>
    protected virtual void CreateUniqueIndex<TEntity>(ModelBuilder modelBuilder) where TEntity : class, IDsUniqueDbRecord
    {
        var entity = modelBuilder.Entity<TEntity>();
        entity.HasIndex(a => a.UniqueKey).IsUnique();
    }

    /// <summary>
    /// Creates indices for properties of the specified entity type that are of type Guid.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity to create indices for.</typeparam>
    /// <param name="modelBuilder">The model builder for configuring the entity model.</param>
    protected virtual void CreateGuidIndex<TEntity>(ModelBuilder modelBuilder) where TEntity : class, IDsDbRecord
    {
        Type type = typeof(TEntity);

        var entity = modelBuilder.Entity<TEntity>();

        foreach (var prop in type.GetProperties().Where(p => p.PropertyType == typeof(Guid)))
        {
            if (prop.GetCustomAttribute(typeof(NotMappedAttribute)) != null) continue;

            var param = Expression.Parameter(typeof(TEntity), "e");
            Expression expression = Expression.Property(param, prop);
            if (prop.PropertyType.IsValueType) expression = Expression.Convert(expression, typeof(object));
            var result = Expression.Lambda<Func<TEntity, object>>(expression, param);

            entity.HasIndex(result);
        }
    }

    /// <summary>
    /// Creates indices for properties of the specified entity type based on properties identified by <see cref="DsCreateIndexAttribute"/>.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity to create indices for.</typeparam>
    /// <param name="modelBuilder">The model builder for configuring the entity model.</param>
    protected virtual void CreateDsIndexes<TEntity>(ModelBuilder modelBuilder) where TEntity : class, IDsRecord
    {
        Type type = typeof(TEntity);
        var entity = modelBuilder.Entity<TEntity>();
        Dictionary<PropertyInfo, DsCreateIndexAttribute> indexes = new();

        foreach (var prop in type.GetProperties())
        {
            if (prop.GetCustomAttribute(typeof(NotMappedAttribute)) != null) continue;
            if (prop.GetCustomAttribute(typeof(DsCreateIndexAttribute)) == null) continue;

            indexes.Add(prop, (DsCreateIndexAttribute)prop.GetCustomAttribute(typeof(DsCreateIndexAttribute)));
        }
        if (!indexes.Any()) return;

        //Create Multi-column indexes
        var multiColIndexes = indexes.Where(a => a.Value.Order > -1).GroupBy(a => a.Value.Name);
        foreach (IGrouping<string, KeyValuePair<PropertyInfo, DsCreateIndexAttribute>> multiColIndex in multiColIndexes)
        {
            var param = Expression.Parameter(typeof(TEntity), "e");
            Expression expression = Expression.Property(param, multiColIndex.First().Key);

            List<Expression> expressions = new();
            foreach (KeyValuePair<PropertyInfo, DsCreateIndexAttribute> indexExp in multiColIndex.OrderBy(a => a.Value.Order))
            {
                if (indexExp.Key.PropertyType.IsValueType) expressions.Add(Expression.Convert(expression, typeof(object)));
            }
            expression = Expression.NewArrayInit(typeof(object), expressions);
            var result = Expression.Lambda<Func<TEntity, object>>(expression, param);

            entity.HasIndex(result, multiColIndex.Key);
        }

        //Create single column indexes
        foreach (KeyValuePair<PropertyInfo, DsCreateIndexAttribute> colIndex in indexes.Where(a => a.Value.Order == -1))
        {
            var param = Expression.Parameter(typeof(TEntity), "e");
            Expression expression = Expression.Property(param, colIndex.Key);
            if (colIndex.Key.PropertyType.IsValueType)
            {
                expression = Expression.Convert(expression, typeof(object));
            }
            var result = Expression.Lambda<Func<TEntity, object>>(expression, param);

            entity.HasIndex(result, GetIndexName(typeof(TEntity), colIndex.Key, colIndex.Value));
        }

        static string GetIndexName(Type entityType, PropertyInfo prop, DsCreateIndexAttribute attr)
        {
            if (!string.IsNullOrEmpty(attr.Name)) return attr.Name;

            return $"IX_{entityType.Name}_{prop.Name}";
        }
    }
}