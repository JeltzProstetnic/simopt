///////////////////////////////////////////////////////////////////////////////////////
//
//    Project:     BlueLogic.SDelta
//    Description: 
//
// ------------------------------------------------------------------------------------
//    Copyright 2007 by Bluelogic Software Solutions.
//    see product licence ( creative commons attribution 3.0 )
// ------------------------------------------------------------------------------------
//
//    History:
//
//    Dienstag, 8. Mai 2007 Matthias Gruber original version
//
//
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace BlueLogic.PasswordKeeper
{
    /// <summary>
    /// a wrapper class for the .net cryptography providers
    /// </summary>
    public static class CryptoWrapper
    {

        //public char[] CharacterData
        //{
        //    get
        //    {
        //        char[] bytes = new char[_secureEntry.Length];
        //        IntPtr ptr = IntPtr.Zero;

        //        try
        //        {
        //            ptr = Marshal.SecureStringToBSTR(_secureEntry);
        //            bytes = new char[_secureEntry.Length];
        //            Marshal.Copy(ptr, bytes, 0, _secureEntry.Length);
        //        }
        //        finally
        //        {
        //            if (ptr != IntPtr.Zero)
        //                Marshal.ZeroFreeBSTR(ptr);
        //        }

        //        return bytes;
        //    }
        //}
        
        internal static void SecureConvert(ref SecureString sString, ref char[] result)
        {
            result = new char[sString.Length];
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToBSTR(sString);
                String theString = Marshal.PtrToStringBSTR(ptr);
                result = theString.ToCharArray();
                
                //Marshal.Copy(ptr, result, 0, sString.Length);
                // todo: secure more?
                
                
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(ptr);
            }
        }
        
        public static void SecureConvert(ref SecureString sString, ref byte[] result)
        {
            result = new byte[sString.Length];
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.SecureStringToBSTR(sString);

                // The String in the SecureString
                String theString = Marshal.PtrToStringBSTR(ptr); 
                
                result = Encoding.ASCII.GetBytes(theString);
                //Marshal.Copy(ptr, result, 0, sString.Length);
                // todo: secure more
                
                
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(ptr);
            }
        }
        
        public static void GetRandomBytes(ref byte[] rnd)
        {
            // Initialize a random number generator.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

            // Fill the salt with cryptographically strong byte values.
            rng.GetNonZeroBytes(rnd);
        }

        public static void GetRijPasword(ref SecureString key)
        {
            SymmetricAlgorithm sym = new RijndaelManaged();
            key = GetPassword(ref sym);
            sym.Clear();
        }

        private static SecureString GetPassword(ref SymmetricAlgorithm mySymmetricAlgorithm)
        {
            // ASCIIEncoding textConverter = new ASCIIEncoding();
            mySymmetricAlgorithm.GenerateKey();
            SecureString result = new SecureString();
            foreach (byte b in mySymmetricAlgorithm.Key)
            {
                result.AppendChar((char)b);
            }
            return result; // textConverter.GetString(mySymmetricAlgorithm.Key, 0, mySymmetricAlgorithm.Key.Length);
        }

        private static SecureString Encrypt(ref SymmetricAlgorithm mySymmetricAlgorithm, ref SecureString key, ref SecureString data)
        {
            ASCIIEncoding textConverter = new ASCIIEncoding();
            
            byte[] encrypted;

            byte[] toEncrypt = null;
            SecureConvert(ref data, ref toEncrypt); // = textConverter.GetBytes(data);

            byte[] myKey = null;
            SecureConvert(ref key, ref myKey); // = textConverter.GetBytes(key);
            
            byte[] IV = textConverter.GetBytes("BlueLogicSoftWar");
            
            //Get an encryptor.
            ICryptoTransform encryptor = mySymmetricAlgorithm.CreateEncryptor(myKey, IV);

            //Encrypt the data.
            MemoryStream msEncrypt = new MemoryStream();
            CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);

            //Write all data to the crypto stream and flush it.
            csEncrypt.Write(toEncrypt, 0, toEncrypt.Length);
            csEncrypt.FlushFinalBlock();
            
            //Get encrypted array of bytes.
            encrypted = msEncrypt.ToArray();
            // char[] carr = textConverter.GetString(encrypted, 0, encrypted.Length).ToCharArray();
            SecureString s = new SecureString();
            foreach (byte b in encrypted)
            {
                s.AppendChar((char)b);
            }
            // Clear(b);
            // Clear(encrypted);
            csEncrypt.Clear();
            return s;
        }

        private static SecureString Decrypt(ref SymmetricAlgorithm mySymmetricAlgorithm, ref SecureString key, SecureString data)
        {
            ASCIIEncoding textConverter = new ASCIIEncoding();

            byte[] decrypted;
            
            byte[] toDecrypt = null;
            SecureConvert(ref data, ref toDecrypt); // = textConverter.GetBytes(data);
            
            byte[] myKey = null;
            SecureConvert(ref key, ref myKey); // = textConverter.GetBytes(key);
            
            byte[] IV = textConverter.GetBytes("BlueLogicSoftWar");

            //Get a decryptor that uses the same key and IV as the encryptor.
            ICryptoTransform decryptor = mySymmetricAlgorithm.CreateDecryptor(myKey, IV);

            MemoryStream msDecrypt = new MemoryStream(toDecrypt);
            CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);

            decrypted = new byte[toDecrypt.Length];

            //Read the data out of the crypto stream.
            csDecrypt.Read(decrypted, 0, decrypted.Length);

            SecureString result = new SecureString();
            for (int i = 1; i < csDecrypt.Length; i++ )
            {
                result.AppendChar((char)csDecrypt.ReadByte());
            }
            
            //SecureConvert the byte array back into a string.
            return result; // textConverter.GetString(decrypted);
        }

        //public static String Encrypt3DES(String key, String data)
        //{
        //    SymmetricAlgorithm sym = TripleDES.Create();
        //    return Encrypt(ref sym, ref key, ref data);
        //}

        //public static String EncryptRC2(String key, String data)
        //{
        //    SymmetricAlgorithm sym = RC2.Create();
        //    return Encrypt(ref sym, ref key, ref data);
        //}

        public static SecureString EncryptRiJ(SecureString key, ref SecureString data)
        {
            SymmetricAlgorithm sym = new RijndaelManaged();
            SecureString result = Encrypt(ref sym, ref key, ref data);
            sym.Clear();
            return result;
        }

        //public static String Decrypt3DES(String key, String data)
        //{
        //    SymmetricAlgorithm sym = TripleDES.Create();
        //    return Decrypt(ref sym, ref key, ref data);
        //}

        //public static String DecryptRC2(String key, String data)
        //{
        //    SymmetricAlgorithm sym = RC2.Create();
        //    return Decrypt(ref sym, ref key, ref data);
        //}

        public static void DecryptRiJ(SecureString key, SecureString data, ref SecureString result)
        {
            SymmetricAlgorithm sym = new RijndaelManaged();
            result = Decrypt(ref sym, ref key, data);
            sym.Clear();
        }
        
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
     }
 }
