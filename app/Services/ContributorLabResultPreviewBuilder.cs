using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace RegProbe.Application.Services;

public static class ContributorLabResultPreviewBuilder
{
    public static string Build(string standardOutput)
    {
        if (string.IsNullOrWhiteSpace(standardOutput))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(standardOutput);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            AppendLine(builder, $"Contributor summary: {Text(root, "status", "unknown")}");
            AppendSingleTweakPreview(builder, root);
            AppendAppQaPreview(builder, root);
            AppendReadinessPreview(builder, root);
            AppendVmHealthPreview(builder, root);

            builder.AppendLine();
            builder.AppendLine("--- Raw JSON ---");
            return builder.ToString().TrimEnd();
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static void AppendSingleTweakPreview(StringBuilder builder, JsonElement root)
    {
        if (!root.TryGetProperty("matches", out var matches) || matches.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        AppendLine(builder, $"Matched records: {Int(root, "match_count", matches.GetArrayLength())}");
        var best = matches.EnumerateArray().FirstOrDefault();
        if (best.ValueKind != JsonValueKind.Object)
        {
            AppendLine(builder, "App-card gate: no matching research/app record.");
            AppendLine(builder, "Next action: run repo/source/evidence search first; do not mutate a VM until the key/path/value has at least a candidate record or explicit no-evidence note.");
            return;
        }

        if (best.ValueKind == JsonValueKind.Object)
        {
            var name = Text(Object(best, "catalog_entry"), "name");
            var candidateId = Text(best, "candidate_id");
            AppendLine(builder, $"Best match: {FirstNonEmpty(name, candidateId, "unknown")} ({Text(best, "promotion_state", "unknown")}, apply_allowed={BoolText(best, "apply_allowed")})");
            AppendLine(builder, $"App mapping: {Text(best, "app_mapping_status", "unknown")}; rollback previous={BoolText(best, "restore_previous_supported")}; rollback default={BoolText(best, "restore_default_supported")}");
            var pathCheck = Object(best, "registry_path_query_check");
            if (!string.IsNullOrWhiteSpace(Text(pathCheck, "query")))
            {
                AppendLine(
                    builder,
                    $"Registry path check: {(Bool(pathCheck, "matched") ? "matched" : "not found on best match")} ({Array(pathCheck, "hits").Count} hit(s))");
            }

            AppendLine(
                builder,
                $"Value story inputs: tracked targets={Array(best, "record_targets").Count}; default/profile records={Array(best, "windows_and_recommended_profiles").Count}; app writes={Array(best, "app_write_targets").Count}");

            var appWrites = Array(best, "app_write_targets")
                .Select(FormatTarget)
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Take(3)
                .ToList();
            AppendLine(
                builder,
                "App writes: " + (appWrites.Count > 0
                    ? string.Join("; ", appWrites)
                    : "none listed (not app-card-ready until the app write is explicit)"));

            var profiles = Array(best, "windows_and_recommended_profiles")
                .Select(FormatProfile)
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Take(2)
                .ToList();
            AppendLine(
                builder,
                "Default/profile story: " + (profiles.Count > 0
                    ? string.Join("; ", profiles)
                    : "none listed (default must be found or marked unknown before app-card review)"));

            var evidenceKinds = Array(best, "evidence")
                .Select(evidence => Text(evidence, "kind"))
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .GroupBy(static value => value, StringComparer.OrdinalIgnoreCase)
                .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(static group => $"{group.Key}={group.Count()}");
            AppendLine(builder, "Evidence lanes: " + FirstNonEmpty(string.Join(", ", evidenceKinds), "none listed"));
        }

        var expectedChecks = matches.EnumerateArray()
            .SelectMany(match => Array(match, "expected_value_checks"))
            .ToList();
        var expected = expectedChecks
            .GroupBy(check => Text(check, "expected_value"))
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .Select(group => $"{group.Key}: {(group.Any(check => Bool(check, "found_any")) ? "found" : "missing")}");
        AppendLine(builder, "Expected values: " + FirstNonEmpty(string.Join(", ", expected), "none requested"));
        AppendLine(builder, "App-card gate: " + BuildSingleTweakGateSummary(best, expectedChecks));
        AppendLine(builder, "Next action: " + BuildSingleTweakNextAction(best, expectedChecks));
    }

    private static void AppendAppQaPreview(StringBuilder builder, JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        AppendLine(builder, $"QA candidates: {Int(root, "qa_candidate_count", candidates.GetArrayLength())}; inspect matches={Int(root, "inspect_match_count", 0)}");
        var first = candidates.EnumerateArray().FirstOrDefault();
        if (first.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var expectations = Object(first, "card_expectations");
        AppendLine(builder, $"Card: {FirstNonEmpty(Text(expectations, "name"), Text(first, "candidate_id"), "unknown")} ({Text(expectations, "category", "unknown")})");
        AppendLine(builder, $"Apply allowed: {BoolText(first, "apply_allowed")}; rollback previous={BoolText(first, "restore_previous_supported")}; rollback default={BoolText(first, "restore_default_supported")}");
        AppendLine(builder, "Value checks: " + FirstNonEmpty(string.Join(", ", Strings(first, "value_expectations")), "none listed"));

        var evidence = Object(first, "evidence_expectations");
        AppendLine(builder, $"Evidence counts: linked={Int(evidence, "linked_evidence_count", 0)}, runtime={Int(evidence, "runtime_read_signal_count", 0)}");
        AppendLine(builder, $"QA report path: {FirstNonEmpty(Text(first, "qa_report_path"), "not listed")}");
    }

    private static void AppendReadinessPreview(StringBuilder builder, JsonElement root)
    {
        var summary = Object(root, "summary");
        if (summary.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var interesting = new[]
        {
            "kvm_app_smoke_status",
            "kvm_lane_health_status",
            "app_card_contract_status",
            "blocked_worklist_status",
            "contributor_lab_smoke_status"
        }
            .Select(key => (Key: key, Value: Text(summary, key)))
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .Select(item => $"{item.Key}={item.Value}");

        var text = string.Join(", ", interesting);
        if (!string.IsNullOrWhiteSpace(text))
        {
            AppendLine(builder, "Readiness: " + text);
        }
    }

    private static void AppendVmHealthPreview(StringBuilder builder, JsonElement root)
    {
        var checks = Object(root, "checks");
        if (checks.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var snapshot = Object(checks, "snapshot");
        var qga = Object(checks, "qga");
        AppendLine(builder, $"VM health: qga={FirstNonEmpty(Text(qga, "status"), Text(root, "status", "unknown"))}; snapshot={FirstNonEmpty(Text(snapshot, "status"), "unknown")}; snapshot_exists={BoolText(snapshot, "exists")}");
    }

    private static string BuildSingleTweakGateSummary(JsonElement match, IReadOnlyList<JsonElement> expectedChecks)
    {
        var missing = SingleTweakMissingItems(match, expectedChecks);
        return missing.Count == 0
            ? "candidate has app write, default/profile story, rollback, evidence, and requested expected values."
            : "missing " + string.Join(", ", missing);
    }

    private static string BuildSingleTweakNextAction(JsonElement match, IReadOnlyList<JsonElement> expectedChecks)
    {
        var missingExpected = MissingExpectedValues(expectedChecks);
        if (missingExpected.Count > 0)
        {
            return $"investigate expected value(s) {string.Join(", ", missingExpected)} via repo/source search before treating them as supported targets.";
        }

        if (Array(match, "app_write_targets").Count == 0)
        {
            return "find the app write target or keep this out of end-user cards; a card cannot apply what the app cannot explicitly write.";
        }

        if (Array(match, "windows_and_recommended_profiles").Count == 0)
        {
            return "find or explicitly mark the default/current value story before app-card review.";
        }

        if (!Bool(match, "restore_previous_supported") && !Bool(match, "restore_default_supported"))
        {
            return "add rollback proof before app-card review; mutation without restore/delete story stays research-only.";
        }

        if (Array(match, "evidence").Count == 0)
        {
            return "attach at least one evidence lane; use repo/source evidence first, then VM runtime proof if the value remains plausible.";
        }

        return Bool(match, "apply_allowed")
            ? "ready for app QA mapping or bounded card review; still verify current/default/target/rollback on the target machine."
            : "record exists but apply is not allowed; keep it as Contributor Lab research or resolve the promotion gate first.";
    }

    private static IReadOnlyList<string> SingleTweakMissingItems(JsonElement match, IReadOnlyList<JsonElement> expectedChecks)
    {
        var missing = new List<string>();
        if (Array(match, "app_write_targets").Count == 0)
        {
            missing.Add("app write");
        }

        var pathCheck = Object(match, "registry_path_query_check");
        if (!string.IsNullOrWhiteSpace(Text(pathCheck, "query")) && !Bool(pathCheck, "matched"))
        {
            missing.Add("registry path");
        }

        if (Array(match, "windows_and_recommended_profiles").Count == 0)
        {
            missing.Add("default/profile");
        }

        if (!Bool(match, "restore_previous_supported") && !Bool(match, "restore_default_supported"))
        {
            missing.Add("rollback");
        }

        if (Array(match, "evidence").Count == 0)
        {
            missing.Add("evidence");
        }

        var missingExpected = MissingExpectedValues(expectedChecks);
        if (missingExpected.Count > 0)
        {
            missing.Add("expected value(s) " + string.Join(", ", missingExpected));
        }

        return missing;
    }

    private static IReadOnlyList<string> MissingExpectedValues(IReadOnlyList<JsonElement> expectedChecks)
        => expectedChecks
            .Where(static check => !Bool(check, "found_any"))
            .Select(static check => Text(check, "expected_value"))
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string FormatTarget(JsonElement target)
    {
        var valueName = Text(target, "value_name");
        var value = Text(target, "value");
        if (string.IsNullOrWhiteSpace(value))
        {
            value = Text(target, "target_value");
        }

        return string.IsNullOrWhiteSpace(valueName)
            ? string.Empty
            : $"{valueName}={FirstNonEmpty(value, "unknown")}";
    }

    private static string FormatProfile(JsonElement profile)
    {
        var label = FirstNonEmpty(Text(profile, "label"), Text(profile, "profile_id"), Text(profile, "profile_type"));
        var states = Array(profile, "states")
            .Select(state =>
            {
                var target = FirstNonEmpty(Text(state, "target_id"), "target");
                var kind = FirstNonEmpty(Text(state, "state_kind"), "unknown");
                var value = Text(state, "value");
                return string.IsNullOrWhiteSpace(value)
                    ? $"{target}:{kind}"
                    : $"{target}:{kind}={value}";
            })
            .Take(3);
        var stateText = string.Join(", ", states);
        return string.IsNullOrWhiteSpace(stateText)
            ? label
            : $"{label} [{stateText}]";
    }

    private static void AppendLine(StringBuilder builder, string line)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            builder.AppendLine(line);
        }
    }

    private static JsonElement Object(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value)
            ? value
            : default;

    private static IReadOnlyList<JsonElement> Array(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object
           && element.TryGetProperty(property, out var value)
           && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().ToList()
            : System.Array.Empty<JsonElement>();

    private static IReadOnlyList<string> Strings(JsonElement element, string property)
        => Array(element, property)
            .Select(static item => item.ToString())
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .ToList();

    private static int Int(JsonElement element, string property, int fallback)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value))
        {
            return fallback;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out var number) => number,
            _ => fallback
        };
    }

    private static string Text(JsonElement element, string property, string fallback = "")
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value))
        {
            return fallback;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? fallback,
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => fallback
        };
    }

    private static bool Bool(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false
        };
    }

    private static string BoolText(JsonElement element, string property)
        => Bool(element, property).ToString().ToLowerInvariant();

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
}
