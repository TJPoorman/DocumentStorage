using DocumentStorage.Domain;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace DocumentStorage.Infrastructure.LiteDb;

/// <inheritdoc/>
/// <remarks>Provides functionality to update and manage entities in a given LiteDb context.</remarks>
public class EntityUpdater : IDsEntityUpdater
{
    private readonly ConcurrentDictionary<Type, ObjectActivator> _objectActivators = new();
    private readonly ConcurrentDictionary<Type, ObjectActivator> _listActivators = new();

    /// <summary>
    /// Delegate for creating instances of objects.
    /// </summary>
    protected delegate object ObjectActivator();

    /// <summary>
    /// Converts a value from one type to another if needed, taking care of specific cases for entities and collections.
    /// </summary>
    /// <param name="sourceType">The type of the source value.</param>
    /// <param name="sourceValue">The source value to convert.</param>
    /// <param name="targetType">The type to convert the value to.</param>
    /// <returns>The converted value, or the original value if no conversion is needed.</returns>
    /// <exception cref="InvalidOperationException">Thrown if provided sourceType is incompatible with the targetType</exception>
    protected object ConvertValueIfNeeded(Type sourceType, object sourceValue, Type targetType)
    {
        if (sourceType == targetType || sourceValue == null) return sourceValue;

        object targetValue = GetSingleObjectActivator(targetType);

        if (typeof(IDsRecord).IsAssignableFrom(sourceType) && typeof(IDsRecord).IsAssignableFrom(targetType))
        {
            IDsRecord target = GetSingleObjectActivator(targetType)() as IDsRecord;

            Update(sourceType, sourceValue, targetType, target);

            return target;
        }
        else if (typeof(IEnumerable).IsAssignableFrom(targetType) && targetType != typeof(string))
        {
            Type targetChildType = DsRecordValidator.GetChildDsRecordType(targetType);
            Type sourceChildType = DsRecordValidator.GetChildDsRecordType(sourceType);

            IEnumerable enumerableSourceValue = sourceValue as IEnumerable;
            if (targetChildType != sourceChildType)
            {
                IList convertedChildren = GetListObjectActivator(targetChildType)() as IList;

                foreach (IDsRecord sourceChild in enumerableSourceValue)
                {
                    convertedChildren.Add(ConvertValueIfNeeded(sourceChildType, sourceChild, targetChildType) as IDsRecord);
                }

                enumerableSourceValue = convertedChildren;
            }

            return enumerableSourceValue;
        }
        else
        {
            throw new InvalidOperationException($"Unable to perform update because type {sourceType.Name} is not compatible with {targetType.Name}");
        }
    }

    /// <summary>
    /// Marks the specified record for deletion within the DbContext.
    /// </summary>
    /// <param name="record">The record to be deleted.</param>
    protected virtual void Delete(IDsRecord record) { }

    /// <summary>
    /// Creates an activator for the specified type using reflection.
    /// </summary>
    /// <param name="type">The type for which to create an activator.</param>
    /// <returns>An activator delegate that creates instances of the specified type.</returns>
    protected virtual ObjectActivator GetObjectActivator(Type type)
    {
        ConstructorInfo emptyConstructor = type.GetConstructor(Type.EmptyTypes);
        DynamicMethod dynamicMethod = new DynamicMethod("CreateInstance", type, Type.EmptyTypes, true);
        ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
        ilGenerator.Emit(OpCodes.Nop);
        ilGenerator.Emit(OpCodes.Newobj, emptyConstructor);
        ilGenerator.Emit(OpCodes.Ret);

        return (ObjectActivator)dynamicMethod.CreateDelegate(typeof(ObjectActivator));
    }

    /// <summary>
    /// Retrieves an activator for a list of the specified type.
    /// </summary>
    /// <param name="type">The type of the list items.</param>
    /// <returns>An activator for creating lists of the specified type.</returns>
    protected ObjectActivator GetListObjectActivator(Type type)
    {
        if (_listActivators.TryGetValue(type, out ObjectActivator activator)) return activator;

        Type genericCollectionType = typeof(List<>);
        Type collectionType = genericCollectionType.MakeGenericType(type);

        activator = GetSingleObjectActivator(collectionType);

        _listActivators.TryAdd(type, activator);

        return GetListObjectActivator(type);
    }

    /// <summary>
    /// Retrieves an activator for a single object of the specified type.
    /// </summary>
    /// <param name="type">The type of the object.</param>
    /// <returns>An activator for creating a single instance of the specified type.</returns>
    protected ObjectActivator GetSingleObjectActivator(Type type)
    {
        if (_objectActivators.TryGetValue(type, out ObjectActivator activator)) return activator;

        _objectActivators.TryAdd(type, GetObjectActivator(type));

        return GetSingleObjectActivator(type);
    }

    /// <summary>
    /// Determines whether the specified property should be ignored during updates.
    /// </summary>
    /// <param name="property">The property to check.</param>
    /// <returns>true if the property should be ignored; otherwise, false.</returns>
    protected bool IsPropertyToIgnore(PropertyInfo property) => property.Name.Equals(nameof(IDsRecord.Id));

    /// <inheritdoc/>
    public void Remove(IDsRecord record) => Remove(record.GetType(), record);

    /// <summary>
    /// Removes a record of the specified type from the context.
    /// </summary>
    /// <param name="recordType">The type of the record to remove.</param>
    /// <param name="record">The record instance to remove.</param>
    protected void Remove(Type recordType, IDsRecord record)
    {
        if (record == null || !ShouldRemove(recordType, record)) return;

        IEnumerable<PropertyInfo> properties = PropertyInfoCache.GetProperties(recordType).Values;

        try
        {
            foreach (PropertyInfo property in properties)
            {
                Type propertyType = property.PropertyType;

                if (typeof(IDsRecord).IsAssignableFrom(propertyType))
                {
                    Remove(propertyType, property.GetValue(record) as IDsRecord);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string) && propertyType != typeof(byte[]))
                {
                    Type childType = DsRecordValidator.GetChildDsRecordType(propertyType);

                    IList childrenList = property.GetValue(record) as IList;
                    foreach (var child in childrenList)
                    {
                        if (child is IDsRecord childRecord)
                        {
                            Remove(childType, childRecord);
                        }
                    }
                }
            }

            Delete(record);
        }
        finally { }
    }

    /// <summary>
    /// Determines whether the specified record can be removed.
    /// </summary>
    /// <param name="recordType">The type of the record.</param>
    /// <param name="record">The record instance.</param>
    /// <returns>true if the record can be removed; otherwise, false.</returns>
    protected virtual bool ShouldRemove(Type recordType, IDsRecord record) => true;

    /// <inheritdoc/>
    public void Update<TSource, TTarget>(TTarget source, TSource target) where TSource : IDsDbRecord where TTarget : IDsDbRecord =>
        Update(typeof(TSource), source, typeof(TTarget), target);

    /// <summary>
    /// Updates the specified target entity using values from the specified source entity.
    /// </summary>
    /// <param name="sourceType">The type of the source entity.</param>
    /// <param name="source">The source entity.</param>
    /// <param name="targetType">The type of the target entity.</param>
    /// <param name="target">The target entity.</param>
    protected void Update(Type sourceType, object source, Type targetType, object target)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        ConcurrentDictionary<string, PropertyInfo> sourceProperties = PropertyInfoCache.GetProperties(sourceType);
        ConcurrentDictionary<string, PropertyInfo> targetProperties = PropertyInfoCache.GetProperties(targetType);

        try
        {
            if (source is IDsRecord sourceDsRecord)
            {
                if (target is IDsRecord targetDsRecord)
                {
                    UpdateIdIfDifferent(targetDsRecord, sourceDsRecord.Id);
                }
            }

            foreach (PropertyInfo sourceProperty in sourceProperties.Values)
            {
                if (IsPropertyToIgnore(sourceProperty) || !targetProperties.TryGetValue(sourceProperty.Name, out PropertyInfo targetProperty)) continue;

                UpdateProperty(sourceProperty, sourceProperty.GetValue(source), targetProperty, target, targetProperty.GetValue(target));
            }
        }
        finally { }
    }

    /// <summary>
    /// Updates the ID of the specified record if it differs from the provided ID.
    /// </summary>
    /// <param name="record">The record whose ID to update.</param>
    /// <param name="id">The new ID.</param>
    protected void UpdateIdIfDifferent(IDsRecord record, Guid id) => record.Id = record.Id != id ? id : record.Id;

    /// <summary>
    /// Updates a specific property on the target entity with a value from the source entity.
    /// Handles null values and collections appropriately.
    /// </summary>
    /// <param name="sourceProperty">The source property information.</param>
    /// <param name="sourceValue">The value from the source entity.</param>
    /// <param name="targetProperty">The target property information.</param>
    /// <param name="target">The target entity.</param>
    /// <param name="targetValue">The current value on the target entity.</param>
    protected void UpdateProperty(PropertyInfo sourceProperty, object sourceValue, PropertyInfo targetProperty, object target, object targetValue)
    {
        if (target == null) return;
        if (targetValue == null && sourceValue == null) return;

        Type targetPropertyType = targetProperty.PropertyType;

        if (targetValue == null && sourceValue != null)
        {
            object value = ConvertValueIfNeeded(sourceProperty.PropertyType, sourceValue, targetProperty.PropertyType);

            targetProperty.SetValue(target, value);

            return;
        }

        if (targetValue != null && sourceValue == null)
        {
            targetProperty.SetValue(target, null);

            Remove(targetPropertyType, targetValue as IDsRecord);

            return;
        }

        if (targetValue is IDsRecord targetDsRecord && sourceValue is IDsRecord sourceDsRecord && targetDsRecord.Id != sourceDsRecord.Id)
        {
            object value = ConvertValueIfNeeded(sourceProperty.PropertyType, sourceValue, targetProperty.PropertyType);

            targetProperty.SetValue(target, value);

            Remove(targetPropertyType, targetDsRecord);

            return;
        }

        if (typeof(IDsRecord).IsAssignableFrom(targetPropertyType))
        {
            Update(sourceValue?.GetType() ?? typeof(object), sourceValue, targetPropertyType, targetValue);
        }
        else if (typeof(IEnumerable).IsAssignableFrom(targetPropertyType) && targetPropertyType != typeof(string))
        {
            Type targetChildType = DsRecordValidator.GetChildDsRecordType(targetPropertyType);
            Type sourceChildType = DsRecordValidator.GetChildDsRecordType(sourceProperty.PropertyType);

            IEnumerable enumerableSourceValue = ConvertValueIfNeeded(sourceProperty.PropertyType, sourceValue, targetPropertyType) as IEnumerable;

            Dictionary<Guid, IDsRecord> sourceChildren = CollectionToLookup(enumerableSourceValue);

            IList targetList = targetValue as IList;

            // Loop through all target items. 
            for (int index = 0; index < targetList.Count; index++)
            {
                if (targetList[index] is IDsRecord targetChild)
                {
                    if (sourceChildren.ContainsKey(targetChild.Id))
                    {
                        // Update target items that also exist in source.
                        Update(sourceChildren[targetChild.Id]?.GetType() ?? typeof(object), sourceChildren[targetChild.Id], targetChildType, targetChild);

                        // Remove used source items.
                        sourceChildren.Remove(targetChild.Id);
                    }
                    else
                    {
                        // Remove target items that don't exist in source.
                        Remove(targetChildType, targetChild);
                    }
                }
                else
                {
                    // Remove null items.
                    targetList.Remove(index--);
                }
            }

            // Add remaining target items to target.
            foreach (IDsRecord sourceChild in sourceChildren.Values)
            {
                targetList.Add(sourceChild);
            }

            Dictionary<Guid, IDsRecord> CollectionToLookup(IEnumerable collection)
            {
                Dictionary<Guid, IDsRecord> lookup = new Dictionary<Guid, IDsRecord>();

                foreach (object child in collection)
                {
                    if (child is IDsRecord childRecord)
                    {
                        lookup.TryAdd(childRecord.Id, childRecord);
                    }
                }

                return lookup;
            }
        }
        else
        {
            targetProperty.SetValue(target, sourceValue);
        }
    }
}
