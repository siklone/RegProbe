using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using RegProbe.Application.Services.TweakProviders;

namespace RegProbe.CLI;

partial class Program
{
    static Command CreateResearchValidateJsonTweaksCommand()
    {
        var command = new Command("validate-json-tweaks", "Validate JSON tweak definitions and emit an invalid-definition report.");
        var inputDirectoryOption = CreateRequiredOption<string>("--input-dir", "Directory containing JSON tweak definitions.");
        var reportOutputOption = CreateOption<string?>("--output", "Optional JSON report output path.");
        command.AddOption(inputDirectoryOption);
        command.AddOption(reportOutputOption);
        command.SetHandler(context =>
        {
            var inputDirectory = NormalizeCliText(context.ParseResult.GetValueForOption(inputDirectoryOption));
            var reportOutput = NormalizeCliText(context.ParseResult.GetValueForOption(reportOutputOption));
            reportOutput = string.IsNullOrWhiteSpace(reportOutput) ? null : reportOutput;
            var inputDirectoryValidationError = ValidateRequiredCliText(inputDirectory, "input-dir");
            if (!string.IsNullOrWhiteSpace(inputDirectoryValidationError))
            {
                Console.Error.WriteLine(inputDirectoryValidationError);
                context.ExitCode = 1;
                return;
            }

            var outputValidationError = reportOutput is null
                ? null
                : ValidateOutputFilePath(reportOutput, "output");
            if (!string.IsNullOrWhiteSpace(outputValidationError))
            {
                Console.Error.WriteLine(outputValidationError);
                context.ExitCode = 1;
                return;
            }

            try
            {
                var fullInputDirectory = Path.GetFullPath(inputDirectory);
                if (!Directory.Exists(fullInputDirectory))
                {
                    throw new DirectoryNotFoundException($"JSON tweak definition directory was not found: {fullInputDirectory}");
                }

                using var loader = new JsonTweakLoader(fullInputDirectory);
                var report = BuildJsonTweakValidationReport(fullInputDirectory, loader);
                var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;

                if (!string.IsNullOrWhiteSpace(reportOutput))
                {
                    var outputPath = Path.GetFullPath(reportOutput);
                    var outputDirectory = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrWhiteSpace(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }

                    File.WriteAllText(outputPath, json);
                    Console.WriteLine(outputPath);
                }
                else
                {
                    Console.Write(json);
                }

                context.ExitCode = loader.ValidationIssues.Count == 0 ? 0 : 2;
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
