namespace RegProbe.Infrastructure.RegistryResearch;

public sealed record RegistryPathParts(string? Hive, string? KeyPath, string? ValueName);
