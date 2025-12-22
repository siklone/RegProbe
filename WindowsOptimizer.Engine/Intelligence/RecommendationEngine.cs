using System.Collections.Generic;
using WindowsOptimizer.Core.Intelligence;
using WindowsOptimizer.Core.Services;

namespace WindowsOptimizer.Engine.Intelligence;

public sealed class RecommendationEngine : IRecommendationEngine
{
    public Task<IEnumerable<TweakRecommendation>> GetRecommendationsAsync(HardwareProfile profile)
    {
        return Task.Run(() => GetRecommendations(profile));
    }

    public IEnumerable<TweakRecommendation> GetRecommendations(HardwareProfile profile)
    {
        var recommendations = new List<TweakRecommendation>();

        // Storage Tweaks
        if (profile.PrimaryDiskType == StorageType.NVMe)
        {
            recommendations.Add(new TweakRecommendation(
                "disable-paging-executive", // Placeholder ID
                0.9,
                "NVMe drive detected. Disabling paging executive can improve system responsiveness by keeping kernel data in RAM."
            ));
            
            recommendations.Add(new TweakRecommendation(
                "optimize-ntfs-mft", // Placeholder ID
                0.85,
                "Fast NVMe storage benefits from refined NTFS MFT scaling."
            ));
        }
        else if (profile.PrimaryDiskType == StorageType.SSD)
        {
             recommendations.Add(new TweakRecommendation(
                "disable-defrag-ssd", 
                1.0,
                "SSD detected. Disabling traditional defragmentation prevents unnecessary wear."
            ));
        }

        // CPU Tweaks
        if (profile.CoreCount >= 8)
        {
            recommendations.Add(new TweakRecommendation(
                "cpu-scheduling-performance", 
                0.8,
                "High core count detected. Tuning CPU scheduling for background tasks can improve multi-tasking stability."
            ));
        }

        // Memory Tweaks
        if (profile.TotalMemoryBytes < 8589934592) // < 8GB
        {
            recommendations.Add(new TweakRecommendation(
                "compact-os-enable", 
                0.95,
                "Low memory detected. Compacting the OS can free up significant system resources."
            ));
        }
        else if (profile.TotalMemoryBytes >= 17179869184) // >= 16GB
        {
            recommendations.Add(new TweakRecommendation(
                "increase-system-cache", 
                0.75,
                "Ample memory detected. Increasing system cache can speed up large file operations."
            ));
        }

        return recommendations;
    }
}
