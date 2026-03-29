using System;
using System.Collections.Generic;
using System.Linq;
using RegProbe.Core;

namespace RegProbe.App.ViewModels;

public sealed class WorkspaceHealthCoordinator : ViewModelBase
{
    private int _globalOptimizationScore;
    private string _healthCalculationSummary = "Health is calculated from detected states. Run Detect to refresh current states.";
    private string _healthStatusMessage = "System needs optimization";
    private int _scorableTweaksApplied;
    private int _scorableTweaksMeasuredTotal;
    private int _scorableTweaksTotal;

    public int ScorableTweaksTotal
    {
        get => _scorableTweaksTotal;
        private set => SetProperty(ref _scorableTweaksTotal, value);
    }

    public int ScorableTweaksMeasuredTotal
    {
        get => _scorableTweaksMeasuredTotal;
        private set => SetProperty(ref _scorableTweaksMeasuredTotal, value);
    }

    public int ScorableTweaksApplied
    {
        get => _scorableTweaksApplied;
        private set => SetProperty(ref _scorableTweaksApplied, value);
    }

    public int GlobalOptimizationScore
    {
        get => _globalOptimizationScore;
        private set => SetProperty(ref _globalOptimizationScore, value);
    }

    public string HealthCalculationSummary
    {
        get => _healthCalculationSummary;
        private set => SetProperty(ref _healthCalculationSummary, value);
    }

    public string HealthStatusMessage
    {
        get => _healthStatusMessage;
        private set => SetProperty(ref _healthStatusMessage, value);
    }

    public void Refresh(IEnumerable<TweakItemViewModel> tweaks)
    {
        var scorableTweaks = (tweaks ?? Enumerable.Empty<TweakItemViewModel>())
            .Where(IsScorableForHealth)
            .ToList();

        var measured = scorableTweaks.Count(t => t.AppliedStatus != TweakAppliedStatus.Unknown);
        var applied = scorableTweaks.Count(t => t.IsApplied);
        var score = measured == 0
            ? 0
            : (int)Math.Round((double)applied / measured * 100, MidpointRounding.AwayFromZero);

        ScorableTweaksTotal = scorableTweaks.Count;
        ScorableTweaksMeasuredTotal = measured;
        ScorableTweaksApplied = applied;
        GlobalOptimizationScore = score;
        HealthCalculationSummary = measured == 0
            ? "Health is calculated from detected states. Run Detect to refresh current states."
            : $"{applied} / {measured} detected settings applied (Safe+Advanced; excludes Demo/Risky).";
        HealthStatusMessage = score switch
        {
            >= 90 => "Excellent optimization level",
            >= 70 => "Good optimization level",
            >= 40 => "Moderate optimization level",
            _ => "System needs optimization"
        };
    }

    private static bool IsScorableForHealth(TweakItemViewModel tweak) =>
        !tweak.Id.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
        && tweak.Risk != TweakRiskLevel.Risky;
}
