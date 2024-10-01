using System;

namespace DocumentStorage.Domain.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class DsReferenceAttribute : Attribute
{
    private string _referenceProperty;

    /// <summary>
    /// Used to populate reference property based on name and id
    /// </summary>
    public DsReferenceAttribute() { }

    /// <summary>
    /// Creates a <see cref="DsReferenceAttribute" /> instance for a property with the given name.
    /// </summary>
    /// <param name="referenceProperty">The index name.</param>
    public DsReferenceAttribute(string referenceProperty)
    {
        if (string.IsNullOrEmpty(referenceProperty)) throw new ArgumentNullException(nameof(referenceProperty));

        _referenceProperty = referenceProperty;
    }

    /// <summary>
    /// The reference property name.
    /// </summary>
    public virtual string ReferenceProperty
    {
        get => _referenceProperty;
        internal set
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

            _referenceProperty = value;
        }
    }

    /// <summary>
    /// Returns true if this attribute specifies the same name and configuration as the given attribute.
    /// </summary>
    /// <param name="other">The attribute to compare.</param>
    /// <returns>True if the other object is equal to this object; otherwise false.</returns>
    protected virtual bool Equals(DsReferenceAttribute other) => _referenceProperty == other._referenceProperty;

    /// <summary>
    /// Returns true if this attribute specifies the same name and configuration as the given attribute.
    /// </summary>
    /// <param name="obj">The attribute to compare.</param>
    /// <returns>True if the other object is equal to this object; otherwise false.</returns>
    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;

        return Equals((DsReferenceAttribute)obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = base.GetHashCode();
            hashCode = hashCode * 397 ^ (_referenceProperty != null ? _referenceProperty.GetHashCode() : 0);
            return hashCode;
        }
    }
}
