using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DocumentStorage.Infrastructure;

public static class IQueryableExtensions
{
    /// <summary>
    /// Asynchronously retrieves the first element of a sequence or a default value if no element is found.
    /// This extension method operates on an <see cref="IQueryable"/> and uses reflection to invoke the 
    /// generic <see cref="Queryable.FirstOrDefault{TSource}"/> method, allowing for a flexible return type specified by <paramref name="resultType"/>.
    /// </summary>
    /// <param name="query">The <see cref="IQueryable"/> to search for the first element.</param>
    /// <param name="resultType">The <see cref="Type"/> to which the result will be cast.</param>
    /// <returns>A task that represents the asynchronous operation, containing the first element of the sequence 
    /// or the default value for the specified type if the sequence is empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/> or <paramref name="resultType"/> is null.</exception>
    public static async Task<object> FirstOrDefaultAsync(this IQueryable query, Type resultType)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(resultType);

        var singleMethod = typeof(Queryable)
            .GetMethods()
            .First(m => m.Name == "FirstOrDefault" && m.GetParameters().Length == 1)
            .MakeGenericMethod(query.ElementType);
        var result = singleMethod.Invoke(null, new object[] { query });
        return await Task.FromResult(Convert.ChangeType(result, resultType));
    }

    /// <summary>
    /// Converts an <see cref="IQueryable"/> to another <see cref="IQueryable"/> with elements of a specified type.
    /// This method uses reflection to apply the <see cref="Queryable.Select{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, TResult}})"/>
    /// method, effectively converting each element of the source sequence to the specified <paramref name="resultType"/>.
    /// </summary>
    /// <param name="source">The original <see cref="IQueryable"/> to be converted.</param>
    /// <param name="resultType">The target <see cref="Type"/> to which the elements of the queryable source will be converted.</param>
    /// <returns>An <see cref="IQueryable"/> with elements cast to the specified <paramref name="resultType"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="resultType"/> is null.</exception>
    public static IQueryable ToQueryable(this IQueryable source, Type resultType)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(resultType);

        var parameter = Expression.Parameter(source.ElementType, "x");
        var conversion = Expression.Convert(parameter, resultType);
        var selector = Expression.Lambda(conversion, parameter);

        var selectMethod = typeof(Queryable)
            .GetMethods()
            .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
            .MakeGenericMethod(source.ElementType, resultType);

        var queryableResult = selectMethod.Invoke(null, new object[] { source, selector });

        return (IQueryable)queryableResult;
    }

    /// <summary>
    /// Dynamically applies a filter to an <see cref="IQueryable"/> based on the specified property name and value.
    /// This method constructs a lambda expression that checks if the property of each element in the sequence
    /// matches the given value, and applies it to the source queryable using the <see cref="Queryable.Where{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/> method.
    /// </summary>
    /// <param name="source">The <see cref="IQueryable"/> to which the dynamic filter will be applied.</param>
    /// <param name="propertyName">The name of the property to filter on. Must not be null or empty.</param>
    /// <param name="value">The value to compare the property against.</param>
    /// <returns>An <see cref="IQueryable"/> with the filter applied based on the specified property and value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="propertyName"/> is null.</exception>
    public static IQueryable WhereDynamic(this IQueryable source, string propertyName, object value)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));

        var elementType = source.ElementType;
        var parameter = Expression.Parameter(elementType, "x");
        var property = Expression.Property(parameter, propertyName);
        var constant = Expression.Constant(value);
        var equality = Expression.Equal(property, constant);
        var predicate = Expression.Lambda(equality, parameter);

        var whereMethod = typeof(Queryable)
            .GetMethods()
            .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);

        var result = whereMethod.Invoke(null, new object[] { source, predicate });

        return (IQueryable)result;
    }
}
