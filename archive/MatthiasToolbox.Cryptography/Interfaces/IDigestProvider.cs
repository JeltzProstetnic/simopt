using System;
using System.IO;

namespace MatthiasToolbox.Cryptography.Interfaces
{
    /// <summary>
    /// a very simple interface for communicating a digest
    /// </summary>
    public interface IDigestProvider
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
        String GetHash(Stream data, byte[] iv);

        /// <summary>
        /// retrieve the class name of an instance
        /// </summary>
        /// <returns>
        /// class name of the instance
        /// </returns>
        string Name { get; }
    }
}