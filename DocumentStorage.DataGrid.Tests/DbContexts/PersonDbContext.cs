using DocumentStorage.DataGrid.EntityFrameworkConnector;
using DocumentStorage.DataGrid.Tests.Models;
using DocumentStorage.Infrastructure;
using DocumentStorage.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DocumentStorage.DataGrid.Tests.DbContexts;

public class PersonDbContext : EntityFrameworkDbContext
{
    public DbSet<Person> People { get; set; }

    public PersonDbContext(DbContextOptions options, DefaultEncryptionProvider encryptionService) : base(options, encryptionService) { }
}

public class PersonRepository : EntityFrameworkDataGridRepository<Person, PersonDbContext>
{
    public PersonRepository(PersonDbContext context) : base(context) { }

    public override IQueryable<Person> IncludeChildren(IQueryable<Person> query) => query
        .Include(p => p.Friend);
}
