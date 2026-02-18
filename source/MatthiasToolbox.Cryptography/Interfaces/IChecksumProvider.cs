namespace MatthiasToolbox.Cryptography.Interfaces
{
    /// <summary>
    /// a very simple interface for communicating a checksum
    /// </summary>
    /// <typeparam name="T">
    /// the data type of the checksum
    /// </typeparam>
    public interface IChecksumProvider<T>
    {
        /// <summary>
        /// calculate a checksum for the given data
        /// </summary>
        /// <param name="data">
        /// the data on which the checksum will be calculated
        /// </param>
        /// <returns>
        /// a checksum for the given data
        /// </returns>
        T GetChecksum(byte[] data);

        /// <summary>
        /// retrieve the class name of an instance
        /// </summary>
        /// <returns>
        /// the name of this instance
        /// </returns>
        string Name { get; }

        /// <summary>
        /// returns the initial value for the checksum
        /// </summary>
        T InitialValue { get; }
    }
}