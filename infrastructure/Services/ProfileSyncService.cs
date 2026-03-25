using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using OpenTraceProject.Core.Services;
using OpenTraceProject.Infrastructure.Security;

namespace OpenTraceProject.Infrastructure.Services;

public sealed class ProfileSyncService : IProfileSyncService
{
    public async Task ExportProfileAsync(string filePath, string password, IEnumerable<string> enabledTweakIds)
    {
        var json = JsonSerializer.Serialize(enabledTweakIds);
        var encrypted = EncryptionHelper.Encrypt(json, password);
        await File.WriteAllTextAsync(filePath, encrypted);
    }

    public async Task<IEnumerable<string>> ImportProfileAsync(string filePath, string password)
    {
        var encrypted = await File.ReadAllTextAsync(filePath);
        var decrypted = EncryptionHelper.Decrypt(encrypted, password);
        return JsonSerializer.Deserialize<List<string>>(decrypted) ?? new List<string>();
    }
}
