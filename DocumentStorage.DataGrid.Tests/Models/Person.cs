using DocumentStorage.Domain;

namespace DocumentStorage.DataGrid.Tests.Models;

public class Person : DsDbRecordBase
{
    public string Name { get; set; }
    public string Notes { get; set; }

    public PersonReference Friend { get; set; }

    public int Int32Test { get; set; }
    public long Int64Test { get; set; }
    public decimal DecimalTest { get; set; }
    public FilterType EnumTest { get; set; }
    public bool BoolTest { get; set; }
}

public class PersonReference : DsRecordBase
{
    public string Name { get; set; }
}
