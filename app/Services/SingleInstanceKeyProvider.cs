using System.Security.Cryptography;
using System.Text;

namespace RegProbe.App.Services;

internal static class SingleInstanceKeyProvider
{
    public static string GetInstanceKey()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory.TrimEnd('\\', '/').ToUpperInvariant();
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(baseDir));
            return Convert.ToHexString(bytes.AsSpan(0, 6)).ToLowerInvariant();
        }
        catch
        {
            return "default";
        }
    }
}
