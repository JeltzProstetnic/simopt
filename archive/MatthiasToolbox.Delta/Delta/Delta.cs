///////////////////////////////////////////////////////////////////////////////////////
//
//    Project:     BlueLogic.SDelta
//    Description: Delta implementation
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

namespace MatthiasToolbox.Delta.Delta
{
    /// <summary>
    /// represents a delta including a digest
    /// contains an order sensitive list of commands
    /// </summary>
    /// <typeparam name="TCommand">
    /// type of the commands
    /// </typeparam>
    public class Delta<TCommand>
    {
        #region classvars
        
        private List<TCommand> myCommands;  // order sensitive!
        private int myBlockSize = 0;        // recommended minimum 2048
        private String myDigest = "";       // a strong hash value of the target file
        
        // meta infos
        private String digestName = "";
        private String hashName = "";
        private String checkName = "";

        #endregion
        #region properties

        /// <summary>
        /// class name of the digest function
        /// </summary>
        public String DigestName
        {
            get { return digestName; }
            set { digestName = value; }
        }
        
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
        /// recommended minimum 2048
        /// </summary>
        public int BlockSize
        {
            get
            {
                return myBlockSize;
            }
            set
            {
                myBlockSize = value;
            }
        }

        /// <summary>
        /// order sensitive list of arbitrarily alternating copy- and insert commands
        /// </summary>
        public List<TCommand> Commands
        {
            get
            {
                return myCommands;
            }
        }

        /// <summary>
        /// strong hash value of the target data which can be reproduced by applying this delta
        /// </summary>
        public String Digest
        {
            get { return myDigest; }
            set { myDigest = value; }
        }

        #endregion
        #region constructor

        /// <summary>
        /// initialization only
        /// </summary>
        public Delta()
        {
            myCommands = new List<TCommand>();
        }

        /// <summary>
        /// initialize and populate
        /// </summary>
        /// <param name="blockSize">
        /// notice: block sizes smaller than 2048 often cause hash collisions in some hash functions (like MD4)
        /// </param>
        /// <param name="commands">
        /// order sensitive list of arbitrarily alternating copy- and insert commands
        /// </param>
        /// <param name="hashName">
        /// should be set to the class name
        /// </param>
        /// <param name="checkName">
        /// should be set to the class name
        /// </param>
        /// <param name="digestName">
        /// should be set to the class name
        /// </param>
        /// <param name="digest">
        /// set this to a strong hash value of the target file so the process of applying the delta can be verified
        /// </param>
        public Delta(int blockSize, List<TCommand> commands, String digestName, String hashName, String checkName, String digest)
        {
            myCommands = commands;
            myBlockSize = blockSize;
            this.hashName = hashName;
            this.checkName = checkName;
            myDigest = digest;
            this.digestName = digestName;
        }

        #endregion
    }
}