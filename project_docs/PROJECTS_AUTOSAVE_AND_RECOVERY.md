# Projects, autosave and recovery

**Purpose:** Canonical project-file, recovery, recent-project, relink and portable-project contracts.  
**Read when:** Implementing `REQ-026`–`REQ-028` or changing persistent editing state.  
**Owner:** Persistence maintainer.  
**Authority:** Project persistence owner.  
**Update trigger:** Schema, migration, write, recovery, relink or retention change.  
**Linked IDs:** `PH-09`, `AC-046`–`AC-051`, `VAL-033`–`VAL-035`, `RISK-022`–`RISK-024`.

## File contract

Extension: `.mediaforge`  
Encoding: UTF-8 JSON  
Top-level minimum:

```text
schemaVersion
applicationVersion
projectId
createdUtc / modifiedUtc
projectRoot (optional portable root)
queue[]
projectDefaults
outputRules
presetReferences[]
uiState (non-authoritative)
extensions (unknown-field preservation)
```

Each source reference contains a stable item ID, relative path when safely possible, absolute fallback when required, media kind, size and last-write UTC fingerprint. A later optional quick hash requires a separate performance decision.

Each queue item contains enabled/order/priority, edit plan, selected preset ID/version, typed per-job overrides and last known output state. Runtime process handles, cancellation tokens, temporary paths and secrets are never persisted.

## Save contract

- Manual Save/Save As is canonical.
- Serialise to a sibling temporary file, flush, then atomically replace/move where supported.
- Preserve the prior valid file as a bounded backup when replacement semantics require it.
- Never overwrite the manual save with autosave data.
- A failed save leaves the session dirty and reports the precise stage/path without claiming success.

## Autosave and recovery

- Autosave only when dirty and idle for a configured debounce interval.
- Write a separate recovery snapshot keyed by project/session ID.
- Snapshot retention is count- and age-bounded.
- Write before risky transitions such as closing, opening another project or starting a long batch when dirty.
- On startup, compare snapshots with manual saves and offer Recover, Discard, Inspect location or Open read-only.
- Successful manual save may prune older matching recovery snapshots only after confirmation of valid write.

## Schema compatibility

- Older supported versions migrate through explicit functions and tests.
- Newer schema opens read-only with clear version information.
- Unknown fields are retained when the current model can safely round-trip them; otherwise save is disabled and the reason shown.
- No migration silently drops queue items, edit plans, output rules or overrides.

## Relinking

Search order: project-relative candidate → recorded absolute candidate → user-selected folder/file search. Match by name plus size/mtime fingerprint; display conflicts and changed fingerprints. Never silently choose between multiple candidates. Relinking updates references only after user confirmation.

## Portable projects

A portable project root may contain the project file, optional copied sources and outputs. Relative paths must remain inside the canonical root after normalisation. Copying sources is explicit, progress-reporting and never deletes originals. Portable packaging and media duplication are optional, not required for schema v1.

## Implemented schema-v1 decisions

- Portable relative source paths are opt-in; non-portable projects persist absolute source references only.
- Portable root is `.` relative to the project file in schema v1. Traversal and reparse-point resolution are rejected.
- Recovery location is local application data, separate from canonical project files. Retention is five snapshots per project and 14 days.
- Recent-project retention is ten entries with case-insensitive path deduplication.
- Source fingerprints use size and last-write UTC with a one-second filesystem tolerance; no content hash is claimed.
- Loaded duplicate queue items are preserved. Interactive imports retain duplicate-source prevention.
- Newer schemas open read-only; schema-v1 save rejects any other schema version.
- Project files and recovery snapshots may contain sensitive local paths and are not included in support bundles by default.

## Privacy

Projects contain local paths and may reveal sensitive names. They remain local, are not attached to support bundles by default and are redacted only in exported diagnostic copies. No encryption claim is made.
