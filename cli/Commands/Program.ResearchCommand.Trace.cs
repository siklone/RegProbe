using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using RegProbe.Infrastructure.RegistryResearch;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateResearchNormalizeRegistryTraceCommand()
    {
        var command = new Command("normalize-registry-trace", "Normalize an ETL or Procmon CSV into a compact registry bundle.");
        var formatOption = CreateRequiredOption<string>("--format", "Normalization format: etl or procmon-csv");
        var inputOption = CreateRequiredOption<string>("--input", "Input trace path");
        var outputOption = CreateRequiredOption<string>("--output", "Output normalized bundle path");
        var runIdOption = CreateRequiredOption<string>("--run-id", "Run identifier");
        var sourceToolOption = CreateOption<string>("--source-tool", () => "imported", "Source tool tag");
        var capturePhaseOption = CreateOption<string>("--capture-phase", () => "runtime", "Capture phase tag");
        var evidenceRefsOption = CreateOption<string[]>("--evidence-ref", () => Array.Empty<string>(), "Evidence reference(s)");
        command.AddOption(formatOption);
        command.AddOption(inputOption);
        command.AddOption(outputOption);
        command.AddOption(runIdOption);
        command.AddOption(sourceToolOption);
        command.AddOption(capturePhaseOption);
        command.AddOption(evidenceRefsOption);
        command.SetHandler(context =>
        {
            var format = context.ParseResult.GetValueForOption(formatOption) ?? string.Empty;
            var input = context.ParseResult.GetValueForOption(inputOption) ?? string.Empty;
            var output = context.ParseResult.GetValueForOption(outputOption) ?? string.Empty;
            var runId = context.ParseResult.GetValueForOption(runIdOption) ?? string.Empty;
            var sourceTool = context.ParseResult.GetValueForOption(sourceToolOption) ?? "imported";
            var capturePhase = context.ParseResult.GetValueForOption(capturePhaseOption) ?? "runtime";
            var evidenceRefs = context.ParseResult.GetValueForOption(evidenceRefsOption) ?? Array.Empty<string>();
            var validationError = ValidateNormalizeRegistryTraceOptions(format, input, output, runId);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                Console.Error.WriteLine(validationError);
                context.ExitCode = 1;
                return;
            }

            try
            {
                IRegistryTraceNormalizer normalizer = format.Trim().ToLowerInvariant() switch
                {
                    "etl" => new TraceEventEtlRegistryNormalizer(),
                    "procmon-csv" => new ProcmonCsvRegistryNormalizer(),
                    _ => throw new InvalidOperationException($"Unsupported normalization format: {format}")
                };

                var bundle = normalizer.Normalize(new RegistryNormalizationRequest(
                    input,
                    runId,
                    sourceTool,
                    capturePhase,
                    evidenceRefs));

                var outputPath = Path.GetFullPath(output);
                var outputDirectory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(outputPath, JsonSerializer.Serialize(bundle, options) + Environment.NewLine);
                Console.WriteLine(outputPath);
                context.ExitCode = string.Equals(bundle.Status, "ok", StringComparison.OrdinalIgnoreCase) ? 0 : 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                context.ExitCode = 1;
            }
        });
        return command;
    }
}
