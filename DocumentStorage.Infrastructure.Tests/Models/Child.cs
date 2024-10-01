using DocumentStorage.Domain;
using System.ComponentModel.DataAnnotations;

namespace DocumentStorage.Infrastructure.Tests.Models;

public class Child : DsRecordBase
{
    [StringLength(200)]
    public string Name { get; set; }
}
