namespace RegProbe.App.Services;

internal sealed class RepositoryScanPayload
{
    public required NohutoRepositoryDefinition Definition { get; init; }
    public required NohutoTrackedRepositoryState State { get; init; }
    public IReadOnlyList<NohutoChangedFile> ChangedFiles { get; init; } = Array.Empty<NohutoChangedFile>();
}
