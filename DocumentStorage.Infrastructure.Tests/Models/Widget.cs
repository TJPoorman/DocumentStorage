using DocumentStorage.Domain;

namespace DocumentStorage.Infrastructure.Tests.Models;

public class Widget : DsDbRecordBase
{
    public string Name { get; set; }
}
