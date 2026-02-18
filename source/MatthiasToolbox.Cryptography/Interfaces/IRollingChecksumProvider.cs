using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MatthiasToolbox.Cryptography.Interfaces
{
    public interface IRollingChecksumProvider<T> : IChecksumProvider<T>
    {
        /// <summary>
        /// calculate a checksum for the given data
        /// </summary>
        /// <param name="checksum">
        /// the checksum for the previous block
        /// </param>
        /// <param name="old">
        /// first byte of old block
        /// </param>
        /// <param name="neu">
        /// last byte of new block
        /// </param>
        /// <returns>
        /// a checksum for the given data
        /// </returns>
        T GetChecksum(T checksum, byte old, byte neu);

        /// <summary>
        /// check if the given value equals the initial value for the checksum function
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool IsInitialValue(T value);
    }
}