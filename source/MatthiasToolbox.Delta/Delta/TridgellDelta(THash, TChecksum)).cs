using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MatthiasToolbox.Delta.Utilities;
using MatthiasToolbox.Cryptography.Hashes;
using MatthiasToolbox.Utilities.IO;
using MatthiasToolbox.Cryptography.Interfaces;
using MatthiasToolbox.Cryptography.Checksums;

namespace MatthiasToolbox.Delta.Delta
{
    /// <summary>
    /// a binary delta class after the rsync Algorithm by (Tridgell 1999)
    /// for maximum flexibility the hash / checksum algorithms are
    /// exchangeable through their interfaces
    /// </summary>
    /// <typeparam name="THash">
    /// type of the hash returned by the IHashProvider in use
    /// </typeparam>
    /// <typeparam name="TChecksum">
    /// type of the checksum returned by the IChecksumProvider in use
    /// </typeparam>
    public class TridgellDelta<THash, TChecksum>
    {
        #region classvars
        
        // delta related
        private IDigestProvider digest;                             // digest provider
        private IHashProvider<THash> hash;                          // hash provider
        private IRollingChecksumProvider<TChecksum> checksum;       // (rolling) checksum provider
        private byte[] iv = Encoding.UTF8.GetBytes("BluLogic");     // initialization vector
        
        /// <summary>
        /// must be > size(hash) + size(checksum)
        /// so for Adler32 (32 bit = 4 bytes) and
        /// MD4 (128 bit = 16 bytes) the absolute
        /// minimum would be 21 bytes
        /// </summary>
        private int blockSize;                                      // block size
        
        // the following are used for the command accumulator
        private const int writeBufferSize = 102400;
        private bool lastCommandWasCopy = false;                    // last command was copy
        private bool lastCommandWasInsert = false;                  // last command was insert
        
        private long tmpFrom;                                       // temporary variables ...
        private long tmpTo;
        private int tmpRep;
        private int tmpLen;
        private byte[] writeBuffer = new byte[writeBufferSize];
        private byte[] outBuffer;
        private int bufferIndex = 0;
        private List<BinaryCommand> tmpDelta;
        
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
        public TridgellDelta()
        {
            blockSize = 2048;
            digest = new SHA256();
            if(typeof(MD4) is IHashProvider<THash> && typeof(Adler32) is IChecksumProvider<TChecksum>)
            {
                hash = (IHashProvider<THash>)(new MD4());
                checksum = (IRollingChecksumProvider<TChecksum>)(new Adler32(blockSize));
            }
            else
            {
                throw new ArgumentException(
                    "The generic types do not match <String, ulong> for MD4 and Adler32 as required by the empty constructor.",
                    "check generic types THash and TChecksum");
            }
        }

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
        public TridgellDelta(int blockSize)
        {
            this.blockSize = blockSize;
            digest = new SHA256();
            if (typeof(MD4) is IHashProvider<THash> && typeof(Adler32) is IRollingChecksumProvider<TChecksum>)
            {
                hash = (IHashProvider<THash>)(new MD4());
                checksum = (IRollingChecksumProvider<TChecksum>)(new Adler32(blockSize));
            }
            else
            {
                throw new ArgumentException(
                    "The generic types do not match <String, ulong> for MD4 and Adler32 as required by the empty constructor.",
                    "check generic types THash and TChecksum");
            }
        }
        
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
        /// <param name="hash">
        /// the IHashProvider to be used for 
        /// verifying block matches
        /// </param>
        /// <param name="checksum">
        /// the IChecksumProvider for a 
        /// rolling checksum (like Adler32)
        /// </param>
        public TridgellDelta(int blockSize, IHashProvider<THash> hash, IRollingChecksumProvider<TChecksum> checksum)
        {
            this.blockSize = blockSize;
            this.hash = hash;
            this.checksum = checksum;
            digest = new SHA256();
        }

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
        /// <param name="hash">
        /// the IHashProvider to be used for 
        /// verifying block matches
        /// </param>
        /// <param name="checksum">
        /// the IChecksumProvider for a 
        /// rolling checksum (like Adler32)
        /// </param>
        /// <param name="digest">
        /// the IDigestProvider for a secure
        /// hash function which will be used for
        /// verification
        /// </param>
        public TridgellDelta(int blockSize, IDigestProvider digest, IHashProvider<THash> hash, IRollingChecksumProvider<TChecksum> checksum)
        {
            this.blockSize = blockSize;
            this.hash = hash;
            this.checksum = checksum;
            this.digest = digest;
        }
        
        #endregion
        #region get signatures
        
        /// <summary>
        /// get a set of hashes for every block
        /// </summary>
        /// <param name="data">
        /// the stream pointing to the data
        /// </param>
        /// <returns>
        /// memory representation of the hash library
        /// </returns>
        private BlockedHashset<THash, TChecksum> GetBlockSignatures(Stream data)
        {
            byte[] buf = new byte[blockSize];
            BlockedHashset<THash, TChecksum> sig = new BlockedHashset<THash, TChecksum>(blockSize, hash.Name, checksum.Name);
            int i;
            for (i = 0; i <= ((int)data.Length - blockSize) + 1; i+=blockSize)
            {
                data.Read(buf, 0, blockSize);
                THash h1 = hash.GetHash(buf, iv);
                TChecksum h2 = checksum.GetChecksum(buf);
                sig.AddPair(h1, h2);
            }
            if(i < (int)data.Length - 1)
            {
                buf = new byte[(int)data.Length - i];
                data.Read(buf, 0, ((int)data.Length - i));
                THash h1 = hash.GetHash(buf, iv);
                TChecksum h2 = checksum.GetChecksum(buf);
                sig.AddPair(h1, h2);
            }
            return sig;
        }

        /// <summary>
        /// get a set of hashes for every block
        /// </summary>
        /// <param name="data">
        /// the stream pointing to the data
        /// </param>
        /// <param name="output">
        /// the stream to which the hash library will be serialized
        /// </param>
        /// <returns>
        /// memory representation of the hash library or null on error
        /// </returns>
        public BlockedHashset<THash, TChecksum> GetBlockSignatures(Stream data, Stream output)
        {
            BlockedHashset<THash, TChecksum> sig = GetBlockSignatures(data);
            XmlSerializationProvider<THash, TChecksum> xsp = new XmlSerializationProvider<THash, TChecksum>();
            if (!xsp.WriteBlockedHashSet(sig, output)) return null;
            return sig;
        }

        #endregion
        #region get delta

        /// <summary>
        /// generates a delta in form of a command list
        /// </summary>
        /// <param name="data">
        /// the target version of the data (in respect to the produced delta)
        /// </param>
        /// <param name="signatures">
        /// the hash library from the source version of the data
        /// </param>
        /// <returns>a generic list of BinaryCommand containing the subsequent commands</returns>
        private List<BinaryCommand> GetDelta(Stream data, BlockedHashset<THash, TChecksum> signatures)
        {
            bool lastWindow;
            long index;

            tmpDelta = new List<BinaryCommand>();
            SlidingStreamWindow window = new SlidingStreamWindow(data, blockSize, 3);

            TChecksum check = checksum.InitialValue;
            byte tmp = 0;
            foreach (byte[] buf in window)
            {
                lastWindow = (buf.Length < blockSize || window.Finished);
                index = signatures.Check(buf, iv, hash, checksum, ref check, ref tmp);
                if(index >= 0)
                {
                    emitCopy(index);
                    window.SkipRestOfWindow();                         // jump to end of block
                    check = checksum.InitialValue;
                }
                else
                {
                    if (lastWindow) emitInsert(buf);
                        else emitInsert(buf[0]);
                }
            }
            flushLastCommand();
            return tmpDelta;
        }

        /// <summary>
        /// generates a delta in form of a command list
        /// </summary>
        /// <param name="data">
        /// the target version of the data (in respect to the produced delta)
        /// </param>
        /// <param name="signatures">
        /// the hash library from the source version of the data
        /// </param>
        /// <param name="output">
        /// a stream to which the delta will be serialized
        /// </param>
        /// <returns>a generic list of BinaryCommand containing the subsequent commands</returns>
        public List<BinaryCommand> GetDelta(Stream data, BlockedHashset<THash, TChecksum> signatures, ref Stream output)
        {
            List<BinaryCommand> deltaList = GetDelta(data, signatures);
            data.Seek(0, 0);
            String sha = digest.GetHash(data, iv);
            Delta<BinaryCommand> deltaObj = new Delta<BinaryCommand>(blockSize, deltaList, digest.Name, hash.Name, checksum.Name, sha);
            XmlSerializationProvider<THash, TChecksum> xsp = new XmlSerializationProvider<THash, TChecksum>();
            xsp.WriteDelta(deltaObj, output);
            return deltaList;
        }

        #endregion
        #region apply delta
        
        /// <summary>
        /// apply a delta transforming one version of a file to another
        /// </summary>
        /// <param name="delta">
        /// the delta commands
        /// </param>
        /// <param name="sourceFile">
        /// source file in respect to delta
        /// </param>
        /// <param name="targetFile">
        /// target file in respect to delta
        /// </param>
        /// <param name="newBlockSize">
        /// block size for addressing and hashing
        /// if this is not the blocksize for which the delta 
        /// was created the reconstruction will fail
        /// </param>
        /// <remarks>
        /// does not close streams
        /// </remarks>
        public void ApplyDelta(List<BinaryCommand> delta, Stream sourceFile, Stream targetFile, int newBlockSize)
        {
            byte[] buf = new byte[newBlockSize];
            int size;
            foreach (BinaryCommand c in delta)
            {
                if (c.CommandType == CommandType.Copy)
                {
                    // random access local version
                    sourceFile.Seek(c.StartAddress * newBlockSize, SeekOrigin.Begin);
                    if(c.Repeat > 0) // repeats
                    {
                        size = sourceFile.Read(buf, 0, newBlockSize);
                        for (int r = 0; r <= c.Repeat; r++)
                        {
                            targetFile.Write(buf, 0, size);
                        }
                    }
                    else // chain
                    {
                        for (long l = c.StartAddress; l <= c.EndAddress; l++)
                        {
                            size = sourceFile.Read(buf, 0, newBlockSize);
                            targetFile.Write(buf, 0, size);
                         }
                    }
                }
                else
                {
                    targetFile.Write(c.Data, 0, c.Data.Length);
                } // if
            } // foreach
            // sourceFile.Close();
            // targetFile.Close();
        } // void

        /// <summary>
        /// apply a delta transforming one version of a file to another
        /// </summary>
        /// <param name="delta">
        /// the delta commands
        /// </param>
        /// <param name="sourceFile">
        /// source file in respect to delta
        /// </param>
        /// <param name="targetFile">
        /// target file in respect to delta
        /// </param>
        /// <remarks>
        /// does not close streams
        /// </remarks>
        public void ApplyDelta(List<BinaryCommand> delta, Stream sourceFile, Stream targetFile)
        {
            ApplyDelta(delta, sourceFile, targetFile, blockSize);
        }

        /// <summary>
        /// apply a delta transforming one version of a file to another
        /// </summary>
        /// <param name="delta">
        /// the delta commands
        /// </param>
        /// <param name="sourceFile">
        /// source file in respect to delta
        /// </param>
        /// <param name="targetFile">
        /// target file in respect to delta
        /// </param>
        /// <returns>
        /// the digest which was added to the delta to check the reconstruction
        /// </returns>
        /// <remarks>
        /// does not close streams
        /// </remarks>
        public String ApplyDelta(Delta<BinaryCommand> delta, Stream sourceFile, Stream targetFile)
        {
            ApplyDelta(delta.Commands, sourceFile, targetFile, delta.BlockSize);
            return delta.Digest;
        }
        
        /// <summary>
        /// apply a delta transforming one version of a file to another
        /// </summary>
        /// <param name="delta">
        /// the stream containing the delta commands
        /// </param>
        /// <param name="sourceFile">
        /// source file in respect to delta
        /// </param>
        /// <param name="targetFile">
        /// target file in respect to delta
        /// </param>
        /// <param name="outDigest">
        /// use this to verify if the reconstruction was successful
        /// </param>
        /// <returns>
        /// block size
        /// </returns>
        /// <remarks>
        /// does not close streams
        /// </remarks>
        public int ApplyDelta(Stream delta, Stream sourceFile, Stream targetFile, out String outDigest)
        {
            XmlSerializationProvider<THash, TChecksum> xst = new XmlSerializationProvider<THash, TChecksum>();
            Delta<BinaryCommand> d = new Delta<BinaryCommand>();
            xst.ReadDelta(ref d, delta);
            outDigest = ApplyDelta(d, sourceFile, targetFile);
            return d.BlockSize;
        }

        #endregion
        #region accumulator

        #region emit

        /// <summary>
        /// collect a copy command
        /// </summary>
        /// <param name="address">
        /// address to copy
        /// </param>
        private void emitCopy(long address)
        {
            if(lastCommandWasInsert)
            {
                lastCommandWasInsert = false;
                // write the last insert command
                outBuffer = new byte[bufferIndex];
                Array.Copy(writeBuffer, 0, outBuffer, 0, bufferIndex);
                writeInsert(outBuffer);
                bufferIndex = 0;
            }
            if(tmpLen == 0)
            {
                tmpLen = 1;
                tmpFrom = address;
                tmpTo = address;
            }
            else if(tmpLen == 1)
            {
                if (tmpTo == address) tmpRep = 1; // repeat
                else if (tmpTo + 1 == address) tmpTo += 1; // chain
                else
                {
                    writeCopy(tmpFrom, tmpTo, 0); // single byte copy
                    // new start
                    tmpFrom = address;
                    tmpTo = address;
                }
                tmpLen += 1;
            }
            else
            {
                if(tmpRep > 0) // following repeats
                {
                    if (tmpTo == address) tmpRep += 1; // repeat again
                    else
                    {
                        writeCopy(tmpFrom, tmpTo, tmpRep); // copy last repetition
                        tmpRep = 0;
                        // new start
                        tmpFrom = address;
                        tmpTo = address;
                    }
                }
                else
                {
                    if (tmpTo + 1 == address) tmpTo += 1; // chain++
                    else
                    {
                        writeCopy(tmpFrom, tmpTo, 0); // copy last chain
                        // new start
                        tmpFrom = address;
                        tmpTo = address;
                    }
                }
                tmpLen += 1;
            }

            lastCommandWasCopy = true;
        }

        /// <summary>
        /// collect a insert command
        /// </summary>
        /// <param name="data">
        /// data to insert
        /// </param>
        private void emitInsert(byte data)
        {
            if (lastCommandWasCopy)
            {
                writeCopy(tmpFrom, tmpTo, tmpRep);
                lastCommandWasCopy = false;
            }
            writeBuffer[bufferIndex] = data;
            bufferIndex += 1;
            if (bufferIndex == writeBufferSize)
            {
                outBuffer = new byte[bufferIndex];
                Array.Copy(writeBuffer, 0, outBuffer, 0, bufferIndex);
                writeInsert(outBuffer);
                bufferIndex = 0;
            }
            lastCommandWasInsert = true;
        }

        /// <summary>
        /// collect a insert command
        /// </summary>
        /// <param name="data">
        /// data to insert
        /// </param>
        private void emitInsert(byte[] data)
        {
            if (lastCommandWasCopy)
            {
                writeCopy(tmpFrom, tmpTo, tmpRep);
                lastCommandWasCopy = false;
            }
            if (bufferIndex + data.Length > writeBufferSize)
            {
                if (bufferIndex > 0)
                {
                    outBuffer = new byte[bufferIndex];
                    Array.Copy(writeBuffer, 0, outBuffer, 0, bufferIndex);
                    writeInsert(outBuffer);
                    bufferIndex = 0;
                }
                else // data is bigger as writeBuffer
                {
                    writeInsert(data);
                    return;
                }
            }
            Array.Copy(data, 0, writeBuffer, bufferIndex, data.Length);
            bufferIndex += data.Length;
            lastCommandWasInsert = true;
        }
        
        #endregion
        
        #region write

        /// <summary>
        /// write a copy command
        /// </summary>
        /// <param name="from">
        /// start address
        /// </param>
        /// <param name="too">
        /// end address
        /// </param>
        /// <param name="repeat">
        /// number of repetitions
        /// </param>
        private void writeCopy(long from, long too, int repeat)
        {
            BinaryCommand copy = new BinaryCommand(CommandType.Copy, null, from, too, repeat);
            tmpDelta.Add(copy);
            tmpLen = 0;
            tmpRep = 0;
        }
        
        /// <summary>
        /// write a insert command
        /// </summary>
        /// <param name="data">
        /// data to insert
        /// </param>
        private void writeInsert(byte[] data)
        {
            BinaryCommand insert = new BinaryCommand(CommandType.Insert, data, 0, 0, 0);
            tmpDelta.Add(insert);
        }

        #endregion
        
        /// <summary>
        /// write yet unwritten commands after the last emit call
        /// </summary>
        private void flushLastCommand()
        {
            if (lastCommandWasCopy)
            {
                writeCopy(tmpFrom, tmpTo, tmpRep);
                lastCommandWasCopy = false;
            }
            if (lastCommandWasInsert)
            {
                lastCommandWasInsert = false;
                outBuffer = new byte[bufferIndex];
                Array.Copy(writeBuffer, 0, outBuffer, 0, bufferIndex);
                writeInsert(outBuffer);
                bufferIndex = 0;
            }
        }

        #endregion
    } // class
} // namespace