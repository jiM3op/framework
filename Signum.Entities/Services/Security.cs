using System.Security.Cryptography;

namespace Signum.Services;

public static class Security
{
    public static Func<string, byte[]> EncodePassword = (string originalPassword) => MD5Hash(originalPassword);

    public static byte[] MD5Hash(string saltedPassword)
    {
        byte[] originalBytes = ASCIIEncoding.Default.GetBytes(saltedPassword);
        byte[] encodedBytes = MD5.Create().ComputeHash(originalBytes);
        return encodedBytes;
    }

    public static string GetSHA1(string str)
    {
        SHA1 sha1 = SHA1.Create();
        ASCIIEncoding encoding = new ASCIIEncoding();
        StringBuilder sb = new StringBuilder();
        byte[] stream = sha1.ComputeHash(encoding.GetBytes(str));
        for (int i = 0; i < stream.Length; i++)
            sb.AppendFormat("{0:x2}", stream[i]);
        return sb.ToString();
    }

 
}

public class CryptorEngine
{
    public static string CalculateMD5Hash(byte[] inputBytes)
    {
        MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(inputBytes);

        // step 2, convert byte array to hex string
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
        {
            sb.Append(hash[i].ToString("X2"));
        }
        return sb.ToString();
    }
}
