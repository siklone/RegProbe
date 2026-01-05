using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WindowsOptimizer.App.Services;

/// <summary>
/// System Restore service for creating restore points before applying tweaks.
/// Requires admin privileges.
/// </summary>
public class SystemRestoreService
{
    private const int BEGIN_SYSTEM_CHANGE = 100;
    private const int END_SYSTEM_CHANGE = 101;
    private const int APPLICATION_INSTALL = 0;
    private const int APPLICATION_UNINSTALL = 1;
    private const int MODIFY_SETTINGS = 12;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RESTOREPOINTINFO
    {
        public int dwEventType;
        public int dwRestorePtType;
        public long llSequenceNumber;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szDescription;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct STATEMGRSTATUS
    {
        public int nStatus;
        public long llSequenceNumber;
    }

    [DllImport("srclient.dll", CharSet = CharSet.Unicode)]
    private static extern int SRSetRestorePointW(ref RESTOREPOINTINFO pRestorePtSpec, out STATEMGRSTATUS pSMgrStatus);

    /// <summary>
    /// Check if System Restore is enabled on the system.
    /// </summary>
    public static bool IsSystemRestoreEnabled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore");
            
            if (key == null) return false;
            
            var value = key.GetValue("RPSessionInterval");
            return value != null && Convert.ToInt32(value) != 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Create a system restore point.
    /// </summary>
    /// <param name="description">Description for the restore point</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<RestorePointResult> CreateRestorePointAsync(string description)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!IsSystemRestoreEnabled())
                {
                    return new RestorePointResult(false, "System Restore is disabled");
                }

                var restorePoint = new RESTOREPOINTINFO
                {
                    dwEventType = BEGIN_SYSTEM_CHANGE,
                    dwRestorePtType = MODIFY_SETTINGS,
                    llSequenceNumber = 0,
                    szDescription = description.Length > 255 ? description[..255] : description
                };

                var status = new STATEMGRSTATUS();
                int result = SRSetRestorePointW(ref restorePoint, out status);

                if (result == 0)
                {
                    Debug.WriteLine($"Restore point created: {description} (Seq: {status.llSequenceNumber})");
                    return new RestorePointResult(true, "Restore point created successfully", status.llSequenceNumber);
                }
                else
                {
                    return new RestorePointResult(false, $"Failed to create restore point (Error: {result})");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Restore point error: {ex.Message}");
                return new RestorePointResult(false, ex.Message);
            }
        });
    }

    /// <summary>
    /// Create a restore point specifically for Windows Optimizer actions.
    /// </summary>
    public Task<RestorePointResult> CreateOptimizerRestorePointAsync(string action)
    {
        var description = $"Windows Optimizer - {action} - {DateTime.Now:yyyy-MM-dd HH:mm}";
        return CreateRestorePointAsync(description);
    }
}

/// <summary>
/// Result of a restore point creation attempt.
/// </summary>
public record RestorePointResult(bool Success, string Message, long SequenceNumber = 0);
