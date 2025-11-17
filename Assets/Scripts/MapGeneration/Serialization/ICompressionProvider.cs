namespace OfficeMice.MapGeneration.Serialization
{
    /// <summary>
    /// Interface for data compression providers supporting different compression algorithms.
    /// </summary>
    public interface ICompressionProvider
    {
        /// <summary>
        /// Compresses data using the specified algorithm.
        /// </summary>
        /// <param name="data">Data to compress</param>
        /// <returns>Compressed data</returns>
        byte[] Compress(byte[] data);
        
        /// <summary>
        /// Decompresses data using the specified algorithm.
        /// </summary>
        /// <param name="compressedData">Compressed data to decompress</param>
        /// <returns>Decompressed data</returns>
        byte[] Decompress(byte[] compressedData);
        
        /// <summary>
        /// Calculates the compression ratio.
        /// </summary>
        /// <param name="original">Original data</param>
        /// <param name="compressed">Compressed data</param>
        /// <returns>Compression ratio (0.0 to 1.0, lower is better)</returns>
        float GetCompressionRatio(byte[] original, byte[] compressed);
    }
}