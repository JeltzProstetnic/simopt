///////////////////////////////////////////////////////////////////////////////////////
//
//        Project: BlueLogic.SDelta
//    Description: BlockedHasset implementation
//         Status: RELEASE
//
// ------------------------------------------------------------------------------------
//    Copyright 2007 by Bluelogic Software Solutions.
//    see product licence ( creative commons attribution 3.0 )
// ------------------------------------------------------------------------------------
//
//    History:
//
//    Dienstag, 08. Mai 2007 Matthias Gruber original version
//    Mittwoch, 09. Mai 2007 BETA
//
//
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using MatthiasToolbox.Delta.Interfaces;
using MatthiasToolbox.Cryptography.Interfaces;

namespace MatthiasToolbox.Delta.Utilities
{
    /// <summary>
    /// this class stores and compares a blockwise collection of a pair of checksums / hashes by using 
    /// </summary>
    /// <typeparam name="THash">
    /// type of the hash returned by the IHashProvider in use
    /// </typeparam>
    /// <typeparam name="TChecksum">
    /// type of the checksum returned by the IChecksumProvider in use
    /// </typeparam>
    public class BlockedHashset<THash, TChecksum>
    {
        #region classvar
        
        // data structures for checksums and hashes
        private Hashtable<TChecksum, List<long>> checksumPointers;
        private SortedList<long, THash> hashList;
        
        // self explaining meta information
        private String hashName;
        private String checkName;
        
        private int blockSize;
        private long increment;     // address counter

        private TChecksum tmpCheck;
        private THash tmpHash;
        private List<long> tmpAddresses;
        
        #endregion
        #region property

        /// <summary>
        /// class name of the hash function
        /// </summary>
        public String HashName
        {
            get { return hashName; }
            set { hashName = value; }
        }

        /// <summary>
        /// class name of the checksum function
        /// </summary>
        public String CheckName
        {
            get { return checkName; }
            set { checkName = value; }
        }

        /// <summary>
        /// size of a data block
        /// </summary>
        public int BlockSize
        {
            get
            {
                return blockSize;
            }
            set
            {
                blockSize = value;
            }
        }

        /// <summary>
        /// this is a collection of checksums with block numbers (addresses) to the related hash(es)
        /// </summary>
        public Hashtable<TChecksum, List<long>> ChecksumPointers
        {
            get
            {
                return checksumPointers;
            }
        }

        /// <summary>
        /// hash ordered by their block number (address)
        /// </summary>
        public SortedList<long, THash> HashList
        {
            get
            {
                return hashList;
            }
        }

        #endregion
        #region constructor

        /// <summary>
        /// default constructor
        /// attention: this initializes blocksize with zero and hash name as well as checksum name with an empty string
        /// </summary>
        public BlockedHashset() : this(0,"","") { }

        /// <summary>
        /// this constructor initializes the class so that it is ready to use
        /// </summary>
        /// <param name="blockSize">
        /// notice: block sizes smaller than 2048 often cause hash collisions in some hash functions (like MD4)
        /// </param>
        /// <param name="hashName">
        /// should be set to the class name
        /// </param>
        /// <param name="checkName">
        /// should be set to the class name
        /// </param>
        public BlockedHashset(int blockSize, String hashName, String checkName)
        {
            checksumPointers = new Hashtable<TChecksum, List<long>>();
            hashList = new SortedList<long, THash>(); 
            this.blockSize = blockSize;
            this.hashName = hashName;
            this.checkName = checkName;
            increment = 0;
        }

        #endregion
        #region public accessors
        
        /// <summary>
        /// add a pair of hashes to the library
        /// </summary>
        /// <param name="strongHash">
        /// reliable hash for block match verification
        /// </param>
        /// <param name="rollingHash">
        /// cheap block match indicator
        /// </param>
        /// <returns>increment</returns>
        public long AddPair(THash strongHash, TChecksum rollingHash)
        {
            // ContainsKey: O(1), ContainsValue: O(n)
            if(checksumPointers.ContainsKey(rollingHash))
            {
                ((List<long>)checksumPointers[rollingHash]).Add(increment);
            }
            else
            {
                List<long> adress = new List<long>();
                adress.Add(increment);
                checksumPointers.Add(rollingHash, adress);
            }
            hashList[increment] = strongHash;
            return increment++;
        }

        /// <summary>
        /// check for a block match
        /// </summary>
        /// <param name="data">data</param>
        /// <param name="iv">initialization vector for the hash function</param>
        /// <param name="hashProvider">hash to use</param>
        /// <param name="checksumProvider">checksum to use</param>
        /// <param name="lastChecksum">a checksum from which to slide on</param>
        /// <param name="old">the byte which dropped out of the window</param>
        /// <returns>
        /// returns address if found, -1 else
        /// </returns>
        public long Check(byte[] data, byte[] iv, MatthiasToolbox.Cryptography.Interfaces.IHashProvider<THash> hashProvider, IRollingChecksumProvider<TChecksum> checksumProvider, ref TChecksum lastChecksum, ref byte old)
        {
            if (checksumProvider.IsInitialValue(lastChecksum))
            {
                tmpCheck = checksumProvider.GetChecksum(data);
            }
            else
            {
                tmpCheck = checksumProvider.GetChecksum(lastChecksum, old, data[data.GetUpperBound(0)]);
            }
            lastChecksum = tmpCheck;
            old = data[0];
            if (!checksumPointers.ContainsKey(tmpCheck)) return -1;
            tmpAddresses = (List<long>)checksumPointers[tmpCheck];
            tmpHash = hashProvider.GetHash(data, iv);
            foreach (long adress in tmpAddresses)
            {
                if (hashList[adress].Equals(tmpHash)) return adress;
            }
            return -1;
        }

        #endregion
    } // class
} // namespace
