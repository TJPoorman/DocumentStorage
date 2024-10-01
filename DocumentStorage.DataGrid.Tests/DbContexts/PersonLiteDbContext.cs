using DocumentStorage.DataGrid.LiteDbConnector;
using DocumentStorage.DataGrid.Tests.Models;
using DocumentStorage.Infrastructure;
using DocumentStorage.Infrastructure.LiteDb;

namespace DocumentStorage.DataGrid.Tests.DbContexts;

public class PersonLiteDbContext : LiteDbContext
{
    public LiteSet<Person> People { get; set; }

    public PersonLiteDbContext(LiteDbContextOptions options, DefaultEncryptionProvider encryptionService) : base(options, encryptionService) { }
}

public class PersonLiteDbRepository : LiteDbDataGridRepository<Person, PersonLiteDbContext>
{
    public PersonLiteDbRepository(PersonLiteDbContext context) : base(context) { }
}