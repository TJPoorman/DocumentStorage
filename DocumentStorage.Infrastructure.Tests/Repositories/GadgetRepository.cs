using DocumentStorage.Infrastructure.EntityFramework;
using DocumentStorage.Infrastructure.LiteDb;
using DocumentStorage.Infrastructure.Tests.DbContexts;
using DocumentStorage.Infrastructure.Tests.Models;
using System.Linq;

namespace DocumentStorage.Infrastructure.Tests.Repositories;

public class GadgetRepository : EntityFrameworkRepository<Gadget, FooDbContext>
{
    public GadgetRepository(FooDbContext context) : base(context) { }

    public override IQueryable<Gadget> IncludeChildren(IQueryable<Gadget> query) => query;
}

public class LiteDbGadgetRepository : LiteDbRepository<Gadget, LiteDbFooContext>
{
    public LiteDbGadgetRepository(LiteDbFooContext context) : base(context) { }
}