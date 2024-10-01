using DocumentStorage.Infrastructure.Tests.DbContexts;
using DocumentStorage.Infrastructure.Tests.Models;
using DocumentStorage.Infrastructure.LiteDb;
using System.Linq;
using DocumentStorage.Infrastructure.EntityFramework;

namespace DocumentStorage.Infrastructure.Tests.Repositories;

public class WidgetRepository : EntityFrameworkRepository<Widget, FooDbContext>
{
    public WidgetRepository(FooDbContext context) : base(context) { }

    public override IQueryable<Widget> IncludeChildren(IQueryable<Widget> query) => query;
}

public class LiteDbWidgetRepository : LiteDbRepository<Widget, LiteDbFooContext>
{
    //public LiteDbWidgetRepository(LiteDbFooContext repositoryService) : base(repositoryService.WidgetRepository) { }
    public LiteDbWidgetRepository(LiteDbFooContext context) : base(context) { }
}
