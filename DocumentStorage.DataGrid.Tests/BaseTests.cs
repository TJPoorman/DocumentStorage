using DocumentStorage.DataGrid.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocumentStorage.DataGrid.Tests;

[TestClass]
public abstract class BaseTests
{
    protected abstract IDataGridQueryService<Person> DataGridQueryService { get; }

    #region Static Data
    internal static Person Person1 => new()
    {
        Id = new Guid("04cb5beb-4ee0-4ba2-a223-49597f047504"),
        Name = "John Doe",
        Notes = "Notes",
        BoolTest = true,
        CreatedDateTime = DateTimeOffset.Parse("1990-01-01").Date,
        LastModifiedDateTime = DateTimeOffset.Now,
        DecimalTest = 2.5m,
        EnumTest = FilterType.Equals,
        Int32Test = 5,
        Int64Test = 10,
    };

    internal static Person Person2 => new()
    {
        Id = new Guid("1977fe7f-ab85-43c2-ae98-1a0eb5c6ff2b"),
        Name = "Jane Doe",
        Notes = "Notes2",
        Friend = new PersonReference() { Name = Person1.Name }
    };

    internal static List<Person> People
    {
        get
        {
            var people = new List<Person>();

            for (int index = 0; index < 100; ++index)
            {
                people.Add(new Person() { Name = $"SomeGuy{index}" });
            }

            people.Add(Person2);

            return people;
        }
    }
    #endregion Static Data

    #region Filter Between

    [TestMethod]
    public async Task Filter_Between_Value_DateTimeOffset_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.CreatedDateTime), FilterType.Between, new DateTimeOffset[] { Person1.CreatedDateTime.AddMinutes(-1), Person1.CreatedDateTime.AddMinutes(1) })
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_Between_Value_Decimal_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.DecimalTest), FilterType.Between, new decimal[] { Person1.DecimalTest - 0.1m, Person1.DecimalTest + 0.1m })
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_Between_Value_Int32_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Int32Test), FilterType.Between, new int[] { Person1.Int32Test - 1, Person1.Int32Test + 1 })
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_Between_Value_Int64_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Int64Test), FilterType.Between, new long[] { Person1.Int64Test -1, Person1.Int64Test + 1 })
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    #endregion

    #region Filter Contains

    [TestMethod]
    public async Task Filter_Contains_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Name), FilterType.Contains, "0")
                },
                Sorters = new List<SortClause>()
                {
                    new(nameof(Person.Name), SortDirection.Ascending)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        // 0, 10, 20, 30, 40, 50, 60, 70, 80, 90
        for (int index = 0; index < 10; ++index)
        {
            Assert.IsTrue(people.MoveNext());

            Person person = people.Current;
            Assert.AreEqual($"SomeGuy{index * 10}", person.Name);
        }

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_Contains_Three_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Name), FilterType.Contains, "ohn"),
                    new(nameof(Person.Name), FilterType.Contains, "oe"),
                    new(nameof(Person.Notes), FilterType.Contains, "ot")
                },
                Sorters = new List<SortClause>()
                {
                    new(nameof(Person.Name), SortDirection.Ascending)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Name, person.Name);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_Contains_Two_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Name), FilterType.Contains, "1"),
                    new(nameof(Person.Name), FilterType.Contains, "2"),
                },
                Sorters = new List<SortClause>()
                {
                    new(nameof(Person.Name), SortDirection.Ascending)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual($"SomeGuy12", person.Name);

        Assert.IsTrue(people.MoveNext());

        person = people.Current;
        Assert.AreEqual($"SomeGuy21", person.Name);

        Assert.IsFalse(people.MoveNext());
    }

    #endregion

    #region Filter Equals

    [TestMethod]
    public async Task Filter_Equals_And_Contains_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Name), FilterType.Equals, Person1.Name),
                    new(nameof(Person.Notes), FilterType.Contains, "o")
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Name, person.Name);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_Equals_Value_Bool_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.BoolTest), FilterType.Equals, Person1.BoolTest)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_Equals_Value_DateTimeOffset_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.CreatedDateTime), FilterType.Equals, Person1.CreatedDateTime)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_Equals_Value_Decimal_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.DecimalTest), FilterType.Equals, Person1.DecimalTest)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_Equals_Value_Enum_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.EnumTest), FilterType.Equals, Person1.EnumTest)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_Equals_Value_Guid_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Id), FilterType.Equals, Person1.Id)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_Equals_Value_Int32_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Int32Test), FilterType.Equals, Person1.Int32Test)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_Equals_Value_Int64_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Int64Test), FilterType.Equals, Person1.Int64Test)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_Equals_Value_String_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Name), FilterType.Equals, Person1.Name)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Name, person.Name);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_Equals_Value_SubDocument_String_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new($"{nameof(Person.Friend)}.{nameof(Person.Name)}", FilterType.Equals, Person1.Name)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person2.Name, person.Name);

        Assert.IsFalse(people.MoveNext());
    }

    #endregion

    #region Filter GreaterThan

    [TestMethod]
    public async Task Filter_GreaterThan_And_Contains_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Name), FilterType.GreaterThan, Person2.Name),
                    new(nameof(Person.Notes), FilterType.Contains, "o")
                },
                Sorters = new List<SortClause>()
                {
                    new(nameof(Person.Name), SortDirection.Ascending)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Name, person.Name);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_GreaterThan_Value_Bool_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.BoolTest), FilterType.GreaterThan, false)
                },
                Sorters = new List<SortClause>()
                {
                    new(nameof(Person.Name), SortDirection.Descending)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_GreaterThan_Value_DateTimeOffset_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.CreatedDateTime), FilterType.GreaterThan, Person1.CreatedDateTime)
                },
                Sorters = new List<SortClause>()
                {
                    new(nameof(Person.Name), SortDirection.Ascending)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreNotEqual(Person1.Id, person.Id);
    }

    [TestMethod]
    public async Task Filter_GreaterThan_Value_Decimal_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.DecimalTest), FilterType.GreaterThan, Person1.DecimalTest - 1)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_GreaterThan_Value_Int32_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Int32Test), FilterType.GreaterThan, Person1.Int32Test - 1)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_GreaterThan_Value_Int64_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Int64Test), FilterType.GreaterThan, Person1.Int64Test - 1)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_GreaterThan_Value_String_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Name), FilterType.GreaterThan, Person1.Name)
                },
                Sorters = new List<SortClause>()
                {
                    new(nameof(Person.Name), SortDirection.Ascending)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual("SomeGuy0", person.Name);

        Assert.IsTrue(people.MoveNext());
    }

    #endregion

    #region Filter In

    [TestMethod]
    public async Task Filter_In_And_Contains_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Name), FilterType.In, new string[] { Person1.Name, Person2.Name }),
                    new(nameof(Person.Notes), FilterType.Contains, "o")
                },
                Sorters = new List<SortClause>()
                {
                    new(nameof(Person.Name), SortDirection.Ascending)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person2.Name, person.Name);

        Assert.IsTrue(people.MoveNext());

        person = people.Current;
        Assert.AreEqual(Person1.Name, person.Name);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_In_Value_Bool_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.BoolTest), FilterType.In, new bool[] { Person1.BoolTest })
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_In_Value_DateTimeOffset_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.CreatedDateTime), FilterType.In, new DateTimeOffset[] { DateTimeOffset.MinValue, Person1.CreatedDateTime })
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_In_Value_Decimal_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.DecimalTest), FilterType.In, new decimal[] { Person1.DecimalTest })
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_In_Value_Enum_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.EnumTest), FilterType.In, new FilterType[] { Person1.EnumTest })
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_In_Value_Guid_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Id), FilterType.In, new Guid[] { Person1.Id })
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_In_Value_Int32_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Int32Test), FilterType.In, new int[] { Person1.Int32Test })
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_In_Value_Int64_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Int64Test), FilterType.In, new long[] { Person1.Int64Test })
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person1.Id, person.Id);

        Assert.IsFalse(people.MoveNext());
    }

    [TestMethod]
    public async Task Filter_In_Value_String_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Filters = new List<FilterClause>()
                {
                    new(nameof(Person.Name), FilterType.In, new string[] { Person1.Name, Person2.Name })
                },
                Sorters = new List<SortClause>()
                {
                    new(nameof(Person.Name), SortDirection.Ascending)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person2.Name, person.Name);

        Assert.IsTrue(people.MoveNext());

        person = people.Current;
        Assert.AreEqual(Person1.Name, person.Name);

        Assert.IsFalse(people.MoveNext());
    }

    #endregion

    #region Sort

    [TestMethod]
    public async Task Sort_Asc_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Sorters = new List<SortClause>()
                {
                    new(nameof(Person.Name), SortDirection.Ascending)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual(Person2.Name, person.Name);
    }

    [TestMethod]
    public async Task Sort_Desc_Test()
    {
        DataGridResponse<Person> response = await DataGridQueryService.QueryAsync(
            new DataGridRequest<Person>()
            {
                Sorters = new List<SortClause>()
                {
                    new(nameof(Person.Name), SortDirection.Descending)
                }
            });

        using IEnumerator<Person> people = response.Data.GetEnumerator();
        Assert.IsTrue(people.MoveNext());

        Person person = people.Current;
        Assert.AreEqual("SomeGuy99", person.Name);
    }

    #endregion
}
