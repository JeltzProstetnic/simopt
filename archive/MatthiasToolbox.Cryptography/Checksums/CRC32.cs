using System;
using System.IO;
using System.Text;
using MatthiasToolbox.Cryptography.Interfaces;

namespace MatthiasToolbox.Cryptography.Checksums
{
    /// <summary>
    /// Cyclic Redundancy Check for .NET
    /// </summary>
    public class CRC32 : IChecksumProvider<int>
    {
        #region cvar

        private uint[] crc32Table;
        private const int BUFFER_SIZE = 1024;

        #endregion
        #region prop

        #region IChecksumProvider<int>

        public string Name
        {
            get { return "CRC32"; }
        }

        public int InitialValue
        {
            get { return (int)(~0xFFFFFFFF); }
        }

        #endregion

        #endregion
        #region ctor

        /// <summary>
        /// This is the official polynomial used by CRC32 in PKZip
        /// Often the polynomial is shown reversed (04C11DB7)
        /// </summary>
        public CRC32()
        {
            uint dwPolynomial = 0xEDB88320;
            crc32Table = new uint[256];
            uint dwCrc;

            for (uint i = 0; i <= 255; i++)
            {
                dwCrc = i;
                for (int j = 8; j >= 1; j--)
                {
                    if ((dwCrc & 1) != 0)
                    {
                        dwCrc = ((dwCrc & 0xFFFFFFFE) / 2) & 0x7FFFFFFF;
                        dwCrc = dwCrc ^ dwPolynomial;
                    }
                    else
                    {
                        dwCrc = ((dwCrc & 0xFFFFFFFE) / 2) & 0x7FFFFFFF;
                    }
                }
                crc32Table[i] = dwCrc;
            }
        }

        #endregion
        #region impl

        public int GetCrc32(FileInfo file)
        {
            FileStream fs = null;

            try
            {
                fs = file.OpenRead();
                return GetCrc32(fs);
            }
            finally
            {
                if (fs != null) fs.Close();
            }
        }

        public int GetCrc32(Stream stream)
        {
            uint crc32Result = 0xFFFFFFFF;

            byte[] buffer = new byte[BUFFER_SIZE];
            int readSize = BUFFER_SIZE;

            int count = stream.Read(buffer, 0, readSize);
            int iLookup;
            while (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    iLookup = (int)((crc32Result & 0xFF) ^ buffer[i]);
                    // crc32Result = ((crc32Result & 0xFFFFFF00) \ 0x100) & 0xFFFFFF   // nasty shr 8 with vb :/
                    crc32Result = crc32Result >> 8;
                    crc32Result = crc32Result ^ crc32Table[iLookup];
                }
                count = stream.Read(buffer, 0, readSize);
            }

            return (int)~crc32Result;
        }

        public int GetCrc32(String text)
        {
            UnicodeEncoding ue = new UnicodeEncoding();
            byte[] data = ue.GetBytes(text);
            uint crc32Result = 0xFFFFFFFF;
            int iLookup;

            for (int i = 0; i < data.GetUpperBound(0); i++)
            {
                iLookup = (int)((crc32Result & 0xFF) ^ data[i]);
                crc32Result = crc32Result >> 8;
                crc32Result = crc32Result ^ crc32Table[iLookup];
            }
            return (int)~crc32Result;
        }

        #region IChecksumProvider<int>

        public int GetChecksum(byte[] data)
        {
            String s = "";
            for (int i = 0; i < data.GetUpperBound(0); i++) {
                s += data[i]; }
            return GetCrc32(s);//.ToString("X");
        }

        #endregion

        //public String GetName()
        //{
        //    return "CRC32";
        //}

        #endregion
    }
}
