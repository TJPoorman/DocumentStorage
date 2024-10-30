using DocumentStorage.Domain;

namespace DocumentStorage.Infrastructure.Tests.Models;

public class Widget : DsDbRecordBase, IDsUniqueDbRecord
{
    public string Name { get; set; }
    public string UniqueKey
    {
        get => Name;
        set => _ = value;
    }
}
