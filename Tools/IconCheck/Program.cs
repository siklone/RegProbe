using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

class Program
{
    static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var value = raw.Trim().ToLowerInvariant();
        value = value.Replace("geforce", "nvidia geforce");
        value = value.Replace("radeon tm", "radeon");
        value = value.Replace('-', ' ');
        value = Regex.Replace(value, "\\b\\(R\\)\\|\\(TM\\)\\|CPU\\|Processor\\|Graphics\\|Adapter\\|Series\\b", " ", RegexOptions.IgnoreCase);
        value = Regex.Replace(value, "[^a-z0-9+\\-\\s]", " ");
        value = Regex.Replace(value, "\\s+", " ").Trim();
        return value;
    }

    static void Main(string[] args)
    {
        var cpu = args.Length > 0 ? string.Join(' ', args) : "AMD Ryzen 7 5700X3D 8-Core Processor";
        Console.WriteLine("CPU input: " + cpu);
        var normalized = Normalize(cpu);
        Console.WriteLine("Normalized: " + normalized);

        var jsonPath = Path.Combine("OpenTraceProject.App", "Assets", "HardwareDb", "hardware_icons.json");
        if (!File.Exists(jsonPath))
        {
            Console.WriteLine("hardware_icons.json not found at " + jsonPath);
            Environment.Exit(2);
        }

        var json = File.ReadAllText(jsonPath);
        var parsed = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Rule>>>(json)
            ?? new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Rule>>();

        if (parsed.TryGetValue("cpu", out var rules))
        {
            var match = rules.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.Match) && !string.IsNullOrWhiteSpace(r.Icon) && normalized.Contains(r.Match, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                Console.WriteLine("Matched rule: match='" + match.Match + "' -> icon='" + match.Icon + "'");
                var iconFile = Path.Combine("OpenTraceProject.App", "Assets", "Icons", match.Icon + ".png");
                Console.WriteLine("Icon exists on disk: " + File.Exists(iconFile) + " (" + iconFile + ")");
            }
            else
            {
                Console.WriteLine("No matching rule found in hardware_icons.json for CPU.");
            }
        }

        // Simulate GetCpuIcon hardcoded behavior
        Console.WriteLine("\nSimulating GetCpuIcon checks:");
        if (normalized.Contains("ryzen 9", StringComparison.Ordinal)) Console.WriteLine("Would return: Assets/Icons/cpu_ryzen9.png");
        else if (normalized.Contains("ryzen 7", StringComparison.Ordinal)) Console.WriteLine("Would return: Assets/Icons/cpu_ryzen7.png");
        else if (normalized.Contains("ryzen 5", StringComparison.Ordinal)) Console.WriteLine("Would return: Assets/Icons/cpu_ryzen5.png");
        else if (normalized.Contains("core i9", StringComparison.Ordinal)) Console.WriteLine("Would return: Assets/Icons/cpu_i9.png");
        else if (normalized.Contains("core i7", StringComparison.Ordinal)) Console.WriteLine("Would return: Assets/Icons/cpu_i7.png");
        else if (normalized.Contains("core i5", StringComparison.Ordinal)) Console.WriteLine("Would return: Assets/Icons/cpu_i5.png");
        else if (normalized.Contains("intel", StringComparison.Ordinal)) Console.WriteLine("Would return: Assets/Icons/cpu_intel.png");
        else if (normalized.Contains("amd", StringComparison.Ordinal)) Console.WriteLine("Would return: Assets/Icons/cpu_amd.png");
        else Console.WriteLine("Would return: Assets/Icons/cpu_default.png");
    }

    private class Rule { public string Match { get; set; } = string.Empty; public string Icon { get; set; } = string.Empty; }
}
