using System;
using System.IO;
using System.IO.Compression;

namespace OfficeMice.MapGeneration.Serialization
{
    /// <summary>
    /// GZIP compression provider for efficient data compression.
    /// </summary>
    public class GzipCompressionProvider : ICompressionProvider
    {
        private readonly CompressionLevel _compressionLevel;
        
        public GzipCompressionProvider(CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            _compressionLevel = compressionLevel;
        }
        
        /// <summary>
        /// Compresses data using GZIP algorithm.
        /// </summary>
        public byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0)
                return data;
                
            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, _compressionLevel, true))
                {
                    gzipStream.Write(data, 0, data.Length);
                }
                return memoryStream.ToArray();
            }
        }
        
        /// <summary>
        /// Decompresses GZIP compressed data.
        /// </summary>
        public byte[] Decompress(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length == 0)
                return compressedData;
                
            using (var memoryStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                gzipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
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