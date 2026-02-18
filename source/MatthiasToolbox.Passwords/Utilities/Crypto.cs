using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace MatthiasToolbox.Passwords.Utilities
{
    /// <summary>
    /// a crypto wrapper by Matthias Gruber
    /// </summary>
    public static class Crypto
    {
        #region classvars

        private static SymmetricAlgorithm rsa = new RijndaelManaged();
        internal static byte[] IV = Encoding.UTF8.GetBytes("BlueLogicSoftWar");
        internal static byte[] IVP;
        
        #endregion
        #region public accessors

        #region hashes

        /// <summary>
        /// Generates a hash for the given plain text value and returns a
        /// base64-encoded result. Before the hash is computed, a random salt
        /// is generated and appended to the plain text. This salt is stored at
        /// the end of the hash value, so it can be used later for hash
        /// verification.
        /// </summary>
        /// <param name="plainText">
        /// Plaintext value to be hashed. The function does not check whether
        /// this parameter is null.
        /// </param>
        /// <param name="hashAlgorithm">
        /// Name of the hash algorithm. Allowed values are: "MD5", "SHA1",
        /// "SHA256", "SHA384", "RIPEMED160" and "SHA512" (if any other value is specified
        /// MD5 hashing algorithm will be used). This value is case-insensitive.
        /// </param>
        /// <param name="saltBytes">
        /// Salt bytes. This parameter can be null, in which case a random salt
        /// value will be generated.
        /// </param>
        /// <returns>
        /// Hash value formatted as a base64-encoded string.
        /// </returns>
        public static string ComputeHash(string plainText,
                                             string hashAlgorithm,
                                             byte[] saltBytes)
            {
                // If salt is not specified, generate it on the fly.
                if (saltBytes == null)
                {
                    // Define min and max salt sizes.
                    int minSaltSize = 4;
                    int maxSaltSize = 8;

                    // Generate a random number for the size of the salt.
                    Random random = new Random();
                    int saltSize = random.Next(minSaltSize, maxSaltSize);

                    // Allocate a byte array, which will hold the salt.
                    saltBytes = new byte[saltSize];

                    // Initialize a random number generator.
                    RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

                    // Fill the salt with cryptographically strong byte values.
                    rng.GetNonZeroBytes(saltBytes);
                }

                // SecureConvert plain text into a byte array.
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

                // Allocate array, which will hold plain text and salt.
                byte[] plainTextWithSaltBytes =
                        new byte[plainTextBytes.Length + saltBytes.Length];

                // Copy plain text bytes into resulting array.
                for (int i = 0; i < plainTextBytes.Length; i++)
                    plainTextWithSaltBytes[i] = plainTextBytes[i];

                // Append salt bytes to the resulting array.
                for (int i = 0; i < saltBytes.Length; i++)
                    plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];

                // Because we support multiple hashing algorithms, we must define
                // hash object as a common (abstract) base class. We will specify the
                // actual hashing algorithm class later during object creation.
                HashAlgorithm hash;

                // Make sure hashing algorithm name is specified.
                if (hashAlgorithm == null)
                    hashAlgorithm = "";

                // Initialize appropriate hashing algorithm class.
                switch (hashAlgorithm.ToUpper())
                {
                    case "SHA1":
                        hash = new SHA1Managed();
                        break;

                    case "SHA256":
                        hash = new SHA256Managed();
                        break;

                    case "SHA384":
                        hash = new SHA384Managed();
                        break;

                    case "SHA512":
                        hash = new SHA512Managed();
                        break;

                    case "RIPEMED160":
                        hash = new RIPEMD160Managed();
                        break;
                        
                    default:
                        hash = new MD5CryptoServiceProvider();
                        break;
                }

                String hashValue;
                using(hash)
                {
                    // Compute hash value of our plain text with appended salt.
                    byte[] hashBytes = hash.ComputeHash(plainTextWithSaltBytes);

                    // Create array which will hold hash and original salt bytes.
                    byte[] hashWithSaltBytes = new byte[hashBytes.Length +
                                                        saltBytes.Length];

                    // Copy hash bytes into resulting array.
                    for (int i = 0; i < hashBytes.Length; i++)
                        hashWithSaltBytes[i] = hashBytes[i];

                    // Append salt bytes to the result.
                    for (int i = 0; i < saltBytes.Length; i++)
                        hashWithSaltBytes[hashBytes.Length + i] = saltBytes[i];

                    // SecureConvert result into a base64-encoded string.
                    hashValue = Convert.ToBase64String(hashWithSaltBytes);

                    hash.Clear();
                }
                
                // Return the result.
                return hashValue;
            }

        /// <summary>
        /// calculate the hash value for the plaintext
        /// </summary>
        /// <param name="plainText">
        /// data source
        /// </param>
        /// <param name="hashAlgorithm">
        /// one of the .net cryptography hash providers
        /// </param>
        /// <param name="saltBytes">
        /// initialization vector
        /// </param>
        /// <returns>the hash value</returns>
        public static string ComputeHash(byte[] plainText,
                                             string hashAlgorithm,
                                             byte[] saltBytes)
        {
            // If salt is not specified, generate it on the fly.
            if (saltBytes == null)
            {
                // Define min and max salt sizes.
                int minSaltSize = 4;
                int maxSaltSize = 8;

                // Generate a random number for the size of the salt.
                Random random = new Random();
                int saltSize = random.Next(minSaltSize, maxSaltSize);

                // Allocate a byte array, which will hold the salt.
                saltBytes = new byte[saltSize];

                // Initialize a random number generator.
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

                // Fill the salt with cryptographically strong byte values.
                rng.GetNonZeroBytes(saltBytes);
            }

            // SecureConvert plain text into a byte array.
            byte[] plainTextBytes = plainText;

            // Allocate array, which will hold plain text and salt.
            byte[] plainTextWithSaltBytes =
                    new byte[plainTextBytes.Length + saltBytes.Length];

            // Copy plain text bytes into resulting array.
            for (int i = 0; i < plainTextBytes.Length; i++)
                plainTextWithSaltBytes[i] = plainTextBytes[i];

            // Append salt bytes to the resulting array.
            for (int i = 0; i < saltBytes.Length; i++)
                plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];

            // Because we support multiple hashing algorithms, we must define
            // hash object as a common (abstract) base class. We will specify the
            // actual hashing algorithm class later during object creation.
            HashAlgorithm hash;

            // Make sure hashing algorithm name is specified.
            if (hashAlgorithm == null)
                hashAlgorithm = "";

            // Initialize appropriate hashing algorithm class.
            switch (hashAlgorithm.ToUpper())
            {
                case "SHA1":
                    hash = new SHA1Managed();
                    break;

                case "SHA256":
                    hash = new SHA256Managed();
                    break;

                case "SHA384":
                    hash = new SHA384Managed();
                    break;

                case "SHA512":
                    hash = new SHA512Managed();
                    break;
                    
                case "RIPEMED160":
                    hash = new RIPEMD160Managed();
                    break;
                        
                default:
                    hash = new MD5CryptoServiceProvider();
                    break;
            }

            String hashValue;
            using(hash)
            {
                // Compute hash value of our plain text with appended salt.
                byte[] hashBytes = hash.ComputeHash(plainTextWithSaltBytes);

                // Create array which will hold hash and original salt bytes.
                byte[] hashWithSaltBytes = new byte[hashBytes.Length +
                                                    saltBytes.Length];

                // Copy hash bytes into resulting array.
                for (int i = 0; i < hashBytes.Length; i++)
                    hashWithSaltBytes[i] = hashBytes[i];

                // Append salt bytes to the result.
                for (int i = 0; i < saltBytes.Length; i++)
                    hashWithSaltBytes[hashBytes.Length + i] = saltBytes[i];

                // SecureConvert result into a base64-encoded string.
                hashValue = Convert.ToBase64String(hashWithSaltBytes);
            }

            // Return the result.
            return hashValue;
        }


        /// <summary>
        /// calculate the hash value for the plaintext
        /// </summary>
        /// <param name="plainText">
        /// data source
        /// </param>
        /// <param name="hashAlgorithm">
        /// one of the .net cryptography hash providers
        /// </param>
        /// <param name="saltBytes">
        /// initialization vector
        /// </param>
        /// <returns>the hash value</returns>
        public static string ComputeHash(Stream plainText,
                                             string hashAlgorithm,
                                             byte[] saltBytes)
        {
            // If salt is not specified, generate it on the fly.
            if (saltBytes == null)
            {
                // Define min and max salt sizes.
                int minSaltSize = 4;
                int maxSaltSize = 8;

                // Generate a random number for the size of the salt.
                Random random = new Random();
                int saltSize = random.Next(minSaltSize, maxSaltSize);

                // Allocate a byte array, which will hold the salt.
                saltBytes = new byte[saltSize];

                // Initialize a random number generator.
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

                // Fill the salt with cryptographically strong byte values.
                rng.GetNonZeroBytes(saltBytes);
            }

            Stream plainText1 = new PostfixStream(plainText, saltBytes);
            
            HashAlgorithm hash;

            // Make sure hashing algorithm name is specified.
            if (hashAlgorithm == null)
                hashAlgorithm = "";

            // Initialize appropriate hashing algorithm class.
            switch (hashAlgorithm.ToUpper())
            {
                case "SHA1":
                    hash = new SHA1Managed();
                    break;

                case "SHA256":
                    hash = new SHA256Managed();
                    break;

                case "SHA384":
                    hash = new SHA384Managed();
                    break;

                case "SHA512":
                    hash = new SHA512Managed();
                    break;
                    
                case "RIPEMED160":
                    hash = new RIPEMD160Managed();
                    break;
                        
                default:
                    hash = new MD5CryptoServiceProvider();
                    break;
            }

            String hashValue;
            using(hash)
            {
                // Compute hash value of our plain text with appended salt.
                byte[] hashBytes = hash.ComputeHash(plainText1);

                // Create array which will hold hash and original salt bytes.
                byte[] hashWithSaltBytes = new byte[hashBytes.Length +
                                                    saltBytes.Length];

                // Copy hash bytes into resulting array.
                for (int i = 0; i < hashBytes.Length; i++)
                    hashWithSaltBytes[i] = hashBytes[i];

                // Append salt bytes to the result.
                for (int i = 0; i < saltBytes.Length; i++)
                    hashWithSaltBytes[hashBytes.Length + i] = saltBytes[i];

                // SecureConvert result into a base64-encoded string.
                hashValue = Convert.ToBase64String(hashWithSaltBytes);
            }
            
            // Return the result.
            return hashValue;
        }
        
            /// <summary>
            /// Compares a hash of the specified plain text value to a given hash
            /// value. Plain text is hashed with the same salt value as the original
            /// hash.
            /// </summary>
            /// <param name="plainText">
            /// Plain text to be verified against the specified hash. The function
            /// does not check whether this parameter is null.
            /// </param>
            /// <param name="hashAlgorithm">
            /// Name of the hash algorithm. Allowed values are: "MD5", "SHA1", 
            /// "SHA256", "SHA384", "RIPEMED160" and "SHA512" (if any other value is specified,
            /// MD5 hashing algorithm will be used). This value is case-insensitive.
            /// </param>
            /// <param name="hashValue">
            /// Base64-encoded hash value produced by ComputeHash function. This value
            /// includes the original salt appended to it.
            /// </param>
            /// <returns>
            /// If computed hash mathes the specified hash the function the return
            /// value is true; otherwise, the function returns false.
            /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToUpper"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation", MessageId = "hashAlgorithm")]
            public static bool VerifyHash(string plainText,
                                          string hashAlgorithm,
                                          string hashValue)
            {
                // SecureConvert base64-encoded hash value into a byte array.
                byte[] hashWithSaltBytes = Convert.FromBase64String(hashValue);

                // We must know size of hash (without salt).
                int hashSizeInBits, hashSizeInBytes;

                // Make sure that hashing algorithm name is specified.
                if (hashAlgorithm == null)
                    hashAlgorithm = "";

                // Size of hash is based on the specified algorithm.
                switch (hashAlgorithm.ToUpper())
                {
                    case "SHA1":
                        hashSizeInBits = 160;
                        break;

                    case "SHA256":
                        hashSizeInBits = 256;
                        break;

                    case "SHA384":
                        hashSizeInBits = 384;
                        break;

                    case "SHA512":
                        hashSizeInBits = 512;
                        break;
                        
                    case "RIPEMED160":
                        hashSizeInBits = 160;
                        break;
                        
                    default: // Must be MD5
                        hashSizeInBits = 128;
                        break;
                }

                // SecureConvert size of hash from bits to bytes.
                hashSizeInBytes = hashSizeInBits / 8;

                // Make sure that the specified hash value is long enough.
                if (hashWithSaltBytes.Length < hashSizeInBytes)
                    return false;

                // Allocate array to hold original salt bytes retrieved from hash.
                byte[] saltBytes = new byte[hashWithSaltBytes.Length -
                                            hashSizeInBytes];

                // Copy salt from the end of the hash to the new array.
                for (int i = 0; i < saltBytes.Length; i++)
                    saltBytes[i] = hashWithSaltBytes[hashSizeInBytes + i];

                // Compute a new hash string.
                string expectedHashString =
                            ComputeHash(plainText, hashAlgorithm, saltBytes);

                // If the computed hash matches the specified hash,
             // the plain text value must be correct.
             return (hashValue == expectedHashString);
        }
        
        #endregion
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="encrypted"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static SecureString DecryptRSA(String encrypted, SecureString key)
        {
            ICryptoTransform ct;
            MemoryStream ms;
            CryptoStream cs;
            byte[] bytes;
            byte[] bkey = Encoding.UTF8.GetBytes(DecryptSecureString(key));
            
            if(IVP != null)
            {
                bkey = Pad(IVP, bkey);
            }
            
            ct = rsa.CreateDecryptor(bkey, IV);
            bytes = Convert.FromBase64String(encrypted);
            ms = new MemoryStream();
            cs = new CryptoStream(ms, ct, CryptoStreamMode.Write);
            cs.Write(bytes, 0, bytes.Length);
            cs.FlushFinalBlock();
            
            cs.Clear();
            cs.Close();
            
            rsa.Clear();
            
            char[] carr = Encoding.UTF8.GetString(ms.ToArray()).ToCharArray();
            
            SecureString result = new SecureString();
            foreach (char c in carr)
            {
                result.AppendChar(c);
            }
            // todo: clear char array
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static String EncryptRSA(SecureString plainText, SecureString key)
        {
            ICryptoTransform ct;
            MemoryStream ms;
            CryptoStream cs;
            byte[] bytes;
            byte[] bkey = Encoding.UTF8.GetBytes(DecryptSecureString(key));
            
            if (IVP != null)
            {
                bkey = Pad(IVP, bkey);
            }
            
            ct = rsa.CreateEncryptor(bkey, IV);
            bytes = Encoding.UTF8.GetBytes(DecryptSecureString(plainText));
            ms = new MemoryStream();
            cs = new CryptoStream(ms, ct, CryptoStreamMode.Write);
            cs.Write(bytes, 0, bytes.Length);
            cs.FlushFinalBlock();
            
            cs.Clear();
            cs.Close();
            
            rsa.Clear();
            
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static String EncryptRSA(String plainText, SecureString key)
        {
            return EncryptRSA(EncryptSecureString(plainText), key);
        }

        /// <summary>
        /// Get a Rijndael key (256 bit)
        /// </summary>
        /// <returns>
        /// A 256 bit Rijndael key
        /// </returns>
        public static SecureString GetRSAKey()
        {
            SymmetricAlgorithm sym = new RijndaelManaged();
            SecureString result = new SecureString();
            try
            {
                sym.GenerateKey();
                foreach (byte b in sym.Key)
                {
                    result.AppendChar((char)b);
                }
            }
            finally
            {
                sym.Clear();
            }
            return result;
        }

        public static byte[] GetRSAIV()
        {
            SymmetricAlgorithm sym = new RijndaelManaged();
            byte[] result;
            try
            {
                sym.GenerateIV();
                result = sym.IV;
            }
            finally
            {
                sym.Clear();
            }
            return result;
        }
        
        /// <summary>
        /// fills the byte array with cryptographically strong random values
        /// </summary>
        /// <param name="rnd">
        /// byte array to fill with random values
        /// </param>
        public static void GetRandomBytes(ref byte[] rnd)
        {
            // Initialize a random number generator.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

            // Fill the salt with cryptographically strong byte values.
            rng.GetNonZeroBytes(rnd);
        }
        
        /// <summary>
        /// compare two SecureStrings and return true if they match
        /// </summary>
        /// <param name="s1">
        /// SecureString to compare
        /// </param>
        /// <param name="s2">
        /// SecureString to compare
        /// </param>
        /// <returns>
        /// true on match
        /// </returns>
        public static bool EqualSecureStrings(SecureString s1, SecureString s2)
        {
            return (DecryptSecureString(s1) == DecryptSecureString(s2));
        }
        
        /// <summary>
        /// return the plaintext from a SecureString
        /// </summary>
        /// <param name="encrypted">
        /// a SecureString
        /// </param>
        /// <returns>
        /// the plaintext
        /// </returns>
        internal static String DecryptSecureString(SecureString encrypted)
        {
            String theString;
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToBSTR(encrypted);
                theString = Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(ptr);
            }
            return theString;
        }
        
        internal static SecureString EncryptSecureString(String plainText)
        {
            SecureString result = new SecureString();
            foreach (char c in plainText)
            {
                result.AppendChar(c);
            }
            return result;
        }

        private static byte[] Pad(byte[] data, byte[] canvas)
        {
            if (data.Length < 1) return canvas;
            
            int len = Math.Max(data.Length, canvas.Length);
            len = Math.Min(len, 16);
            
            byte[] result = new byte[len];
            
            for (int i = 0; i < len; i++)
            {
                if (i < canvas.Length && i < data.Length)
                    result[i] = (byte)(((int)canvas[i] + (int)data[i]) % 255);
                else if (i < canvas.Length) result[i] = canvas[i];
                else result[i] = data[i];
            }
            return result;
        }
        
        #endregion
    } // class
} // namespace
