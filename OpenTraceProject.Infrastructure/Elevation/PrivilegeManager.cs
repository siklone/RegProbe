using System;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace OpenTraceProject.Infrastructure.Elevation;

public sealed class PrivilegeManager : IDisposable
{
    private const int SE_PRIVILEGE_ENABLED = 0x00000002;
    private const int TOKEN_QUERY = 0x0008;
    private const int TOKEN_ADJUST_PRIVILEGES = 0x0020;

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct LUID_AND_ATTRIBUTES
    {
        public long Luid;
        public int Attributes;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct TOKEN_PRIVILEGES
    {
        public int PrivilegeCount;
        public LUID_AND_ATTRIBUTES Privileges;
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, out long lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private readonly IntPtr _token;
    private bool _disposed;

    public PrivilegeManager()
    {
        if (!OpenProcessToken(System.Diagnostics.Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out _token))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public void EnablePrivilege(string privilegeName)
    {
        if (!LookupPrivilegeValue(null, privilegeName, out var luid))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var tp = new TOKEN_PRIVILEGES
        {
            PrivilegeCount = 1,
            Privileges = new LUID_AND_ATTRIBUTES { Luid = luid, Attributes = SE_PRIVILEGE_ENABLED }
        };

        if (!AdjustTokenPrivileges(_token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var error = Marshal.GetLastWin32Error();
        if (error != 0) // ERROR_SUCCESS is 0
        {
             // Note: AdjustTokenPrivileges can return true but still fail if the privilege isn't held by the token.
             // Typically error is ERROR_NOT_ALL_ASSIGNED (1300)
             throw new Win32Exception(error);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            CloseHandle(_token);
            _disposed = true;
        }
    }
}

public static class PrivilegeNames
{
    public const string TakeOwnership = "SeTakeOwnershipPrivilege";
    public const string Restore = "SeRestorePrivilege";
    public const string Backup = "SeBackupPrivilege";
    public const string Security = "SeSecurityPrivilege";
}
