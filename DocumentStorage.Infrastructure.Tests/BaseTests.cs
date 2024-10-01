using DocumentStorage.Infrastructure.Tests.Models;
using DocumentStorage.Infrastructure.Tests.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentStorage.Infrastructure.Tests;

public abstract class BaseTests
{
    #region Static Data
    protected static Bar BestBar => new()
    {
        Id = new Guid("832c52e7-35ab-46d9-bb99-00541208296c"),
        BarName = "Bar1",
        RequiredChild = new Child()
        {
            Id = new Guid("1e7bc790-f1e7-477f-b733-55009e86e708"),
            Name = "Child1"
        }
    };

    protected static Widget TestWidget => new()
    {
        Id = new Guid("31781812-3727-4853-a947-b86a899c63bb"),
        Name = "Widget1",
    };

    protected static Foo TestFoo => new()
    {
        Id = new Guid("27d6ef9c-d126-48f2-aa6a-7e1ac77251a0"),
        AllBars = new List<Bar>()
            {
                BestBar,
                {
                    new()
                    {
                        Id = new Guid("94586ba1-eb75-493a-aa66-b917b0de7386"),
                        BarName = "Bar2",
                        RequiredChild = new Child()
                        {
                            Id = new Guid("365c708e-7cf9-4ed8-b34f-f8074c0dcb64"),
                            Name = "Child2"
                        },
                        OptionalChild = new Child()
                        {
                            Id = new Guid("365c708e-7cf9-4ed8-b34f-f9874c0dcb64"),
                            Name = "OptionalChild2"
                        }
                    }
                },
                {
                    new()
                    {
                        Id = new Guid("d11dfc8b-8b6c-438a-b684-b2ea9aaf061a"),
                        BarName = "Bar3",
                        RequiredChild = new Child()
                        {
                            Id = new Guid("365c708e-7cf9-4ed8-b34f-f8074c0dcb89"),
                            Name = "Child3"
                        }
                    }
                },
            },
        BestBarId = BestBar.Id,
        FooName = "Foo1",
        Ssn = "TestEncryption",
        Birthday = "1/1/2000",
    };
    #endregion

    protected abstract IFooRepository GetFooRepository();

    protected abstract IDsRepository<Widget> GetWidgetRepository();

    [TestMethod]
    public async Task Foo_AfterDeleteEventFires()
    {
        var fooRepository = GetFooRepository();
        fooRepository.AfterRecordDeleted += FooRepositoryAfterRecordDeleted;

        await fooRepository.UpsertAsync(TestFoo);
        bool deleted = await fooRepository.DeleteAsync(TestFoo.Id);

        Assert.IsTrue(deleted);

        void FooRepositoryAfterRecordDeleted(object sender, DsRecordEventArgs<Foo> e)
        {
            Assert.IsNotNull(e.RecordAffected);
            Assert.AreEqual(e.RecordAffected.Id, TestFoo.Id);
            Assert.IsNotNull(e.RecordAffected.FooName);
            Assert.AreEqual(e.RecordAffected.FooName, TestFoo.FooName);
        }
    }

    [TestMethod]
    public async Task Foo_AfterUpsertEventFires()
    {
        var fooRepository = GetFooRepository();
        fooRepository.AfterRecordUpserted += FooRepositoryAfterRecordUpserted;

        await fooRepository.UpsertAsync(TestFoo);

        void FooRepositoryAfterRecordUpserted(object sender, DsRecordEventArgs<Foo> e)
        {
            Assert.IsNotNull(e.RecordAffected);
            Assert.AreEqual(e.RecordAffected.Id, TestFoo.Id);
        }
    }

    [TestMethod]
    public async Task Foo_CanDelete()
    {
        var fooRepository = GetFooRepository();
        await fooRepository.UpsertAsync(TestFoo);
        bool deleted = await fooRepository.DeleteAsync(TestFoo.Id);

        Assert.IsTrue(deleted);
    }

    [TestMethod]
    public async Task Foo_CanEncryptDecryptField()
    {
        var fooRepository = GetFooRepository();

        Foo origFoo = TestFoo;

        await fooRepository.UpsertAsync(origFoo);

        Foo foo = await fooRepository.GetAsync(origFoo.Id);

        Assert.IsNotNull(foo);
        Assert.AreNotEqual(origFoo.Ssn, TestFoo.Ssn);
        Assert.AreEqual(foo.Ssn, TestFoo.Ssn);
    }

    [TestMethod]
    public async Task Foo_CanGet()
    {
        var fooRepository = GetFooRepository();
        await fooRepository.UpsertAsync(TestFoo);

        Foo foo = await fooRepository.GetAsync(TestFoo.Id);

        Assert.AreEqual(TestFoo.Id, foo.Id);
        Assert.AreEqual(TestFoo.AllBars.Count, foo.AllBars.Count);
        Assert.AreEqual(TestFoo.AllBars.Select(b => b.RequiredChild).Count(), foo.AllBars.Select(b => b.RequiredChild).Count());
    }

    [TestMethod]
    public async Task Foo_CanGetByChildField()
    {
        var fooRepository = GetFooRepository();
        await fooRepository.UpsertAsync(TestFoo);

        var foos = await fooRepository.FindAsync(a => a.AllBars.Select(b => b.BarName).Any(b => b == "Bar2"));
        var foo = foos.FirstOrDefault();

        Assert.IsNotNull(foos);
        Assert.IsTrue(foos.Any());
        Assert.AreEqual(TestFoo.AllBars.Count, foo.AllBars.Count);
        Assert.AreEqual(TestFoo.AllBars.Select(b => b.RequiredChild).Count(), foo.AllBars.Select(b => b.RequiredChild).Count());
    }

    [TestMethod]
    public async Task Foo_CanGetByEncryptedField()
    {
        var fooRepository = GetFooRepository();
        await fooRepository.UpsertAsync(TestFoo);

        Foo foo = await fooRepository.GetBySsn(TestFoo.Ssn);

        Assert.AreEqual(TestFoo.Id, foo.Id);
        Assert.AreEqual(TestFoo.AllBars.Count, foo.AllBars.Count);
        Assert.AreEqual(TestFoo.AllBars.Select(b => b.RequiredChild).Count(), foo.AllBars.Select(b => b.RequiredChild).Count());
    }

    [TestMethod]
    public async Task Foo_CanRemoveChild()
    {
        var fooRepository = GetFooRepository();
        await fooRepository.UpsertAsync(TestFoo);

        Foo foo = await fooRepository.GetAsync(TestFoo.Id);

        foo.AllBars.RemoveAt(0);

        await fooRepository.UpsertAsync(foo);

        Assert.AreEqual(TestFoo.AllBars.Count - 1, foo.AllBars.Count);

        foo = await fooRepository.GetAsync(TestFoo.Id);

        Assert.AreEqual(TestFoo.AllBars.Count - 1, foo.AllBars.Count);
    }

    [TestMethod]
    public async Task Foo_CanUpdateChild()
    {
        var fooRepository = GetFooRepository();
        await fooRepository.UpsertAsync(TestFoo);

        Foo foo = await fooRepository.GetAsync(TestFoo.Id);

        foo.AllBars.FirstOrDefault(b => b.Id == foo.BestBarId).BarName = "test";

        await fooRepository.UpsertAsync(foo);

        foo = await fooRepository.GetAsync(TestFoo.Id);

        Assert.AreEqual("test", foo.AllBars.FirstOrDefault(b => b.Id == foo.BestBarId).BarName);
    }

    [TestMethod]
    public async Task Foo_CanUpsert()
    {
        var fooRepository = GetFooRepository();
        await fooRepository.UpsertAsync(TestFoo);
    }

    [TestMethod]
    public async Task Foo_LastModifiedDateTimeIsUpdated()
    {
        var fooRepository = GetFooRepository();

        Foo foo = TestFoo;

        DateTimeOffset lastModifiedDateTime = foo.LastModifiedDateTime;

        Assert.AreNotEqual(DateTimeOffset.MinValue, lastModifiedDateTime);

        await Task.Delay(2);

        await fooRepository.UpsertAsync(foo);

        foo = await fooRepository.GetAsync(foo.Id);

        Assert.AreNotEqual(lastModifiedDateTime, foo.LastModifiedDateTime);
    }

    [TestMethod]
    public async Task Foo_ValidationExceptionIsThrown()
    {
        var fooRepository = GetFooRepository();

        await Assert.ThrowsExceptionAsync<DsDbRecordValidationException>(async () =>
            await fooRepository.UpsertAsync(new Foo()
            {
                FooName = new string(new char[201]),
                AllBars = new List<Bar>()
                {
                        new()
                        {
                            BarName = new string(new char[201])
                        }
                }
            }));
    }
}
