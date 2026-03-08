using System;
using System.Buffers.Binary;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsOptimizer.Infrastructure.Elevation;

public static class PipeMessageSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    static PipeMessageSerializer()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public static async Task WriteAsync<T>(Stream stream, T message, CancellationToken ct)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions);
        var lengthBuffer = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(lengthBuffer, payload.Length);

        await stream.WriteAsync(lengthBuffer, ct);
        await stream.WriteAsync(payload, ct);
        await stream.FlushAsync(ct);
    }

    public static async Task<T> ReadAsync<T>(Stream stream, CancellationToken ct)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var lengthBuffer = new byte[sizeof(int)];
        await stream.ReadExactlyAsync(lengthBuffer, 0, lengthBuffer.Length, ct);
        var length = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
        if (length <= 0)
        {
            throw new InvalidDataException("Invalid payload length.");
        }

        var payload = new byte[length];
        await stream.ReadExactlyAsync(payload, 0, payload.Length, ct);

        var message = JsonSerializer.Deserialize<T>(payload, JsonOptions);
        if (message is null)
        {
            throw new InvalidDataException("Failed to deserialize pipe message.");
        }

        return message;
    }
}
