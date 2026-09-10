# Presets and professional queue control

**Purpose:** Canonical preset, effective-option and queue-state contracts.  
**Read when:** Implementing `REQ-029`–`REQ-032`.  
**Owner:** Workflow maintainer.  
**Authority:** Preset and queue owner.  
**Update trigger:** Option field, preset schema, queue command or state transition change.  
**Status:** Implemented in source; native Windows validation pending.  
**Linked IDs:** `PH-10`, `AC-052`–`AC-059`, `VAL-036`–`VAL-038`, `RISK-025`–`RISK-027`.

## Preset model

Extension: `.mediaforge-preset`; versioned UTF-8 JSON. Fields include ID, name, group, description, schema version, media applicability, typed option values, locked field keys, created/modified source and optional compatibility notes.

Built-in presets are versioned, read-only and grouped initially as:

- Web upload
- YouTube/general streaming
- Discord/messaging
- Email attachment
- Mobile compatible
- Archival master
- High-quality image
- Animated image
- Podcast audio
- Music archive
- Social portrait/square/landscape
- Editing intermediate where the active FFmpeg build supports it

A built-in name is not a compatibility guarantee. Availability and warnings are resolved later by the capability engine.

## User operations

Save current settings as preset; duplicate; rename; move group; delete; choose default; import/export; apply to selected jobs; lock/unlock fields in a user preset. Deleting a referenced preset does not erase effective values from existing projects; the project stores enough typed state to reopen and warns that the source preset is missing.

Imported presets:

- must pass schema and range validation;
- cannot contain executable paths or raw FFmpeg fragments treated as trusted arguments;
- preserve safe unknown schema-v1 fields as extension data while rejecting executable/command/raw-argument property names;
- are copied into the user catalogue rather than executed in place.

## Effective option precedence

```text
application defaults
  → user global defaults
  → selected preset values
  → per-job overrides
  → edit-plan requirements/guards
  → capability/compatibility resolution (later)
  → immutable run snapshot
```

One `EffectiveOptionsResolver` owns this merge. UI, project save and batch execution must not reimplement it. Each field can show source: Default, Global, Preset, Job override or Edit requirement. Locked fields block job edits until the user explicitly unlocks/duplicates the preset; they do not secretly discard values.

## Queue identity and state

Each job has a stable GUID independent of source path. Proposed states:

`Pending`, `Ready`, `Running`, `PauseRequested`, `Paused`, `Completed`, `CompletedWithWarnings`, `VerificationFailed`, `Failed`, `Cancelled`, `Skipped`, `Disabled`.

Running jobs use immutable source/edit/options snapshots captured in `QueueRunItem`; live queue state receives progress and terminal results only. Reorder, bulk edit and priority changes affect pending jobs only unless a command explicitly schedules a future rerun.

## Queue commands

- Multi-select bulk preset/override edit
- Drag/button reorder and priority
- Enable/disable
- Duplicate with independent overrides
- Retry one/selected failed job
- Open source/output folder
- Copy diagnostic summary with full local paths redacted by default
- Pause after current; pause/resume pending dispatch
- Save/load `.mediaforge-queue`

Pause never suspends an FFmpeg process mid-write in the first implementation. It stops dispatching new jobs after current running work reaches a terminal state. Cancel remains separate.

## Estimates

Estimated output size and processing time are advisory, may be unavailable and include a low/unproven confidence label. The first implementation uses source-size plus format/quality/throughput heuristics; estimates cannot alter settings or block a run.
