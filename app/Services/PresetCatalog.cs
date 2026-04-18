using RegProbe.Application.Models;

namespace RegProbe.Application.Services;

internal sealed class PresetCatalog
{
    public List<PresetModel> GetAll()
    {
        return new List<PresetModel>
        {
            CreateGamingPreset(),
            CreatePrivacyPreset(),
            CreateMinimalistPreset()
        };
    }

    public PresetModel? FindById(string presetId)
    {
        return GetAll().FirstOrDefault(p => p.Id == presetId);
    }

    private static PresetModel CreateGamingPreset()
    {
        return new PresetModel(
            Id: "gaming",
            Name: "Gaming Optimization",
            Description: "Maximize FPS and minimize latency for gaming. Disables background services, enables high performance mode, and optimizes network settings.",
            IconPath: "pack://application:,,,/Resources/Icons/gaming.png",
            Category: PresetCategory.Gaming,
            TweakIds: new List<string>
            {
                "disable-game-bar",
                "disable-game-dvr",
            },
            Level: PresetDifficulty.Beginner
        );
    }

    private static PresetModel CreatePrivacyPreset()
    {
        return new PresetModel(
            Id: "privacy",
            Name: "Privacy Protection",
            Description: "Maximum privacy by disabling telemetry, tracking, and data collection. Includes disabling Cortana, ad tracking, and cloud sync.",
            IconPath: "pack://application:,,,/Resources/Icons/privacy.png",
            Category: PresetCategory.Privacy,
            TweakIds: new List<string>
            {
                "disable-telemetry",
                "disable-activity-history",
                "disable-advertising-id",
            },
            Level: PresetDifficulty.Beginner
        );
    }

    private static PresetModel CreateMinimalistPreset()
    {
        return new PresetModel(
            Id: "minimalist",
            Name: "Minimalist Interface",
            Description: "Clean, fast interface by removing visual effects, disabling widgets, and removing taskbar bloat. Best for older hardware.",
            IconPath: "pack://application:,,,/Resources/Icons/minimal.png",
            Category: PresetCategory.Minimal,
            TweakIds: new List<string>
            {
                "disable-animations",
                "disable-transparency",
            },
            Level: PresetDifficulty.Beginner
        );
    }
}
