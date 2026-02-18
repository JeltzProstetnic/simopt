using MatthiasToolbox.Cryptography.Interfaces;

namespace MatthiasToolbox.Cryptography.Checksums
{
    /// <summary>
    /// Adler32 (Deutsch 1996) see also Zlib (RFC1950)
    /// </summary>
    public class Adler32 : IRollingChecksumProvider<ulong>
    {
        #region classvar

        private int blockSize = 2048;           // needed for rolling function
        
        private static ulong modulo = 65521;    // Largest prime smaller than 2^16
        private static int moduloDelay = 5552;  // NMAX is the largest n such that 255n(n+1)/2 + (n+1)(BASE-1) <= 2^32-1
        
        private static ulong s1;                // temporary
        private static ulong s2;
        private static int i;
        private static int k;
        private static int adr;
        private static int len;

        #endregion
        #region properties

        /// <summary>
        /// blocksize for rolling processing
        /// </summary>
        public int BlockSize
        {
            get { return blockSize; }
            set { blockSize = value; }
        }
        
        /// <summary>
        /// returns the name of the checksum function
        /// </summary>
        public string Name
        {
            get { return "Adler32"; }
        }

        /// <summary>
        /// returns the initial value
        /// </summary>
        public ulong InitialValue
        {
            get { return 1; }
        }
        
        #endregion
        #region constructor

        /// <summary>
        /// constructor for rolling checksum
        /// </summary>
        /// <param name="blockSize">
        /// needed for rolling property
        /// </param>
        public Adler32(int blockSize)
        {
            this.blockSize = blockSize;
        }
        
        #endregion
        #region public accessors

        /// <summary>
        /// get checksum value
        /// </summary>
        /// <param name="data">
        /// the source data for the hash function
        /// </param>
        /// <returns>
        /// a hash value
        /// </returns>
        public ulong GetChecksum(byte[] data)
        {
            return adler32(1, data);
        }

        /// <summary>
        /// get checksum value
        /// </summary>
        /// <param name="checksum">
        /// checksum of previous block
        /// </param>
        /// <param name="old">
        /// first byte of previous block
        /// </param>
        /// <param name="neu">
        /// last byte of new block
        /// </param>
        /// <returns>
        /// a hash value
        /// </returns>
        public ulong GetChecksum(ulong checksum, byte old, byte neu)
        {
            s1 = (checksum & 0xffff) - old + neu;
            s2 = ((checksum >> 16) & 0xffff) - (ulong)old * (ulong)blockSize + s1 - 1;

            s1 %= modulo;
            s2 %= modulo;
            return (s2 << 16) | s1;
        }

        /// <summary>
        /// checks if the value is equal to the initial value
        /// </summary>
        /// <param name="value">
        /// value to be checked
        /// </param>
        /// <returns>
        /// true if the values match
        /// </returns>
        public bool IsInitialValue(ulong value)
        {
            return (value == 1);
        }
        
        #endregion
        #region private
        
        private static ulong adler32(ulong adler, byte[] buf)
        {
            s1 = adler & 0xffff;
            s2 = (adler >> 16) & 0xffff; 
            
            adr = 0;
            len = buf.GetUpperBound(0);

            while (len > 0)
            {
                k = (len < moduloDelay) ? len : moduloDelay;
                len -= k;

                while (k >= 16)
                {
                    for (i = 0; i < 16; i++ )
                    {
                        s1 += buf[adr + i];
                        s2 += s1;
                    }
                    adr += 16;
                    k -= 16;
                }

                if (k != 0)
                {
                    do
                    {
                        s1 += buf[adr];
                        adr += 1;
                        s2 += s1;
                        --k;
                    }
                    while (k >= 0);
                }

                s1 %= modulo;
                s2 %= modulo;
            }

            return (s2 << 16) | s1;
        }

        #endregion
    } // class
} // namespace