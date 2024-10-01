using DocumentStorage.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocumentStorage.Infrastructure.Tests.Models;

public class Foo : FooReference, IDsDbRecord
{
    public DateTimeOffset CreatedDateTime { get; set; } = DateTimeOffset.Now;
    [ConcurrencyCheck]
    public DateTimeOffset LastModifiedDateTime { get; set; } = DateTimeOffset.Now;
    public string CreatedBy { get; set; }
    public string LastModifiedBy { get; set; }
    public List<Bar> AllBars { get; set; }

    [NotMapped]
    public Widget Widget { get; set; }
}
