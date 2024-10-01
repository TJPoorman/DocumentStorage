using DocumentStorage.Domain;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace DocumentStorage.DataGrid.EntityFrameworkConnector;

/// <summary>
/// Static class containing extension methods for IQueryable to facilitate filtering and ordering 
/// operations based on the provided DataGridRequest.
/// </summary>
public static class IQueryableExtensions
{
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, IContainsHelper>> _containsLookup = new();
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, IOrderByHelper>> _orderByAssistantLookup = new();
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyDescriptor>> _propertyLookup = new();

    /// <summary>
    /// Adds filtering criteria for "Contains" operations to the provided IQueryable based on the 
    /// specified DataGridRequest.
    /// </summary>
    /// <typeparam name="TRecord">The type of records in the IQueryable.</typeparam>
    /// <param name="queryable">The IQueryable collection to filter.</param>
    /// <param name="request">The DataGridRequest containing filtering information.</param>
    /// <returns>An IQueryable collection with "Contains" filter applied.</returns>
    public static IQueryable<TRecord> AddContainsWhereCriteria<TRecord>(this IQueryable<TRecord> queryable, DataGridRequest request)
        where TRecord : class, IDsRecord
    {
        foreach (FilterClause filter in request?.Filters?.Where((f) => f != null && f.FilterType == FilterType.Contains && f.Value is string))
        {
            IContainsHelper assistant = GetContainsHelper(typeof(TRecord), filter.FieldName);

            GetExpressionAndParameter<TRecord>(filter.FieldName, out LambdaExpression expression, out MemberExpression expressionParameter);

            queryable = (IQueryable<TRecord>)assistant.Contains(queryable, expression, expressionParameter, filter.Value);
        }

        return queryable;
    }

    /// <summary>
    /// Adds multi-value filtering criteria to the IQueryable based on the specified DataGridRequest.
    /// Supports "Between" and "In" filter types for collections of values.
    /// </summary>
    /// <typeparam name="TRecord">The type of records in the IQueryable.</typeparam>
    /// <param name="queryable">The IQueryable collection to filter.</param>
    /// <param name="request">The DataGridRequest containing filtering information.</param>
    /// <returns>An IQueryable collection with multi-value filter criteria applied.</returns>
    public static IQueryable<TRecord> AddMultiValueWhereCriteria<TRecord>(this IQueryable<TRecord> queryable, DataGridRequest request)
        where TRecord : class, IDsRecord
    {
        foreach (FilterClause filter in request?.Filters?
            .Where((f) => f is not null && f.Value is not null && f.Value is not string && f.Value is IEnumerable && (f.FilterType == FilterType.Between || f.FilterType == FilterType.In)))
        {
            List<object> values = new();
            foreach (object value in filter.Value as IEnumerable)
            {
                values.Add(value);
            }

            if (values.Count < 1) continue;

            queryable = queryable.AddMultiValueWhereCriteria(filter, values);
        }

        return queryable;
    }

    /// <summary>
    /// Adds ordering criteria to the IQueryable based on the specified DataGridRequest. 
    /// If no sorters are specified, defaults to ordering by IDsRecord.Id in ascending order.
    /// </summary>
    /// <typeparam name="TRecord">The type of records in the IQueryable.</typeparam>
    /// <param name="queryable">The IQueryable collection to order.</param>
    /// <param name="request">The DataGridRequest containing sorting information.</param>
    /// <returns>An IOrderedQueryable collection sorted based on the specified criteria.</returns>
    public static IOrderedQueryable<TRecord> AddOrderBy<TRecord>(this IQueryable<TRecord> queryable, DataGridRequest request)
        where TRecord : class, IDsRecord
    {
        request ??= new DataGridRequest();

        if (request?.Sorters?.Count < 1)
        {
            request.Sorters = new List<SortClause>()
            {
                { new(nameof(IDsRecord.Id), SortDirection.Ascending) }
            };
        }

        foreach (SortClause sort in request.Sorters)
        {
            IOrderByHelper assistant = GetOrderByHelper(typeof(TRecord), sort.FieldName);

            GetExpressionAndParameter<TRecord>(sort.FieldName, out LambdaExpression expression, out MemberExpression expressionParameter);

            queryable = (IQueryable<TRecord>)assistant.OrderBy(queryable, expression, expressionParameter, sort.Direction);
        }

        return (IOrderedQueryable<TRecord>)queryable;
    }

    /// <summary>
    /// Adds single-value filtering criteria to the IQueryable based on the specified DataGridRequest.
    /// Supports equality and comparison filter types.
    /// </summary>
    /// <typeparam name="TRecord">The type of records in the IQueryable.</typeparam>
    /// <param name="queryable">The IQueryable collection to filter.</param>
    /// <param name="request">The DataGridRequest containing filtering information.</param>
    /// <returns>An IQueryable collection with single-value filter criteria applied.</returns>
    public static IQueryable<TRecord> AddSingleValueWhereCriteria<TRecord>(this IQueryable<TRecord> queryable, DataGridRequest request)
        where TRecord : class, IDsRecord
    {
        foreach (FilterClause filter in request?.Filters?
            .Where((f) => f is not null && f.Value is not null &&
                (f.Value is string || f.Value is not IEnumerable) &&
                (f.FilterType == FilterType.Equals || f.FilterType == FilterType.GreaterThan ||
                f.FilterType == FilterType.GreaterThanOrEqualTo || f.FilterType == FilterType.LessThan ||
                f.FilterType == FilterType.LessThanOrEqualTo || f.FilterType == FilterType.NotEqualTo)))
        {
            queryable = queryable.AddSingleValueWhereCriteria(filter);
        }

        return queryable;
    }

    /// <summary>
    /// Adds multi-value filtering criteria to the given IQueryable based on the specified filter clause.
    /// </summary>
    /// <typeparam name="TRecord">The type of the records that implement the IDsRecord interface.</typeparam>
    /// <param name="queryable">The IQueryable to which the filtering criteria will be applied.</param>
    /// <param name="filter">The filter clause containing the field name and filter type.</param>
    /// <param name="values">A list of values to filter by, depending on the filter type.</param>
    /// <returns>An IQueryable containing only the records that match the multi-value filter criteria.</returns>
    private static IQueryable<TRecord> AddMultiValueWhereCriteria<TRecord>(this IQueryable<TRecord> queryable, FilterClause filter, List<object> values) where TRecord : class, IDsRecord =>
        queryable.Where(GetMultiValueWhereCriteria<TRecord>(filter, values));

    /// <summary>
    /// Adds single-value filtering criteria to the given IQueryable based on the specified filter clause.
    /// </summary>
    /// <typeparam name="TRecord">The type of the records that implement the IDsRecord interface.</typeparam>
    /// <param name="queryable">The IQueryable to which the filtering criteria will be applied.</param>
    /// <param name="filter">The filter clause containing the field name and filter type.</param>
    /// <returns>An IQueryable containing only the records that match the single-value filter criteria.</returns>
    private static IQueryable<TRecord> AddSingleValueWhereCriteria<TRecord>(this IQueryable<TRecord> queryable, FilterClause filter) where TRecord : class, IDsRecord =>
        queryable.Where(GetSingleValueWhereCriteria<TRecord>(filter));

    /// <summary>
    /// Retrieves the appropriate contains helper for the specified type and field name, creating 
    /// a new instance if necessary.
    /// </summary>
    /// <param name="type">The type of records.</param>
    /// <param name="fieldName">The name of the field to apply contains filtering.</param>
    /// <returns>An instance of IContainsHelper for the specified field.</returns>
    private static IContainsHelper GetContainsHelper(Type type, string fieldName)
    {
        PropertyDescriptorCollection props = TypeDescriptor.GetProperties(type);
        PropertyDescriptor prop = GetProperty(props, fieldName);

        if (_containsLookup.TryGetValue(type, out ConcurrentDictionary<string, IContainsHelper> fieldLookup))
        {
            if (fieldLookup.TryGetValue(fieldName, out IContainsHelper existingAssistant)) return existingAssistant;

            Type assistantType = typeof(ContainsHelper<,>).MakeGenericType(type, prop.PropertyType);

            IContainsHelper assistant = (IContainsHelper)Activator.CreateInstance(assistantType);

            fieldLookup.TryAdd(fieldName, assistant);
            return GetContainsHelper(type, fieldName);
        }
        else
        {
            _containsLookup.TryAdd(type, new ConcurrentDictionary<string, IContainsHelper>(StringComparer.InvariantCultureIgnoreCase));
            return GetContainsHelper(type, fieldName);
        }
    }

    /// <summary>
    /// Gets the expression and member expression for the specified field name of the specified type.
    /// </summary>
    /// <typeparam name="TRecord">The type of records.</typeparam>
    /// <param name="fieldName">The name of the field to get the expressions for.</param>
    /// <param name="expression">The lambda expression representing the field.</param>
    /// <param name="expressionParameter">The member expression representing the field.</param>
    private static void GetExpressionAndParameter<TRecord>(string fieldName, out LambdaExpression expression, out MemberExpression expressionParameter)
    {
        PropertyDescriptor _ = GetProperty(typeof(TRecord), fieldName) ?? throw new NotSupportedException();
        ParameterExpression parameter = Expression.Parameter(typeof(TRecord));
        expressionParameter = GetMemberExpression(parameter, fieldName);
        expression = Expression.Lambda(expressionParameter, parameter);
    }

    /// <summary>
    /// Creates a member expression for the specified property name based on the provided parameter.
    /// Supports nested property names separated by dots.
    /// </summary>
    /// <param name="parameter">The parameter expression to use as the root.</param>
    /// <param name="propName">The property name to create the expression for.</param>
    /// <returns>A MemberExpression representing the specified property.</returns>
    private static MemberExpression GetMemberExpression(ParameterExpression parameter, string propName)
    {
        if (string.IsNullOrEmpty(propName)) return null;

        var propertiesName = propName.Split('.');
        if (propertiesName.Length == 2) return Expression.Property(Expression.Property(parameter, propertiesName[0]), propertiesName[1]);

        return Expression.Property(parameter, propName);
    }

    /// <summary>
    /// Constructs a lambda expression representing a multi-value filtering criteria for a specified field.
    /// This method supports two filter types: 'Between' and 'In'.
    /// </summary>
    /// <typeparam name="TRecord">The type of the records that implement the IDsRecord interface.</typeparam>
    /// <param name="filter">The filter clause containing the field name and filter type.</param>
    /// <param name="values">A list of values to filter by, depending on the filter type.</param>
    /// <returns>A lambda expression that can be used in a LINQ query to filter records.</returns>
    /// <exception cref="NotSupportedException">Thrown if the specified field is not found or if an unsupported filter type is provided.</exception>
    private static Expression<Func<TRecord, bool>> GetMultiValueWhereCriteria<TRecord>(FilterClause filter, List<object> values)
        where TRecord : class, IDsRecord
    {
        PropertyDescriptor prop = GetProperty(typeof(TRecord), filter.FieldName);
        ParameterExpression parameter = Expression.Parameter(typeof(TRecord));
        MemberExpression expressionParameter = GetMemberExpression(parameter, filter.FieldName);

        if (prop != null)
        {
            BinaryExpression body = null;

            switch (filter.FilterType)
            {
                case FilterType.Between:
                    body = Expression.And(
                        Expression.GreaterThanOrEqual(expressionParameter, Expression.Constant(values[0], prop.PropertyType)),
                        Expression.LessThanOrEqual(expressionParameter, Expression.Constant(values[1], prop.PropertyType)));

                    break;

                case FilterType.In:
                    foreach (object value in values)
                    {
                        if (body is null)
                        {
                            body = Expression.Equal(expressionParameter, Expression.Constant(value, prop.PropertyType));
                        }
                        else
                        {
                            body = Expression.Or(body, Expression.Equal(expressionParameter, Expression.Constant(value, prop.PropertyType)));
                        }
                    }

                    break;

                default:

                    throw new NotSupportedException();
            }

            if (body is null) throw new NotSupportedException();
            return Expression.Lambda<Func<TRecord, bool>>(body, parameter);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Retrieves the appropriate order by helper for the specified type and field name, creating 
    /// a new instance if necessary.
    /// </summary>
    /// <param name="type">The type of records.</param>
    /// <param name="fieldName">The name of the field to apply ordering.</param>
    /// <returns>An instance of IOrderByHelper for the specified field.</returns>
    private static IOrderByHelper GetOrderByHelper(Type type, string fieldName)
    {
        if (_orderByAssistantLookup.TryGetValue(type, out ConcurrentDictionary<string, IOrderByHelper> fieldLookup))
        {
            if (fieldLookup.TryGetValue(fieldName, out IOrderByHelper existingAssistant)) return existingAssistant;

            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(type);
            PropertyDescriptor prop = GetProperty(props, fieldName);

            Type assistantType = typeof(OrderByHelper<,>).MakeGenericType(type, prop.PropertyType);

            IOrderByHelper assistant = (IOrderByHelper)Activator.CreateInstance(assistantType);

            fieldLookup.TryAdd(fieldName, assistant);
            return GetOrderByHelper(type, fieldName);
        }
        else
        {
            _orderByAssistantLookup.TryAdd(type, new ConcurrentDictionary<string, IOrderByHelper>(StringComparer.InvariantCultureIgnoreCase));
            return GetOrderByHelper(type, fieldName);
        }
    }

    /// <summary>
    /// Retrieves the property descriptor for the specified property name from the collection.
    /// </summary>
    /// <param name="props">The property descriptor collection.</param>
    /// <param name="fieldName">The name of the property to find.</param>
    /// <returns>The PropertyDescriptor for the specified property name, or null if not found.</returns>
    private static PropertyDescriptor GetProperty(Type type, string fieldName)
    {
        if (_propertyLookup.TryGetValue(type, out ConcurrentDictionary<string, PropertyDescriptor> fieldLookup))
        {
            if (fieldLookup.TryGetValue(fieldName, out PropertyDescriptor property)) return property;

            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(type);
            PropertyDescriptor prop = GetProperty(props, fieldName);

            fieldLookup.TryAdd(fieldName, prop);
            return GetProperty(type, fieldName);
        }
        else
        {
            _propertyLookup.TryAdd(type, new ConcurrentDictionary<string, PropertyDescriptor>(StringComparer.InvariantCultureIgnoreCase));
            return GetProperty(type, fieldName);
        }
    }

    /// <summary>
    /// Retrieves the PropertyDescriptor from the given PropertyDescriptorCollection based on the specified field name.
    /// </summary>
    /// <param name="props">The collection of properties from which to find the specified property.</param>
    /// <param name="fieldName">The name of the field/property to find.</param>
    /// <returns>The PropertyDescriptor for the specified field, or null if not found.</returns>
    private static PropertyDescriptor GetProperty(PropertyDescriptorCollection props, string fieldName)
    {
        if (!fieldName.Contains('.')) return props.Find(fieldName, true);

        string[] fieldNameProperty = fieldName.Split('.');
        return props.Find(fieldNameProperty[0], true).GetChildProperties().Find(fieldNameProperty[1], true);
    }

    /// <summary>
    /// Constructs a predicate expression for filtering records based on a single value criteria from the given filter clause.
    /// </summary>
    /// <typeparam name="TRecord">The type of the records that implement the IDsRecord interface.</typeparam>
    /// <param name="filter">The filter clause containing the field name and value for filtering.</param>
    /// <returns>A lambda expression that can be used to filter records based on the specified criteria.</returns>
    /// <exception cref="NotSupportedException">Thrown if the property or value is not valid</exception>
    private static Expression<Func<TRecord, bool>> GetSingleValueWhereCriteria<TRecord>(FilterClause filter)
        where TRecord : class, IDsRecord
    {
        PropertyDescriptor prop = GetProperty(typeof(TRecord), filter.FieldName);
        if (prop is null || filter.Value is null) throw new NotSupportedException();

        ParameterExpression parameter = Expression.Parameter(typeof(TRecord));
        MemberExpression expressionParameter = GetMemberExpression(parameter, filter.FieldName);
        BinaryExpression body = filter.FilterType switch
        {
            FilterType.Equals => Expression.Equal(expressionParameter, Expression.Constant(filter.Value, prop.PropertyType)),
            FilterType.GreaterThan => CreateGreaterThanExpression(expressionParameter, Expression.Constant(filter.Value, prop.PropertyType)),
            FilterType.GreaterThanOrEqualTo => CreateGreaterThanOrEqualExpression(expressionParameter, Expression.Constant(filter.Value, prop.PropertyType)),
            FilterType.LessThan => CreateLessThanExpression(expressionParameter, Expression.Constant(filter.Value, prop.PropertyType)),
            FilterType.LessThanOrEqualTo => CreateLessThanOrEqualExpression(expressionParameter, Expression.Constant(filter.Value, prop.PropertyType)),
            FilterType.NotEqualTo => Expression.NotEqual(expressionParameter, Expression.Constant(filter.Value, prop.PropertyType)),
            _ => throw new NotSupportedException(),
        };
        return Expression.Lambda<Func<TRecord, bool>>(body, parameter);
    }

    /// <summary>
    /// Creates a binary expression representing a greater-than comparison for the given member expression and right-hand side value.
    /// </summary>
    /// <param name="expressionParameter">The member expression representing the left-hand side of the comparison.</param>
    /// <param name="right">The right-hand side expression to compare against.</param>
    /// <returns>A binary expression representing the greater-than comparison.</returns>
    /// <exception cref="NotSupportedException">Thrown if the type is unsupported</exception>
    private static BinaryExpression CreateGreaterThanExpression(MemberExpression expressionParameter, Expression right)
    {
        if (right.Type == typeof(string))
        {
            var compareMethod = typeof(string).GetMethod("Compare", new[] { typeof(string), typeof(string) });
            var stringComparisonExpression = Expression.Call(compareMethod, expressionParameter, right);
            return Expression.GreaterThan(stringComparisonExpression, Expression.Constant(0));
        }
        else if (right.Type == typeof(bool))
        {
            return Expression.GreaterThan(Expression.Convert(expressionParameter, typeof(int)), Expression.Convert(right, typeof(int)));
        }
        else if (right.Type.IsValueType && right.Type != typeof(string))
        {
            return Expression.GreaterThan(expressionParameter, right);
        }

        throw new NotSupportedException($"GreaterThan comparison is not supported for type {right.Type.Name}");
    }

    /// <summary>
    /// Creates a binary expression representing a greater-than-or-equal-to comparison for the given member expression and right-hand side value.
    /// </summary>
    /// <param name="expressionParameter">The member expression representing the left-hand side of the comparison.</param>
    /// <param name="right">The right-hand side expression to compare against.</param>
    /// <returns>A binary expression representing the greater-than comparison.</returns>
    /// <exception cref="NotSupportedException">Thrown if the type is unsupported</exception>
    private static BinaryExpression CreateGreaterThanOrEqualExpression(MemberExpression expressionParameter, Expression right)
    {
        if (right.Type == typeof(string))
        {
            var compareMethod = typeof(string).GetMethod("Compare", new[] { typeof(string), typeof(string) });
            var stringComparisonExpression = Expression.Call(compareMethod, expressionParameter, right);
            return Expression.GreaterThanOrEqual(stringComparisonExpression, Expression.Constant(0));
        }
        else if (right.Type == typeof(bool))
        {
            return Expression.GreaterThanOrEqual(Expression.Convert(expressionParameter, typeof(int)), Expression.Convert(right, typeof(int)));
        }
        else if (right.Type.IsValueType && right.Type != typeof(string))
        {
            return Expression.GreaterThanOrEqual(expressionParameter, right);
        }

        throw new NotSupportedException($"GreaterThan comparison is not supported for type {right.Type.Name}");
    }

    /// <summary>
    /// Creates a binary expression representing a less-than comparison for the given member expression and right-hand side value.
    /// </summary>
    /// <param name="expressionParameter">The member expression representing the left-hand side of the comparison.</param>
    /// <param name="right">The right-hand side expression to compare against.</param>
    /// <returns>A binary expression representing the greater-than comparison.</returns>
    /// <exception cref="NotSupportedException">Thrown if the type is unsupported</exception>
    private static BinaryExpression CreateLessThanExpression(MemberExpression expressionParameter, Expression right)
    {
        if (right.Type == typeof(string))
        {
            var compareMethod = typeof(string).GetMethod("Compare", new[] { typeof(string), typeof(string) });
            var stringComparisonExpression = Expression.Call(compareMethod, expressionParameter, right);
            return Expression.LessThan(stringComparisonExpression, Expression.Constant(0));
        }
        else if (right.Type == typeof(bool))
        {
            return Expression.LessThan(Expression.Convert(expressionParameter, typeof(int)), Expression.Convert(right, typeof(int)));
        }
        else if (right.Type.IsValueType && right.Type != typeof(string))
        {
            return Expression.LessThan(expressionParameter, right);
        }

        throw new NotSupportedException($"GreaterThan comparison is not supported for type {right.Type.Name}");
    }

    /// <summary>
    /// Creates a binary expression representing a less-than-or-equal-to comparison for the given member expression and right-hand side value.
    /// </summary>
    /// <param name="expressionParameter">The member expression representing the left-hand side of the comparison.</param>
    /// <param name="right">The right-hand side expression to compare against.</param>
    /// <returns>A binary expression representing the greater-than comparison.</returns>
    /// <exception cref="NotSupportedException">Thrown if the type is unsupported</exception>
    private static BinaryExpression CreateLessThanOrEqualExpression(MemberExpression expressionParameter, Expression right)
    {
        if (right.Type == typeof(string))
        {
            var compareMethod = typeof(string).GetMethod("Compare", new[] { typeof(string), typeof(string) });
            var stringComparisonExpression = Expression.Call(compareMethod, expressionParameter, right);
            return Expression.LessThanOrEqual(stringComparisonExpression, Expression.Constant(0));
        }
        else if (right.Type == typeof(bool))
        {
            return Expression.LessThanOrEqual(Expression.Convert(expressionParameter, typeof(int)), Expression.Convert(right, typeof(int)));
        }
        else if (right.Type.IsValueType && right.Type != typeof(string))
        {
            return Expression.LessThanOrEqual(expressionParameter, right);
        }

        throw new NotSupportedException($"GreaterThan comparison is not supported for type {right.Type.Name}");
    }
}