namespace OpenTraceProject.Core.Registry;

public sealed record RegistryValueReadResult(bool Exists, RegistryValueData? Value);
