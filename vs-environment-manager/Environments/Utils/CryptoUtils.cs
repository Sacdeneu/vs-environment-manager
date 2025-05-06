using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace vs_environment_manager.Environments.Utils
{
    public static class CryptoUtils
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("0123456789abcdef"); // 16 bytes key (exemple)
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("abcdef9876543210");  // 16 bytes IV

        public static string Encrypt(string plainText)
        {
            using Aes aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;

            using MemoryStream ms = new();
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (StreamWriter sw = new(cs))
            {
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            using Aes aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;

            using MemoryStream ms = new(Convert.FromBase64String(cipherText));
            using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader sr = new(cs);
            return sr.ReadToEnd();
        }
    }

}
