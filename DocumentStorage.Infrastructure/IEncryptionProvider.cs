namespace DocumentStorage.Infrastructure;

/// <summary>
/// Defines methods for encrypting and decrypting data using both standard and deterministic encryption techniques.
/// </summary>
public interface IEncryptionProvider
{
    /// <summary>
    /// Decrypts the specified encrypted value using standard encryption techniques.
    /// </summary>
    /// <param name="value">The encrypted string to decrypt.</param>
    /// <returns>The decrypted string.</returns>
    string Decrypt(string value);

    /// <summary>
    /// Decrypts the specified deterministically encrypted value.
    /// Deterministic decryption ensures that the same input value will always produce the same encrypted output.
    /// </summary>
    /// <param name="value">The deterministically encrypted string to decrypt.</param>
    /// <returns>The decrypted string.</returns>
    string DecryptDeterministic(string value);

    /// <summary>
    /// Encrypts the specified value using standard encryption techniques.
    /// </summary>
    /// <param name="value">The plain string to encrypt.</param>
    /// <returns>The encrypted string.</returns>
    string Encrypt(string value);

    /// <summary>
    /// Encrypts the specified value deterministically.
    /// Deterministic encryption ensures that the same input value will always produce the same encrypted output.
    /// This is useful for scenarios where encrypted values need to be searched or indexed.
    /// </summary>
    /// <param name="value">The plain string to encrypt deterministically.</param>
    /// <returns>The deterministically encrypted string.</returns>
    string EncryptDeterministic(string value);
}