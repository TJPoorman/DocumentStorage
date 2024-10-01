using DocumentStorage.Domain;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DocumentStorage.DataGrid.EntityFrameworkConnector;

/// <summary>
/// Interface defining a helper for applying a 'Contains' filter to an IQueryable collection.
/// </summary>
public interface IContainsHelper
{
    /// <summary>
    /// Applies a 'Contains' filter to the provided queryable collection.
    /// </summary>
    /// <param name="queryable">The IQueryable collection to filter.</param>
    /// <param name="expression">The lambda expression representing the property to check.</param>
    /// <param name="expressionParameter">The member expression representing the property to filter.</param>
    /// <param name="value">The value to check for presence in the property.</param>
    /// <returns>An IQueryable collection filtered by the 'Contains' condition.</returns>
    IQueryable Contains(IQueryable queryable, LambdaExpression expression, MemberExpression expressionParameter, object value);
}

/// <summary>
/// Implements the IContainsHelper interface for applying 'Contains' filters to IQueryable collections 
/// for a specific record type and key type.
/// </summary>
/// <typeparam name="TRecord">The type of records in the collection.</typeparam>
/// <typeparam name="TKey">The type of the key used for the 'Contains' method.</typeparam>
public class ContainsHelper<TRecord, TKey> : IContainsHelper
    where TRecord : class, IDsRecord
{
    private readonly Lazy<MethodInfo> _methodInfo = new(() =>
        typeof(TKey).GetMethods().FirstOrDefault(m => m.Name.Equals(nameof(string.Contains)) && m.GetParameters().Length == 1), true);

    /// <summary>
    /// Applies a 'Contains' filter to the provided queryable collection.
    /// </summary>
    /// <param name="queryable">The IQueryable collection to filter.</param>
    /// <param name="expression">The lambda expression representing the property to check.</param>
    /// <param name="expressionParameter">The member expression representing the property to filter.</param>
    /// <param name="value">The value to check for presence in the property.</param>
    /// <returns>An IQueryable collection filtered by the 'Contains' condition.</returns>
    public IQueryable Contains(IQueryable queryable, LambdaExpression expression, MemberExpression expressionParameter, object value)
    {
        MethodCallExpression containsExpression = Expression.Call(
            expressionParameter,
            _methodInfo.Value,
            Expression.Constant(value, ((PropertyInfo)expressionParameter.Member).PropertyType));

        return ((IQueryable<TRecord>)queryable).Where(Expression.Lambda<Func<TRecord, bool>>(containsExpression, expression.Parameters[0]));
    }

}
