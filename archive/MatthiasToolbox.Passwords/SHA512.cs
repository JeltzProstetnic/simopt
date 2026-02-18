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
using MatthiasToolbox.Passwords.Utilities;

namespace MatthiasToolbox.Passwords
{
    /// <summary>
    /// a sha256 hash provider
    /// </summary>
    public class SHA512
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
            return Crypto.ComputeHash(data, "SHA512", iv);
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
        public String GetHash(Stream data, byte[] iv)
        {
            return Crypto.ComputeHash(data, "SHA512", iv);
        }

        /// <summary>
        /// returns the name of the hash function
        /// </summary>
        public string Name
        {
            get { return "SHA512"; }
        }
    }
}
