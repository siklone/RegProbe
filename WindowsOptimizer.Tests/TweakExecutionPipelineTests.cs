using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine;
using WindowsOptimizer.Infrastructure;
using Xunit;

public sealed class TweakExecutionPipelineTests
{
    [Fact]
    public async Task DryRun_SkipsApplyVerifyRollback()
    {
        var logger = new RecordingLogger();
        var pipeline = new TweakExecutionPipeline(logger);
        var tweak = new RecordingTweak();

        var report = await pipeline.ExecuteAsync(tweak);

        Assert.Equal(1, tweak.DetectCalls);
        Assert.Equal(0, tweak.ApplyCalls);
        Assert.Equal(0, tweak.VerifyCalls);
        Assert.Equal(0, tweak.RollbackCalls);
        Assert.True(report.DryRun);
        Assert.Contains(report.Steps, step => step.Action == TweakAction.Apply && step.Result.Status == TweakStatus.Skipped);
        Assert.Contains(report.Steps, step => step.Action == TweakAction.Verify && step.Result.Status == TweakStatus.Skipped);
        Assert.Contains(report.Steps, step => step.Action == TweakAction.Rollback && step.Result.Status == TweakStatus.Skipped);
    }

    [Fact]
    public async Task VerifyFailure_RollsBack()
    {
        var logger = new RecordingLogger();
        var pipeline = new TweakExecutionPipeline(logger);
        var tweak = new RecordingTweak
        {
            VerifyStatus = TweakStatus.Failed
        };

        var options = new TweakExecutionOptions
        {
            DryRun = false,
            VerifyAfterApply = true,
            RollbackOnFailure = true
        };

        var report = await pipeline.ExecuteAsync(tweak, options);

        Assert.Equal(1, tweak.ApplyCalls);
        Assert.Equal(1, tweak.VerifyCalls);
        Assert.Equal(1, tweak.RollbackCalls);
        Assert.Contains(report.Steps, step => step.Action == TweakAction.Rollback && step.Result.Status == TweakStatus.RolledBack);
    }

    [Fact]
    public async Task ExecuteStepAsync_RunsRequestedAction()
    {
        var logger = new RecordingLogger();
        var pipeline = new TweakExecutionPipeline(logger);
        var tweak = new RecordingTweak();

        var step = await pipeline.ExecuteStepAsync(tweak, TweakAction.Verify);

        Assert.Equal(1, tweak.VerifyCalls);
        Assert.Equal(TweakAction.Verify, step.Action);
        Assert.Equal(TweakStatus.Verified, step.Result.Status);
    }

    [Fact]
    public async Task CancelDuringApply_DoesNotRollback()
    {
        var logger = new RecordingLogger();
        var pipeline = new TweakExecutionPipeline(logger);
        var tweak = new CancelableTweak();
        using var cts = new CancellationTokenSource();

        var options = new TweakExecutionOptions
        {
            DryRun = false,
            VerifyAfterApply = true,
            RollbackOnFailure = true
        };

        var task = pipeline.ExecuteAsync(tweak, options, null, cts.Token);

        var startedTask = await Task.WhenAny(tweak.ApplyStarted.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        Assert.Same(tweak.ApplyStarted.Task, startedTask);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Equal(1, tweak.ApplyCalls);
        Assert.Equal(0, tweak.RollbackCalls);
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

        public void Log(LogLevel level, string message, Exception? exception = null)
        {
            Entries.Add((level, message, exception));
        }
    }

    private sealed class RecordingTweak : ITweak
    {
        public string Id { get; } = "test.tweak";
        public string Name { get; } = "Test Tweak";
        public string Description { get; } = "Test tweak for pipeline.";
        public TweakRiskLevel Risk { get; } = TweakRiskLevel.Safe;
        public bool RequiresElevation { get; } = false;

        public int DetectCalls { get; private set; }
        public int ApplyCalls { get; private set; }
        public int VerifyCalls { get; private set; }
        public int RollbackCalls { get; private set; }

        public TweakStatus DetectStatus { get; set; } = TweakStatus.Detected;
        public TweakStatus ApplyStatus { get; set; } = TweakStatus.Applied;
        public TweakStatus VerifyStatus { get; set; } = TweakStatus.Verified;
        public TweakStatus RollbackStatus { get; set; } = TweakStatus.RolledBack;

        public Task<TweakResult> DetectAsync(CancellationToken ct)
        {
            DetectCalls++;
            return Task.FromResult(Result(DetectStatus, "Detect"));
        }

        public Task<TweakResult> ApplyAsync(CancellationToken ct)
        {
            ApplyCalls++;
            return Task.FromResult(Result(ApplyStatus, "Apply"));
        }

        public Task<TweakResult> VerifyAsync(CancellationToken ct)
        {
            VerifyCalls++;
            return Task.FromResult(Result(VerifyStatus, "Verify"));
        }

        public Task<TweakResult> RollbackAsync(CancellationToken ct)
        {
            RollbackCalls++;
            return Task.FromResult(Result(RollbackStatus, "Rollback"));
        }

        private static TweakResult Result(TweakStatus status, string message)
            => new(status, message, DateTimeOffset.UtcNow);
    }

    private sealed class CancelableTweak : ITweak
    {
        public TaskCompletionSource<bool> ApplyStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string Id { get; } = "test.cancelable";
        public string Name { get; } = "Cancelable Tweak";
        public string Description { get; } = "Cancel during apply.";
        public TweakRiskLevel Risk { get; } = TweakRiskLevel.Safe;
        public bool RequiresElevation { get; } = false;

        public int DetectCalls { get; private set; }
        public int ApplyCalls { get; private set; }
        public int VerifyCalls { get; private set; }
        public int RollbackCalls { get; private set; }

        public Task<TweakResult> DetectAsync(CancellationToken ct)
        {
            DetectCalls++;
            return Task.FromResult(new TweakResult(TweakStatus.Detected, "Detect", DateTimeOffset.UtcNow));
        }

        public async Task<TweakResult> ApplyAsync(CancellationToken ct)
        {
            ApplyCalls++;
            ApplyStarted.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new TweakResult(TweakStatus.Applied, "Apply", DateTimeOffset.UtcNow);
        }

        public Task<TweakResult> VerifyAsync(CancellationToken ct)
        {
            VerifyCalls++;
            return Task.FromResult(new TweakResult(TweakStatus.Verified, "Verify", DateTimeOffset.UtcNow));
        }

        public Task<TweakResult> RollbackAsync(CancellationToken ct)
        {
            RollbackCalls++;
            return Task.FromResult(new TweakResult(TweakStatus.RolledBack, "Rollback", DateTimeOffset.UtcNow));
        }
    }
}
