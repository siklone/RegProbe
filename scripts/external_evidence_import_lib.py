from __future__ import annotations

import csv
import json
import re
from abc import ABC, abstractmethod
from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Iterable

try:
    import yaml  # type: ignore
except Exception:  # pragma: no cover - optional
    yaml = None


REPO_ROOT = Path(__file__).resolve().parents[1]


def now_utc() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def portable_path(value: Path | str) -> str:
    path = Path(value)
    try:
        resolved = path.resolve()
    except Exception:
        return path.as_posix()

    try:
        return resolved.relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return resolved.as_posix()


def slugify(value: str) -> str:
    text = re.sub(r"[^A-Za-z0-9]+", "-", value).strip("-").lower()
    return text or "imported-candidate"


def split_hive(key_path: str) -> tuple[str | None, str]:
    normalized = key_path.replace("/", "\\").strip()
    for hive in ("HKLM", "HKCU", "HKCR", "HKU", "HKCC"):
        if normalized.startswith(hive + "\\"):
            return hive, normalized[len(hive) + 1 :]
        if normalized == hive:
            return hive, ""
    return None, normalized


@dataclass(slots=True)
class ImportedCandidateObservation:
    candidate_id: str
    feature_area: str
    source_tool: str
    key_path: str
    value_name: str | None = None
    value_type: str | None = None
    observed_data: Any = None
    recommended_value: Any = None
    rollback_value: Any = None
    trigger_action: str | None = None
    required_privilege: str | None = None
    confidence: str = "Probable"
    notes: str | None = None
    evidence_refs: list[str] = field(default_factory=list)


class ExternalEvidenceImporter(ABC):
    source_tool: str
    importer_name: str

    @abstractmethod
    def can_import(self, input_path: Path, payload: Any) -> bool:
        raise NotImplementedError

    @abstractmethod
    def import_observations(self, input_path: Path, payload: Any, run_id: str) -> list[ImportedCandidateObservation]:
        raise NotImplementedError


class VelociraptorRegistryHunterImporter(ExternalEvidenceImporter):
    source_tool = "velociraptor"
    importer_name = "VelociraptorRegistryHunterImporter"

    def can_import(self, input_path: Path, payload: Any) -> bool:
        name = input_path.name.lower()
        if "velociraptor" in name or "registry-hunter" in name:
            return True
        return isinstance(payload, (list, dict)) and "velociraptor" in json.dumps(payload).lower()

    def import_observations(self, input_path: Path, payload: Any, run_id: str) -> list[ImportedCandidateObservation]:
        rows = list(_flatten_records(payload))
        observations: list[ImportedCandidateObservation] = []
        for row in rows:
            key_path = _first_value(row, "KeyPath", "Path", "FullPath", "RegistryPath", "OSPath")
            if not key_path or not _looks_like_registry_path(key_path):
                continue
            value_name = _first_value(row, "ValueName", "Name", "Value")
            candidate = build_candidate_id(key_path, value_name)
            observations.append(
                ImportedCandidateObservation(
                    candidate_id=candidate,
                    feature_area=infer_feature_area(key_path),
                    source_tool=self.source_tool,
                    key_path=normalize_key_path(key_path),
                    value_name=value_name,
                    value_type=_first_value(row, "Type", "ValueType", "DataType"),
                    observed_data=_first_value(row, "Data", "ValueData", "DataPreview"),
                    confidence="Probable",
                    notes="Imported from Velociraptor/Registry Hunter export.",
                    evidence_refs=[input_path.as_posix()],
                )
            )
        return observations


class OsqueryRegistryImporter(ExternalEvidenceImporter):
    source_tool = "osquery"
    importer_name = "OsqueryRegistryImporter"

    def can_import(self, input_path: Path, payload: Any) -> bool:
        name = input_path.name.lower()
        if "osquery" in name:
            return True
        if isinstance(payload, list) and payload:
            keys = {str(key).lower() for key in payload[0].keys()} if isinstance(payload[0], dict) else set()
            return bool({"path", "key", "name"} & keys)
        return False

    def import_observations(self, input_path: Path, payload: Any, run_id: str) -> list[ImportedCandidateObservation]:
        rows = payload if isinstance(payload, list) else []
        observations: list[ImportedCandidateObservation] = []
        for row in rows:
            if not isinstance(row, dict):
                continue
            key_path = _first_value(row, "path", "key")
            if not key_path or not _looks_like_registry_path(key_path):
                continue
            value_name = _first_value(row, "name", "value_name")
            candidate = build_candidate_id(key_path, value_name)
            observations.append(
                ImportedCandidateObservation(
                    candidate_id=candidate,
                    feature_area=infer_feature_area(key_path),
                    source_tool=self.source_tool,
                    key_path=normalize_key_path(key_path),
                    value_name=value_name,
                    value_type=_first_value(row, "type", "data_type"),
                    observed_data=_first_value(row, "data", "value", "default"),
                    confidence="Probable",
                    notes="Imported from osquery registry export.",
                    evidence_refs=[input_path.as_posix()],
                )
            )
        return observations


class RegshotImporter(ExternalEvidenceImporter):
    source_tool = "regshot"
    importer_name = "RegshotImporter"

    def can_import(self, input_path: Path, payload: Any) -> bool:
        name = input_path.name.lower()
        return "regshot" in name or input_path.suffix.lower() in {".txt", ".log"}

    def import_observations(self, input_path: Path, payload: Any, run_id: str) -> list[ImportedCandidateObservation]:
        text = payload if isinstance(payload, str) else ""
        observations: list[ImportedCandidateObservation] = []
        for line in text.splitlines():
            match = re.search(r"(HKLM|HKCU|HKCR|HKU|HKCC)\\[^\r\n=]+", line, re.IGNORECASE)
            if not match:
                continue
            key_path = normalize_key_path(match.group(0))
            value_name = None
            if "=" in line:
                maybe_name = line.split("=", 1)[0].rsplit("\\", 1)[-1].strip()
                if maybe_name and maybe_name.upper() not in {"HKLM", "HKCU", "HKCR", "HKU", "HKCC"}:
                    value_name = maybe_name
            candidate = build_candidate_id(key_path, value_name)
            observations.append(
                ImportedCandidateObservation(
                    candidate_id=candidate,
                    feature_area=infer_feature_area(key_path),
                    source_tool=self.source_tool,
                    key_path=key_path,
                    value_name=value_name,
                    observed_data=line.strip(),
                    confidence="Weak Lead",
                    notes="Imported from Regshot diff text; requires corroboration.",
                    evidence_refs=[input_path.as_posix()],
                )
            )
        return observations


IMPORTERS: list[ExternalEvidenceImporter] = [
    VelociraptorRegistryHunterImporter(),
    OsqueryRegistryImporter(),
    RegshotImporter(),
]


def load_external_payload(input_path: Path) -> Any:
    suffix = input_path.suffix.lower()
    text = input_path.read_text(encoding="utf-8-sig")
    if suffix == ".json":
        return json.loads(text)
    if suffix in {".yaml", ".yml"}:
        if yaml is None:
            raise RuntimeError("PyYAML is required to import YAML external evidence.")
        return yaml.safe_load(text)
    if suffix == ".csv":
        with input_path.open("r", encoding="utf-8-sig", newline="") as handle:
            return list(csv.DictReader(handle))
    return text


def detect_importer(input_path: Path, payload: Any, source_tool: str | None = None) -> ExternalEvidenceImporter:
    if source_tool:
        normalized = source_tool.strip().lower()
        for importer in IMPORTERS:
            if importer.source_tool == normalized:
                return importer
        raise ValueError(f"Unsupported external source tool: {source_tool}")

    for importer in IMPORTERS:
        if importer.can_import(input_path, payload):
            return importer
    raise ValueError(f"No importer matched {input_path}")


def import_external_evidence(
    input_path: Path,
    *,
    source_tool: str | None = None,
    run_id: str | None = None,
) -> dict[str, Any]:
    payload = load_external_payload(input_path)
    importer = detect_importer(input_path, payload, source_tool)
    effective_run_id = run_id or f"external-{input_path.stem}"
    observations = importer.import_observations(input_path, payload, effective_run_id)
    return {
        "$schema": "registry-research-framework/schemas/external-evidence-bundle.schema.json",
        "run_id": effective_run_id,
        "generated_utc": now_utc(),
        "source_tool": importer.source_tool,
        "importer_name": importer.importer_name,
        "input_path": input_path.as_posix(),
        "observation_count": len(observations),
        "observations": [asdict(item) for item in observations],
    }


def build_normalized_bundle_from_external_evidence(bundle: dict[str, Any]) -> dict[str, Any]:
    run_id = str(bundle.get("run_id") or "")
    generated_utc = str(bundle.get("generated_utc") or now_utc())
    input_path = str(bundle.get("input_path") or "")
    source_tool = str(bundle.get("source_tool") or "imported")
    importer_name = str(bundle.get("importer_name") or "ExternalEvidenceImporter")

    event_refs: list[str] = []
    events: list[dict[str, Any]] = []
    for observation in bundle.get("observations", []):
        if not isinstance(observation, dict):
            continue

        hive, key_path = split_hive(str(observation.get("key_path") or ""))
        evidence_refs = [
            str(ref)
            for ref in observation.get("evidence_refs", [])
            if isinstance(ref, str) and ref.strip()
        ]
        for ref in evidence_refs:
            if ref not in event_refs:
                event_refs.append(ref)

        observed_data = observation.get("observed_data")
        if observed_data is None:
            data_text = None
        elif isinstance(observed_data, str):
            data_text = observed_data
        else:
            data_text = json.dumps(observed_data, ensure_ascii=False, sort_keys=True)

        events.append(
            {
                "run_id": run_id,
                "source_tool": "imported",
                "capture_phase": "runtime",
                "process_name": None,
                "pid": None,
                "operation": "imported-observation",
                "timestamp_utc": generated_utc,
                "hive": hive,
                "key_path": key_path,
                "value_name": observation.get("value_name"),
                "value_type": observation.get("value_type"),
                "data_text": data_text,
                "result": source_tool,
                "evidence_refs": evidence_refs,
            }
        )

    if input_path and input_path not in event_refs:
        event_refs.append(input_path)

    return {
        "$schema": "registry-research-framework/schemas/normalized-registry-bundle.schema.json",
        "run_id": run_id,
        "source_tool": "imported",
        "capture_phase": "runtime",
        "generated_utc": generated_utc,
        "normalizer_name": f"{importer_name}Adapter",
        "input_path": input_path,
        "status": "ok",
        "error_kind": None,
        "errors": [],
        "event_count": len(events),
        "filtered_event_count": len(events),
        "evidence_refs": event_refs,
        "events": events,
    }


def materialize_external_research_artifacts(
    bundle: dict[str, Any],
    output_root: Path,
    *,
    bundle_root: Path | None = None,
) -> dict[str, str]:
    output_root.mkdir(parents=True, exist_ok=True)
    effective_bundle_root = bundle_root or output_root
    effective_bundle_root.mkdir(parents=True, exist_ok=True)
    note_root = output_root / "note-stubs"
    seed_root = output_root / "record-seeds"
    note_root.mkdir(parents=True, exist_ok=True)
    seed_root.mkdir(parents=True, exist_ok=True)

    bundle_path = effective_bundle_root / "external-evidence-bundle.json"
    bundle_path.write_text(json.dumps(bundle, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    normalized_bundle = build_normalized_bundle_from_external_evidence(bundle)
    normalized_bundle_path = effective_bundle_root / "normalized-registry-bundle.json"
    normalized_bundle_path.write_text(json.dumps(normalized_bundle, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    queue_path = output_root / "candidate-queue.csv"
    with queue_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=[
                "candidate_id",
                "feature_area",
                "source_tool",
                "key_path",
                "value_name",
                "confidence",
                "note_stub",
                "record_seed",
                "bundle_path",
                "normalized_bundle_path",
            ],
        )
        writer.writeheader()
        for observation in bundle.get("observations", []):
            candidate_id = str(observation["candidate_id"])
            note_path = note_root / f"{candidate_id}.md"
            seed_path = seed_root / f"{candidate_id}.json"
            note_path.write_text(build_note_stub(observation, bundle_path, normalized_bundle_path), encoding="utf-8")
            seed_path.write_text(json.dumps(build_record_seed(observation, bundle, normalized_bundle_path), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
            writer.writerow(
                {
                    "candidate_id": candidate_id,
                    "feature_area": observation.get("feature_area"),
                    "source_tool": observation.get("source_tool"),
                    "key_path": observation.get("key_path"),
                    "value_name": observation.get("value_name"),
                    "confidence": observation.get("confidence"),
                    "note_stub": portable_path(note_path),
                    "record_seed": portable_path(seed_path),
                    "bundle_path": portable_path(bundle_path),
                    "normalized_bundle_path": portable_path(normalized_bundle_path),
                }
            )

    return {
        "bundle_path": portable_path(bundle_path),
        "normalized_bundle_path": portable_path(normalized_bundle_path),
        "bundle_root": portable_path(effective_bundle_root),
        "candidate_queue": portable_path(queue_path),
        "note_root": portable_path(note_root),
        "record_seed_root": portable_path(seed_root),
        "artifact_root": portable_path(output_root),
    }


CONFIDENCE_ORDER = {
    "Weak Lead": 0,
    "Probable": 1,
    "Strongly Supported": 2,
    "Confirmed": 3,
}


def build_imported_candidate_backlog(imported_root: Path) -> dict[str, Any]:
    imported_root = imported_root.resolve()
    entries_by_candidate: dict[str, dict[str, Any]] = {}
    counts_by_source_tool: dict[str, int] = {}
    counts_by_confidence: dict[str, int] = {}
    queue_files: list[str] = []
    import_count = 0

    if imported_root.exists():
        for queue_path in sorted(imported_root.glob("*/candidate-queue.csv")):
            queue_files.append(portable_path(queue_path))
            run_id = queue_path.parent.name
            with queue_path.open("r", encoding="utf-8-sig", newline="") as handle:
                for row in csv.DictReader(handle):
                    candidate_id = str(row.get("candidate_id") or "").strip()
                    if not candidate_id:
                        continue

                    import_count += 1
                    source_tool = str(row.get("source_tool") or "").strip() or "unknown"
                    confidence = str(row.get("confidence") or "").strip() or "Weak Lead"
                    counts_by_source_tool[source_tool] = counts_by_source_tool.get(source_tool, 0) + 1
                    counts_by_confidence[confidence] = counts_by_confidence.get(confidence, 0) + 1

                    entry = entries_by_candidate.setdefault(
                        candidate_id,
                        {
                            "candidate_id": candidate_id,
                            "feature_area": str(row.get("feature_area") or "").strip() or "registry",
                            "source_tools": set(),
                            "key_paths": set(),
                            "value_names": set(),
                            "bundle_paths": set(),
                            "normalized_bundle_paths": set(),
                            "note_stub_paths": set(),
                            "record_seed_paths": set(),
                            "highest_confidence": confidence,
                            "imports": [],
                        },
                    )

                    entry["source_tools"].add(source_tool)
                    if row.get("key_path"):
                        entry["key_paths"].add(str(row["key_path"]))
                    if row.get("value_name"):
                        entry["value_names"].add(str(row["value_name"]))
                    if row.get("bundle_path"):
                        entry["bundle_paths"].add(str(row["bundle_path"]))
                    if row.get("normalized_bundle_path"):
                        entry["normalized_bundle_paths"].add(str(row["normalized_bundle_path"]))
                    if row.get("note_stub"):
                        entry["note_stub_paths"].add(str(row["note_stub"]))
                    if row.get("record_seed"):
                        entry["record_seed_paths"].add(str(row["record_seed"]))
                    if CONFIDENCE_ORDER.get(confidence, -1) > CONFIDENCE_ORDER.get(entry["highest_confidence"], -1):
                        entry["highest_confidence"] = confidence

                    entry["imports"].append(
                        {
                            "run_id": run_id,
                            "source_tool": source_tool,
                            "key_path": row.get("key_path") or None,
                            "value_name": row.get("value_name") or None,
                            "confidence": confidence,
                            "bundle_path": row.get("bundle_path") or None,
                            "normalized_bundle_path": row.get("normalized_bundle_path") or None,
                            "note_stub": row.get("note_stub") or None,
                            "record_seed": row.get("record_seed") or None,
                        }
                    )

    entries: list[dict[str, Any]] = []
    for candidate_id in sorted(entries_by_candidate.keys()):
        entry = entries_by_candidate[candidate_id]
        imports = sorted(entry["imports"], key=lambda item: (str(item.get("run_id") or ""), str(item.get("source_tool") or "")))
        entries.append(
            {
                "candidate_id": candidate_id,
                "feature_area": entry["feature_area"],
                "source_tools": sorted(entry["source_tools"]),
                "key_paths": sorted(entry["key_paths"]),
                "value_names": sorted(entry["value_names"]),
                "highest_confidence": entry["highest_confidence"],
                "bundle_paths": sorted(entry["bundle_paths"]),
                "normalized_bundle_paths": sorted(entry["normalized_bundle_paths"]),
                "note_stub_paths": sorted(entry["note_stub_paths"]),
                "record_seed_paths": sorted(entry["record_seed_paths"]),
                "import_count": len(imports),
                "imports": imports,
            }
        )

    return {
        "$schema": "registry-research-framework/schemas/imported-candidate-backlog.schema.json",
        "schema_version": "1.0",
        "generated_utc": now_utc(),
        "backlog_type": "imported-candidates",
        "source_import_root": portable_path(imported_root),
        "source_queue_files": queue_files,
        "source_run_count": len(queue_files),
        "candidate_count": len(entries),
        "import_count": import_count,
        "counts_by_source_tool": dict(sorted(counts_by_source_tool.items())),
        "counts_by_confidence": dict(sorted(counts_by_confidence.items())),
        "entries": entries,
    }


def write_imported_candidate_backlog(imported_root: Path, output_path: Path) -> Path:
    backlog = build_imported_candidate_backlog(imported_root)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(json.dumps(backlog, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    return output_path


def build_note_stub(observation: dict[str, Any], bundle_path: Path, normalized_bundle_path: Path) -> str:
    return (
        f"# Imported Candidate: {observation['candidate_id']}\n\n"
        f"- Source tool: `{observation.get('source_tool')}`\n"
        f"- Key path: `{observation.get('key_path')}`\n"
        f"- Value name: `{observation.get('value_name')}`\n"
        f"- Value type: `{observation.get('value_type')}`\n"
        f"- Confidence: `{observation.get('confidence')}`\n"
        f"- External bundle: `{portable_path(bundle_path)}`\n\n"
        f"- Normalized bundle: `{portable_path(normalized_bundle_path)}`\n\n"
        "Needs documentation-first review before any record promotion or tweak ingestion.\n"
    )


def build_record_seed(observation: dict[str, Any], bundle: dict[str, Any], normalized_bundle_path: Path) -> dict[str, Any]:
    hive, key_path = split_hive(str(observation["key_path"]))
    return {
        "tweak_id": observation["candidate_id"],
        "status": "imported-seed",
        "source_tool": observation.get("source_tool"),
        "feature_area": observation.get("feature_area"),
        "setting": {
            "targets": [
                {
                    "hive": hive,
                    "key_path": key_path,
                    "value_name": observation.get("value_name"),
                    "value_type": observation.get("value_type"),
                }
            ]
        },
        "imported_observation": observation,
        "evidence": {
            "external_bundle": bundle.get("input_path"),
            "bundle_run_id": bundle.get("run_id"),
            "normalized_bundle_path": portable_path(normalized_bundle_path),
            "evidence_refs": observation.get("evidence_refs", []),
        },
        "notes": [
            "Auto-generated external evidence seed.",
            "Imported evidence is corroboration or candidate seeding only; do not promote without repo-native proof.",
        ],
    }


def normalize_key_path(key_path: str) -> str:
    return key_path.replace("/", "\\").replace("HKEY_LOCAL_MACHINE", "HKLM").replace("HKEY_CURRENT_USER", "HKCU")


def infer_feature_area(key_path: str) -> str:
    lowered = key_path.lower()
    if "session manager" in lowered or "\\kernel" in lowered:
        return "system"
    if "\\power" in lowered or "hibernate" in lowered:
        return "power"
    if "policies" in lowered:
        return "policy"
    return "registry"


def build_candidate_id(key_path: str, value_name: str | None) -> str:
    base = normalize_key_path(key_path)
    tail = f"{base}\\{value_name}" if value_name else base
    return slugify(tail)


def _flatten_records(payload: Any) -> Iterable[dict[str, Any]]:
    if isinstance(payload, dict):
        if any(isinstance(value, (str, int, float, bool, type(None))) for value in payload.values()):
            yield payload
        for value in payload.values():
            yield from _flatten_records(value)
    elif isinstance(payload, list):
        for item in payload:
            yield from _flatten_records(item)


def _first_value(row: dict[str, Any], *names: str) -> str | None:
    for name in names:
        for key, value in row.items():
            if str(key).lower() == name.lower() and value not in (None, ""):
                return str(value)
    return None


def _looks_like_registry_path(value: str) -> bool:
    normalized = value.replace("/", "\\").strip()
    return normalized.upper().startswith(("HKLM\\", "HKCU\\", "HKCR\\", "HKU\\", "HKCC\\"))
