using System;

namespace DocumentStorage.Domain.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DsIndexGuidsAttribute : Attribute
{
    /// <summary>
    /// The <see cref="DsIndexGuidsAttribute"/> attribute is a marker used to signify that 
    /// the implementing class should have indexes applied to its <see cref="Guid"/> fields in the database.
    /// </summary>
    public DsIndexGuidsAttribute() { }
}
