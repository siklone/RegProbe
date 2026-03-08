using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core.Commands;
using WindowsOptimizer.Infrastructure.Commands;
using WindowsOptimizer.Infrastructure.Elevation;
using WindowsOptimizer.Infrastructure.Registry;
using WindowsOptimizer.Core.Files;
using WindowsOptimizer.Core.Registry;
using WindowsOptimizer.Core.Services;
using WindowsOptimizer.Core.Tasks;

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
        var registryAccessor = new LocalRegistryAccessor();
        var serviceManager = new LocalServiceManager();
        var taskManager = new LocalScheduledTaskManager();
        var fileSystemAccessor = new LocalFileSystemAccessor();
        var commandAllowlist = CommandAllowlist.CreateDefault();
        var commandRunner = new LocalCommandRunner();

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

            await HandleConnectionAsync(server, registryAccessor, serviceManager, taskManager, fileSystemAccessor, commandAllowlist, commandRunner, ct);
        }
    }

    private static async Task HandleConnectionAsync(
        NamedPipeServerStream server,
        IRegistryAccessor registryAccessor,
        IServiceManager serviceManager,
        IScheduledTaskManager taskManager,
        IFileSystemAccessor fileSystemAccessor,
        CommandAllowlist commandAllowlist,
        ICommandRunner commandRunner,
        CancellationToken ct)
    {
        ElevatedHostRequest? request = null;
        try
        {
            request = await PipeMessageSerializer.ReadAsync<ElevatedHostRequest>(server, ct);
            var response = await HandleRequestAsync(
                request,
                registryAccessor,
                serviceManager,
                taskManager,
                fileSystemAccessor,
                commandAllowlist,
                commandRunner,
                ct);
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

            var response = CreateErrorResponse(request, ex.Message);
            try
            {
                await PipeMessageSerializer.WriteAsync(server, response, CancellationToken.None);
            }
            catch
            {
            }
        }
    }

    private static async Task<ElevatedHostResponse> HandleRequestAsync(
        ElevatedHostRequest request,
        IRegistryAccessor registryAccessor,
        IServiceManager serviceManager,
        IScheduledTaskManager taskManager,
        IFileSystemAccessor fileSystemAccessor,
        CommandAllowlist commandAllowlist,
        ICommandRunner commandRunner,
        CancellationToken ct)
    {
        switch (request.RequestType)
        {
            case ElevatedHostRequestType.Registry:
            {
                if (request.RegistryRequest is null)
                {
                    return new ElevatedHostResponse(
                        request.RequestId,
                        ElevatedHostRequestType.Registry,
                        RegistryResponse: new ElevatedRegistryResponse(
                            request.RequestId,
                            false,
                            "Registry request payload is required.",
                            null));
                }

                var registryResponse = await HandleRegistryRequestAsync(request.RegistryRequest, registryAccessor, ct);
                return new ElevatedHostResponse(
                    request.RequestId,
                    ElevatedHostRequestType.Registry,
                    RegistryResponse: registryResponse);
            }
            case ElevatedHostRequestType.Service:
            {
                if (request.ServiceRequest is null)
                {
                    return new ElevatedHostResponse(
                        request.RequestId,
                        ElevatedHostRequestType.Service,
                        ServiceResponse: new ElevatedServiceResponse(
                            request.RequestId,
                            false,
                            "Service request payload is required."));
                }

                var serviceResponse = await HandleServiceRequestAsync(request.ServiceRequest, serviceManager, ct);
                return new ElevatedHostResponse(
                    request.RequestId,
                    ElevatedHostRequestType.Service,
                    ServiceResponse: serviceResponse);
            }
            case ElevatedHostRequestType.ScheduledTask:
            {
                if (request.ScheduledTaskRequest is null)
                {
                    return new ElevatedHostResponse(
                        request.RequestId,
                        ElevatedHostRequestType.ScheduledTask,
                        ScheduledTaskResponse: new ElevatedScheduledTaskResponse(
                            request.RequestId,
                            false,
                            "Scheduled task request payload is required."));
                }

                var taskResponse = await HandleScheduledTaskRequestAsync(request.ScheduledTaskRequest, taskManager, ct);
                return new ElevatedHostResponse(
                    request.RequestId,
                    ElevatedHostRequestType.ScheduledTask,
                    ScheduledTaskResponse: taskResponse);
            }
            case ElevatedHostRequestType.FileSystem:
            {
                if (request.FileRequest is null)
                {
                    return new ElevatedHostResponse(
                        request.RequestId,
                        ElevatedHostRequestType.FileSystem,
                        FileResponse: new ElevatedFileResponse(
                            request.RequestId,
                            false,
                            "File request payload is required."));
                }

                var fileResponse = await HandleFileRequestAsync(request.FileRequest, fileSystemAccessor, ct);
                return new ElevatedHostResponse(
                    request.RequestId,
                    ElevatedHostRequestType.FileSystem,
                    FileResponse: fileResponse);
            }
            case ElevatedHostRequestType.Command:
            {
                if (request.CommandRequest is null)
                {
                    return new ElevatedHostResponse(
                        request.RequestId,
                        ElevatedHostRequestType.Command,
                        CommandResponse: new ElevatedCommandResponse(
                            request.RequestId,
                            false,
                            "Command request payload is required.",
                            null));
                }

                var commandResponse = await HandleCommandRequestAsync(request.CommandRequest, commandAllowlist, commandRunner, ct);
                return new ElevatedHostResponse(
                    request.RequestId,
                    ElevatedHostRequestType.Command,
                    CommandResponse: commandResponse);
            }
            case ElevatedHostRequestType.Ping:
            {
                return new ElevatedHostResponse(
                    request.RequestId,
                    ElevatedHostRequestType.Ping);
            }
            default:
                return new ElevatedHostResponse(
                    request.RequestId,
                    request.RequestType,
                    FileResponse: new ElevatedFileResponse(
                        request.RequestId,
                        false,
                        "Unsupported elevated host request."));
        }
    }

    private static async Task<ElevatedRegistryResponse> HandleRegistryRequestAsync(
        ElevatedRegistryRequest request,
        IRegistryAccessor registryAccessor,
        CancellationToken ct)
    {
        try
        {
            switch (request.Operation)
            {
                case ElevatedRegistryOperation.ReadValue:
                {
                    var result = await registryAccessor.ReadValueAsync(request.Reference, ct);
                    return new ElevatedRegistryResponse(request.RequestId, true, null, result);
                }
                case ElevatedRegistryOperation.SetValue:
                {
                    if (request.Value is null)
                    {
                        throw new InvalidDataException("Registry value payload is required.");
                    }

                    await registryAccessor.SetValueAsync(request.Reference, request.Value, ct);
                    return new ElevatedRegistryResponse(request.RequestId, true, null, null);
                }
                case ElevatedRegistryOperation.DeleteValue:
                {
                    await registryAccessor.DeleteValueAsync(request.Reference, ct);
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

    private static async Task<ElevatedServiceResponse> HandleServiceRequestAsync(
        ElevatedServiceRequest request,
        IServiceManager serviceManager,
        CancellationToken ct)
    {
        try
        {
            switch (request.Operation)
            {
                case ElevatedServiceOperation.Query:
                {
                    var info = await serviceManager.QueryAsync(request.ServiceName, ct);
                    return new ElevatedServiceResponse(request.RequestId, true, null, info);
                }
                case ElevatedServiceOperation.SetStartMode:
                {
                    if (request.StartMode is null)
                    {
                        throw new InvalidDataException("Service start mode is required.");
                    }

                    await serviceManager.SetStartModeAsync(request.ServiceName, request.StartMode.Value, ct);
                    return new ElevatedServiceResponse(request.RequestId, true, null);
                }
                case ElevatedServiceOperation.Start:
                {
                    await serviceManager.StartAsync(request.ServiceName, ct);
                    return new ElevatedServiceResponse(request.RequestId, true, null);
                }
                case ElevatedServiceOperation.Stop:
                {
                    await serviceManager.StopAsync(request.ServiceName, ct);
                    return new ElevatedServiceResponse(request.RequestId, true, null);
                }
                case ElevatedServiceOperation.List:
                {
                    var names = await serviceManager.ListServiceNamesAsync(ct);
                    return new ElevatedServiceResponse(request.RequestId, true, null, null, names);
                }
                default:
                    return new ElevatedServiceResponse(
                        request.RequestId,
                        false,
                        "Unsupported service operation.");
            }
        }
        catch (Exception ex)
        {
            return new ElevatedServiceResponse(
                request.RequestId,
                false,
                ex.Message);
        }
    }

    private static async Task<ElevatedScheduledTaskResponse> HandleScheduledTaskRequestAsync(
        ElevatedScheduledTaskRequest request,
        IScheduledTaskManager taskManager,
        CancellationToken ct)
    {
        try
        {
            switch (request.Operation)
            {
                case ElevatedScheduledTaskOperation.Query:
                {
                    var info = await taskManager.QueryAsync(request.TaskPath, ct);
                    return new ElevatedScheduledTaskResponse(request.RequestId, true, null, info);
                }
                case ElevatedScheduledTaskOperation.SetEnabled:
                {
                    if (request.Enabled is null)
                    {
                        throw new InvalidDataException("Scheduled task enabled state is required.");
                    }

                    await taskManager.SetEnabledAsync(request.TaskPath, request.Enabled.Value, ct);
                    return new ElevatedScheduledTaskResponse(request.RequestId, true, null);
                }
                default:
                    return new ElevatedScheduledTaskResponse(
                        request.RequestId,
                        false,
                        "Unsupported scheduled task operation.");
            }
        }
        catch (Exception ex)
        {
            return new ElevatedScheduledTaskResponse(
                request.RequestId,
                false,
                ex.Message);
        }
    }

    private static async Task<ElevatedFileResponse> HandleFileRequestAsync(
        ElevatedFileRequest request,
        IFileSystemAccessor fileSystemAccessor,
        CancellationToken ct)
    {
        try
        {
            switch (request.Operation)
            {
                case ElevatedFileOperation.Exists:
                {
                    var exists = await fileSystemAccessor.FileExistsAsync(request.SourcePath, ct);
                    return new ElevatedFileResponse(request.RequestId, true, null, exists);
                }
                case ElevatedFileOperation.Move:
                {
                    if (string.IsNullOrWhiteSpace(request.DestinationPath))
                    {
                        throw new InvalidDataException("Destination path is required.");
                    }

                    await fileSystemAccessor.MoveFileAsync(request.SourcePath, request.DestinationPath, ct);
                    return new ElevatedFileResponse(request.RequestId, true, null);
                }
                default:
                    return new ElevatedFileResponse(
                        request.RequestId,
                        false,
                        "Unsupported file operation.");
            }
        }
        catch (Exception ex)
        {
            return new ElevatedFileResponse(
                request.RequestId,
                false,
                ex.Message);
        }
    }

    private static async Task<ElevatedCommandResponse> HandleCommandRequestAsync(
        ElevatedCommandRequest request,
        CommandAllowlist allowlist,
        ICommandRunner runner,
        CancellationToken ct)
    {
        try
        {
            if (!allowlist.IsAllowed(request.Command, out var reason))
            {
                return new ElevatedCommandResponse(
                    request.RequestId,
                    false,
                    $"Command not allowed: {reason}",
                    null);
            }

            var result = await runner.RunAsync(request.Command, ct);

            return new ElevatedCommandResponse(
                request.RequestId,
                true,
                null,
                result);
        }
        catch (Exception ex)
        {
            return new ElevatedCommandResponse(
                request.RequestId,
                false,
                ex.Message,
                null);
        }
    }

    private static ElevatedHostResponse CreateErrorResponse(ElevatedHostRequest request, string message)
    {
        return request.RequestType switch
        {
            ElevatedHostRequestType.Registry => new ElevatedHostResponse(
                request.RequestId,
                ElevatedHostRequestType.Registry,
                RegistryResponse: new ElevatedRegistryResponse(request.RequestId, false, message, null)),
            ElevatedHostRequestType.Service => new ElevatedHostResponse(
                request.RequestId,
                ElevatedHostRequestType.Service,
                ServiceResponse: new ElevatedServiceResponse(request.RequestId, false, message)),
            ElevatedHostRequestType.ScheduledTask => new ElevatedHostResponse(
                request.RequestId,
                ElevatedHostRequestType.ScheduledTask,
                ScheduledTaskResponse: new ElevatedScheduledTaskResponse(request.RequestId, false, message)),
            ElevatedHostRequestType.FileSystem => new ElevatedHostResponse(
                request.RequestId,
                ElevatedHostRequestType.FileSystem,
                FileResponse: new ElevatedFileResponse(request.RequestId, false, message)),
            ElevatedHostRequestType.Command => new ElevatedHostResponse(
                request.RequestId,
                ElevatedHostRequestType.Command,
                CommandResponse: new ElevatedCommandResponse(request.RequestId, false, message, null)),
            _ => new ElevatedHostResponse(
                request.RequestId,
                request.RequestType,
                FileResponse: new ElevatedFileResponse(request.RequestId, false, message))
        };
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
