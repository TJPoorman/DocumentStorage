using DocumentStorage.Domain;
using System.ComponentModel.DataAnnotations;

namespace DocumentStorage.Infrastructure.Tests.Models;

public class Bar : DsRecordBase
{
    [StringLength(200)]
    public string BarName { get; set; }
    [Required]
    public Child RequiredChild { get; set; }
    public Child OptionalChild { get; set; }
}
