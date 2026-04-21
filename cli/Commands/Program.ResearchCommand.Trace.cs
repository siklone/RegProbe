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
        evidenceRefsOption.AllowMultipleArgumentsPerToken = true;
        command.AddOption(formatOption);
        command.AddOption(inputOption);
        command.AddOption(outputOption);
        command.AddOption(runIdOption);
        command.AddOption(sourceToolOption);
        command.AddOption(capturePhaseOption);
        command.AddOption(evidenceRefsOption);
        command.SetHandler(context =>
        {
            var format = NormalizeCliText(context.ParseResult.GetValueForOption(formatOption));
            var input = NormalizeCliText(context.ParseResult.GetValueForOption(inputOption));
            var output = NormalizeCliText(context.ParseResult.GetValueForOption(outputOption));
            var runId = NormalizeCliText(context.ParseResult.GetValueForOption(runIdOption));
            var sourceTool = NormalizeCliText(context.ParseResult.GetValueForOption(sourceToolOption));
            sourceTool = string.IsNullOrWhiteSpace(sourceTool) ? "imported" : sourceTool;
            var capturePhase = NormalizeCliText(context.ParseResult.GetValueForOption(capturePhaseOption));
            capturePhase = string.IsNullOrWhiteSpace(capturePhase) ? "runtime" : capturePhase;
            var evidenceRefs = (context.ParseResult.GetValueForOption(evidenceRefsOption) ?? Array.Empty<string>())
                .Select(NormalizeCliText)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var validationError = ValidateNormalizeRegistryTraceOptions(format, input, output, runId);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                Console.Error.WriteLine(validationError);
                context.ExitCode = 1;
                return;
            }

            var outputValidationError = ValidateOutputFilePath(output, "output");
            if (!string.IsNullOrWhiteSpace(outputValidationError))
            {
                Console.Error.WriteLine(outputValidationError);
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
