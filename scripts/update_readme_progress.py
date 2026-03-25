#!/usr/bin/env python3
import json
import re
from collections import Counter
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
README_PATH = REPO_ROOT / "README.md"
DOCS_ROOT = REPO_ROOT / "Docs"
TWEAKS_VIEW_MODEL = REPO_ROOT / "OpenTraceProject.App" / "ViewModels" / "TweaksViewModel.cs"
PROGRESS_SVG = REPO_ROOT / "assets" / "progress.svg"
CONFIG_PATH = REPO_ROOT / "scripts" / "progress_config.json"

DEFAULT_CONFIG = {
    "manual_rows": [
        {
            "area": "UI/UX shell",
            "percent": 45,
            "notes": "MVVM shell, filters, bulk actions done; polish ongoing",
        },
        {
            "area": "Elevation",
            "percent": 70,
            "notes": "ElevatedHost + registry/services/tasks/files",
        },
        {
            "area": "Logging/export",
            "percent": 75,
            "notes": "app.log + tweak-log.csv + export",
        },
        {
            "area": "Tests",
            "percent": 25,
            "notes": "Unit tests for pipeline/tweaks/adapters",
        },
        {
            "area": "Docs/guides",
            "percent": 35,
            "notes": "Docs exist, README expanding",
        },
    ],
    "tweaks_row": {
        "area": "Tweaks coverage (docs)",
        "notes": "Top-level tweak IDs vs docs headings",
    },
    "exclude_prefixes": ["demo"],
    "prefix_map": {
        "audio": "peripheral",
        "notifications": "system",
    },
}


def load_config():
    if not CONFIG_PATH.exists():
        return DEFAULT_CONFIG
    data = json.loads(CONFIG_PATH.read_text(encoding="utf-8"))
    config = DEFAULT_CONFIG.copy()
    for key in ("manual_rows", "tweaks_row", "exclude_prefixes", "prefix_map"):
        if key in data:
            config[key] = data[key]
    return config


def count_headings(text):
    count = 0
    in_fence = False
    for line in text.splitlines():
        stripped = line.lstrip()
        if stripped.startswith("```") or stripped.startswith("~~~"):
            in_fence = not in_fence
            continue
        if in_fence:
            continue
        if line.startswith("# "):
            count += 1
    return count


def collect_doc_counts():
    counts = Counter()
    for path in DOCS_ROOT.rglob("*.md"):
        rel = path.relative_to(DOCS_ROOT)
        category = rel.parts[0]
        if category == "tweaks":
            continue
        counts[category] += count_headings(path.read_text(encoding="utf-8"))
    return counts


def read_string(source, index):
    index += 1
    out = []
    while index < len(source):
        char = source[index]
        if char == "\\":
            if index + 1 < len(source):
                out.append(source[index + 1])
                index += 2
                continue
        if char == '"':
            return "".join(out), index + 1
        out.append(char)
        index += 1
    return None, index


def extract_top_level_tweak_ids(text):
    anchor = "Tweaks = new ObservableCollection<TweakItemViewModel>"
    start = text.find(anchor)
    if start == -1:
        raise RuntimeError("Failed to locate tweak list initializer.")
    brace = text.find("{", start)
    if brace == -1:
        raise RuntimeError("Failed to locate tweak list opening brace.")

    ids = []
    idx = brace + 1
    curly_depth = 1
    in_string = False
    in_char = False
    in_line_comment = False
    in_block_comment = False

    while idx < len(text) and curly_depth > 0:
        char = text[idx]
        nxt = text[idx + 1] if idx + 1 < len(text) else ""

        if in_line_comment:
            if char == "\n":
                in_line_comment = False
            idx += 1
            continue
        if in_block_comment:
            if char == "*" and nxt == "/":
                in_block_comment = False
                idx += 2
                continue
            idx += 1
            continue
        if in_string:
            if char == "\\":
                idx += 2
                continue
            if char == '"':
                in_string = False
            idx += 1
            continue
        if in_char:
            if char == "\\":
                idx += 2
                continue
            if char == "'":
                in_char = False
            idx += 1
            continue
        if char == "/" and nxt == "/":
            in_line_comment = True
            idx += 2
            continue
        if char == "/" and nxt == "*":
            in_block_comment = True
            idx += 2
            continue
        if char == '"':
            in_string = True
            idx += 1
            continue
        if char == "'":
            in_char = True
            idx += 1
            continue
        if char == "{":
            curly_depth += 1
            idx += 1
            continue
        if char == "}":
            curly_depth -= 1
            idx += 1
            continue

        if curly_depth == 1 and text.startswith("new(", idx):
            j = idx
            while j < len(text):
                if text[j] == '"':
                    value, end = read_string(text, j)
                    if value:
                        ids.append(value)
                    idx = end
                    break
                j += 1
            else:
                idx += 1
            continue

        idx += 1

    return [value for value in ids if "." in value]


def collect_tweak_counts(config, doc_categories):
    text = TWEAKS_VIEW_MODEL.read_text(encoding="utf-8")
    ids = extract_top_level_tweak_ids(text)
    counts = Counter()
    unmapped = []

    exclude_prefixes = set(config.get("exclude_prefixes", []))
    prefix_map = config.get("prefix_map", {})

    for tweak_id in ids:
        prefix = tweak_id.split(".", 1)[0]
        if prefix in exclude_prefixes:
            continue
        category = prefix_map.get(prefix, prefix)
        if category in doc_categories:
            counts[category] += 1
        else:
            unmapped.append(tweak_id)

    return counts, unmapped


def percent(value, total, cap=100):
    if total <= 0:
        return 0
    pct = int(round((value / total) * 100))
    return max(0, min(cap, pct))


def render_summary_table(tweaks_percent, tweaks_done, tweaks_total, config):
    tweaks_row = config["tweaks_row"]
    rows = [
        {
            "area": tweaks_row["area"],
            "percent": tweaks_percent,
            "notes": tweaks_row["notes"],
            "details": f"{tweaks_percent}% ({tweaks_done}/{tweaks_total}) <progress value=\"{tweaks_percent}\" max=\"100\"></progress>",
        }
    ]
    for row in config["manual_rows"]:
        pct = int(row["percent"])
        rows.append(
            {
                "area": row["area"],
                "percent": pct,
                "notes": row["notes"],
                "details": f"{pct}% <progress value=\"{pct}\" max=\"100\"></progress>",
            }
        )

    lines = ["| Area | Progress | Notes |", "| --- | --- | --- |"]
    for row in rows:
        lines.append(f"| {row['area']} | {row['details']} | {row['notes']} |")
    return "\n".join(lines), rows


def render_tweaks_table(doc_counts, tweak_counts):
    lines = ["| Doc Area | Implemented | Total | Coverage |", "| --- | --- | --- | --- |"]
    for category in sorted(doc_counts.keys()):
        total = doc_counts[category]
        if total <= 0:
            continue
        done = tweak_counts.get(category, 0)
        coverage = percent(done, total)
        lines.append(f"| {category} | {done} | {total} | {coverage}% |")

    total_done = sum(tweak_counts.get(category, 0) for category in doc_counts.keys())
    total_docs = sum(doc_counts[category] for category in doc_counts.keys())
    total_percent = percent(total_done, total_docs)
    lines.append(f"| total | {total_done} | {total_docs} | {total_percent}% |")

    return "\n".join(lines)


def replace_block(content, start_marker, end_marker, body):
    pattern = re.compile(
        re.escape(start_marker) + r".*?" + re.escape(end_marker),
        re.DOTALL,
    )
    match = pattern.search(content)
    if not match:
        raise RuntimeError(f"Missing markers: {start_marker} ... {end_marker}")
    return pattern.sub(f"{start_marker}\n{body}\n{end_marker}", content)


def render_progress_svg(percent_value):
    bar_width = 712
    fill_width = int(round(bar_width * percent_value / 100))
    return (
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"760\" height=\"120\" "
        "viewBox=\"0 0 760 120\" role=\"img\" aria-label=\"General completion "
        f"{percent_value} percent\">\n"
        "  <defs>\n"
        "    <linearGradient id=\"bg\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"0\">\n"
        "      <stop offset=\"0%\" stop-color=\"#0b1020\" />\n"
        "      <stop offset=\"100%\" stop-color=\"#111827\" />\n"
        "    </linearGradient>\n"
        "    <linearGradient id=\"bar\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"0\">\n"
        "      <stop offset=\"0%\" stop-color=\"#22c55e\" />\n"
        "      <stop offset=\"100%\" stop-color=\"#16a34a\" />\n"
        "    </linearGradient>\n"
        "    <linearGradient id=\"shine\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"0\">\n"
        "      <stop offset=\"0%\" stop-color=\"#ffffff\" stop-opacity=\"0\" />\n"
        "      <stop offset=\"50%\" stop-color=\"#ffffff\" stop-opacity=\"0.35\" />\n"
        "      <stop offset=\"100%\" stop-color=\"#ffffff\" stop-opacity=\"0\" />\n"
        "    </linearGradient>\n"
        "    <clipPath id=\"barClip\">\n"
        f"      <rect x=\"24\" y=\"54\" width=\"{fill_width}\" height=\"20\" rx=\"10\" />\n"
        "    </clipPath>\n"
        "  </defs>\n\n"
        "  <rect x=\"0\" y=\"0\" width=\"760\" height=\"120\" rx=\"14\" fill=\"url(#bg)\" />\n\n"
        "  <text x=\"24\" y=\"32\" fill=\"#e2e8f0\" font-size=\"16\" "
        "font-family=\"Segoe UI, Arial, sans-serif\">General completion</text>\n"
        f"  <text x=\"736\" y=\"32\" fill=\"#e2e8f0\" font-size=\"16\" "
        f"font-family=\"Segoe UI, Arial, sans-serif\" text-anchor=\"end\">{percent_value}%</text>\n\n"
        "  <rect x=\"24\" y=\"54\" width=\"712\" height=\"20\" rx=\"10\" fill=\"#1f2937\" />\n"
        f"  <rect x=\"24\" y=\"54\" width=\"{fill_width}\" height=\"20\" rx=\"10\" fill=\"url(#bar)\" />\n\n"
        "  <g clip-path=\"url(#barClip)\">\n"
        "    <rect x=\"-120\" y=\"54\" width=\"120\" height=\"20\" fill=\"url(#shine)\">\n"
        "      <animate attributeName=\"x\" values=\"-120;344;-120\" dur=\"3s\" repeatCount=\"indefinite\" />\n"
        "    </rect>\n"
        "  </g>\n"
        "</svg>\n"
    )


def main():
    config = load_config()
    doc_counts = collect_doc_counts()
    tweak_counts, unmapped = collect_tweak_counts(config, doc_counts.keys())

    total_done = sum(tweak_counts.get(category, 0) for category in doc_counts.keys())
    total_docs = sum(doc_counts[category] for category in doc_counts.keys())
    tweaks_percent = percent(total_done, total_docs)

    summary_table, summary_rows = render_summary_table(
        tweaks_percent, total_done, total_docs, config
    )
    tweaks_table = render_tweaks_table(doc_counts, tweak_counts)

    readme = README_PATH.read_text(encoding="utf-8")
    readme = replace_block(
        readme,
        "<!-- progress:summary:start -->",
        "<!-- progress:summary:end -->",
        summary_table,
    )
    readme = replace_block(
        readme,
        "<!-- progress:tweaks:start -->",
        "<!-- progress:tweaks:end -->",
        tweaks_table,
    )

    if summary_rows:
        general = int(round(sum(row["percent"] for row in summary_rows) / len(summary_rows)))
    else:
        general = 0

    readme = replace_block(
        readme,
        "<!-- progress:overall:start -->",
        "<!-- progress:overall:end -->",
        f"General completion: {general}%",
    )

    README_PATH.write_text(readme, encoding="utf-8")
    PROGRESS_SVG.write_text(render_progress_svg(general), encoding="utf-8")

    if unmapped:
        print("Unmapped tweak IDs (no matching docs category):")
        for tweak_id in unmapped:
            print(f"- {tweak_id}")

    print(f"Tweaks coverage: {tweaks_percent}% ({total_done}/{total_docs})")
    print(f"General completion: {general}%")


if __name__ == "__main__":
    main()
