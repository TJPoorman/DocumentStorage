using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace DocumentStorage.Domain.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class DsCreateIndexAttribute : Attribute
{
    private string _name;
    private int _order = -1;
    private string[] _includeColumns;

    /// <summary>
    /// Creates a <see cref="DsCreateIndexAttribute" /> instance for an index that will be named by convention and
    /// has no column order, or include column(s) specified.
    /// </summary>
    public DsCreateIndexAttribute() { }

    /// <summary>
    /// Creates a <see cref="DsCreateIndexAttribute" /> instance for an index with the given name and
    /// has no column order, or include column(s) specified.
    /// </summary>
    /// <param name="name">The index name.</param>
    public DsCreateIndexAttribute(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

        _name = name;
    }

    /// <summary>
    /// Creates a <see cref="DsCreateIndexAttribute" /> instance for an index with the given name and column order, 
    /// but with no include column(s) specified.
    /// </summary>
    /// <remarks>
    /// Multi-column indexes are created by using the same index name in multiple attributes. The information
    /// in these attributes is then merged together to specify the actual database index.
    /// </remarks>
    /// <param name="name">The index name.</param>
    /// <param name="order">A number which will be used to determine column ordering for multi-column indexes.</param>
    public DsCreateIndexAttribute(string name, int order)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
        if (order < 0) throw new ArgumentOutOfRangeException(nameof(order));

        _name = name;
        _order = order;
    }

    /// <summary>
    /// Creates a <see cref="DsCreateIndexAttribute" /> instance for an index with the given name and included column(s).
    /// </summary>
    /// <param name="name">The index name.</param>
    /// <param name="includeColumns">The name(s) of additional columns to include in the index.</param>
    public DsCreateIndexAttribute(string name, string[] includeColumns)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
        if (includeColumns?.Length <= 0) throw new ArgumentNullException(nameof(includeColumns));

        _name = name;
        _includeColumns = includeColumns;
    }

    /// <summary>
    /// Creates a <see cref="DsCreateIndexAttribute" /> instance for an index with the given name, column order,
    /// and included column(s).
    /// </summary>
    /// <remarks>
    /// Multi-column indexes are created by using the same index name in multiple attributes. The information
    /// in these attributes is then merged together to specify the actual database index.
    /// </remarks>
    /// <param name="name">The index name.</param>
    /// <param name="order">A number which will be used to determine column ordering for multi-column indexes.</param>
    /// <param name="includeColumns">The name(s) of additional columns to include in the index.</param>
    public DsCreateIndexAttribute(string name, int order, string[] includeColumns)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
        if (order < 0) throw new ArgumentOutOfRangeException(nameof(order));
        if (includeColumns?.Length <= 0) throw new ArgumentNullException(nameof(includeColumns));

        _name = name;
        _order = order;
        _includeColumns = includeColumns;
    }

    /// <summary>
    /// The index name.
    /// </summary>
    /// <remarks>
    /// Multi-column indexes are created by using the same index name in multiple attributes. The information
    /// in these attributes is then merged together to specify the actual database index.
    /// </remarks>
    public virtual string Name
    {
        get => _name;
        internal set
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));

            _name = value;
        }
    }

    /// <summary>
    /// A number which will be used to determine column ordering for multi-column indexes. This will be -1 if no
    /// column order has been specified.
    /// </summary>
    /// <remarks>
    /// Multi-column indexes are created by using the same index name in multiple attributes. The information
    /// in these attributes is then merged together to specify the actual database index.
    /// </remarks>
    public virtual int Order
    {
        get => _order;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));

            _order = value;
        }
    }

    /// <summary>
    /// Set this property to the name of additional column(s) to include in the index.
    /// </summary>
    /// <remarks>
    /// The value of this property is only relevant if <see cref="IsIncludeColumnsConfigured"/> returns true.
    /// If <see cref="IsIncludeColumnsConfigured"/> returns false, then the value of this property is meaningless.
    /// </remarks>
    public virtual string[] IncludeColumns
    {
        get => _includeColumns;
        set => _includeColumns = value;
    }

    /// <summary>
    /// Returns true if <see cref="IncludeColumns"/> has been set to a value.
    /// </summary>
    public virtual bool IsIncludeColumnsConfigured => _includeColumns.Length > 0;

    /// <summary>
    /// Returns a different ID for each object instance such that type descriptors won't
    /// attempt to combine all IndexAttribute instances into a single instance.
    /// </summary>
    public override object TypeId => RuntimeHelpers.GetHashCode(this);

    /// <summary>
    /// Returns true if this attribute specifies the same name and configuration as the given attribute.
    /// </summary>
    /// <param name="other">The attribute to compare.</param>
    /// <returns>True if the other object is equal to this object; otherwise false.</returns>
    protected virtual bool Equals(DsCreateIndexAttribute other)
    {
        return _name == other._name
            && _order == other._order
            && _includeColumns.Equals(other._includeColumns);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (this is null) throw new ArgumentNullException(nameof(DsCreateIndexAttribute));

        var builder = new StringBuilder("{ ");

        if (!string.IsNullOrWhiteSpace(Name)) builder.Append("Name: ").Append(Name.Replace(",", @"\,").Replace("{", @"\{"));

        if (Order != -1)
        {
            if (builder.Length > 2) builder.Append(", ");

            builder.Append("Order: ").Append(Order);
        }

        if (IsIncludeColumnsConfigured)
        {
            if (builder.Length > 2) builder.Append(", ");

            builder.Append("IncludeColumns: [").Append(string.Join(", ", IncludeColumns)).Append(']');
        }

        if (builder.Length > 2) builder.Append(' ');

        builder.Append('}');

        return builder.ToString();
    }

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

        return Equals((DsCreateIndexAttribute)obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = base.GetHashCode();
            hashCode = hashCode * 397 ^ (_name != null ? _name.GetHashCode() : 0);
            hashCode = hashCode * 397 ^ _order;
            hashCode = hashCode * 397 ^ _includeColumns.GetHashCode();
            return hashCode;
        }
    }
}
