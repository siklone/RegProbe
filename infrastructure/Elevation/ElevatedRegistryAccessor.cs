using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using RegProbe.Core.Commands;
using RegProbe.Core.Registry;

namespace RegProbe.Infrastructure.Elevation;

public sealed class ElevatedRegistryAccessor : IRegistryAccessor
{
    private const string System32RegExe = "reg.exe";
    private const string PoliciesPrefix = @"Software\Policies\";
    private const string LegacyPoliciesPrefix = @"Software\Microsoft\Windows\CurrentVersion\Policies\";
    private const int HResultAccessDenied = unchecked((int)0x80070005);
    private const int HResultPrivilegeNotHeld = unchecked((int)0x80070522);
    private readonly IElevatedHostClient _client;
    private readonly ElevatedCommandRunner _commandRunner;

    public ElevatedRegistryAccessor(IElevatedHostClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _commandRunner = new ElevatedCommandRunner(client);
    }

    public async Task<RegistryValueReadResult> ReadValueAsync(RegistryValueReference reference, CancellationToken ct)
    {
        var request = new ElevatedRegistryRequest(
            Guid.NewGuid(),
            ElevatedRegistryOperation.ReadValue,
            reference,
            null);

        var response = await SendAsync(request, ct);

        if (response.ReadResult is null)
        {
            throw new ElevatedHostException("Elevated host did not return a read result.");
        }

        return response.ReadResult;
    }

    public async Task SetValueAsync(RegistryValueReference reference, RegistryValueData value, CancellationToken ct)
    {
        if (ShouldPreferCommandExecution(reference))
        {
            await RunRegAsync(BuildSetRequest(reference, value), ct);
            return;
        }

        try
        {
            var request = new ElevatedRegistryRequest(
                Guid.NewGuid(),
                ElevatedRegistryOperation.SetValue,
                reference,
                value);

            await SendAsync(request, ct);
        }
        catch (ElevatedHostException ex) when (IsAccessDeniedException(ex))
        {
            await RunRegAsync(BuildSetRequest(reference, value), ct);
        }
        catch (UnauthorizedAccessException ex) when (IsAccessDeniedException(ex))
        {
            await RunRegAsync(BuildSetRequest(reference, value), ct);
        }
        catch (Win32Exception ex) when (IsAccessDeniedException(ex))
        {
            await RunRegAsync(BuildSetRequest(reference, value), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && IsAccessDeniedException(ex))
        {
            await RunRegAsync(BuildSetRequest(reference, value), ct);
        }
    }

    public async Task DeleteValueAsync(RegistryValueReference reference, CancellationToken ct)
    {
        if (ShouldPreferCommandExecution(reference))
        {
            await RunRegAsync(BuildDeleteRequest(reference), ct, allowMissingDelete: true);
            return;
        }

        try
        {
            var request = new ElevatedRegistryRequest(
                Guid.NewGuid(),
                ElevatedRegistryOperation.DeleteValue,
                reference,
                null);

            await SendAsync(request, ct);
        }
        catch (ElevatedHostException ex) when (IsAccessDeniedException(ex))
        {
            await RunRegAsync(BuildDeleteRequest(reference), ct, allowMissingDelete: true);
        }
        catch (UnauthorizedAccessException ex) when (IsAccessDeniedException(ex))
        {
            await RunRegAsync(BuildDeleteRequest(reference), ct, allowMissingDelete: true);
        }
        catch (Win32Exception ex) when (IsAccessDeniedException(ex))
        {
            await RunRegAsync(BuildDeleteRequest(reference), ct, allowMissingDelete: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && IsAccessDeniedException(ex))
        {
            await RunRegAsync(BuildDeleteRequest(reference), ct, allowMissingDelete: true);
        }
    }

    private async Task<ElevatedRegistryResponse> SendAsync(
        ElevatedRegistryRequest request,
        CancellationToken ct)
    {
        var hostRequest = new ElevatedHostRequest(
            request.RequestId,
            ElevatedHostRequestType.Registry,
            RegistryRequest: request);

        var hostResponse = await _client.SendAsync(hostRequest, ct);
        if (hostResponse.ResponseType != ElevatedHostRequestType.Registry || hostResponse.RegistryResponse is null)
        {
            throw new ElevatedHostException("Elevated host did not return a registry response.");
        }

        var response = hostResponse.RegistryResponse;
        if (response.RequestId != request.RequestId)
        {
            throw new ElevatedHostException("Elevated host response did not match the request.");
        }

        if (!response.Success)
        {
            var message = string.IsNullOrWhiteSpace(response.Error)
                ? "Elevated host reported an error."
                : response.Error;
            throw new ElevatedHostException(message, response.HResult);
        }

        return response;
    }

    private async Task RunRegAsync(CommandRequest request, CancellationToken ct, bool allowMissingDelete = false)
    {
        var result = await _commandRunner.RunAsync(request, ct);

        if (result.TimedOut)
        {
            throw new ElevatedHostException($"reg.exe timed out: {string.Join(' ', request.Arguments)}");
        }

        if (result.ExitCode == 0)
        {
            return;
        }

        if (allowMissingDelete && IsMissingDeleteResult(result))
        {
            return;
        }

        var errorText = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput
            : result.StandardError;

        throw new ElevatedHostException($"reg.exe failed ({result.ExitCode}): {errorText}".Trim());
    }

    private static bool IsAccessDeniedException(Exception ex)
    {
        if (ex is UnauthorizedAccessException)
        {
            return true;
        }

        if (ex is Win32Exception win32)
        {
            return win32.NativeErrorCode is 5 or 1314;
        }

        if (ex is ElevatedHostException elevated && elevated.ErrorHResult.HasValue)
        {
            return IsAccessDeniedHResult(elevated.ErrorHResult.Value);
        }

        return IsAccessDeniedHResult(ex.HResult);
    }

    private static bool IsAccessDeniedHResult(int hresult)
    {
        return hresult == HResultAccessDenied || hresult == HResultPrivilegeNotHeld;
    }

    private static bool ShouldPreferCommandExecution(RegistryValueReference reference)
    {
        if (reference.Hive != RegistryHive.CurrentUser)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(reference.KeyPath))
        {
            return false;
        }

        var normalized = reference.KeyPath.TrimStart('\\');
        return normalized.StartsWith(PoliciesPrefix, StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith(LegacyPoliciesPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMissingDeleteResult(CommandResult result)
    {
        var combined = $"{result.StandardOutput}\n{result.StandardError}";
        return combined.Contains("unable to find", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("cannot find", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("specified registry key or value", StringComparison.OrdinalIgnoreCase);
    }

    private static CommandRequest BuildSetRequest(RegistryValueReference reference, RegistryValueData value)
    {
        var arguments = new List<string>
        {
            "add",
            FormatRegistryPath(reference),
        };

        if (string.IsNullOrWhiteSpace(reference.ValueName))
        {
            arguments.Add("/ve");
        }
        else
        {
            arguments.Add("/v");
            arguments.Add(reference.ValueName);
        }

        arguments.Add("/t");
        arguments.Add(GetRegistryTypeName(value.Kind));
        arguments.Add("/d");
        arguments.Add(GetRegistryData(value));
        arguments.Add("/f");

        return new CommandRequest(
            global::System.IO.Path.Combine(Environment.SystemDirectory, System32RegExe),
            new ReadOnlyCollection<string>(arguments));
    }

    private static CommandRequest BuildDeleteRequest(RegistryValueReference reference)
    {
        var arguments = new List<string>
        {
            "delete",
            FormatRegistryPath(reference),
        };

        if (string.IsNullOrWhiteSpace(reference.ValueName))
        {
            arguments.Add("/ve");
        }
        else
        {
            arguments.Add("/v");
            arguments.Add(reference.ValueName);
        }

        arguments.Add("/f");

        return new CommandRequest(
            global::System.IO.Path.Combine(Environment.SystemDirectory, System32RegExe),
            new ReadOnlyCollection<string>(arguments));
    }

    private static string FormatRegistryPath(RegistryValueReference reference)
    {
        var hive = reference.Hive switch
        {
            RegistryHive.LocalMachine => "HKLM",
            RegistryHive.CurrentUser => "HKCU",
            RegistryHive.ClassesRoot => "HKCR",
            RegistryHive.Users => "HKU",
            RegistryHive.CurrentConfig => "HKCC",
            _ => throw new ArgumentOutOfRangeException(nameof(reference), reference.Hive, "Unsupported registry hive.")
        };

        return string.IsNullOrWhiteSpace(reference.KeyPath)
            ? hive
            : $"{hive}\\{reference.KeyPath.TrimStart('\\')}";
    }

    private static string GetRegistryTypeName(RegistryValueKind kind)
    {
        return kind switch
        {
            RegistryValueKind.String => "REG_SZ",
            RegistryValueKind.ExpandString => "REG_EXPAND_SZ",
            RegistryValueKind.MultiString => "REG_MULTI_SZ",
            RegistryValueKind.Binary => "REG_BINARY",
            RegistryValueKind.DWord => "REG_DWORD",
            RegistryValueKind.QWord => "REG_QWORD",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported registry value kind for reg.exe.")
        };
    }

    private static string GetRegistryData(RegistryValueData value)
    {
        return value.Kind switch
        {
            RegistryValueKind.DWord or RegistryValueKind.QWord => RegistryValueComparer.FormatNumericValueForRegExe(value.Kind, value.NumericValue),
            RegistryValueKind.String or RegistryValueKind.ExpandString => value.StringValue ?? string.Empty,
            RegistryValueKind.MultiString => string.Join("\\0", value.MultiStringValue ?? Array.Empty<string>()),
            RegistryValueKind.Binary => BitConverter.ToString(value.BinaryValue ?? Array.Empty<byte>()).Replace("-", string.Empty, StringComparison.Ordinal),
            _ => throw new ArgumentOutOfRangeException(nameof(value.Kind), value.Kind, "Unsupported registry value kind for reg.exe.")
        };
    }
}
