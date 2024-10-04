using DocumentStorage.DataGrid.LiteDbConnector;
using DocumentStorage.DataGrid.Tests.DbContexts;
using DocumentStorage.DataGrid.Tests.Models;
using DocumentStorage.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DocumentStorage.DataGrid.Tests;

[TestClass]
public class LiteDbDataGridQueryServiceTests : BaseTests
{
    private static readonly string _path = @"D:\Tests\";
    private static PersonLiteDbContext _context;

    protected override IDataGridQueryService<Person> DataGridQueryService
    {
        get
        {
            if (_context == null) throw new AssertInconclusiveException("Unable to create repository.");

            return new LiteDbDataGridQueryService<Person, PersonLiteDbContext>(_context);
        }
    }

    #region Cleanup and Initialize
    [ClassInitialize]
    public static void Initialize(TestContext testContext)
    {
        _context = null;
#if DEBUG
        _context = new PersonLiteDbContext(new Infrastructure.LiteDb.LiteDbContextOptions { Filename = _path }, new DefaultEncryptionProvider());

        _context.EnsureDeleted();
        _context.EnsureCreated();

        _context.Set<Person>().Insert(Person1);
        foreach (Person person in People)
        {
            _context.Set<Person>().Insert(person);
        }
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
    #endregion Cleanup and Initialize
}
