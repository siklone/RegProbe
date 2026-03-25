using System.Collections.Generic;
using System.Threading.Tasks;
using OpenTraceProject.Core.Models;

namespace OpenTraceProject.Core.Services;

public interface IProfileManager
{
    Task<TweakProfile> LoadProfileAsync(string filePath);
    Task SaveProfileAsync(TweakProfile profile, string filePath);
    Task<List<TweakProfile>> GetPresetsAsync();
    Task InitializePresetsAsync();
    Task<TweakProfile> CreatePresetAsync(string presetName);
}
