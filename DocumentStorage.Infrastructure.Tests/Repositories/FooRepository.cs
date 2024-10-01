using DocumentStorage.Infrastructure.Tests.DbContexts;
using DocumentStorage.Infrastructure.Tests.Models;
using DocumentStorage.Infrastructure.LiteDb;
using LiteDB;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using DocumentStorage.Infrastructure.EntityFramework;

namespace DocumentStorage.Infrastructure.Tests.Repositories;

public interface IFooRepository : IDsRepository<Foo>
{
    Task<Foo> GetBySsn(string ssn);
}

public class FooRepository : EntityFrameworkRepository<Foo, FooDbContext>, IFooRepository
{
    public FooRepository(FooDbContext context) : base(context) { }

    public async Task<Foo> GetBySsn(string ssn)
    {
        if (string.IsNullOrWhiteSpace(ssn)) throw new ArgumentNullException(nameof(ssn));

        var query = GetQueryable(true);
        query = IncludeChildren(query);
        var foos = await query.Where(x => x.Ssn == _context.EncryptionProvider.EncryptDeterministic(ssn)).ToListAsync();

        return foos.SingleOrDefault(a => a.Ssn == ssn);
    }

    public override IQueryable<Foo> IncludeChildren(IQueryable<Foo> query) => query
        .Include(foo => foo.AllBars)
        .ThenInclude(bar => bar.RequiredChild)
        .Include(foo => foo.AllBars)
        .ThenInclude(bar => bar.OptionalChild);
}

public class LiteDbFooRepository : LiteDbRepository<Foo, LiteDbFooContext>, IFooRepository
{
    public LiteDbFooRepository(LiteDbFooContext context) : base(context) { }

    public async Task<Foo> GetBySsn(string ssn)
    {
        string s = ssn;
        var q = await FindAsync(a => a.Ssn == s);
        return q.FirstOrDefault();
    }
}
