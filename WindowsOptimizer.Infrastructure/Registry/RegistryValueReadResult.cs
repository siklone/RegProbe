namespace WindowsOptimizer.Infrastructure.Registry;

public sealed record RegistryValueReadResult(bool Exists, RegistryValueData? Value);
