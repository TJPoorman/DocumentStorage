using DocumentStorage.Infrastructure.EntityFramework;
using DocumentStorage.Infrastructure.Tests.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentStorage.Infrastructure.Tests.DbContexts;

public class FooDbContext : EntityFrameworkDbContext
{
    public DbSet<Foo> Foos { get; set; }
    public DbSet<Widget> Widgets { get; set; }

    public FooDbContext(DbContextOptions options, IEncryptionProvider encryptionService) : base(options, encryptionService) { }
}
