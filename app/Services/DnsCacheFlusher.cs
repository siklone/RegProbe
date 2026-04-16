using System.Diagnostics;

namespace RegProbe.Application.Services;

internal sealed class DnsCacheFlusher
{
    public bool FlushDnsCache()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ipconfig",
                Arguments = "/flushdns",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit();

            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
