///////////////////////////////////////////////////////////////////////////////////////
//
//    Project:     BlueLogic.SDelta
//    Description: BinaryCommand structure implementation
//
// ------------------------------------------------------------------------------------
//    Copyright 2007 by Bluelogic Software Solutions.
//    see product licence ( creative commons attribution 3.0 )
// ------------------------------------------------------------------------------------
//
//    History:
//
//    Dienstag, 08. Mai 2007 Matthias Gruber original version
//    Mittwoch, 09. Mai 2007 FINAL
//
//
///////////////////////////////////////////////////////////////////////////////////////

namespace MatthiasToolbox.Delta.Utilities
{
    /// <summary>
    /// contains a command for binary delta application
    /// </summary>
    public struct BinaryCommand
    {
        #region classvar
        
        private CommandType myType;     // here: only copy or insert are supported
        private byte[] myData;          // in case of insert contains binary data
        
        private long startAddress;      // in case of copy
        private long endAddress;
        private int repeat;
        
        #endregion
        #region properties

        /// <summary>
        /// the type of command, here only copy or insert are supported
        /// </summary>
        public CommandType CommandType
        {
            get { return myType; }
            set { myType = value; }
        } // CommandType

        /// <summary>
        /// contains binary data in case of an insert command
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public byte[] Data
        {
            get { return myData; }
            set { myData = value; }
        } // byte[]

        /// <summary>
        /// contains start address in case of a copy command
        /// </summary>
        public long StartAddress
        {
            get { return startAddress; }
            set { startAddress = value; }
        } // long

        /// <summary>
        /// contains end address in case of a copy command
        /// </summary>
        public long EndAddress
        {
            get { return endAddress; }
            set { endAddress = value; }
        } // long
        
        /// <summary>
        /// number of repetitions
        /// </summary>
        public int Repeat
        {
            get{ return repeat; }
            set{ repeat = value; }
        } // int
        
        #endregion
        #region constructor

        /// <summary>
        /// initializes and populates this structure
        /// </summary>
        /// <param name="command">only copy or insert are supported</param>
        /// <param name="data">binary data in case of an insert command</param>
        /// <param name="startAddress">first block number in case of a copy command</param>
        /// <param name="endAddress">last block number in case of a copy command</param>        
        /// <param name="repeat">number of repetitions</param>
        public BinaryCommand(CommandType command, byte[] data, long startAddress, long endAddress, int repeat)
        {
            this.myType = command;
            this.myData = data;
            this.startAddress = startAddress;
            this.endAddress = endAddress;
            this.repeat = repeat;
        } // void

        #endregion
        #region overrides

        /// <summary>
        /// comparer overload
        /// </summary>
        /// <param name="a">instance 1</param>
        /// <param name="b">instance 2</param>
        /// <returns>true if the given traces match</returns>
        public static bool operator ==(BinaryCommand a, BinaryCommand b)
        {
            return (a.CommandType == b.CommandType && a.StartAddress == b.StartAddress && a.EndAddress == b.EndAddress && a.Data == b.Data);
        }

        /// <summary>
        /// comparer overload
        /// </summary>
        /// <param name="a">instance 1</param>
        /// <param name="b">instance 2</param>
        /// <returns>false if the given traces match</returns>
        public static bool operator !=(BinaryCommand a, BinaryCommand b)
        {
            return (a.CommandType != b.CommandType || a.StartAddress != b.StartAddress || a.EndAddress != b.EndAddress || a.Data != b.Data);
        }

        /// <summary>
        /// compares this instance to the given one
        /// </summary>
        /// <param name="obj">instance to compare</param>
        /// <returns>
        /// true if the instances match
        /// </returns>
        public override bool Equals(object obj)
        {
            return (obj is BinaryCommand && (BinaryCommand)obj == this);
        }

        /// <summary>
        /// returns a non secure hash code for this instance
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        
        #endregion
    } // struct
} // namespace
