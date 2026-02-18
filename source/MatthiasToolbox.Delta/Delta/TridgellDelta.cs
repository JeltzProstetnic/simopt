using System;
using System.Collections.Generic;
using System.IO;
using MatthiasToolbox.Cryptography.Hashes;
using MatthiasToolbox.Cryptography.Checksums;
using MatthiasToolbox.Delta.Utilities;
using MatthiasToolbox.Cryptography.Interfaces;

namespace MatthiasToolbox.Delta.Delta
{
    /// <summary>
    /// a non generic wrapper for the generic TridgellDelta class
    /// </summary>
    public class TridgellDelta
    {
        #region classvar

        private TridgellDelta<String, ulong> myDelta;
        
        #endregion
        #region constructors

        /// <summary>
        /// default constructor using following values:
        /// block size 2048
        /// digest: SHA 256
        /// hash: MD4
        /// checksum: Adler32
        /// </summary>
        /// <exception>
        /// throws an argument exception if THash != String or TChecksum != ulong
        /// </exception>
        public TridgellDelta() : this(2048, new SHA256()) {}

        /// <summary>
        /// extended constructor for use with 
        /// alternate hash / checksum algorithms
        /// </summary>
        /// <param name="blockSize">        
        /// should be chosen regarding
        /// total data amount, expected difference
        /// and required resolution for matches.
        /// Blocksizes smaller than 2048 often cause 
        /// collisions in certain hash functions (like MD4) 
        /// and are therefore not recommended.
        /// </param>
        public TridgellDelta(int blockSize): this(blockSize, new SHA256()) {}

        /// <summary>
        /// extended constructor for use with 
        /// alternate hash / checksum / digest algorithms
        /// </summary>
        /// <param name="blockSize">        
        /// should be chosen regarding
        /// total data amount, expected difference
        /// and required resolution for matches.
        /// Blocksizes smaller than 2048 often cause 
        /// collisions in certain hash functions (like MD4) 
        /// and are therefore not recommended.
        /// </param>
        /// <param name="digest">
        /// the IDigestProvider for a secure
        /// hash function which will be used for
        /// verification
        /// </param>
        public TridgellDelta(int blockSize, IDigestProvider digest)
        {
            myDelta = new TridgellDelta<string, ulong>(blockSize, digest, new MD4(), new Adler32(blockSize));
        }

        #endregion
        #region public accessors

        /// <summary>
        /// get a set of hashes for every block
        /// </summary>
        /// <param name="source">
        /// the stream pointing to the data
        /// </param>
        /// <param name="target">
        /// the stream to which the hash library will be serialized
        /// </param>
        /// <returns>
        /// memory representation of the hash library or null on error
        /// </returns>
        public BlockedHashset<String, ulong> GetBlockSignatures(Stream source, Stream target)
        {
            return myDelta.GetBlockSignatures(source, target);
        }

        /// <summary>
        /// generates a delta in form of a command list
        /// </summary>
        /// <param name="target">
        /// the target version of the data (in respect to the produced delta)
        /// </param>
        /// <param name="hashset">
        /// the hash library from the source version of the data
        /// </param>
        /// <param name="delta">
        /// a stream to which the delta will be serialized
        /// </param>
        /// <returns>a generic list of BinaryCommand containing the subsequent commands</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "2#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        public List<BinaryCommand> GetDelta(Stream target, BlockedHashset<String, ulong> hashset, ref Stream delta)
        {
            return this.myDelta.GetDelta(target, hashset, ref delta);
        }

        /// <summary>
        /// apply a delta transforming one version of a file to another
        /// </summary>
        /// <param name="delta">
        /// the stream containing the delta commands
        /// </param>
        /// <param name="data">
        /// source file in respect to delta
        /// </param>
        /// <param name="result">
        /// output file
        /// </param>
        /// <param name="dig">
        /// use this to verify if the reconstruction was successful
        /// </param>
        /// <returns>
        /// block size
        /// </returns>
        /// <remarks>
        /// does not close streams
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "3#")]
        public int ApplyDelta(Stream delta, Stream data, Stream result, out String dig)
        {
            return this.myDelta.ApplyDelta(delta, data, result, out dig);
        }
        #endregion
    }
}