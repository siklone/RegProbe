using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using WindowsOptimizer.Core;
using WindowsOptimizer.Core.Tasks;
using WindowsOptimizer.Engine.Tweaks;

public sealed class ScheduledTaskBatchTweakTests
{
    [Fact]
    public async Task DetectAsync_TreatsNotFoundTasksAsMissing()
    {
        var manager = new Mock<IScheduledTaskManager>();
        manager
            .Setup(m => m.QueryAsync(@"\Missing\Task", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new COMException("The system cannot find the file specified.", unchecked((int)0x80070002)));
        manager
            .Setup(m => m.QueryAsync(@"\Exists\Task", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduledTaskInfo(true, true));

        var tweak = new ScheduledTaskBatchTweak(
            "system.disable-scheduled-tasks",
            "Disable Scheduled Tasks",
            "Test",
            TweakRiskLevel.Risky,
            new[] { @"\Missing\Task", @"\Exists\Task" },
            manager.Object);

        var result = await tweak.DetectAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Detected, result.Status);
        Assert.Contains("1 missing", result.Message);
    }

    [Fact]
    public async Task DetectAsync_ReturnsFailed_WhenAllTasksError()
    {
        var manager = new Mock<IScheduledTaskManager>();
        manager
            .Setup(m => m.QueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Boom"));

        var tweak = new ScheduledTaskBatchTweak(
            "system.disable-scheduled-tasks",
            "Disable Scheduled Tasks",
            "Test",
            TweakRiskLevel.Risky,
            new[] { @"\One", @"\Two" },
            manager.Object);

        var result = await tweak.DetectAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Failed, result.Status);
        Assert.StartsWith("Detect failed:", result.Message);
    }

    [Fact]
    public async Task ApplyAsync_SkipsMissingTasks_WhenDetectNotRun()
    {
        var manager = new Mock<IScheduledTaskManager>();
        manager
            .Setup(m => m.QueryAsync(@"\Missing\Task", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new COMException("The system cannot find the file specified.", unchecked((int)0x80070002)));
        manager
            .Setup(m => m.QueryAsync(@"\Exists\Task", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduledTaskInfo(true, true));
        manager
            .Setup(m => m.SetEnabledAsync(@"\Exists\Task", false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var tweak = new ScheduledTaskBatchTweak(
            "system.disable-scheduled-tasks",
            "Disable Scheduled Tasks",
            "Test",
            TweakRiskLevel.Risky,
            new[] { @"\Missing\Task", @"\Exists\Task" },
            manager.Object);

        var result = await tweak.ApplyAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Applied, result.Status);
        manager.Verify(m => m.SetEnabledAsync(@"\Exists\Task", false, It.IsAny<CancellationToken>()), Times.Once);
        manager.Verify(m => m.SetEnabledAsync(@"\Missing\Task", false, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task VerifyAsync_IgnoresNotFoundTasks()
    {
        var manager = new Mock<IScheduledTaskManager>();
        manager
            .Setup(m => m.QueryAsync(@"\Missing\Task", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new COMException("The system cannot find the file specified.", unchecked((int)0x80070002)));
        manager
            .Setup(m => m.QueryAsync(@"\Exists\Task", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScheduledTaskInfo(true, false));

        var tweak = new ScheduledTaskBatchTweak(
            "system.disable-scheduled-tasks",
            "Disable Scheduled Tasks",
            "Test",
            TweakRiskLevel.Risky,
            new[] { @"\Missing\Task", @"\Exists\Task" },
            manager.Object);

        var result = await tweak.VerifyAsync(CancellationToken.None);

        Assert.Equal(TweakStatus.Verified, result.Status);
    }
}
