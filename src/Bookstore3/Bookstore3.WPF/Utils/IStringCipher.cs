namespace Bookstore3.WPF.Utils;

public interface IStringCipher
{
    string Encrypt(string plainText);

    string Decrypt(string cipherText);
}
