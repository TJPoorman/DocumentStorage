using DocumentStorage.Infrastructure.LiteDb;
using DocumentStorage.Infrastructure.Tests.Models;

namespace DocumentStorage.Infrastructure.Tests.DbContexts;

public class LiteDbFooContext : LiteDbContext
{
    public LiteSet<Foo> FooRepository { get; set; }

    public LiteSet<Widget> WidgetRepository { get; set; }

    public LiteDbFooContext(LiteDbContextOptions options, IEncryptionProvider encryptionService) : base(options, encryptionService) { }
}
