using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DocumentStorage.Infrastructure.EntityFramework;

/// <summary>
/// Interceptor that handles encryption of entity properties before they are saved to the database.
/// This class extends <see cref="SaveChangesInterceptor"/> to intercept the saving changes process
/// and apply encryption to relevant entities.
/// </summary>
public sealed class EncryptInterceptor : SaveChangesInterceptor
{
    private readonly IEncryptionProvider _encryptionProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptInterceptor"/> class.
    /// </summary>
    /// <param name="encryptionProvider">The encryption provider used to encrypt entity properties.</param>
    public EncryptInterceptor(IEncryptionProvider encryptionProvider) => _encryptionProvider = encryptionProvider;

    /// <summary>
    /// Intercepts the saving changes process and encrypts properties of entities marked for addition or modification.
    /// </summary>
    /// <param name="eventData">The event data for the DbContext event.</param>
    /// <param name="result">The current result of the interception.</param>
    /// <returns>An <see cref="InterceptionResult{T}"/> representing the result of the interception.</returns>
    public sealed override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        eventData.Context.ChangeTracker.DetectChanges();
        var pendingEntities = eventData.Context.ChangeTracker.Entries()
            .Where(c => c.State.Equals(EntityState.Added) || c.State.Equals(EntityState.Modified)).ToList();

        foreach (var entry in pendingEntities)
            entry.Entity.EncryptEntity(_encryptionProvider);

        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Asynchronously intercepts the saving changes process and encrypts properties of entities marked for addition or modification.
    /// </summary>
    /// <param name="eventData">The event data for the DbContext event.</param>
    /// <param name="result">The current result of the interception.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, with an <see cref="InterceptionResult{T}"/> as the result.</returns>
    public sealed override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        eventData.Context.ChangeTracker.DetectChanges();
        var pendingEntities = eventData.Context.ChangeTracker.Entries()
            .Where(c => c.State.Equals(EntityState.Added) || c.State.Equals(EntityState.Modified)).ToList();

        foreach (var entry in pendingEntities)
            entry.Entity.EncryptEntity(_encryptionProvider);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

/// <summary>
/// Interceptor that handles decryption of entity properties after they are materialized from the database.
/// This class implements <see cref="IMaterializationInterceptor"/> to intercept the materialization process
/// and apply decryption to relevant entities.
/// </summary>
public sealed class DecryptInterceptor : IMaterializationInterceptor
{
    private readonly IEncryptionProvider _encryptionProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DecryptInterceptor"/> class.
    /// </summary>
    /// <param name="encryptionProvider">The encryption provider used to decrypt entity properties.</param>
    public DecryptInterceptor(IEncryptionProvider encryptionProvider) => _encryptionProvider = encryptionProvider;

    /// <summary>
    /// Intercepts the initialization of an entity instance and decrypts its properties.
    /// </summary>
    /// <param name="materializationData">The data for the materialization process.</param>
    /// <param name="instance">The instance of the entity being initialized.</param>
    /// <returns>The instance with decrypted properties.</returns>
    public object InitializedInstance(MaterializationInterceptionData materializationData, object instance) => instance.DecryptEntity(_encryptionProvider);
}
