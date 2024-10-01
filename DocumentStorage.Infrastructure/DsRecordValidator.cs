using DocumentStorage.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace DocumentStorage.Infrastructure;

/// <summary>
/// Provides validation utilities for objects implementing the <see cref="IDsRecord"/> interface, 
/// including recursive validation of nested or enumerable records.
/// </summary>
public static class DsRecordValidator
{
    /// <summary>
    /// Retrieves the type of child records in an enumerable collection. This method validates whether the 
    /// provided type is a collection and whether its elements implement <see cref="IDsRecord"/>.
    /// </summary>
    /// <param name="enumerableType">The enumerable type to examine.</param>
    /// <returns>The type of the child records within the collection.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the provided type is not enumerable, does not have exactly one generic argument, 
    /// does not implement <see cref="IList"/>, or the elements do not implement <see cref="IDsRecord"/>.
    /// </exception>
    public static Type GetChildDsRecordType(Type enumerableType)
    {
        if (!typeof(IEnumerable).IsAssignableFrom(enumerableType))
        {
            throw new InvalidOperationException($"Unable to update property of type '{enumerableType.Name}'. It is not enumerable.");
        }

        if (enumerableType.GenericTypeArguments.Length != 1)
        {
            throw new InvalidOperationException($"Unable to update property of type '{enumerableType.Name}'. Collections must have exactly 1 generic type argument.");
        }

        if (!typeof(IList).IsAssignableFrom(enumerableType))
        {
            throw new InvalidOperationException($"Unable to update property of type '{enumerableType.Name}'. Collections must implement '{nameof(IList)}'.");
        }

        Type childType = enumerableType.GetGenericArguments()[0];
        if (!typeof(IDsRecord).IsAssignableFrom(childType))
        {
            throw new InvalidOperationException($"Unable to update property of type '{enumerableType.Name}'.  Objects in collections must implement '{nameof(IDsRecord)}'.");
        }

        return childType;
    }

    /// <summary>
    /// Recursively validates the provided record and any nested or enumerable records it contains. 
    /// Validation errors are added to the provided list of <see cref="ValidationResult"/>.
    /// </summary>
    /// <param name="record">The record to validate.</param>
    /// <param name="results">A list to collect validation errors.</param>
    /// <returns>Returns true if the record is valid; otherwise, returns false.</returns>
    public static bool TryValidateObjectRecursive(IDsRecord record, List<ValidationResult> results)
    {
        if (record == null) return true;

        Type recordType = record.GetType();
        IEnumerable<PropertyInfo> properties = PropertyInfoCache.GetProperties(recordType).Values;

        foreach (PropertyInfo property in properties)
        {
            Type propertyType = property.PropertyType;
            if (typeof(IDsRecord).IsAssignableFrom(propertyType))
            {
                TryValidateObjectRecursive(property.GetValue(record) as IDsRecord, results);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string))
            {
                if (property.GetValue(record) is IEnumerable children)
                {
                    GetChildDsRecordType(propertyType);

                    foreach (object child in children)
                    {
                        TryValidateObjectRecursive(child as IDsRecord, results);
                    }
                }
            }
        }

        List<ValidationResult> scopedResults = new();
        Validator.TryValidateObject(record, new ValidationContext(record), scopedResults, true);

        foreach (ValidationResult result in scopedResults)
        {
            if (!string.IsNullOrEmpty(result?.ErrorMessage))
            {
                results.Add(new ValidationResult($"({recordType.Name}) {result?.ErrorMessage}"));
            }
        }

        results.Reverse();

        return results.Count < 1;
    }
}