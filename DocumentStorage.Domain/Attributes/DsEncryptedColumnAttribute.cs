using System;

namespace DocumentStorage.Domain.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class DsEncryptedColumnAttribute : Attribute
{
    private bool _searchable;

    /// <summary>
    /// Creates a <see cref="DsEncryptedColumnAttribute"/> instance for a property.
    /// </summary>
    /// <param name="searchable">Optional: Changes the type of encryption if the field needs to be searchable</param>
    public DsEncryptedColumnAttribute(bool searchable = false) => _searchable = searchable;

    /// <summary>
    /// The reference property name.
    /// </summary>
    public virtual bool Searchable
    {
        get => _searchable;
        internal set
        {
            _searchable = value;
        }
    }

    /// <summary>
    /// Returns true if this attribute specifies the same name and configuration as the given attribute.
    /// </summary>
    /// <param name="other">The attribute to compare.</param>
    /// <returns>True if the other object is equal to this object; otherwise false.</returns>
    protected virtual bool Equals(DsEncryptedColumnAttribute other) => _searchable == other._searchable;

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

        return Equals((DsEncryptedColumnAttribute)obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = base.GetHashCode();
            hashCode = hashCode * 397 ^ (_searchable.GetHashCode());
            return hashCode;
        }
    }
}