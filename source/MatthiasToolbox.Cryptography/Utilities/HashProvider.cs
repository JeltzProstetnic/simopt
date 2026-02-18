using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MatthiasToolbox.Cryptography.Checksums;
using MatthiasToolbox.Cryptography.Utilities;
using MatthiasToolbox.Cryptography.Hashes;
using MatthiasToolbox.Cryptography.Enumerations;

namespace MatthiasToolbox.Cryptography.Utilities
{
    public static class HashProvider
    {
        public static String Get(Algorithm algorithm, HashEncoding coding, Stream data, HashMode hashMode, byte[] iv)
        {
            if(hashMode != HashMode.Default)
            {
                if (iv == null || iv.Length < 1) hashMode = HashMode.Default;
            }
            byte[] hash = null;
            switch(algorithm)
            {
                case Algorithm.Adler32:
                    hash = adler32(data, hashMode, iv);
                    break;
                case Algorithm.CRC32:
                    hash = crc32(data, hashMode, iv);
                    break;
                case Algorithm.MD4:
                    hash = md4(data, hashMode, iv);
                    break;
                case Algorithm.MD5:
                    hash = md5(data, hashMode, iv);
                    break;
                case Algorithm.RIPEMED160:
                    hash = ripemed160(data, hashMode, iv);
                    break;
                case Algorithm.SHA1:
                    hash = sha1(data, hashMode, iv);
                    break;
                case Algorithm.SHA256:
                    hash = sha256(data, hashMode, iv);
                    break;
                case Algorithm.SHA384:
                    hash = sha384(data, hashMode, iv);
                    break;
                case Algorithm.SHA512:
                    hash = sha512(data, hashMode, iv);
                    break;
            }
            return Encode(coding, hash);
        }
        public static String Get(Algorithm algorithm, HashEncoding coding, Stream data)
        {
            byte[] hash = null;
            switch (algorithm)
            {
                case Algorithm.Adler32:
                    hash = adler32(data, HashMode.Default, null);
                    break;
                case Algorithm.CRC32:
                    hash = crc32(data, HashMode.Default, null);
                    break;
                case Algorithm.MD4:
                    hash = md4(data, HashMode.Default, null);
                    break;
                case Algorithm.MD5:
                    hash = md5(data, HashMode.Default, null);
                    break;
                case Algorithm.RIPEMED160:
                    hash = ripemed160(data, HashMode.Default, null);
                    break;
                case Algorithm.SHA1:
                    hash = sha1(data, HashMode.Default, null);
                    break;
                case Algorithm.SHA256:
                    hash = sha256(data, HashMode.Default, null);
                    break;
                case Algorithm.SHA384:
                    hash = sha384(data, HashMode.Default, null);
                    break;
                case Algorithm.SHA512:
                    hash = sha512(data, HashMode.Default, null);
                    break;
            }
            return Encode(coding, hash);
        }
        private static String Encode(HashEncoding coding, byte[] data)
        {
            switch (coding)
            {
                case HashEncoding.ASCII:
                    return Encoding.ASCII.GetString(data);
                case HashEncoding.Hexadecimal:
                    String result = "";
                    String token = "";
                    foreach (byte b in data)
                    {
                        token = b.ToString("X");
                        if (token.Length == 1) token = "0" + token;
                        result += token;// b.ToString("X");
                    }
                    return result;
                case HashEncoding.Base64:
                    return Convert.ToBase64String(data);
                default:
                    return "";
            }
        }
        private static byte[] adler32(Stream data)
        {
            Adler32 a32 = new Adler32(1);
            byte[] all = new byte[data.Length];
            data.Read(all, 0, (int)data.Length);
            ulong a = a32.GetChecksum(all);
            byte[] result = new byte[4];
            result[0] = (byte)((a & 0xff000000) >> 24);
            result[1] = (byte)((a & 0x00ff0000) >> 16);
            result[2] = (byte)((a & 0x0000ff00) >> 8);
            result[3] = (byte)(a & 0x000000ff);
            return result;
        }
        private static byte[] adler32(Stream data, HashMode hashMode, byte[] iv)
        {
            switch (hashMode)
            {
                case HashMode.Default:
                    return adler32(data);
                case HashMode.IV:
                    Stream dataiv = new PostfixStream(data, iv);
                    return concat(adler32(dataiv), iv);
                case HashMode.RMX:
                    Stream datarmx = new RMXStream(data, iv);
                    return adler32(datarmx);
            }
            return null;
        }
        private static byte[] crc32(Stream data)
        {
            CRC32 crc = new CRC32();
            data.Seek(0, 0);
            int a = crc.GetCrc32(data);
            byte[] result = new byte[4];
            result[0] = (byte)((a & 0xff000000) >> 24);
            result[1] = (byte)((a & 0x00ff0000) >> 16);
            result[2] = (byte)((a & 0x0000ff00) >> 8);
            result[3] = (byte)(a & 0x000000ff);
            return result;
        }
        private static byte[] crc32(Stream data, HashMode hashMode, byte[] iv)
        {
            switch (hashMode)
            {
                case HashMode.Default:
                    return crc32(data);
                case HashMode.IV:
                    Stream dataiv = new PostfixStream(data, iv);
                    return concat(crc32(dataiv), iv);
                case HashMode.RMX:
                    Stream datarmx = new RMXStream(data, iv);
                    return crc32(datarmx);
            }
            return null;
        }
        private static byte[] md4(Stream data, HashMode hashMode, byte[] iv)
        {
            MD4 md4 = new MD4();
            switch (hashMode)
            {
                case HashMode.Default:
                    byte[] all = new byte[data.Length];
                    data.Read(all, 0, (int)data.Length);
                    return md4.GetByteHashFromBytes(all);
                case HashMode.IV:
                    Stream dataiv = new PostfixStream(data, iv);
                    byte[] alliv = new byte[dataiv.Length];
                    dataiv.Read(alliv, 0, (int)dataiv.Length);
                    return concat(md4.GetByteHashFromBytes(alliv), iv);
                case HashMode.RMX:
                    Stream datarmx = new RMXStream(data, iv);
                    byte[] allrmx = new byte[datarmx.Length];
                    datarmx.Read(allrmx, 0, (int)datarmx.Length);
                    return md4.GetByteHashFromBytes(allrmx);
            }
            return null;
        }
        private static byte[] md5(Stream data, HashMode hashMode, byte[] iv)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            switch(hashMode)
            {
                case HashMode.Default:
                    return md5.ComputeHash(data);
                case HashMode.IV:
                    Stream dataiv = new PostfixStream(data, iv);
                    return concat(md5.ComputeHash(dataiv), iv);
                case HashMode.RMX:
                    Stream datarmx = new RMXStream(data, iv);
                    return md5.ComputeHash(datarmx);
            }
            return null;
        }
        private static byte[] ripemed160(Stream data, HashMode hashMode, byte[] iv)
        {
            RIPEMD160Managed ripemed160 = new RIPEMD160Managed();
            switch (hashMode)
            {
                case HashMode.Default:
                    return ripemed160.ComputeHash(data);
                case HashMode.IV:
                    Stream dataiv = new PostfixStream(data, iv);
                    return concat(ripemed160.ComputeHash(dataiv), iv);
                case HashMode.RMX:
                    Stream datarmx = new RMXStream(data, iv);
                    return ripemed160.ComputeHash(datarmx);
            }
            return null;
        }
        private static byte[] sha1(Stream data, HashMode hashMode, byte[] iv)
        {
            SHA1Managed sha1 = new SHA1Managed();
            switch (hashMode)
            {
                case HashMode.Default:
                    return sha1.ComputeHash(data);
                case HashMode.IV:
                    Stream dataiv = new PostfixStream(data, iv);
                    return concat(sha1.ComputeHash(dataiv), iv);
                case HashMode.RMX:
                    Stream datarmx = new RMXStream(data, iv);
                    return sha1.ComputeHash(datarmx);
            }
            return null;
        }
        private static byte[] sha256(Stream data, HashMode hashMode, byte[] iv)
        {
            SHA256Managed sha256 = new SHA256Managed();
            switch (hashMode)
            {
                case HashMode.Default:
                    return sha256.ComputeHash(data);
                case HashMode.IV:
                    Stream dataiv = new PostfixStream(data, iv);
                    return concat(sha256.ComputeHash(dataiv), iv);
                case HashMode.RMX:
                    Stream datarmx = new RMXStream(data, iv);
                    return sha256.ComputeHash(datarmx);
            }
            return null;
        }
        private static byte[] sha384(Stream data, HashMode hashMode, byte[] iv)
        {
            SHA384Managed sha384 = new SHA384Managed();
            switch (hashMode)
            {
                case HashMode.Default:
                    return sha384.ComputeHash(data);
                case HashMode.IV:
                    Stream dataiv = new PostfixStream(data, iv);
                    return concat(sha384.ComputeHash(dataiv), iv);
                case HashMode.RMX:
                    Stream datarmx = new RMXStream(data, iv);
                    return sha384.ComputeHash(datarmx);
            }
            return null;
        }
        private static byte[] sha512(Stream data, HashMode hashMode, byte[] iv)
        {
            SHA512Managed sha512 = new SHA512Managed();
            switch (hashMode)
            {
                case HashMode.Default:
                    return sha512.ComputeHash(data);
                case HashMode.IV:
                    Stream dataiv = new PostfixStream(data, iv);
                    return concat(sha512.ComputeHash(dataiv), iv);
                case HashMode.RMX:
                    Stream datarmx = new RMXStream(data, iv);
                    return sha512.ComputeHash(datarmx);
            }
            return null;
        }

        private static byte[] concat(byte[] a, byte[] b)
        {
            byte[] result = new byte[a.Length + b.Length];

            for (int i = 0; i < a.Length; i++) result[i] = a[i];

            for (int i = 0; i < b.Length; i++) result[a.Length + i] = b[i];

            return result;
        }
    }
}
