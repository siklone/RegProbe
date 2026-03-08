#!/usr/bin/env python3
from __future__ import annotations

import csv
import html
import re
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
CATALOG_CSV = REPO_ROOT / "Docs" / "tweaks" / "tweak-catalog.csv"
DETAILS_HTML = REPO_ROOT / "Docs" / "tweaks" / "tweak-details.html"
CATALOG_HTML = REPO_ROOT / "Docs" / "tweaks" / "tweak-catalog.html"
REPORT_MD = REPO_ROOT / "Docs" / "tweaks" / "tweak-docs-report.md"
REPORT_CSV = REPO_ROOT / "Docs" / "tweaks" / "tweak-docs-report.csv"
REPORT_HTML = REPO_ROOT / "Docs" / "tweaks" / "tweak-docs-report.html"
REPORT_MISSING_MD = REPO_ROOT / "Docs" / "tweaks" / "tweak-docs-missing.md"
REPORT_MISSING_CSV = REPO_ROOT / "Docs" / "tweaks" / "tweak-docs-missing.csv"
REPORT_MISSING_HTML = REPO_ROOT / "Docs" / "tweaks" / "tweak-docs-missing.html"
REPORT_MISSING_PRIORITY_MD = REPO_ROOT / "Docs" / "tweaks" / "tweak-docs-missing-priority.md"
REPORT_MISSING_PRIORITY_CSV = REPO_ROOT / "Docs" / "tweaks" / "tweak-docs-missing-priority.csv"
REPORT_MISSING_PRIORITY_HTML = REPO_ROOT / "Docs" / "tweaks" / "tweak-docs-missing-priority.html"

ANCHOR_RE = re.compile(r"id\s*=\s*\"([^\"]+)\"", re.IGNORECASE)
RISK_SCORE = {"Risky": 3, "Advanced": 2, "Safe": 1}
AREA_SCORE = {"Registry": 2, "Service": 2, "Task": 2, "Power": 2, "Security": 2, "Network": 1, "Command": 1, "Cleanup": 1, "File": 1, "Composite": 0, "Other": 0}


def load_catalog() -> list[dict[str, str]]:
    if not CATALOG_CSV.exists():
        raise FileNotFoundError(f"Missing catalog: {CATALOG_CSV}")

    entries: list[dict[str, str]] = []
    with CATALOG_CSV.open("r", encoding="utf-8", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            entries.append({
                "id": (row.get("id") or "").strip(),
                "name": (row.get("name") or "").strip(),
                "category": (row.get("category") or "").strip(),
                "area": (row.get("area") or "").strip(),
                "risk": (row.get("risk") or "").strip(),
                "source": (row.get("source") or "").strip(),
                "docs": (row.get("docs") or "").strip(),
            })
    return [entry for entry in entries if entry["id"]]


def load_anchors(path: Path) -> set[str]:
    if not path.exists():
        return set()
    try:
        text = path.read_text(encoding="utf-8")
    except Exception:
        return set()
    return {match.group(1).strip() for match in ANCHOR_RE.finditer(text) if match.group(1).strip()}


def resolve_doc_path(doc_path: str) -> Path:
    if not doc_path:
        return REPO_ROOT / "Docs" / "tweaks" / "tweaks.md"
    doc_path = doc_path.replace("\\", "/")
    if doc_path.startswith("Docs/"):
        return REPO_ROOT / doc_path
    return REPO_ROOT / doc_path


def priority_for(entry: dict[str, str]) -> tuple[int, str]:
    risk = entry.get("risk", "")
    area = entry.get("area", "")
    risk_score = RISK_SCORE.get(risk, 0)
    area_score = AREA_SCORE.get(area, 0)
    score = risk_score * 10 + area_score

    if score >= 32:
        label = "P0"
    elif score >= 22:
        label = "P1"
    elif score >= 12:
        label = "P2"
    else:
        label = "P3"

    return score, label


def build_html_report(
    title: str,
    entries: list[dict[str, str]],
    table_headers: list[str],
    table_rows: list[list[str]],
    summary_lines: list[str],
) -> str:
    def escape(value: str) -> str:
        return html.escape(str(value))

    header_cells = "".join(f"<th>{escape(h)}</th>" for h in table_headers)
    rows_html = []
    for row in table_rows:
        cells = "".join(f"<td><code>{escape(cell)}</code></td>" for cell in row)
        rows_html.append(f"        <tr>{cells}</tr>")

    summary_html = "".join(f"<li>{escape(line)}</li>" for line in summary_lines)
    quick_links = (
        "<div class=\"links\">"
        "<a href=\"tweak-details.html\">Tweak Details</a>"
        "<span>•</span>"
        "<a href=\"tweak-catalog.html\">Tweak Catalog</a>"
        "</div>"
    )

    return "\n".join([
        "<!doctype html>",
        "<html lang=\"en\">",
        "<head>",
        "  <meta charset=\"utf-8\">",
        "  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">",
        f"  <title>{escape(title)}</title>",
        "  <style>",
        "    body { font-family: Segoe UI, Arial, sans-serif; background: #f6f7fb; color: #1f2430; }",
        "    .container { max-width: 1100px; margin: 24px auto; padding: 0 16px; }",
        "    h1 { font-size: 22px; margin: 0 0 8px; }",
        "    ul { margin: 0 0 12px 18px; padding: 0; }",
        "    .links { margin-bottom: 16px; font-size: 13px; color: #54606f; }",
        "    .links a { color: #2f6f9b; text-decoration: none; }",
        "    .links a:hover { text-decoration: underline; }",
        "    table { width: 100%; border-collapse: collapse; background: #fff; border-radius: 8px; overflow: hidden; }",
        "    thead { background: #2f3a4a; color: #fff; }",
        "    th, td { padding: 8px 10px; font-size: 12px; border-bottom: 1px solid #e5e8ef; vertical-align: top; }",
        "    tbody tr:nth-child(even) { background: #f9fafc; }",
        "    code { font-family: Consolas, monospace; font-size: 11px; }",
        "  </style>",
        "</head>",
        "<body>",
        "  <div class=\"container\">",
        f"    <h1>{escape(title)}</h1>",
        f"    <ul>{summary_html}</ul>",
        f"    {quick_links}",
        "    <table>",
        "      <thead>",
        f"        <tr>{header_cells}</tr>",
        "      </thead>",
        "      <tbody>",
        *rows_html,
        "      </tbody>",
        "    </table>",
        "  </div>",
        "</body>",
        "</html>",
    ]) + "\n"


def main() -> int:
    entries = load_catalog()

    details_anchors = load_anchors(DETAILS_HTML)
    catalog_anchors = load_anchors(CATALOG_HTML)

    rows: list[dict[str, str]] = []
    missing_rows: list[dict[str, str]] = []
    missing_docs = 0
    missing_doc_anchor = 0
    missing_details_anchor = 0
    missing_catalog_anchor = 0

    doc_anchor_cache: dict[str, set[str]] = {}

    for entry in entries:
        tweak_id = entry["id"]
        doc_path = resolve_doc_path(entry["docs"])
        doc_key = str(doc_path)

        if doc_key not in doc_anchor_cache:
            doc_anchor_cache[doc_key] = load_anchors(doc_path)

        doc_exists = doc_path.exists()
        doc_has_anchor = tweak_id in doc_anchor_cache[doc_key]
        details_has_anchor = tweak_id in details_anchors
        catalog_has_anchor = tweak_id in catalog_anchors

        if not doc_exists:
            missing_docs += 1
        if doc_exists and not doc_has_anchor:
            missing_doc_anchor += 1
        if not details_has_anchor:
            missing_details_anchor += 1
        if not catalog_has_anchor:
            missing_catalog_anchor += 1

        score, label = priority_for(entry)
        row = {
            "id": tweak_id,
            "name": entry["name"],
            "docs": str(doc_path.relative_to(REPO_ROOT)),
            "docs_exists": "yes" if doc_exists else "no",
            "docs_anchor": "yes" if doc_has_anchor else "no",
            "details_anchor": "yes" if details_has_anchor else "no",
            "catalog_anchor": "yes" if catalog_has_anchor else "no",
            "source": entry["source"],
            "risk": entry["risk"],
            "area": entry["area"],
            "priority_score": str(score),
            "priority": label,
        }
        rows.append(row)

        if not doc_exists or not doc_has_anchor or not details_has_anchor or not catalog_has_anchor:
            missing_rows.append(row)

    REPORT_CSV.parent.mkdir(parents=True, exist_ok=True)
    with REPORT_CSV.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=[
            "id",
            "name",
            "docs",
            "docs_exists",
            "docs_anchor",
            "details_anchor",
            "catalog_anchor",
            "source",
        ], extrasaction="ignore")
        writer.writeheader()
        writer.writerows(rows)

    with REPORT_MISSING_CSV.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=[
            "id",
            "name",
            "docs",
            "docs_exists",
            "docs_anchor",
            "details_anchor",
            "catalog_anchor",
            "source",
            "risk",
            "area",
            "priority",
        ], extrasaction="ignore")
        writer.writeheader()
        writer.writerows(missing_rows)

    summary_lines = [
        "# Tweak Docs Report (Generated)",
        "",
        f"Total tweaks: {len(entries)}",
        f"Missing docs files: {missing_docs}",
        f"Missing docs anchors: {missing_doc_anchor}",
        f"Missing details anchors: {missing_details_anchor}",
        f"Missing catalog anchors: {missing_catalog_anchor}",
        "",
        "Quick links: [Tweak Details](tweak-details.html) | [Tweak Catalog](tweak-catalog.html)",
        "",
        "| ID | Docs | Doc Exists | Doc Anchor | Details Anchor | Catalog Anchor |",
        "| --- | --- | --- | --- | --- | --- |",
    ]

    for row in rows:
        summary_lines.append(
            f"| `{row['id']}` | `{row['docs']}` | {row['docs_exists']} | {row['docs_anchor']} | "
            f"{row['details_anchor']} | {row['catalog_anchor']} |"
        )

    REPORT_MD.write_text("\n".join(summary_lines) + "\n", encoding="utf-8")
    REPORT_HTML.write_text(
        build_html_report(
            "Tweak Docs Report",
            entries=rows,
            table_headers=["ID", "Docs", "Doc Exists", "Doc Anchor", "Details Anchor", "Catalog Anchor"],
            table_rows=[
                [row["id"], row["docs"], row["docs_exists"], row["docs_anchor"], row["details_anchor"], row["catalog_anchor"]]
                for row in rows
            ],
            summary_lines=[
                f"Total tweaks: {len(entries)}",
                f"Missing docs files: {missing_docs}",
                f"Missing docs anchors: {missing_doc_anchor}",
                f"Missing details anchors: {missing_details_anchor}",
                f"Missing catalog anchors: {missing_catalog_anchor}",
            ],
        ),
        encoding="utf-8",
    )
    missing_lines = [
        "# Tweak Docs Missing Report (Generated)",
        "",
        f"Total tweaks: {len(entries)}",
        f"Missing entries: {len(missing_rows)}",
        "",
        "Quick links: [Tweak Details](tweak-details.html) | [Tweak Catalog](tweak-catalog.html)",
        "",
        "| ID | Risk | Area | Docs | Doc Exists | Doc Anchor | Details Anchor | Catalog Anchor |",
        "| --- | --- | --- | --- | --- | --- | --- | --- |",
    ]

    for row in missing_rows:
        missing_lines.append(
            f"| `{row['id']}` | {row['risk']} | {row['area']} | `{row['docs']}` | "
            f"{row['docs_exists']} | {row['docs_anchor']} | {row['details_anchor']} | {row['catalog_anchor']} |"
        )

    REPORT_MISSING_MD.write_text("\n".join(missing_lines) + "\n", encoding="utf-8")
    REPORT_MISSING_HTML.write_text(
        build_html_report(
            "Tweak Docs Missing Report",
            entries=missing_rows,
            table_headers=["ID", "Risk", "Area", "Docs", "Doc Exists", "Doc Anchor", "Details Anchor", "Catalog Anchor"],
            table_rows=[
                [
                    row["id"],
                    row["risk"],
                    row["area"],
                    row["docs"],
                    row["docs_exists"],
                    row["docs_anchor"],
                    row["details_anchor"],
                    row["catalog_anchor"],
                ]
                for row in missing_rows
            ],
            summary_lines=[
                f"Total tweaks: {len(entries)}",
                f"Missing entries: {len(missing_rows)}",
            ],
        ),
        encoding="utf-8",
    )

    missing_sorted = sorted(
        missing_rows,
        key=lambda row: (
            int(row["priority_score"]),
            row["risk"],
            row["area"],
            row["id"],
        ),
        reverse=True,
    )

    with REPORT_MISSING_PRIORITY_CSV.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=[
            "priority",
            "id",
            "name",
            "risk",
            "area",
            "docs",
            "docs_exists",
            "docs_anchor",
            "details_anchor",
            "catalog_anchor",
            "source",
        ], extrasaction="ignore")
        writer.writeheader()
        for row in missing_sorted:
            writer.writerow({
                "priority": row["priority"],
                "id": row["id"],
                "name": row["name"],
                "risk": row["risk"],
                "area": row["area"],
                "docs": row["docs"],
                "docs_exists": row["docs_exists"],
                "docs_anchor": row["docs_anchor"],
                "details_anchor": row["details_anchor"],
                "catalog_anchor": row["catalog_anchor"],
                "source": row["source"],
            })

    priority_lines = [
        "# Tweak Docs Missing Priority Report (Generated)",
        "",
        f"Total tweaks: {len(entries)}",
        f"Missing entries: {len(missing_rows)}",
        "",
        "Quick links: [Tweak Details](tweak-details.html) | [Tweak Catalog](tweak-catalog.html)",
        "",
        "| Priority | ID | Risk | Area | Docs | Doc Exists | Doc Anchor | Details Anchor | Catalog Anchor |",
        "| --- | --- | --- | --- | --- | --- | --- | --- | --- |",
    ]

    for row in missing_sorted:
        priority_lines.append(
            f"| {row['priority']} | `{row['id']}` | {row['risk']} | {row['area']} | `{row['docs']}` | "
            f"{row['docs_exists']} | {row['docs_anchor']} | {row['details_anchor']} | {row['catalog_anchor']} |"
        )

    REPORT_MISSING_PRIORITY_MD.write_text("\n".join(priority_lines) + "\n", encoding="utf-8")
    REPORT_MISSING_PRIORITY_HTML.write_text(
        build_html_report(
            "Tweak Docs Missing Priority Report",
            entries=missing_sorted,
            table_headers=[
                "Priority",
                "ID",
                "Risk",
                "Area",
                "Docs",
                "Doc Exists",
                "Doc Anchor",
                "Details Anchor",
                "Catalog Anchor",
            ],
            table_rows=[
                [
                    row["priority"],
                    row["id"],
                    row["risk"],
                    row["area"],
                    row["docs"],
                    row["docs_exists"],
                    row["docs_anchor"],
                    row["details_anchor"],
                    row["catalog_anchor"],
                ]
                for row in missing_sorted
            ],
            summary_lines=[
                f"Total tweaks: {len(entries)}",
                f"Missing entries: {len(missing_rows)}",
            ],
        ),
        encoding="utf-8",
    )

    print(
        "Wrote "
        f"{REPORT_MD}, {REPORT_CSV}, {REPORT_HTML}, "
        f"{REPORT_MISSING_MD}, {REPORT_MISSING_CSV}, {REPORT_MISSING_HTML}, "
        f"{REPORT_MISSING_PRIORITY_MD}, {REPORT_MISSING_PRIORITY_CSV}, {REPORT_MISSING_PRIORITY_HTML}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
