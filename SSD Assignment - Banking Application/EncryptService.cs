using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SSD_Assignment___Banking_Application
{
    public sealed class EncryptService
    {
        private static readonly Lazy<EncryptService> _instance =
             new Lazy<EncryptService>(() => new EncryptService());

        private readonly byte[] _masterKey;
        private const string KeyStorePath = "secure_key.bin";

        public static EncryptService Instance => _instance.Value;

        //If the secure key file exists, it is read and unprotected.
        //Otherwise, a new 256-bit AES key is generated and protected before being written to the secure key file.
        private EncryptService()
        {
            if (File.Exists(KeyStorePath))
            {
                byte[] protectedKey = File.ReadAllBytes(KeyStorePath);
                _masterKey = ProtectedData.Unprotect(protectedKey, null, DataProtectionScope.CurrentUser);
            }
            else
            {
                using (var rng = RandomNumberGenerator.Create())
                {
                    _masterKey = new byte[32]; // 256-bit AES key
                    rng.GetBytes(_masterKey);
                }

                byte[] protectedKey = ProtectedData.Protect(_masterKey, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(KeyStorePath, protectedKey);
            }
        }


        //Encrypts the given plaintext using the 256-bit AES key stored securely.
        public string Encrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext)) return plaintext;

            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = _masterKey;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plaintext);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        //Decrypts the given ciphertext using the 256-bit AES key stored securely.
        public string Decrypt(string ciphertext)
        {
            if (string.IsNullOrEmpty(ciphertext)) return ciphertext;

            byte[] buffer = Convert.FromBase64String(ciphertext);

            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = _masterKey;

                byte[] iv = new byte[16];
                Array.Copy(buffer, 0, iv, 0, 16);
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(buffer, 16, buffer.Length - 16))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}
