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
                TweakId: "disable-paging-executive", // Placeholder ID
                ConfidenceScore: 0.9,
                Reason: "NVMe drive detected. Disabling paging executive can improve system responsiveness by keeping kernel data in RAM."
            ));
            
            recommendations.Add(new TweakRecommendation(
                TweakId: "optimize-ntfs-mft", // Placeholder ID
                ConfidenceScore: 0.85,
                Reason: "Fast NVMe storage benefits from refined NTFS MFT scaling."
            ));
        }
        else if (profile.PrimaryDiskType == StorageType.SSD)
        {
             recommendations.Add(new TweakRecommendation(
                TweakId: "disable-defrag-ssd", 
                ConfidenceScore: 1.0,
                Reason: "SSD detected. Disabling traditional defragmentation prevents unnecessary wear."
            ));
        }

        // CPU Tweaks
        if (profile.CoreCount >= 8)
        {
            recommendations.Add(new TweakRecommendation(
                TweakId: "cpu-scheduling-performance", 
                ConfidenceScore: 0.8,
                Reason: "High core count detected. Tuning CPU scheduling for background tasks can improve multi-tasking stability."
            ));
        }

        // Memory Tweaks
        if (profile.TotalMemoryBytes < 8589934592) // < 8GB
        {
            recommendations.Add(new TweakRecommendation(
                TweakId: "compact-os-enable", 
                ConfidenceScore: 0.95,
                Reason: "Low memory detected. Compacting the OS can free up significant system resources."
            ));
        }
        else if (profile.TotalMemoryBytes >= 17179869184) // >= 16GB
        {
            recommendations.Add(new TweakRecommendation(
                TweakId: "increase-system-cache", 
                ConfidenceScore: 0.75,
                Reason: "Ample memory detected. Increasing system cache can speed up large file operations."
            ));
        }

        return recommendations;
    }
}
