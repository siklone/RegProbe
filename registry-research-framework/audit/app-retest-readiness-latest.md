App retest readiness
Status: PASS
Generated UTC: 2026-05-16T11:28:55Z

Summary:
  app surface: 265 entries | app-only backlog: 0
  rollback: 261 apply-allowed | missing story: 0
  KVM: app smoke=ok | contributor lab=ok | lane health=ok
  evidence: 356 records

Checks:
  - public_repo_hygiene_pass: True
  - tweak_catalog_truth_pass: True
  - no_app_only_tweaks: True
  - surface_entries_resolve_to_gate_ids: True
  - surface_entries_resolve_to_record_ids: True
  - surface_documentation_paths_exist: True
  - surface_documentation_paths_match_record_files: True
  - record_corpus_matches_evidence_counts: True
  - evidence_audit_active_count_matches_index_summary: True
  - evidence_audit_active_count_matches_gate_states: True
  - evidence_audit_validation_proof_count_matches_index_summary: True
  - evidence_index_source_files_exist: True
  - evidence_atlas_summary_matches_json: True
  - evidence_atlas_category_counts_match_json: True
  - apply_allowed_records_have_rollback_story: True
  - kvm_app_publish_deploy_smoke_ok: True
  - kvm_contributor_lab_smoke_ok: True
  - kvm_research_lane_health_ok: True

Audit JSON: registry-research-framework/audit/app-retest-readiness-latest.json
Audit Markdown: registry-research-framework/audit/app-retest-readiness-latest.md
ne_health_ok`: `True`
