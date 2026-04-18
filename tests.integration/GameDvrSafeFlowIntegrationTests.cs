using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using RegProbe.Core;
using RegProbe.Core.Commands;
using RegProbe.Core.Files;
using RegProbe.Core.Registry;
using RegProbe.Core.Services;
using RegProbe.Core.Tasks;
using RegProbe.Engine;
using RegProbe.Infrastructure;

namespace RegProbe.Tests.Integration;

public sealed class GameDvrSafeFlowIntegrationTests
{
    private static readonly RegistryValueReference GameDvrReference = new(
        RegistryHive.LocalMachine,
        RegistryView.Default,
        @"SOFTWARE\Policies\Microsoft\Windows\GameDVR",
        "AllowGameDVR");

    [Fact]
    public async Task GameDvrPolicy_SafeFlow_DryRunApplyVerifyRollback_RestoresOriginalValue()
    {
        var registry = new InMemoryRegistryAccessor();
        registry.Seed(GameDvrReference, RegistryValueData.FromObject(RegistryValueKind.DWord, 1));

        var tweak = CreateGameDvrTweak(registry, registry);
        var logger = new RecordingLogger();
        var rollbackStore = new RecordingRollbackStateStore();
        var pipeline = new TweakExecutionPipeline(logger, rollbackStore: rollbackStore);

        var dryRunReport = await pipeline.ExecuteAsync(
            tweak,
            new TweakExecutionOptions
            {
                DryRun = true,
                VerifyAfterApply = true,
                RollbackOnFailure = true
            },
            null,
            CancellationToken.None);

        Assert.True(dryRunReport.DryRun);
        Assert.Equal(0, registry.SetValueCalls);
        Assert.Equal(1, registry.ReadValue(GameDvrReference)?.ToObject());

        var applyReport = await pipeline.ExecuteAsync(
            tweak,
            new TweakExecutionOptions
            {
                DryRun = false,
                VerifyAfterApply = true,
                RollbackOnFailure = true
            },
            null,
            CancellationToken.None);

        Assert.True(applyReport.Succeeded);
        Assert.True(applyReport.Applied);
        Assert.True(applyReport.Verified);
        Assert.Equal(0, registry.ReadValue(GameDvrReference)?.ToObject());
        Assert.Single(rollbackStore.MarkAppliedCalls);
        Assert.Contains(
            applyReport.Steps,
            step => step.Action == TweakAction.Verify && step.Result.Status == TweakStatus.Verified);

        var rollbackStep = await pipeline.ExecuteStepAsync(tweak, TweakAction.Rollback, null, CancellationToken.None);

        Assert.Equal(TweakStatus.RolledBack, rollbackStep.Result.Status);
        Assert.Equal(1, registry.ReadValue(GameDvrReference)?.ToObject());

        var restoredDetect = await pipeline.ExecuteStepAsync(tweak, TweakAction.Detect, null, CancellationToken.None);
        Assert.Equal(TweakStatus.Detected, restoredDetect.Result.Status);
        Assert.Contains("matches: 0", restoredDetect.Result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GameDvrPolicy_VerifyFailure_TriggersRollbackOnFailure_AndRestoresWriteSurface()
    {
        var readRegistry = new InMemoryRegistryAccessor();
        var writeRegistry = new InMemoryRegistryAccessor();
        readRegistry.Seed(GameDvrReference, RegistryValueData.FromObject(RegistryValueKind.DWord, 1));
        writeRegistry.Seed(GameDvrReference, RegistryValueData.FromObject(RegistryValueKind.DWord, 1));

        var tweak = CreateGameDvrTweak(readRegistry, writeRegistry);
        var logger = new RecordingLogger();
        var rollbackStore = new RecordingRollbackStateStore();
        var pipeline = new TweakExecutionPipeline(logger, rollbackStore: rollbackStore);

        var report = await pipeline.ExecuteAsync(
            tweak,
            new TweakExecutionOptions
            {
                DryRun = false,
                VerifyAfterApply = true,
                RollbackOnFailure = true
            },
            null,
            CancellationToken.None);

        Assert.False(report.Succeeded);
        Assert.Contains(
            report.Steps,
            step => step.Action == TweakAction.Verify && step.Result.Status == TweakStatus.Failed);
        Assert.Contains(
            report.Steps,
            step => step.Action == TweakAction.Rollback && step.Result.Status == TweakStatus.RolledBack);
        Assert.Equal(1, writeRegistry.ReadValue(GameDvrReference)?.ToObject());
        Assert.Single(rollbackStore.MarkRolledBackCalls);
    }

    private static ITweak CreateGameDvrTweak(IRegistryAccessor readRegistry, IRegistryAccessor writeRegistry)
    {
        var provider = new SystemRegistryTweakProvider();
        var context = new TweakContext(
            readRegistry,
            writeRegistry,
            new NoOpServiceManager(),
            new NoOpScheduledTaskManager(),
            new NoOpFileSystemAccessor(),
            new NoOpCommandRunner());

        return provider.CreateTweaks(default!, context, false)
            .Single(tweak => string.Equals(tweak.Id, "system.disable-game-recording-broadcasting", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class InMemoryRegistryAccessor : IRegistryAccessor
    {
        private readonly Dictionary<RegistryValueReference, RegistryValueData> _values = new();

        public int SetValueCalls { get; private set; }
        public int DeleteValueCalls { get; private set; }

        public void Seed(RegistryValueReference reference, RegistryValueData value)
        {
            _values[reference] = value;
        }

        public RegistryValueData? ReadValue(RegistryValueReference reference)
        {
            return _values.TryGetValue(reference, out var value) ? value : null;
        }

        public Task<RegistryValueReadResult> ReadValueAsync(RegistryValueReference reference, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(
                _values.TryGetValue(reference, out var value)
                    ? new RegistryValueReadResult(true, value)
                    : new RegistryValueReadResult(false, null));
        }

        public Task SetValueAsync(RegistryValueReference reference, RegistryValueData value, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            SetValueCalls++;
            _values[reference] = value;
            return Task.CompletedTask;
        }

        public Task DeleteValueAsync(RegistryValueReference reference, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            DeleteValueCalls++;
            _values.Remove(reference);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingLogger : IAppLogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public void Log(LogLevel level, string message, Exception? exception = null)
        {
            Entries.Add((level, message));
        }
    }

    private sealed class RecordingRollbackStateStore : IRollbackStateStore
    {
        public List<string> MarkAppliedCalls { get; } = new();
        public List<string> MarkRolledBackCalls { get; } = new();

        public Task SaveOriginalStateAsync(RollbackEntry entry, CancellationToken ct) => Task.CompletedTask;

        public Task SaveSnapshotAsync(TweakRollbackSnapshot snapshot, CancellationToken ct) => Task.CompletedTask;

        public Task MarkAppliedAsync(string tweakId, CancellationToken ct)
        {
            MarkAppliedCalls.Add(tweakId);
            return Task.CompletedTask;
        }

        public Task MarkRolledBackAsync(string tweakId, CancellationToken ct)
        {
            MarkRolledBackCalls.Add(tweakId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RollbackEntry>> GetPendingRollbacksAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<RollbackEntry>>(Array.Empty<RollbackEntry>());

        public Task<RollbackEntry?> GetOriginalStateAsync(string tweakId, CancellationToken ct)
            => Task.FromResult<RollbackEntry?>(null);

        public Task<TweakRollbackSnapshot?> GetSnapshotAsync(string tweakId, CancellationToken ct)
            => Task.FromResult<TweakRollbackSnapshot?>(null);

        public Task ClearAllAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class NoOpServiceManager : IServiceManager
    {
        public Task<ServiceInfo> QueryAsync(string serviceName, CancellationToken ct)
            => Task.FromResult(new ServiceInfo(false, ServiceStartMode.Unknown, ServiceStatus.Unknown));

        public Task SetStartModeAsync(string serviceName, ServiceStartMode startMode, CancellationToken ct) => Task.CompletedTask;

        public Task StartAsync(string serviceName, CancellationToken ct) => Task.CompletedTask;

        public Task StopAsync(string serviceName, CancellationToken ct) => Task.CompletedTask;

        public Task<IReadOnlyList<string>> ListServiceNamesAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    private sealed class NoOpScheduledTaskManager : IScheduledTaskManager
    {
        public Task<ScheduledTaskInfo> QueryAsync(string taskPath, CancellationToken ct)
            => Task.FromResult(new ScheduledTaskInfo(false, false));

        public Task SetEnabledAsync(string taskPath, bool enabled, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class NoOpFileSystemAccessor : IFileSystemAccessor
    {
        public Task<bool> FileExistsAsync(string path, CancellationToken ct) => Task.FromResult(false);

        public Task MoveFileAsync(string sourcePath, string destinationPath, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class NoOpCommandRunner : ICommandRunner
    {
        public Task<CommandResult> RunAsync(CommandRequest request, CancellationToken ct)
            => Task.FromResult(new CommandResult(0, string.Empty, string.Empty, false, TimeSpan.Zero));
    }
}
