using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace DocumentStorage.DataGrid;

/// <summary>
/// Represents a filter clause used in data queries, specifying the field to filter, 
/// the filter type, and the value to filter by.
/// </summary>
public class FilterClause
{
    /// <summary>
    /// Gets or sets the name of the field being filtered.
    /// </summary>
    public string FieldName { get; set; }

    /// <summary>
    /// Gets or sets the JSON string representation of the filter value.
    /// </summary>
    public string ValueJsonStr { get; set; }

    /// <summary>
    /// Gets or sets the type of filter operation (e.g., Equals, Contains, In).
    /// </summary>
    public FilterType FilterType { get; set; }

    /// <summary>
    /// Gets or sets the filter value. The value is serialized or deserialized to and from JSON as needed.
    /// </summary>
    public virtual object Value
    {
        get => ValueJsonStr != null ? JsonSerializer.Deserialize<object>(ValueJsonStr) : null;
        set => ValueJsonStr = value != null ? JsonSerializer.Serialize(value) : null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterClause"/> class.
    /// </summary>
    public FilterClause() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterClause"/> class with specified parameters.
    /// </summary>
    /// <param name="fieldName">The field to filter by.</param>
    /// <param name="type">The type of filter.</param>
    /// <param name="value">The value for filtering.</param>
    public FilterClause(string fieldName, FilterType type, object value)
    {
        FieldName = fieldName;
        FilterType = type;
        Value = value;
    }
}

/// <summary>
/// Represents a typed filter clause for a specific type of record, providing 
/// additional handling for complex data types and collections.
/// </summary>
/// <typeparam name="T">The type of the record being filtered.</typeparam>
public class FilterClause<T> : FilterClause
{
    /// <summary>
    /// A lazy-loaded dictionary mapping field names to their enumerable field types.
    /// </summary>
    private static readonly Lazy<ConcurrentDictionary<string, Type>> _recordEnumerableFieldTypes =
        new(() =>
        {
            ConcurrentDictionary<string, Type> recordEnumerableFieldTypes = new(StringComparer.InvariantCultureIgnoreCase);

            foreach (KeyValuePair<string, Type> keyValue in _recordFieldTypes.Value)
            {
                recordEnumerableFieldTypes.TryAdd(keyValue.Key, typeof(IEnumerable<>).MakeGenericType(keyValue.Value));
            }

            return recordEnumerableFieldTypes;
        }, true);

    /// <summary>
    /// A lazy-loaded dictionary mapping field names to their types for the given record type <typeparamref name="T"/>.
    /// </summary>
    private static readonly Lazy<ConcurrentDictionary<string, Type>> _recordFieldTypes =
        new(() =>
        {
            ConcurrentDictionary<string, Type> recordFieldTypes = new(StringComparer.InvariantCultureIgnoreCase);

            AddProperties(typeof(T));

            void AddProperties(Type typeToAdd, string fieldNamePrefix = null, HashSet<Type> parentTypes = null)
            {
                fieldNamePrefix ??= string.Empty;
                parentTypes ??= new HashSet<Type>();

                HashSet<Type> childParentTypes = new(parentTypes)
                {
                    typeToAdd
                };

                foreach (PropertyInfo propInfo in typeToAdd.GetProperties())
                {
                    if (propInfo.PropertyType is { IsValueType: false, IsAbstract: false, IsClass: true } &&
                        !typeof(IEnumerable).IsAssignableFrom(propInfo.PropertyType))
                    {
                        if (parentTypes.Contains(propInfo.PropertyType))
                        {
                            throw new NotSupportedException($"Self referencing type: '{propInfo.PropertyType.Name}' is not supported.");
                        }

                        AddProperties(propInfo.PropertyType, fieldNamePrefix + $"{propInfo.Name}.", childParentTypes);
                    }
                    else
                    {
                        recordFieldTypes.TryAdd(fieldNamePrefix + propInfo.Name, propInfo.PropertyType);
                    }
                }
            }

            return recordFieldTypes;
        }, true);

    /// <summary>
    /// Gets the data type of the field being filtered.
    /// </summary>
    public Type FieldType { get; }

    /// <inheritdoc/>
    public override object Value => ValueJsonStr != null ? JsonSerializer.Deserialize(ValueJsonStr, FieldType) : null;

    /// <inheritdoc/>
    public FilterClause(string fieldName, FilterType type, object value)
        : this(new FilterClause(fieldName, type, value)) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterClause{T}"/> class from an existing filter clause.
    /// </summary>
    /// <param name="clause">The base filter clause to copy from.</param>
    public FilterClause(FilterClause clause)
        : base(clause.FieldName, clause.FilterType, clause.Value)
    {
        ValueJsonStr = clause?.ValueJsonStr;

        if (clause.FilterType is FilterType.In or FilterType.Between)
        {
            if (_recordEnumerableFieldTypes.Value.TryGetValue(FieldName, out Type fieldType))
            {
                FieldType = fieldType;
            }
            else
            {
                FieldType = typeof(object);
            }
        }
        else
        {
            if (_recordFieldTypes.Value.TryGetValue(FieldName, out Type fieldType))
            {
                FieldType = fieldType;
            }
            else
            {
                FieldType = typeof(object);
            }
        }
    }
}
