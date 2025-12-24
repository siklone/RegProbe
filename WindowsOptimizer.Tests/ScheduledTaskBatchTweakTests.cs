using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WindowsOptimizer.Core;
using WindowsOptimizer.Engine.Tweaks;
using WindowsOptimizer.Core.Tasks;
using Xunit;

public sealed class ScheduledTaskBatchTweakTests
{
    [Fact]
    public async Task ApplyRollback_DisablesAndRestoresTasks()
    {
        var manager = new FakeTaskManager();
        manager.AddTask("\\Test\\TaskA", true);
        manager.AddTask("\\Test\\TaskB", false);

        var tweak = new ScheduledTaskBatchTweak(
            "test.tasks",
            "Test scheduled tasks",
            "Disables scheduled tasks in bulk.",
            TweakRiskLevel.Advanced,
            new[] { "\\Test\\TaskA", "\\Test\\TaskB" },
            manager,
            requiresElevation: true);

        var detect = await tweak.DetectAsync(CancellationToken.None);
        Assert.Equal(TweakStatus.Detected, detect.Status);

        var apply = await tweak.ApplyAsync(CancellationToken.None);
        Assert.Equal(TweakStatus.Applied, apply.Status);

        var verify = await tweak.VerifyAsync(CancellationToken.None);
        Assert.Equal(TweakStatus.Verified, verify.Status);

        var rollback = await tweak.RollbackAsync(CancellationToken.None);
        Assert.Equal(TweakStatus.RolledBack, rollback.Status);

        Assert.True((await manager.QueryAsync("\\Test\\TaskA", CancellationToken.None)).Enabled);
        Assert.False((await manager.QueryAsync("\\Test\\TaskB", CancellationToken.None)).Enabled);
    }

    private sealed class FakeTaskManager : IScheduledTaskManager
    {
        private readonly Dictionary<string, ScheduledTaskInfo> _tasks = new(StringComparer.OrdinalIgnoreCase);

        public void AddTask(string path, bool enabled)
        {
            _tasks[path] = new ScheduledTaskInfo(true, enabled);
        }

        public Task<ScheduledTaskInfo> QueryAsync(string taskPath, CancellationToken ct)
        {
            if (_tasks.TryGetValue(taskPath, out var info))
            {
                return Task.FromResult(info);
            }

            return Task.FromResult(new ScheduledTaskInfo(false, false));
        }

        public Task SetEnabledAsync(string taskPath, bool enabled, CancellationToken ct)
        {
            if (_tasks.TryGetValue(taskPath, out var info))
            {
                _tasks[taskPath] = info with { Enabled = enabled };
            }

            return Task.CompletedTask;
        }
    }
}
