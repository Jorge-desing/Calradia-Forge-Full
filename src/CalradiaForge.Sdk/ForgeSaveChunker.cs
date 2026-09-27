using System;
using System.Collections.Generic;
using System.Text;

namespace CalradiaForge.Sdk
{
    /// <summary>
    /// Prevents save game corruption caused by TaleWorlds' ~31 KB binary serializer limit (short.MaxValue - 1024).
    /// Safely chunks large JSON strings into string[] arrays for persistence, and reassembles them upon loading.
    /// Complies strictly with bannerlord_save_system rules.
    /// </summary>
    public static class ForgeSaveChunker
    {
        public const int SafeChunkSize = 30000;

        /// <summary>
        /// Checks whether the string exceeds the engine's safe single-string serialization limit.
        /// </summary>
        public static bool NeedsChunking(string data, int maxChunkSize = SafeChunkSize)
        {
            return !string.IsNullOrEmpty(data) && data.Length > maxChunkSize;
        }

        /// <summary>
        /// Splits a large string or JSON payload into an array of smaller string chunks.
        /// </summary>
        /// <param name="data">The string to chunk.</param>
        /// <param name="maxChunkSize">Maximum characters per chunk (default 30,000).</param>
        /// <returns>An array of chunk strings.</returns>
        public static string[] Chunk(string data, int maxChunkSize = SafeChunkSize)
        {
            if (data == null) return Array.Empty<string>();
            if (data.Length == 0) return new[] { string.Empty };
            if (maxChunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(maxChunkSize), "Chunk size must be positive.");

            if (data.Length <= maxChunkSize)
            {
                return new[] { data };
            }

            int chunkCount = (data.Length + maxChunkSize - 1) / maxChunkSize;
            var chunks = new string[chunkCount];

            for (int i = 0; i < chunkCount; i++)
            {
                int startIndex = i * maxChunkSize;
                int length = Math.Min(maxChunkSize, data.Length - startIndex);
                chunks[i] = data.Substring(startIndex, length);
            }

            return chunks;
        }

        /// <summary>
        /// Reassembles an array of chunks back into the original string.
        /// </summary>
        /// <param name="chunks">The chunks to reassemble.</param>
        /// <returns>The combined original string.</returns>
        public static string Reassemble(string[] chunks)
        {
            if (chunks == null || chunks.Length == 0) return string.Empty;
            if (chunks.Length == 1) return chunks[0] ?? string.Empty;

            int totalLength = 0;
            for (int i = 0; i < chunks.Length; i++)
            {
                if (chunks[i] != null)
                {
                    totalLength += chunks[i].Length;
                }
            }

            var sb = new StringBuilder(totalLength);
            for (int i = 0; i < chunks.Length; i++)
            {
                if (chunks[i] != null)
                {
                    sb.Append(chunks[i]);
                }
            }
            return sb.ToString();
        }
    }
}
