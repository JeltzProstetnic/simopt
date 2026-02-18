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
//    Donnerstag, 31. Mai 2007 Matthias Gruber original version
//
//
///////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using MatthiasToolbox.Cryptography.Interfaces;
using MatthiasToolbox.Cryptography.Utilities;

namespace MatthiasToolbox.Cryptography.Hashes
{
    /// <summary>
    /// a sha1 hash provider
    /// </summary>
    public class SHA1 : IHashProvider<String>, IDigestProvider
    {
        /// <summary>
        /// get hash value
        /// </summary>
        /// <param name="data">
        /// the source data for the hash function
        /// </param>
        /// <param name="iv">
        /// the initialization vector for the hash function
        /// </param>
        /// <returns>
        /// a hash value
        /// </returns>
        public String GetHash(byte[] data, byte[] iv)
        {
            return NetHashes.ComputeHash(data, "SHA1", iv);
        }

        /// <summary>
        /// get hash value
        /// </summary>
        /// <param name="data">
        /// the source data for the hash function
        /// </param>
        /// <param name="iv">
        /// the initialization vector for the hash function
        /// </param>
        /// <returns>
        /// a hash value
        /// </returns>
        public string GetHash(Stream data, byte[] iv)
        {
            return NetHashes.ComputeHash(data, "SHA1", iv);
        }

        /// <summary>
        /// returns the name of the hash function
        /// </summary>
        public string Name
        {
            get { return "SHA1"; }
        }
    }
}
