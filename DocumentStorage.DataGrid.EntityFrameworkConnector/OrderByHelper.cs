using DocumentStorage.Domain;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DocumentStorage.DataGrid.EntityFrameworkConnector;

/// <summary>
/// Interface defining a helper for applying an ordering operation to an IQueryable collection.
/// </summary>
public interface IOrderByHelper
{
    /// <summary>
    /// Applies an ordering operation to the provided queryable collection based on the specified direction.
    /// </summary>
    /// <param name="queryable">The IQueryable collection to order.</param>
    /// <param name="expression">The lambda expression representing the property to order by.</param>
    /// <param name="expressionParameter">The member expression representing the property to order.</param>
    /// <param name="direction">The direction of the sort (ascending or descending).</param>
    /// <returns>An ordered IQueryable collection based on the specified property and direction.</returns>
    IQueryable OrderBy(IQueryable queryable, LambdaExpression expression, MemberExpression expressionParameter, SortDirection direction);
}

/// <summary>
/// Implements the IOrderByHelper interface for applying ordering operations to IQueryable collections 
/// for a specific record type and key type.
/// </summary>
/// <typeparam name="TRecord">The type of records in the collection.</typeparam>
/// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
public class OrderByHelper<TRecord, TKey> : IOrderByHelper
    where TRecord : class, IDsRecord
{
    private readonly ConcurrentDictionary<string, Func<IQueryable<TRecord>, Expression<Func<TRecord, TKey>>, IOrderedQueryable<TRecord>>> _orderByFunctions = new(StringComparer.InvariantCultureIgnoreCase);

    /// <summary>
    /// Applies an ordering operation to the provided queryable collection based on the specified direction.
    /// </summary>
    /// <param name="queryable">The IQueryable collection to order.</param>
    /// <param name="expression">The lambda expression representing the property to order by.</param>
    /// <param name="expressionParameter">The member expression representing the property to order.</param>
    /// <param name="direction">The direction of the sort (ascending or descending).</param>
    /// <returns>An ordered IQueryable collection based on the specified property and direction.</returns>
    public IQueryable OrderBy(IQueryable queryable, LambdaExpression expression, MemberExpression expressionParameter, SortDirection direction) =>
        OrderBy(expressionParameter, direction)((IQueryable<TRecord>)queryable, (Expression<Func<TRecord, TKey>>)expression);

    /// <summary>
    /// Determines the appropriate ordering method based on the specified direction and constructs 
    /// the corresponding order function.
    /// </summary>
    /// <param name="expressionParameter">The member expression representing the property to order.</param>
    /// <param name="direction">The direction of the sort (ascending or descending).</param>
    /// <returns>A function that performs the ordering operation on the provided IQueryable collection.</returns>
    private Func<IQueryable<TRecord>, Expression<Func<TRecord, TKey>>, IQueryable<TRecord>> OrderBy(MemberExpression expressionParameter, SortDirection direction)
    {
        string methodName = direction switch
        {
            SortDirection.Ascending => nameof(Queryable.OrderBy),
            SortDirection.Descending => nameof(Queryable.OrderByDescending),
            _ => throw new NotSupportedException(),
        };

        if (_orderByFunctions.TryGetValue(methodName, out var orderByFunc)) return orderByFunc;

        MethodInfo methodInfo = typeof(Queryable).GetMethods().FirstOrDefault(m => m.Name.Equals(methodName) && m.GetParameters().Length == 2);
        methodInfo = methodInfo.MakeGenericMethod(typeof(TRecord), ((PropertyInfo)expressionParameter.Member).PropertyType);

        _orderByFunctions.TryAdd(
            methodName,
            (Func<IQueryable<TRecord>, Expression<Func<TRecord, TKey>>, IOrderedQueryable<TRecord>>)methodInfo
                .CreateDelegate(typeof(Func<IQueryable<TRecord>, Expression<Func<TRecord, TKey>>, IOrderedQueryable<TRecord>>)));

        return OrderBy(expressionParameter, direction);
    }
}
