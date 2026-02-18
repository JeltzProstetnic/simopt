namespace MatthiasToolbox.Cryptography.Interfaces
{
    /// <summary>
    /// a very simple interface for communicating a hash value
    /// </summary>
    /// <typeparam name="T">data type of the hash value</typeparam>
    public interface IHashProvider<T>
    {
        /// <summary>
        /// calculate hash value for the given data
        /// </summary>
        /// <param name="data">data for which the hash value is calculated</param>
        /// <param name="iv">initialization vector</param>
        /// <returns>returns the checksum for the given data</returns>
        T GetHash(byte[] data, byte[] iv);

        /// <summary>
        /// retrieve the class name of an instance
        /// </summary>
        /// <returns>class name of this instance</returns>
        string Name { get; }
    }
}