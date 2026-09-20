namespace IgnisEducationSuite.ServerServices.Security;

public interface IEncryptionService
{
    string Encrypt(string plainText);

    string Decrypt(string encryptedText, string keyVersion);
}