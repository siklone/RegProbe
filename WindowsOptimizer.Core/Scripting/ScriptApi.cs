using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinRegistry = Microsoft.Win32.Registry;
using WinRegistryValueKind = Microsoft.Win32.RegistryValueKind;
using WinRegistryKey = Microsoft.Win32.RegistryKey;

namespace WindowsOptimizer.Core.Scripting;

/// <summary>
/// API exposed to scripts for safe Windows optimization operations
/// All methods check security context before execution
/// </summary>
public sealed class ScriptApi
{
    private readonly ScriptSecurityContext _securityContext;
    private readonly List<string> _outputLines = new();

    public ScriptApi(ScriptSecurityContext securityContext)
    {
        _securityContext = securityContext;
    }

    /// <summary>
    /// Get output lines collected during script execution
    /// </summary>
    public List<string> GetOutputLines() => _outputLines;

    #region Console Output

    /// <summary>
    /// Print a message to the script output
    /// </summary>
    public void Print(string message)
    {
        _outputLines.Add(message);
        Debug.WriteLine($"[Script] {message}");
    }

    #endregion

    #region Registry Operations

    /// <summary>
    /// Get a registry value
    /// </summary>
    public object? RegistryGet(string keyPath, string valueName)
    {
        if (!_securityContext.AllowRegistryAccess)
        {
            throw new UnauthorizedAccessException("Registry access is not allowed in this security context");
        }

        try
        {
            return WinRegistry.GetValue(keyPath, valueName, null);
        }
        catch (Exception ex)
        {
            Print($"Registry read error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Set a registry value
    /// </summary>
    public bool RegistrySet(string keyPath, string valueName, object value, string valueKind = "String")
    {
        if (!_securityContext.AllowRegistryAccess)
        {
            throw new UnauthorizedAccessException("Registry access is not allowed in this security context");
        }

        try
        {
            var kind = Enum.Parse<WinRegistryValueKind>(valueKind);
            WinRegistry.SetValue(keyPath, valueName, value, kind);
            Print($"Registry set: {keyPath}\\{valueName} = {value}");
            return true;
        }
        catch (Exception ex)
        {
            Print($"Registry write error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Delete a registry value
    /// </summary>
    public bool RegistryDelete(string keyPath, string valueName)
    {
        if (!_securityContext.AllowRegistryAccess)
        {
            throw new UnauthorizedAccessException("Registry access is not allowed in this security context");
        }

        try
        {
            using var key = GetRegistryKey(keyPath, writable: true);
            if (key != null)
            {
                key.DeleteValue(valueName, throwOnMissingValue: false);
                Print($"Registry deleted: {keyPath}\\{valueName}");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Print($"Registry delete error: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region File System Operations

    /// <summary>
    /// Read a file
    /// </summary>
    public string? FileRead(string path)
    {
        if (!_securityContext.AllowFileSystemAccess)
        {
            throw new UnauthorizedAccessException("File system access is not allowed in this security context");
        }

        if (!IsPathAllowed(path))
        {
            throw new UnauthorizedAccessException($"Access to path '{path}' is not allowed");
        }

        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            Print($"File read error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Write to a file
    /// </summary>
    public bool FileWrite(string path, string content)
    {
        if (!_securityContext.AllowFileSystemAccess)
        {
            throw new UnauthorizedAccessException("File system access is not allowed in this security context");
        }

        if (!IsPathAllowed(path))
        {
            throw new UnauthorizedAccessException($"Access to path '{path}' is not allowed");
        }

        try
        {
            File.WriteAllText(path, content);
            Print($"File written: {path}");
            return true;
        }
        catch (Exception ex)
        {
            Print($"File write error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if a file exists
    /// </summary>
    public bool FileExists(string path)
    {
        if (!_securityContext.AllowFileSystemAccess)
        {
            throw new UnauthorizedAccessException("File system access is not allowed in this security context");
        }

        return File.Exists(path);
    }

    #endregion

    #region Process Operations

    /// <summary>
    /// Execute a command and return output
    /// </summary>
    public string? Execute(string command, string arguments = "")
    {
        if (!_securityContext.AllowProcessExecution)
        {
            throw new UnauthorizedAccessException("Process execution is not allowed in this security context");
        }

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrEmpty(error))
            {
                Print($"Process error: {error}");
            }

            return output;
        }
        catch (Exception ex)
        {
            Print($"Process execution error: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region System Information

    /// <summary>
    /// Get system information
    /// </summary>
    public Dictionary<string, object> GetSystemInfo()
    {
        return new Dictionary<string, object>
        {
            ["OS"] = Environment.OSVersion.ToString(),
            ["MachineName"] = Environment.MachineName,
            ["ProcessorCount"] = Environment.ProcessorCount,
            ["Is64Bit"] = Environment.Is64BitOperatingSystem,
            ["UserName"] = Environment.UserName,
            ["SystemDirectory"] = Environment.SystemDirectory
        };
    }

    /// <summary>
    /// Get environment variable
    /// </summary>
    public string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name);
    }

    #endregion

    #region Utility Functions

    /// <summary>
    /// Sleep for specified milliseconds
    /// </summary>
    public void Sleep(int milliseconds)
    {
        System.Threading.Thread.Sleep(milliseconds);
    }

    /// <summary>
    /// Get current timestamp
    /// </summary>
    public string GetTimestamp()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    #endregion

    #region Helper Methods

    private bool IsPathAllowed(string path)
    {
        if (_securityContext.AllowedPaths.Count == 0)
        {
            return true; // No restrictions
        }

        var fullPath = Path.GetFullPath(path);
        return _securityContext.AllowedPaths.Any(allowedPath =>
            fullPath.StartsWith(Path.GetFullPath(allowedPath), StringComparison.OrdinalIgnoreCase));
    }

    private WinRegistryKey? GetRegistryKey(string keyPath, bool writable)
    {
        var parts = keyPath.Split('\\');
        if (parts.Length < 2) return null;

        var hive = parts[0].ToUpperInvariant() switch
        {
            "HKEY_CURRENT_USER" or "HKCU" => WinRegistry.CurrentUser,
            "HKEY_LOCAL_MACHINE" or "HKLM" => WinRegistry.LocalMachine,
            "HKEY_CLASSES_ROOT" or "HKCR" => WinRegistry.ClassesRoot,
            "HKEY_USERS" or "HKU" => WinRegistry.Users,
            "HKEY_CURRENT_CONFIG" or "HKCC" => WinRegistry.CurrentConfig,
            _ => null
        };

        if (hive == null) return null;

        var subKeyPath = string.Join("\\", parts.Skip(1));
        return writable
            ? hive.OpenSubKey(subKeyPath, writable: true)
            : hive.OpenSubKey(subKeyPath, writable: false);
    }

    #endregion
}
