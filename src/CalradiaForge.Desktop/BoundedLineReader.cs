using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CalradiaForge.Desktop
{
    /// <summary>Reads newline-delimited protocol frames without buffering an unbounded line.</summary>
    internal sealed class BoundedLineReader
    {
        readonly TextReader reader;
        readonly int maximumCharacters;
        readonly char[] buffer;
        int bufferedCharacters;
        int offset;
        bool skipLineFeedAfterCarriageReturn;

        internal BoundedLineReader(TextReader reader, int maximumCharacters, int bufferSize = 4096)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            if (maximumCharacters <= 0) throw new ArgumentOutOfRangeException(nameof(maximumCharacters));
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));
            this.maximumCharacters = maximumCharacters;
            buffer = new char[bufferSize];
        }

        internal async Task<string> ReadLineAsync(CancellationToken cancellationToken)
        {
            var line = new StringBuilder(Math.Min(maximumCharacters, buffer.Length));
            while (true)
            {
                if (offset >= bufferedCharacters)
                {
                    bufferedCharacters = await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                    offset = 0;
                    if (bufferedCharacters == 0)
                        return line.Length == 0 ? null : line.ToString();
                }

                if (skipLineFeedAfterCarriageReturn)
                {
                    skipLineFeedAfterCarriageReturn = false;
                    if (buffer[offset] == '\n') offset++;
                    if (offset >= bufferedCharacters) continue;
                }

                var carriageReturn = Array.IndexOf(buffer, '\r', offset, bufferedCharacters - offset);
                var lineFeed = Array.IndexOf(buffer, '\n', offset, bufferedCharacters - offset);
                var terminator = Earliest(carriageReturn, lineFeed);
                if (terminator < 0)
                {
                    AppendBounded(line, offset, bufferedCharacters - offset);
                    offset = bufferedCharacters;
                    continue;
                }

                AppendBounded(line, offset, terminator - offset);
                var isCarriageReturn = terminator == carriageReturn;
                offset = terminator + 1;
                if (isCarriageReturn)
                {
                    if (offset < bufferedCharacters && buffer[offset] == '\n') offset++;
                    else if (offset >= bufferedCharacters) skipLineFeedAfterCarriageReturn = true;
                }
                return line.ToString();
            }
        }

        static int Earliest(int first, int second)
        {
            if (first < 0) return second;
            if (second < 0) return first;
            return Math.Min(first, second);
        }

        void AppendBounded(StringBuilder line, int start, int length)
        {
            if (length > maximumCharacters - line.Length)
                throw new IOException("Response too large");
            line.Append(buffer, start, length);
        }
    }
}
