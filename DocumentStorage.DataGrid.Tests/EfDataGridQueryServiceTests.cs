using DocumentStorage.DataGrid.EntityFrameworkConnector;
using DocumentStorage.DataGrid.Tests.DbContexts;
using DocumentStorage.DataGrid.Tests.Models;
using DocumentStorage.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DocumentStorage.DataGrid.Tests;

[TestClass]
public class EfDataGridQueryServiceTests : BaseTests
{
    private static PersonDbContext _context;

    protected override IDataGridQueryService<Person> DataGridQueryService
    {
        get
        {
            if (_context == null) throw new AssertInconclusiveException("Unable to create DbContext.");

            return new EntityFrameworkDataGridQueryService<Person, PersonDbContext>(_context, (query) => query.Include(p => p.Friend));
        }
    }

    #region Cleanup and Initialize
    [ClassInitialize]
    public static void Initialize(TestContext testContext)
    {
        _context = null;
#if DEBUG
        var options = new DbContextOptionsBuilder<PersonDbContext>();
        options.UseSqlServer(@"Server=.\SQLEXPRESS;Database=EntityFrameworkDataGridQueryServiceTests;Trusted_Connection=True;TrustServerCertificate=True;");
        _context = new PersonDbContext(options.Options, new DefaultEncryptionProvider());

        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();

        _context.Set<Person>().Add(Person1);
        _context.SaveChanges();

        foreach (Person person in People)
        {
            _context.Set<Person>().Add(person);
            _context.SaveChanges();
        }
#endif
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
    #endregion Cleanup and Initialize
}
