using DocumentStorage.Infrastructure.Tests.DbContexts;
using DocumentStorage.Infrastructure.Tests.Models;
using DocumentStorage.Infrastructure.Tests.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace DocumentStorage.Infrastructure.Tests;

[TestClass]
public class LiteDbTests : BaseTests
{
    private static readonly string _path = @"D:\Tests\";
    private static LiteDbFooContext _context;

    private static LiteDbFooContext GetNewContext(IEncryptionProvider provider)
    {
        LiteDbFooContext context = null;

#if DEBUG
        context = new LiteDbFooContext(new LiteDb.LiteDbContextOptions { Filename = _path }, provider);
#endif
        return context;
    }

    protected override LiteDbFooRepository GetFooRepository()
    {
        if (_context == null) throw new AssertInconclusiveException("Unable to create DbContext.");
        
        var r = new LiteDbFooRepository(_context);
        r.InitializeAsync().Wait();

        return r;
    }

    protected override LiteDbWidgetRepository GetWidgetRepository()
    {
        if (_context == null) throw new AssertInconclusiveException("Unable to create DbContext.");
        
        var r = new LiteDbWidgetRepository(_context);
        r.InitializeAsync().Wait();

        return r;
    }

    protected override LiteDbGadgetRepository GetGadgetRepository()
    {
        if (_context == null) throw new AssertInconclusiveException("Unable to create DbContext.");

        var r = new LiteDbGadgetRepository(_context);
        r.InitializeAsync().Wait();

        return r;
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
    {
        _context = GetNewContext(new DefaultEncryptionProvider());
#if DEBUG
        _context.EnsureDeleted();
        _context.EnsureCreated();
#endif
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
#if DEBUG
        _context.EnsureDeleted();
#endif
        _context?.Dispose();
    }

    [TestMethod]
    public async Task Gadget_CanUpsertWithNullList()
    {
        try
        {
            var gadgetRepository = GetGadgetRepository();
            await gadgetRepository.UpsertAsync(TestGadget1);
            Gadget gadget = await gadgetRepository.GetAsync(TestGadget1.Id);

            Assert.IsNotNull(gadget);
        }
        catch (Exception e)
        {
            Assert.Fail(e.Message);
        }
    }

    [TestMethod]
    public async Task Gadget_CanUpsertWithSimpleList()
    {
        try
        {
            var gadgetRepository = GetGadgetRepository();
            await gadgetRepository.UpsertAsync(TestGadget2);
            Gadget gadget = await gadgetRepository.GetAsync(TestGadget2.Id);

            Assert.IsNotNull(gadget);
            Assert.IsTrue(gadget.MyStringList.Count == 3);
        }
        catch (Exception e)
        {
            Assert.Fail(e.Message);
        }
    }
}
