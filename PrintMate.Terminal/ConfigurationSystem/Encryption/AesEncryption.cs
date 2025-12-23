using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PrintMate.Terminal.ConfigurationSystem.Encryption
{
    /// <summary>
    /// Provides AES-256 encryption/decryption for sensitive configuration data.
    /// Uses machine-specific key derivation for additional security.
    /// </summary>
    public static class AesEncryption
    {
        private const int KeySize = 256;
        private const int IvSize = 16; // 128 bits for AES

        /// <summary>
        /// Encrypts plaintext using AES-256-CBC.
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <param name="passphrase">Encryption passphrase (should be stored securely)</param>
        /// <returns>Base64-encoded encrypted data with IV prepended</returns>
        public static string Encrypt(string plainText, string passphrase)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Derive key from passphrase using PBKDF2
                var key = DeriveKey(passphrase);
                aes.Key = key;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    // Write IV first (needed for decryption)
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cs, Encoding.UTF8))
                    {
                        writer.Write(plainText);
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// Decrypts AES-256 encrypted data.
        /// </summary>
        /// <param name="cipherText">Base64-encoded encrypted data</param>
        /// <param name="passphrase">Decryption passphrase</param>
        /// <returns>Decrypted plaintext</returns>
        public static string Decrypt(string cipherText, string passphrase)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = KeySize;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    var key = DeriveKey(passphrase);
                    aes.Key = key;

                    // Extract IV from the beginning of ciphertext
                    var iv = new byte[IvSize];
                    Array.Copy(cipherBytes, 0, iv, 0, IvSize);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(cipherBytes, IvSize, cipherBytes.Length - IvSize))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cs, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Failed to decrypt data. The passphrase may be incorrect.", ex);
            }
        }

        /// <summary>
        /// Derives a 256-bit key from a passphrase using PBKDF2.
        /// Combines passphrase with machine-specific salt for additional security.
        /// </summary>
        private static byte[] DeriveKey(string passphrase)
        {
            // Use machine name as salt (ensures configs are machine-bound if desired)
            // You can also use a fixed salt if configs need to be portable between machines
            var salt = Encoding.UTF8.GetBytes(Environment.MachineName + "_PrintMate_Salt");

            using (var deriveBytes = new Rfc2898DeriveBytes(passphrase, salt, 10000, HashAlgorithmName.SHA256))
            {
                return deriveBytes.GetBytes(KeySize / 8); // 256 bits = 32 bytes
            }
        }

        /// <summary>
        /// Generates a random secure passphrase suitable for encryption.
        /// </summary>
        public static string GeneratePassphrase(int length = 32)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=";
            var result = new char[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                var randomBytes = new byte[length];
                rng.GetBytes(randomBytes);

                for (int i = 0; i < length; i++)
                {
                    result[i] = chars[randomBytes[i] % chars.Length];
                }
            }

            return new string(result);
        }
    }
}
