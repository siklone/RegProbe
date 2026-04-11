# ETL discovery corpus gap follow-up - 2026-04-09

Dalga 1 envanterinden sonra ETL discovery tarafında yeni bir davranis degisikligi yok; durumun kendisi netlestirildi.

## Current state

- Repo icinde ham `.etl` corpus henuz yok.
- Inventory su an yalnizca placeholder `.etl.md` yuzeyleri goruyor.
- Bu nedenle `parse_etl_registry_touches.py` su anda `0` ETL-derived discovery candidate uretiyor.

## Important clarification

- Bu bir parser failure degil.
- Bu bir corpus-availability gap.
- Ilk gercek trace `.etl` artefact'i repo'ya eklendigi anda `parse_etl_registry_touches.py` config-driven parser secimiyle otomatik beslenmeye hazir.

## Follow-up

- Ilk ham ETL geldiginde beklenen akıs:
  - inventory -> `parsed`
  - `tracerpt` first-pass XML export
  - registry touch extraction
  - normalized discovery candidate output
  - `discovery-events.jsonl` append
  - `research-queue.json` refresh
