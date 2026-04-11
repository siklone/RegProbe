namespace RegProbe.Infrastructure.RegistryResearch;

public interface IRegistryTraceNormalizer
{
    string Name { get; }

    bool CanNormalize(string inputPath);

    NormalizedRegistryBundle Normalize(RegistryNormalizationRequest request);
}
