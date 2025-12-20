using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Infrastructure.Elevation;
using WindowsOptimizer.Infrastructure.Registry;

namespace WindowsOptimizer.ElevatedHost;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var options = HostOptions.Parse(args);
        using var cts = new CancellationTokenSource();

        if (options.ParentProcessId > 0)
        {
            _ = MonitorParentAsync(options.ParentProcessId, cts);
        }

        try
        {
            await RunServerAsync(options.PipeName, cts.Token);
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static async Task RunServerAsync(string pipeName, CancellationToken ct)
    {
        var accessor = new LocalRegistryAccessor();

        while (!ct.IsCancellationRequested)
        {
            using var server = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

            try
            {
                await server.WaitForConnectionAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await HandleConnectionAsync(server, accessor, ct);
        }
    }

    private static async Task HandleConnectionAsync(
        NamedPipeServerStream server,
        IRegistryAccessor accessor,
        CancellationToken ct)
    {
        ElevatedRegistryRequest? request = null;
        try
        {
            request = await PipeMessageSerializer.ReadAsync<ElevatedRegistryRequest>(server, ct);
            var response = await HandleRequestAsync(request, accessor, ct);
            await PipeMessageSerializer.WriteAsync(server, response, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (EndOfStreamException)
        {
        }
        catch (IOException)
        {
        }
        catch (Exception ex)
        {
            if (request is null)
            {
                return;
            }

            var response = new ElevatedRegistryResponse(
                request.RequestId,
                false,
                ex.Message,
                null);
            try
            {
                await PipeMessageSerializer.WriteAsync(server, response, CancellationToken.None);
            }
            catch
            {
            }
        }
    }

    private static async Task<ElevatedRegistryResponse> HandleRequestAsync(
        ElevatedRegistryRequest request,
        IRegistryAccessor accessor,
        CancellationToken ct)
    {
        try
        {
            switch (request.Operation)
            {
                case ElevatedRegistryOperation.ReadValue:
                {
                    var result = await accessor.ReadValueAsync(request.Reference, ct);
                    return new ElevatedRegistryResponse(request.RequestId, true, null, result);
                }
                case ElevatedRegistryOperation.SetValue:
                {
                    if (request.Value is null)
                    {
                        throw new InvalidDataException("Registry value payload is required.");
                    }

                    await accessor.SetValueAsync(request.Reference, request.Value, ct);
                    return new ElevatedRegistryResponse(request.RequestId, true, null, null);
                }
                case ElevatedRegistryOperation.DeleteValue:
                {
                    await accessor.DeleteValueAsync(request.Reference, ct);
                    return new ElevatedRegistryResponse(request.RequestId, true, null, null);
                }
                default:
                    return new ElevatedRegistryResponse(
                        request.RequestId,
                        false,
                        "Unsupported registry operation.",
                        null);
            }
        }
        catch (Exception ex)
        {
            return new ElevatedRegistryResponse(
                request.RequestId,
                false,
                ex.Message,
                null);
        }
    }

    private static async Task MonitorParentAsync(int parentProcessId, CancellationTokenSource cts)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(parentProcessId);
            await process.WaitForExitAsync();
        }
        catch
        {
        }
        finally
        {
            cts.Cancel();
        }
    }

    private sealed class HostOptions
    {
        public string PipeName { get; init; } = ElevatedHostDefaults.PipeName;
        public int ParentProcessId { get; init; }

        public static HostOptions Parse(string[] args)
        {
            var pipeName = ElevatedHostDefaults.PipeName;
            var parentPid = 0;

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, "--pipe", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    pipeName = args[++i];
                }
                else if (string.Equals(arg, "--parent-pid", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out var parsed))
                    {
                        parentPid = parsed;
                    }
                }
            }

            return new HostOptions
            {
                PipeName = string.IsNullOrWhiteSpace(pipeName) ? ElevatedHostDefaults.PipeName : pipeName,
                ParentProcessId = parentPid
            };
        }
    }
}
