namespace RegProbe.Engine;

public sealed class TweakExecutionOptions
{
    public bool DryRun { get; set; } = true;
    public bool VerifyAfterApply { get; set; } = true;
    public bool RollbackOnFailure { get; set; } = true;
}
