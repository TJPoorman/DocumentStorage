namespace DocumentStorage.Infrastructure.Tests;

public class DummyEncryptionProvider : IEncryptionProvider
{
    public string Decrypt(string value) => value;

    public string DecryptDeterministic(string value) => value;

    public string Encrypt(string value) => value;

    public string EncryptDeterministic(string value) => value;
}
