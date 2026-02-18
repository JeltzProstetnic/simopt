///////////////////////////////////////////////////////////////////////////////////////
//
//    Project:     BlueLogic.SDelta
//    Description: CommandType enumeration
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

namespace MatthiasToolbox.Delta.Utilities
{
    /// <summary>
    /// non additive command type identifier
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32")]
    public enum CommandType : byte
    {
        /// <summary>
        /// no command
        /// </summary>
        Noop, 
        /// <summary>
        /// insert command
        /// </summary>
        Insert, 
        /// <summary>
        /// delete command
        /// </summary>
        Delete, 
        /// <summary>
        /// copy command
        /// </summary>
        Copy, 
        /// <summary>
        /// repeat command
        /// </summary>
        Repeat
    }
}
