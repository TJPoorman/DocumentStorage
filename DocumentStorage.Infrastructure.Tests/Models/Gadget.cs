using DocumentStorage.Domain;
using System.Collections.Generic;

namespace DocumentStorage.Infrastructure.Tests.Models;

public class Gadget : DsDbRecordBase
{
    public string Name { get; set; }
    public List<string> MyStringList { get; set; }
}
