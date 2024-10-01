using DocumentStorage.Infrastructure.Tests.DbContexts;
using DocumentStorage.Infrastructure.Tests.Models;
using DocumentStorage.Infrastructure.Tests.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentStorage.Infrastructure.Tests;

[TestClass]
public class EfTests : BaseTests
{
    private static FooDbContext _context;

    private static FooDbContext GetNewContext(IEncryptionProvider provider)
    {
        FooDbContext context = null;

#if DEBUG
        var options = new DbContextOptionsBuilder<FooDbContext>();
        options.UseSqlServer(@"Server=.\SQLEXPRESS;Database=DsTests;Trusted_Connection=True;TrustServerCertificate=True;");

        context = new FooDbContext(options.Options, provider);
#endif
        return context;
    }

    protected override FooRepository GetFooRepository()
    {
        if (_context == null) throw new AssertInconclusiveException("Unable to create DbContext.");

        return new FooRepository(_context);
    }

    private FooRepository GetFooRepository(FooDbContext context)
    {
        if (context == null) throw new AssertInconclusiveException("Unable to create DbContext.");

        return new FooRepository(context);
    }

    protected override WidgetRepository GetWidgetRepository()
    {
        if (_context == null) throw new AssertInconclusiveException("Unable to create DbContext.");

        return new WidgetRepository(_context);
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
    {
        _context = GetNewContext(new DefaultEncryptionProvider());
#if DEBUG
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();
#endif
    }

    [TestInitialize]
    public async Task TestInitialize()
    {
        FooRepository fooRepository = GetFooRepository();
        await fooRepository.DeleteAsync(TestFoo.Id);
        WidgetRepository widgetRepository = GetWidgetRepository();
        await widgetRepository.DeleteAsync(TestWidget.Id);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
#if DEBUG
        _context.Database.EnsureDeleted();
#endif
    }

    [TestCleanup]
    public void TestCleanup()
    {
#if DEBUG
        _context.ChangeTracker.Clear();
#endif
    }

    [TestMethod]
    public async Task Foo_CanGetWithReference()
    {
        var fooRepository = GetFooRepository();
        var widgetRepository = GetWidgetRepository();
        await widgetRepository.UpsertAsync(TestWidget);
        Foo newFoo = TestFoo;
        newFoo.WidgetRef = TestWidget.Id;
        await fooRepository.UpsertAsync(newFoo);

        Foo foo = await fooRepository.GetAsync(TestFoo.Id);

        Assert.AreEqual(TestFoo.Id, foo.Id);
        Assert.AreEqual(TestFoo.AllBars.Count, foo.AllBars.Count);
        Assert.AreEqual(TestFoo.AllBars.Select(b => b.RequiredChild).Count(), foo.AllBars.Select(b => b.RequiredChild).Count());
        Assert.IsNotNull(foo.Widget);
        Assert.AreEqual(TestWidget.Id, foo.Widget.Id);
    }

    [TestMethod]
    public async Task Foo_DeleteInsideTransactionDoesNotSave()
    {
        using FooDbContext writeDbContext = GetNewContext(new DefaultEncryptionProvider());
        using FooDbContext readDbContext = GetNewContext(new DefaultEncryptionProvider());
        FooRepository writeRepository = GetFooRepository(writeDbContext);
        FooRepository readRepository = GetFooRepository(readDbContext);

        Foo origFoo = TestFoo;

        await writeRepository.UpsertAsync(origFoo);

        Foo foo = await readRepository.GetAsync(origFoo.Id);

        Assert.IsNotNull(foo);

        using (IDisposable _ = await writeRepository.BeginTransactionAsync())
        {
            await writeRepository.DeleteAsync(origFoo.Id);
        }

        foo = await readRepository.GetAsync(origFoo.Id);

        Assert.IsNotNull(foo);
    }

    [TestMethod]
    public async Task Foo_DeleteOutsideTransactionSaves()
    {
        using FooDbContext writeDbContext = GetNewContext(new DefaultEncryptionProvider());
        using FooDbContext readDbContext = GetNewContext(new DefaultEncryptionProvider());
        FooRepository writeRepository = GetFooRepository(writeDbContext);
        FooRepository readRepository = GetFooRepository(readDbContext);

        Foo origFoo = TestFoo;

        await writeRepository.UpsertAsync(origFoo);

        Foo foo = await readRepository.GetAsync(origFoo.Id);

        Assert.IsNotNull(foo);

        await writeRepository.DeleteAsync(origFoo.Id);

        foo = await readRepository.GetAsync(origFoo.Id);

        Assert.IsNull(foo);
    }

    [TestMethod]
    public async Task Foo_OptimisticConcurrencyErrorThrown()
    {
        using (FooDbContext outerDbContext = GetNewContext(new DefaultEncryptionProvider()))
        using (FooDbContext innerDbContext = GetNewContext(new DefaultEncryptionProvider()))
        using (FooDbContext observerDbContext = GetNewContext(new DefaultEncryptionProvider()))
        {
            FooRepository outerfooRepository = GetFooRepository(outerDbContext);

            await outerfooRepository.UpsertAsync(TestFoo);

            using (IDisposable transaction = await outerfooRepository.BeginTransactionAsync())
            {
                await UpdateBarNamesAsync(outerfooRepository, TestFoo.Id, "outer-test");

                FooRepository innerfooRepository = GetFooRepository(innerDbContext);
                await UpdateBarNamesAsync(innerfooRepository, TestFoo.Id, "inner-test");

                await Assert.ThrowsExceptionAsync<DsDbUpdateConcurrencyException>(async () => await outerfooRepository.CommitTransactionAsync(transaction));
            }

            Assert.AreNotEqual(outerDbContext, innerDbContext);

            Foo foo = await GetFooRepository(observerDbContext).GetAsync(TestFoo.Id);
            Assert.AreEqual("inner-test", foo.AllBars.FirstOrDefault().BarName);
        }

        async Task UpdateBarNamesAsync(FooRepository fooRepository, Guid fooId, string barName)
        {
            Foo foo = await fooRepository.GetAsync(fooId);
            foo.AllBars.ForEach(b => b.BarName = barName);
            await fooRepository.UpsertAsync(foo);
        }
    }

    [TestMethod]
    public async Task Foo_TransactionCommitSavesDelete()
    {
        using FooDbContext writeDbContext = GetNewContext(new DefaultEncryptionProvider());
        using FooDbContext readDbContext = GetNewContext(new DefaultEncryptionProvider());
        FooRepository writeRepository = GetFooRepository(writeDbContext);
        FooRepository readRepository = GetFooRepository(readDbContext);

        Foo origFoo = TestFoo;

        await writeRepository.UpsertAsync(origFoo);

        Foo foo = await readRepository.GetAsync(origFoo.Id);

        Assert.IsNotNull(foo);

        using (IDisposable transaction = await writeRepository.BeginTransactionAsync())
        {
            await writeRepository.DeleteAsync(origFoo.Id);
            await writeRepository.CommitTransactionAsync(transaction);
        }

        foo = await readRepository.GetAsync(origFoo.Id);

        Assert.IsNull(foo);
    }

    [TestMethod]
    public async Task Foo_TransactionCommitSavesUpsert()
    {
        using FooDbContext writeDbContext = GetNewContext(new DefaultEncryptionProvider());
        using FooDbContext readDbContext = GetNewContext(new DefaultEncryptionProvider());
        FooRepository writeRepository = GetFooRepository(writeDbContext);
        FooRepository readRepository = GetFooRepository(readDbContext);

        Foo origFoo = TestFoo;

        await writeRepository.DeleteAsync(origFoo.Id);

        Foo foo = await readRepository.GetAsync(origFoo.Id);

        Assert.IsNull(foo);

        using (IDisposable transaction = await writeRepository.BeginTransactionAsync())
        {
            await writeRepository.UpsertAsync(origFoo);
            await writeRepository.CommitTransactionAsync(transaction);
        }

        foo = await readRepository.GetAsync(origFoo.Id);

        Assert.IsNotNull(foo);
    }

    [TestMethod]
    public async Task Foo_UpsertInsideTransactionDoesNotSave()
    {
        using FooDbContext writeDbContext = GetNewContext(new DefaultEncryptionProvider());
        using FooDbContext readDbContext = GetNewContext(new DefaultEncryptionProvider());
        FooRepository writeRepository = GetFooRepository(writeDbContext);
        FooRepository readRepository = GetFooRepository(readDbContext);

        Foo origFoo = TestFoo;

        await writeRepository.DeleteAsync(origFoo.Id);

        Foo foo = await readRepository.GetAsync(origFoo.Id);

        Assert.IsNull(foo);

        using (IDisposable _ = await writeRepository.BeginTransactionAsync())
        {
            await writeRepository.UpsertAsync(origFoo);
        }

        foo = await readRepository.GetAsync(origFoo.Id);

        Assert.IsNull(foo);
    }

    [TestMethod]
    public async Task Foo_UpsertOutsideTransactionSaves()
    {
        using FooDbContext writeDbContext = GetNewContext(new DefaultEncryptionProvider());
        using FooDbContext readDbContext = GetNewContext(new DefaultEncryptionProvider());
        FooRepository writeRepository = GetFooRepository(writeDbContext);
        FooRepository readRepository = GetFooRepository(readDbContext);

        Foo origFoo = TestFoo;

        await writeRepository.DeleteAsync(origFoo.Id);

        Foo foo = await readRepository.GetAsync(origFoo.Id);

        Assert.IsNull(foo);

        await writeRepository.UpsertAsync(origFoo);

        foo = await readRepository.GetAsync(origFoo.Id);

        Assert.IsNotNull(foo);
    }
}
