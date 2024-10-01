using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DocumentStorage.Infrastructure;

/// <summary>
/// Provides a caching mechanism for retrieving property information of types to improve performance 
/// when frequently accessing property metadata using reflection.
/// </summary>
public static class PropertyInfoCache
{
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, PropertyInfo>> _propertyCache = new();

    /// <summary>
    /// Retrieves a cached dictionary of property information for the specified type. If the type's properties are not yet cached, 
    /// they are retrieved using reflection, cached, and then returned.
    /// </summary>
    /// <param name="type">The type whose properties are to be retrieved and cached.</param>
    /// <returns>A thread-safe dictionary containing property names and their corresponding <see cref="PropertyInfo"/> objects for the specified type.</returns>
    public static ConcurrentDictionary<string, PropertyInfo> GetProperties(Type type)
    {
        if (_propertyCache.TryGetValue(type, out ConcurrentDictionary<string, PropertyInfo> existingProperties)) return existingProperties;

        IEnumerable<PropertyInfo> properties = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty)
            .Where(p => p.CanRead && p.CanWrite);

        ConcurrentDictionary<string, PropertyInfo> propertyLookup = new();

        foreach (PropertyInfo property in properties)
        {
            propertyLookup.TryAdd(property.Name, property);
        }

        _propertyCache.TryAdd(type, propertyLookup);

        return propertyLookup;
    }
}
