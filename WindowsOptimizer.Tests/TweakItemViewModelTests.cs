using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.App.ViewModels;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Infrastructure;

namespace WindowsOptimizer.Tests;

public sealed class TweakItemViewModelTests
{
    [Fact]
    public void Category_Uses_Known_Hyphenated_Prefix_When_Id_Has_No_Dots()
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new TestTweak("system-check-disk-health");

        var viewModel = new TweakItemViewModel(tweak, pipeline, isElevated: false);

        Assert.Equal("System", viewModel.Category);
    }

    [Fact]
    public void ChoiceTweaks_Expose_Default_Action_And_Guidance()
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new ChoiceTestTweak();

        var viewModel = new TweakItemViewModel(tweak, pipeline, isElevated: false);

        Assert.True(viewModel.HasChoiceOptions);
        Assert.True(viewModel.HasDefaultChoice);
        Assert.Equal("Privacy-friendly summary", viewModel.FriendlyDescription);
        Assert.Equal("Privacy", viewModel.SelectedChoiceOption?.Label);
        Assert.Contains("Restore Default", viewModel.RestoreDefaultButtonText);
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public void Log(LogLevel level, string message, Exception? exception = null)
        {
        }
    }

    private sealed class TestTweak : ITweak
    {
        public TestTweak(string id)
        {
            Id = id;
        }

        public string Id { get; }
        public string Name => "Test";
        public string Description => "Test";
        public TweakRiskLevel Risk => TweakRiskLevel.Safe;
        public bool RequiresElevation => false;

        public Task<TweakResult> DetectAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Detected, "Detected", DateTimeOffset.UtcNow));

        public Task<TweakResult> ApplyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Applied, "Applied", DateTimeOffset.UtcNow));

        public Task<TweakResult> VerifyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Verified, "Verified", DateTimeOffset.UtcNow));

        public Task<TweakResult> RollbackAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.RolledBack, "Rolled back", DateTimeOffset.UtcNow));
    }

    private sealed class ChoiceTestTweak : IChoiceTweak, ITweakWithGuidance
    {
        public string Id => "misc.choice-test";
        public string Name => "Choice";
        public string Description => "Choice test";
        public TweakRiskLevel Risk => TweakRiskLevel.Safe;
        public bool RequiresElevation => false;

        public IReadOnlyList<TweakChoiceDefinition> Choices { get; } =
        [
            new("default", "Default", "Default description"),
            new("privacy", "Privacy", "Privacy description")
        ];

        public string SelectedChoiceKey { get; set; } = "privacy";
        public string SelectedChoiceLabel => "Privacy";
        public string SelectedChoiceDescription => "Privacy description";
        public string? MatchedChoiceKey => "privacy";
        public string? MatchedChoiceLabel => "Privacy";
        public string? DefaultChoiceKey => "default";
        public string? DefaultChoiceLabel => "Default";

        public TweakGuidance Guidance => new()
        {
            CasualSummary = "Privacy-friendly summary",
            WhenHelpful = "Helpful",
            Tradeoffs = "Tradeoffs",
            DefaultVsPrevious = "Default vs previous",
            ProfessionalNotes = "Notes"
        };

        public Task<TweakResult> DetectAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Applied, "Applied", DateTimeOffset.UtcNow));

        public Task<TweakResult> ApplyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Applied, "Applied", DateTimeOffset.UtcNow));

        public Task<TweakResult> VerifyAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.Verified, "Verified", DateTimeOffset.UtcNow));

        public Task<TweakResult> RollbackAsync(CancellationToken ct)
            => Task.FromResult(new TweakResult(TweakStatus.RolledBack, "Rolled back", DateTimeOffset.UtcNow));
    }
}
