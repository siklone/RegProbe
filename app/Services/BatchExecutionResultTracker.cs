using RegProbe.Core;
using RegProbe.Engine;

namespace RegProbe.App.Services;

internal sealed class BatchExecutionResultTracker
{
    private readonly object _gate = new();
    private readonly Dictionary<string, TweakStatus> _results = new();
    private readonly Dictionary<string, string> _errors = new();

    private int _completed;
    private int _successful;
    private int _failed;
    private int _skipped;

    public (int Completed, TweakStatus Status) Record(ITweak tweak, TweakExecutionReport result)
    {
        lock (_gate)
        {
            var status = GetStatus(result);
            _results[tweak.Id] = status;

            if (result.Applied || result.Verified || result.RolledBack)
            {
                _successful++;
            }
            else if (!result.Succeeded)
            {
                _failed++;
                var failedStep = result.Steps.FirstOrDefault(s => s.Result.Status == TweakStatus.Failed);
                if (failedStep != null)
                {
                    _errors[tweak.Id] = failedStep.Result.Message ?? "Unknown error";
                }
            }
            else
            {
                _skipped++;
            }

            _completed++;
            return (_completed, status);
        }
    }

    public BatchExecutionResult ToResult(int totalTweaks, TimeSpan duration)
    {
        lock (_gate)
        {
            return new BatchExecutionResult
            {
                TotalTweaks = totalTweaks,
                Successful = _successful,
                Failed = _failed,
                Skipped = _skipped,
                Duration = duration,
                Results = new Dictionary<string, TweakStatus>(_results),
                Errors = new Dictionary<string, string>(_errors)
            };
        }
    }

    private static TweakStatus GetStatus(TweakExecutionReport result)
    {
        if (result.RolledBack) return TweakStatus.RolledBack;
        if (result.Verified) return TweakStatus.Verified;
        if (result.Applied) return TweakStatus.Applied;
        if (!result.Succeeded) return TweakStatus.Failed;
        return TweakStatus.Skipped;
    }
}
