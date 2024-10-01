using DocumentStorage.Domain;
using DocumentStorage.Domain.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace DocumentStorage.Infrastructure.Tests.Models;

public class FooReference : DsRecordBase
{
    [StringLength(200)]
    [DsCreateIndex]
    public string FooName { get; set; }
    [DsEncryptedColumn(true)]
    public string Ssn { get; set; }
    [DsEncryptedColumn]
    public string Birthday { get; set; }
    public Guid BestBarId { get; set; }

    [DsReference(nameof(Widget))]
    public Guid? WidgetRef { get; set; }
}