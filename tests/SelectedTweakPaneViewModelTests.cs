using System;
using System.Threading;
using System.Threading.Tasks;
using RegProbe.App.ViewModels;
using RegProbe.Core;
using RegProbe.Engine;
using RegProbe.Infrastructure;

namespace RegProbe.Tests;

public sealed class SelectedTweakPaneViewModelTests
{
    [Fact]
    public void SyncSelection_PreservesCurrentSelection_WhenStillVisible()
    {
        var first = CreateViewModel("system.first");
        var second = CreateViewModel("system.second");
        var pane = new SelectedTweakPaneViewModel(_ => { });

        pane.SyncSelection(new[] { first, second }, forceFirstVisible: true);
        pane.SelectedTweak = second;

        pane.SyncSelection(new[] { first, second });

        Assert.Same(second, pane.SelectedTweak);
    }

    [Fact]
    public async Task SelectedTweak_RunDetails_AutoExpandDrawer_WhenExecutionOutputAppears()
    {
        var selected = CreateViewModel("system.execution");
        var pane = new SelectedTweakPaneViewModel(_ => { })
        {
            SelectedTweak = selected
        };

        await selected.RunDetectAsync(CancellationToken.None);

        Assert.True(pane.IsPlanDrawerExpanded);
        Assert.True(pane.IsExecutionMode);
        Assert.Equal("Execution log", pane.DrawerTitle);
        Assert.Contains("Detect started.", pane.ExecutionLogText);
    }

    private static TweakItemViewModel CreateViewModel(string id)
    {
        var pipeline = new TweakExecutionPipeline(new RecordingLogger());
        var tweak = new PaneTestTweak(id);
        return new TweakItemViewModel(tweak, pipeline, isElevated: false);
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public void Log(LogLevel level, string message, Exception? exception = null)
        {
        }
    }

    private sealed class PaneTestTweak(string id) : ITweak
    {
        public string Id { get; } = id;
        public string Name => "Pane Test";
        public string Description => "Pane test";
        public TweakRiskLevel Risk => TweakRiskLevel.Advanced;
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
}
