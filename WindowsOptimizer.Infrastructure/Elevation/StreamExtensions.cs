using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Infrastructure.Elevation;

internal static class StreamExtensions
{
    public static async Task ReadExactlyAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, ct);
            if (read == 0)
            {
                throw new EndOfStreamException("Unexpected end of stream.");
            }

            totalRead += read;
        }
    }
}
