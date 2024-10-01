using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DocumentStorage.Infrastructure;

/// <inheritdoc/>
/// <remarks>Provides default implementations for encrypting and decrypting data using both RSA and AES encryption algorithms.</remarks>
public class DefaultEncryptionProvider : IEncryptionProvider
{
    /// <summary>
    /// Gets or sets the path to the RSA public key file. Default is "public.key".
    /// </summary>
    public string PublicKeyPath { get; set; } = "public.key";

    /// <summary>
    /// Gets or sets the path to the RSA private key file. Default is "private.key".
    /// </summary>
    public string PrivateKeyPath { get; set; } = "private.key";

    /// <summary>
    /// Gets or sets the path to the AES key file. Default is "aes.key".
    /// </summary>
    public string AesKeyPath { get; set; } = "aes.key";

    /// <summary>
    /// Gets or sets the path to the AES initialization vector (IV) file. Default is "iv.key".
    /// </summary>
    public string AesIvPath { get; set; } = "iv.key";

    /// <inheritdoc/>
    /// <remarks>Decrypts the provided base64-encoded string using RSA with the private key.</remarks>
    public string Decrypt(string value)
    {
        byte[] dataToDecrypt = Convert.FromBase64String(value);
        using RSACryptoServiceProvider rsa = new();
        rsa.ImportParameters(GetKey(PrivateKeyPath));
        byte[] decryptedValue = rsa.Decrypt(dataToDecrypt, false);

        return Encoding.Unicode.GetString(decryptedValue);
    }

    /// <inheritdoc/>
    /// <remarks>Decrypts the provided base64-encoded string using AES with deterministic encryption.</remarks>
    public string DecryptDeterministic(string value)
    {
        byte[] dataToDecrypt = Convert.FromBase64String(value);
        var key = GetKeyDeterministic(AesKeyPath, AesIvPath);
        using Aes aes = Aes.Create();
        ICryptoTransform decryptor = aes.CreateDecryptor(key.key, key.iv);
        byte[] decryptedValue = PerformCryptography(dataToDecrypt, decryptor);

        return Encoding.Unicode.GetString(decryptedValue);
    }

    /// <inheritdoc/>
    /// <remarks>Encrypts the provided string using RSA with the public key and returns the result as a base64-encoded string.</remarks>
    public string Encrypt(string value)
    {
        byte[] dataToEncrypt = Encoding.Unicode.GetBytes(value);
        using RSACryptoServiceProvider rsa = new();
        rsa.ImportParameters(GetKey(PublicKeyPath));
        byte[] encryptedValue = rsa.Encrypt(dataToEncrypt, false);

        return Convert.ToBase64String(encryptedValue);
    }

    /// <inheritdoc/>
    /// <remarks>Encrypts the provided string using AES with deterministic encryption and returns the result as a base64-encoded string.</remarks>
    public string EncryptDeterministic(string value)
    {
        byte[] dataToEncrypt = Encoding.Unicode.GetBytes(value);
        var key = GetKeyDeterministic(AesKeyPath, AesIvPath);
        using Aes aes = Aes.Create();
        ICryptoTransform encryptor = aes.CreateEncryptor(key.key, key.iv);
        byte[] encryptedValue = PerformCryptography(dataToEncrypt, encryptor);

        return Convert.ToBase64String(encryptedValue);
    }

    private RSAParameters GetKey(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!File.Exists(path)) MakeKey();

        string privateKeyString = File.ReadAllText(path);
        var sr = new StringReader(privateKeyString);
        var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

        return (RSAParameters)xs.Deserialize(sr);
    }

    private (byte[] key, byte[] iv) GetKeyDeterministic(string keyPath, string ivPath)
    {
        ArgumentNullException.ThrowIfNull(keyPath);
        ArgumentNullException.ThrowIfNull(ivPath);

        if (!File.Exists(keyPath) || !File.Exists(ivPath)) MakeKeyDeterministic();

        using RSACryptoServiceProvider rsa = new();
        rsa.ImportParameters(GetKey(PrivateKeyPath));

        return (rsa.Decrypt(File.ReadAllBytes(keyPath), false), rsa.Decrypt(File.ReadAllBytes(ivPath), false));
    }

    private void MakeKey()
    {
        RSACryptoServiceProvider csp = new(2048);
        RSAParameters privateKey = csp.ExportParameters(true);
        RSAParameters publicKey = csp.ExportParameters(false);

        string publicKeyString;
        {
            var sw = new StringWriter();
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

            xs.Serialize(sw, publicKey);
            publicKeyString = sw.ToString();
            File.WriteAllText(PublicKeyPath, publicKeyString);
        }

        string privateKeyString;
        {
            var sw = new StringWriter();
            var xs = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));

            xs.Serialize(sw, privateKey);
            privateKeyString = sw.ToString();
            File.WriteAllText(PrivateKeyPath, privateKeyString);
        }
    }

    private void MakeKeyDeterministic()
    {
        using Aes aes = Aes.Create();
        aes.GenerateKey();
        using RSACryptoServiceProvider rsa = new();
        rsa.ImportParameters(GetKey(PublicKeyPath));

        File.WriteAllBytes(AesKeyPath, rsa.Encrypt(aes.Key, false));
        File.WriteAllBytes(AesIvPath, rsa.Encrypt(aes.IV, false));
    }

    private byte[] PerformCryptography(byte[] data, ICryptoTransform cryptoTransform)
    {
        using MemoryStream memoryStream = new();
        using CryptoStream cryptoStream = new(memoryStream, cryptoTransform, CryptoStreamMode.Write);
        cryptoStream.Write(data, 0, data.Length);
        cryptoStream.FlushFinalBlock();
        return memoryStream.ToArray();
    }
}
