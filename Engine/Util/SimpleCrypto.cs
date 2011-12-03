using System;
using System.IO;
using System.Security.Cryptography;

namespace Engine.Util
{/// <summary>
    /// Utility class that provides a simplified interface to
    /// encrypting and decrypting data in the form of byte[]
    /// using AES (via the RijndaelManaged class).
    /// </summary>
    public sealed class SimpleCrypto
    {
        #region Statics

        /// <summary>
        /// Generates a random key.
        /// </summary>
        /// <returns>a new, random key.</returns>
        static public byte[] GenerateKey()
        {
            using (RijndaelManaged rm = new RijndaelManaged())
            {
                rm.GenerateKey();
                return rm.Key;
            }
        }

        /// <summary>
        /// Generates a random initialization vector.
        /// </summary>
        /// <returns>a new, random initialization vector.</returns>
        static public byte[] GenerateVector()
        {
            using (RijndaelManaged rm = new RijndaelManaged())
            {
                rm.GenerateIV();
                return rm.IV;
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// The key to use for encrypting / decrypting data.
        /// </summary>
        private byte[] key;

        /// <summary>
        /// The initial vector to use.
        /// </summary>
        private byte[] vector;

        #endregion

        #region Constructor / Cleanup

        /// <summary>
        /// Creates a new cryptography provider using the given key and initialization vector.
        /// </summary>
        /// <param name="key">the key to use.</param>
        /// <param name="vector">the initialization vector to use.</param>
        public SimpleCrypto(byte[] key, byte[] vector)
        {
            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException("key");
            }
            if (vector == null || vector.Length <= 0)
            {
                throw new ArgumentNullException("vector");
            }
            this.key = key;
            this.vector = vector;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Encrypts a complete byte array.
        /// </summary>
        /// <param name="value">the byte array to encrypt.</param>
        /// <returns>the encrypted data.</returns>
        public byte[] Encrypt(byte[] value)
        {
            return Encrypt(value, 0, value.Length);
        }

        /// <summary>
        /// Encrypts part of a byte array.
        /// </summary>
        /// <param name="value">the byte array to encrypt.</param>
        /// <param name="offset">the position to start reading at.</param>
        /// <param name="length">the number of bytes to read.</param>
        /// <returns>the encrypted data.</returns>
        public byte[] Encrypt(byte[] value, int offset, int length)
        {
            if (value == null || value.Length <= 0)
            {
                throw new ArgumentNullException("value");
            }
            using (MemoryStream output = new MemoryStream())
            {
                using (var rijndael = new RijndaelManaged())
                using (var transform = rijndael.CreateEncryptor(key, vector))
                using (var stream = new CryptoStream(output, transform, CryptoStreamMode.Write))
                {
                    stream.Write(value, offset, length);
                }
                return output.ToArray();
            }
        }

        /// <summary>
        /// Decrypts a complete byte array.
        /// </summary>
        /// <param name="value">the byte array to decrypt.</param>
        /// <returns>the decrypted data.</returns>
        public byte[] Decrypt(byte[] value)
        {
            return Decrypt(value, 0, value.Length);
        }

        /// <summary>
        /// Decrypts part of a byte array.
        /// </summary>
        /// <param name="value">the byte array to decrypt.</param>
        /// <param name="offset">the position to start reading at.</param>
        /// <param name="length">the number of bytes to read.</param>
        /// <returns>the decrypted data.</returns>
        public byte[] Decrypt(byte[] value, int offset, int length)
        {
            if (value == null || value.Length <= 0)
            {
                throw new ArgumentNullException("value");
            }
            using (MemoryStream output = new MemoryStream())
            {
                try
                {
                    using (var rijndael = new RijndaelManaged())
                    using (var transform = rijndael.CreateDecryptor(key, vector))
                    using (var stream = new CryptoStream(output, transform, CryptoStreamMode.Write))
                    {
                        stream.Write(value, offset, length);
                    }
                    return output.ToArray();
                }
                catch (CryptographicException)
                {
                    return null;
                }
            }
        }

        #endregion
    }
}
