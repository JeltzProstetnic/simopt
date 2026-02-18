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
using MatthiasToolbox.Delta.Delta;

namespace MatthiasToolbox.Delta.Utilities
{
    /// <summary>
    /// generic erialization provider for deltas and blocked hashsets
    /// </summary>
    /// <typeparam name="THash">
    /// hash type
    /// </typeparam>
    /// <typeparam name="TCheck">
    /// checksum type
    /// </typeparam>
    public abstract class SerializationProvider<THash, TCheck>
    {
        #region writer
        
        /// <summary>
        /// write a blocked hashset to disk
        /// </summary>
        /// <param name="hashset">
        /// the blocked hashset to save
        /// </param>
        /// <param name="file">
        /// full name and path of the file to which the hashset should be written
        /// </param>
        /// <returns>
        /// true on success
        /// </returns>
        public bool WriteBlockedHashSet(BlockedHashset<THash, TCheck> hashset, String file)
        {
            Stream fs; 
            try
            {
                fs = new FileStream(file, FileMode.Create, FileAccess.Write);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return WriteBlockedHashSet(hashset, fs);
        }

        /// <summary>
        /// write a blocked hashset to disk
        /// </summary>
        /// <param name="hashset">
        /// the blocked hashset to save
        /// </param>
        /// <param name="file">
        /// full name and path of the file to which the hashset should be written
        /// </param>
        /// <returns>
        /// true on success
        /// </returns>
        public abstract bool WriteBlockedHashSet(BlockedHashset<THash, TCheck> hashset, Stream file);

        /// <summary>
        /// write a delta to disk
        /// </summary>
        /// <param name="delta">
        /// the delta to save
        /// </param>
        /// <param name="file">
        /// full name and path of the file to which the delta should be written
        /// </param>
        /// <returns>
        /// true on success
        /// </returns>
        public bool WriteDelta(Delta<BinaryCommand> delta, String file)
        {
            Stream fs;
            try
            {
                fs = new FileStream(file, FileMode.Create, FileAccess.Write);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return WriteDelta(delta, fs);
        }

        /// <summary>
        /// write a delta to disk
        /// </summary>
        /// <param name="delta">
        /// the delta to save
        /// </param>
        /// <param name="file">
        /// full name and path of the file to which the delta should be written
        /// </param>
        /// <returns>
        /// true on success
        /// </returns>
        public abstract bool WriteDelta(Delta<BinaryCommand> delta, Stream file);
        
        #endregion
        #region reader

        /// <summary>
        /// load a blocked hashset from disk
        /// </summary>
        /// <param name="hashset">
        /// the hashset to populate
        /// </param>
        /// <param name="file">
        /// full name and path of the file from which the hashset should be read
        /// </param>
        /// <returns>
        /// true on success
        /// </returns>
        public bool ReadBlockedHashSet(ref BlockedHashset<THash, TCheck> hashset, String file)
        {
            if(!File.Exists(file)) return false;
            Stream fs;
            try
            {
                fs = new FileStream(file, FileMode.Open, FileAccess.Read);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return ReadBlockedHashSet(ref hashset, fs);
        }

        /// <summary>
        /// load a blocked hashset from disk
        /// </summary>
        /// <param name="hashset">
        /// the hashset to populate
        /// </param>
        /// <param name="file">
        /// full name and path of the file from which the hashset should be read
        /// </param>
        /// <returns>
        /// true on success
        /// </returns>
        public abstract bool ReadBlockedHashSet(ref BlockedHashset<THash, TCheck> hashset, Stream file);

        /// <summary>
        /// load a delta from disk
        /// </summary>
        /// <param name="delta">
        /// the delta to populate
        /// </param>
        /// <param name="file">
        /// full name and path of the file from which the delta should be read
        /// </param>
        /// <returns>
        /// true on success
        /// </returns>
        public bool ReadDelta(ref Delta<BinaryCommand> delta, String file)
        {
            if (!File.Exists(file)) return false;
            Stream fs;
            try
            {
                fs = new FileStream(file, FileMode.Open, FileAccess.Read);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return ReadDelta(ref delta, fs);
        }

        /// <summary>
        /// load a delta from disk
        /// </summary>
        /// <param name="delta">
        /// the delta to populate
        /// </param>
        /// <param name="file">
        /// full name and path of the file from which the delta should be read
        /// </param>
        /// <returns>
        /// true on success
        /// </returns>
        public abstract bool ReadDelta(ref Delta<BinaryCommand> delta, Stream file);
        
        #endregion
    }
}
