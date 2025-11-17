using System;

namespace OfficeMice.MapGeneration.Serialization
{
    /// <summary>
    /// LZ4 compression provider for fast compression with good ratios.
    /// Note: This is a placeholder implementation. In a production environment,
    /// you would use a proper LZ4 library like K4os.Compression.LZ4.
    /// </summary>
    public class LZ4CompressionProvider : ICompressionProvider
    {
        private readonly int _compressionLevel;
        
        public LZ4CompressionProvider(int compressionLevel = 1)
        {
            _compressionLevel = Math.Max(0, Math.Min(16, compressionLevel));
        }
        
        /// <summary>
        /// Compresses data using LZ4 algorithm.
        /// </summary>
        public byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0)
                return data;
                
            // Placeholder implementation - in production, use actual LZ4 library
            // For now, fall back to GZIP
            var gzipProvider = new GzipCompressionProvider();
            return gzipProvider.Compress(data);
        }
        
        /// <summary>
        /// Decompresses LZ4 compressed data.
        /// </summary>
        public byte[] Decompress(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length == 0)
                return compressedData;
                
            // Placeholder implementation - in production, use actual LZ4 library
            // For now, fall back to GZIP
            var gzipProvider = new GzipCompressionProvider();
            return gzipProvider.Decompress(compressedData);
        }
        
        /// <summary>
        /// Calculates the compression ratio.
        /// </summary>
        public float GetCompressionRatio(byte[] original, byte[] compressed)
        {
            if (original == null || original.Length == 0)
                return 0f;
                
            if (compressed == null || compressed.Length == 0)
                return 1f;
                
            return (float)compressed.Length / original.Length;
        }
    }
}