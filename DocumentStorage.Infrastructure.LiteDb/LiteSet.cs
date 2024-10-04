using DocumentStorage.Domain;
using LiteDB;

namespace DocumentStorage.Infrastructure.LiteDb;

/// <summary>
/// Represents a LiteDB set of type <typeparamref name="T"/> in a LiteDB context.
/// </summary>
/// <typeparam name="T">The type of records, which must implement the <see cref="IDsRecord"/> interface.</typeparam>
public class LiteSet<T> where T : IDsRecord { }