
# DocumentStorage: A Simple Wrapper for EF Core and LiteDb

DocumentStorage is a versatile .NET library designed to simplify data storage and management using Entity Framework Core (EF Core) and LiteDB. By providing a common base model for both parent and child records, DocumentStorage allows developers to structure their data in a document-like manner, similar to NoSQL systems, while also supporting relational data scenarios.

This library abstracts the complexities of interacting with EF Core and LiteDB, enabling seamless CRUD operations through a unified repository interface. With features like built-in data validation, indexing, and encryption, DocumentStorage offers a comprehensive solution for applications that require efficient data handling.

Whether you are building a new application or enhancing an existing one, DocumentStorage empowers you to leverage the strengths of both EF Core and LiteDB, allowing for flexible and efficient data management tailored to your project’s needs.

## Table of Contents

- [DocumentStorage: A Simple Wrapper for EF Core and LiteDb](#documentstorage-a-simple-wrapper-for-ef-core-and-litedb)
  * [Features](#features)
  * [Installation](#installation)
  * [Configuration](#configuration)
    + [Entity Framework](#entity-framework)
    + [LiteDB](#litedb)
  * [Usage](#usage)
    + [Entity Framework](#entity-framework-1)
      - [Context](#context)
      - [Repository](#repository)
    + [LiteDB](#litedb-1)
      - [Context](#context-1)
      - [Repository](#repository-1)
    + [Model](#model)
  * [Data Operations](#data-operations)
  * [DataGrid Functionality](#datagrid-functionality)
    + [Installation](#installation-1)
    + [Request/Response](#requestresponse)
    + [IDataGridQueryService](#idatagridqueryservice)
    + [Usage](#usage-1)
  * [Versioning and Compatibility](#versioning-and-compatibility)
  * [Additional Features](#additional-features)
    + [Indexing](#indexing)
    + [Encryption](#encryption)
  * [License](#license)
  * [Contributing](#contributing)

## Features

- Common base model for parent and child records.
- Integration with EF Core and LiteDb.
- Simplified CRUD operations via IDsRepository.
- Support for encryption, indexing, and validation.

## Installation

To install DocumentStorage in your project, you can use the following NuGet command for the library:
```bash
dotnet add package DocumentStorage.Infrastructure.EntityFramework
dotnet add package DocumentStorage.Infrastructure.LiteDb
```
Installing the top level package will also install the shared libraries used by both: `DocumentStorage.Infrastructure` and `DocumentStorage.Domain`
If using `DocumentStorage.Infrastructure.EntityFramework` you will also need to install the corresponding EF Core package for your target system. See [here](https://learn.microsoft.com/en-us/ef/core/providers/) for more details.

## Configuration

### Entity Framework

```csharp
using DocumentStorage.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

var host = Host.CreateApplicationBuilder(args);
host.AddDocumentStorage<FooDbContext>(o =>
{
     o.UseSqlServer(@"Server=.\SQLEXPRESS;Database=MyDb;Trusted_Connection=True;");
});
```

### LiteDB

```csharp
using DocumentStorage.Infrastructure.LiteDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

var host = Host.CreateApplicationBuilder(args);
host.AddDocumentStorage<FooDbContext>(o =>
{
     o.Filename = @"D:\MyLiteDbs";
});
```

The `AddDocumentStorage<>` method has the following parameters, all of which are optional:

- **optionsAction:** An action to configure the options for the context.
- **autoMapRepositories:** A flag to automatically add the repositories in your app to the DI container.
- **encryptionProvider:** A custom `IEncryptionProvider` used for encryption/decryption.
- **contextLifetime:** The service lifetime to register the context as.
- **optionsLifetime:** The service lifetime to register the options as.

## Usage

### Entity Framework

#### Context

```csharp
using DocumentStorage.Infrastructure.EntityFramework;

public class FooDbContext : EntityFrameworkDbContext
{
    public DbSet<Foo> Foos { get; set; }
    public DbSet<Widget> Widgets { get; set; }

    public FooDbContext(DbContextOptions options, IEncryptionProvider encryptionService) : base(options, encryptionService) { }
}
```

#### Repository

```csharp
using DocumentStorage.Infrastructure;
using DocumentStorage.Infrastructure.EntityFramework;

// Optional: Can define interface per repository
public interface IFooRepository : IDsRepository<Foo>
{
    Task<Foo> GetBySsn(string ssn);
}

public class FooRepository : EntityFrameworkRepository<Foo, FooDbContext>, IFooRepository
{
    public FooRepository(FooDbContext context) : base(context) { }

    public async Task<Foo?> GetBySsn(string ssn)
    {
        if (string.IsNullOrWhiteSpace(ssn)) throw new ArgumentNullException(nameof(ssn));

        return (await FindAsync(x => x.Ssn == ssn)).SingleOrDefault(a => a.Ssn == ssn);
    }

    public override IQueryable<Foo> IncludeChildren(IQueryable<Foo> query) => query
        .Include(foo => foo.AllBars)
        .ThenInclude(bar => bar.RequiredChild)
        .Include(foo => foo.AllBars)
        .ThenInclude(bar => bar.OptionalChild);
}
```

### LiteDB

#### Context

```csharp
using DocumentStorage.Infrastructure.LiteDb;

public class FooDbContext : LiteDbContext
{
    public LiteSet<Foo> Foos { get; set; }
    public LiteSet<Widget> Widgets { get; set; }

    public FooDbContext(LiteDbContextOptions options, IEncryptionProvider encryptionService) : base(options, encryptionService) { }
}
```

#### Repository

```csharp
using DocumentStorage.Infrastructure;
using DocumentStorage.Infrastructure.LiteDb;

// Optional: Can define interface per repository
public interface IFooRepository : IDsRepository<Foo>
{
    Task<Foo> GetBySsn(string ssn);
}

public class FooRepository : LiteDbRepository<Foo, FooDbContext>, IFooRepository
{
    public FooRepository(FooDbContext context) : base(context) { }

    public async Task<Foo?> GetBySsn(string ssn)
    {
        if (string.IsNullOrWhiteSpace(ssn)) throw new ArgumentNullException(nameof(ssn));

        return (await FindAsync(x => x.Ssn == ssn)).SingleOrDefault(a => a.Ssn == ssn);
    }
}
```

### Model

```csharp
[DsIndexGuids]
public class Foo : DsDbRecordBase
{
    [StringLength(200)]
    [DsCreateIndex]
    public string FooName { get; set; }
    
    [DsEncryptedColumn(true)]
    public string Ssn { get; set; }
    
    [DsEncryptedColumn]
    public string Birthday { get; set; }

    public Guid BestBarId { get; set; }

    public Guid? WidgetRef { get; set; }

    public List<Bar> AllBars { get; set; }

    [NotMapped]
    public Widget Widget { get; set; }
}

public class Bar : DsRecordBase
{
    [StringLength(200)]
    public string BarName { get; set; }

    [Required]
    public Child RequiredChild { get; set; }

    public Child? OptionalChild { get; set; }
}

public class Child : DsRecordBase
{
    [StringLength(200)]
    public string Name { get; set; }
}

public class Widget : DsDbRecordBase
{
    public string Name { get; set; }
}
```

Top-level parent models inherit from `DsDbRecordBase` while child models inherit from `DsRecordBase`.

## Data Operations

Repository classes inherit from either `EntityFrameworkRepository<TRecord, TContext>` or `LiteDbRepository<TRecord, TContext>`.  These base abstract classes all inherit from `IDsRepository<TRecord>` interface with the following methods:

```csharp
public interface IDsRepository<TRecord>
    where TRecord : class, IDsDbRecord
{
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<TRecord>> FindAsync(Expression<Func<TRecord, bool>> request);
    Task<TRecord> GetAsync(Guid id);
    Task InitializeAsync();
    bool IsDuplicate(IDsUniqueDbRecord record);
    Task UpsertAsync(TRecord record);
    event DsRecordEventHandler<TRecord> AfterRecordUpserted;
    event DsRecordEventHandler<TRecord> AfterRecordDeleted;
}
```

Since this is a document-like structure, all interactions are handled at the parent level.  So to change a child entry do the following:
1. Retrieve the parent entity.
2. Modify the child.
3. Upsert the modified entity.

```csharp
public async Task ModifyChild()
{
    Foo foo = await _repository.GetAsync(TestFoo.Id);
    foo.AllBars.FirstOrDefault(b => b.Id == foo.BestBarId).BarName = "test";
    await _repository.UpsertAsync(foo);
}
```

Searching can be done utilizing the `FindAsync()` method.  This takes any valid LINQ query which is passed to the provider framework to retrieve an `IEnumerable<T>`.

```csharp
public async Task GetFoosByBarName()
{
    var foos = await _repository.FindAsync(a => a.AllBars.Select(b => b.BarName).Any(b => b == "Bar2"));
}
```

## DataGrid Functionality

The optional DataGrid packages provide easy search functionality via a simple request/response model.  This is easily mapped to 3rd party providers such as [Tabulator](https://tabulator.info/).

### Installation

To install DocumentStorage.DataGrid in your project, you can use the following NuGet command for the library:
```bash
dotnet add package DocumentStorage.DataGrid.EntityFrameworkConnector
dotnet add package DocumentStorage.DataGrid.LiteDbConnector
```
Installing the top level package will also install the shared library used by both: `DocumentStorage.DataGrid`

### Request/Response

The following models are used to create the request that is passed to the `IDataGridQueryService` and the corresponding response:

```csharp
public class DataGridRequest<T>
{
    public virtual List<FilterClause> Filters { get; set; } = new();
    public int Limit { get; set; }
    public int Skip { get; set; }
    public List<SortClause> Sorters { get; set; } = new();
}

public class DataGridResponse<T>
{
    public IEnumerable<T> Data { get; set; }
    public int TotalRecordCount { get; set; }
    public int TotalPageCount { get; set; }
}
```

### IDataGridQueryService

Each package has it's own implementation of `IDataGridQueryService`:

- **EntityFramework:** EntityFrameworkDataGridQueryService
- **LiteDB:** LiteDbDataGridQueryService

These can be manually implemented or you can utilize the extended Repository classes that handle the QueryService via DI automatically:

- **EntityFramework:** EntityFrameworkDataGridRepository<TRecord, TContext>
- **LiteDB:** LiteDbDataGridRepository<TRecord, TContext>

### Usage

```csharp
public async Task DataGridGetPersonByName()
{
    DataGridResponse<Person> response = await _dataGridQueryService.QueryAsync(
        new DataGridRequest<Person>()
        {
            Filters = new List<FilterClause>()
            {
                new(nameof(Person.Name), FilterType.Equals, Person1.Name)
            },
            Sorters = new List<SortClause>()
            {
                new(nameof(Person.Name), SortDirection.Ascending)
            }
        });

    List<Person> people = response.Data.ToList();
}
```

Multiple filters are treated as logical `AND`.
If multiple sorters are provided, the framework applies them in order as a `ThenBy`

## Versioning and Compatibility

DocumentStorage is built to be compatible with .NET Core versions 6.0 and 8.0, utilizing the latest features and improvements in both Entity Framework Core and LiteDB. The following versions are supported:

- .NET 6.0
   - EF Core: 7.0.20
   - LiteDB: 5.0.21
- .NET 8.0
   - EF Core: 8.0.8
   - LiteDB: 5.0.21

## Additional Features

### Indexing

There are 2 options for defining indexes; `DsIndexGuidsAttribute` and `DsCreateIndexAttribute`.

The `DsIndexGuidsAttribute` is applied at the class level and will create an index on all fields of type `GUID`.  This is helpful for reference fields to facilitate reverse lookups.
```csharp
[DsIndexGuids]
public class Foo : DsDbRecordBase
```

The `DsCreateIndexAttribute` is applied at the property level. This option supports multiple columns and include columns via properties defined in the attribute.  If multiple attributes are defined with the same name, you must indicate an `order` that the columns will be indexed in.

```csharp
public class Foo : DsDbRecordBase
{
    [DsCreateIndex]
    public string FooName { get; set; }

    [DsCreateIndex("MyIndex", 0)]
    public DateTimeOffset FooDate { get; set; }

    [DsCreateIndex("MyIndex", 1)]
    public string FooNote { get; set; }

    [DsCreateIndex("MyIncludeIndex", new string[] { nameof(FooName), nameof(FooNote) })]
    public Guid BestBarId { get; set; }
}
```

### Encryption

Encryption is automatically enabled by default utilizing a `DefaultEncryptionProvider` that implements `IEncryptionProvider`.  This utilizes RSA encryption for normal and an RSA encrypted AES key for Deterministic encryption if the field needs to be searchable.

The `DsEncryptedColumnAttribute` is applied at the property level with an optional bool field to flag if the field needs to be searchable.

```csharp
public class Foo : DsDbRecordBase
{
    [DsEncryptedColumn(true)]
    public string Ssn { get; set; }
    
    [DsEncryptedColumn]
    public string Birthday { get; set; }
}
```

## License

DocumentStorage is licensed under the GPL License. See the [LICENSE](https://spdx.org/licenses/GPL-3.0-or-later.html) for more information.

## Contributing

Contributions are always welcome! Feel free to open an issue or submit a pull request.
