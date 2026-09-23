# Project index

**Purpose:** Compact manifest, routing, ownership, supersession, active phase and delivery ledger.
**Read when:** Always after `/AGENTS.md`.
**Owner:** Project maintainer.
**Authority:** Canonical routing/index owner; it does not duplicate full records.
**Update trigger:** File ownership, route, ID status, phase pointer, supersession or delivery change.
**Linked IDs:** `PH-07`–`PH-31`, `ROUTE-001`–`ROUTE-015`, reports through `BR-20260919-02`.

## Active pointer

- **Current project state:** PH-22A/B and PH-22C are merged; PH-22C merge `3d885760f9e91dd84866a7d61a044e6db643ce0f` is the rollback base. The retained shell evidence and fixture-backed projectless Home-to-Advanced conversion support the unchanged PH-22 acceptance scope. User authorised PH-22 closure; `DEC-039` was not adopted. `BR-20260924-01` makes PH-22 exit effective only when its closure PR is verified merged. Downstream V2 task-first and deferred safety/accessibility evidence remain mandatory.
- **Active phase:** `PH-23` WorkflowIntent, compatibility and ProcessingPlan seam (effective on verified closure PR merge; prior to merge, PH-22 remains the last delivered phase).
- **Active pointer:** After verified merge of PH-22 closure `BR-20260924-01`, begin PH-23 from its merged commit using `IMPLEMENTATION_PLAN.md` and `NEXT_BUILD_PLAN.md`. Keep full `AC-102`, `AC-103`, `VAL-067`, `VAL-079`, `VAL-080` and inherited V1 safety/persistence work downstream under existing governance.
- **Active product milestone:** MediaForge 2 task-first rebuild; exact 2.x package/version remains unresolved.
- **Stop rule:** do not claim PH-22 exit from source presence alone; inherited safety contracts remain mandatory; deferred V1 validation remains Unproven until executed; migration/version claims require evidence.
## Reading routes

| ID | Task category | Required governance |
|---|---|---|
| ROUTE-001 | Current build and safety baseline | `NEXT_BUILD_PLAN.md`, `VALIDATION_AND_EVIDENCE.md`, `/VERIFICATION.md`, `REPOSITORY_AND_VERSIONING.md` |
| ROUTE-002 | Projects, autosave and recovery | `PROJECTS_AUTOSAVE_AND_RECOVERY.md`, `PROJECT_FOUNDATION.md`, `SECURITY_PRIVACY_AND_RISK.md` |
| ROUTE-003 | Presets and queue control | `PRESETS_AND_QUEUE_CONTROL.md`, `PROJECT_FOUNDATION.md`, `TRACEABILITY.md` |
| ROUTE-004 | Capability and compatibility | `CAPABILITY_COMPATIBILITY_AND_PROCESSING_PLAN.md`, `/ARCHITECTURE.md`, `TEST_FIXTURE_MATRIX.md` |
| ROUTE-005 | Preview and editing | `PREVIEW_AND_EDITOR.md`, `CAPABILITY_COMPATIBILITY_AND_PROCESSING_PLAN.md`, `TRACEABILITY.md` |
| ROUTE-006 | Hardware, quality and sample encoding | `QUALITY_AUDIO_IMAGE_AND_FILTERS.md`, `CAPABILITY_COMPATIBILITY_AND_PROCESSING_PLAN.md` |
| ROUTE-007 | Streams, subtitles, metadata and lossless | `STREAMS_METADATA_AND_LOSSLESS.md`, `OUTPUT_VERIFICATION_AND_HISTORY.md` |
| ROUTE-008 | Output verification and history | `OUTPUT_VERIFICATION_AND_HISTORY.md`, `VALIDATION_AND_EVIDENCE.md`, `/VERIFICATION.md` |
| ROUTE-009 | Advanced audio/image/video filters | `QUALITY_AUDIO_IMAGE_AND_FILTERS.md`, `TEST_FIXTURE_MATRIX.md` |
| ROUTE-010 | Watch folders and completion actions | `AUTOMATION_AND_COMPLETION.md`, `SECURITY_PRIVACY_AND_RISK.md` |
| ROUTE-011 | UI, accessibility and localisation | `UI_UX_ACCESSIBILITY_AND_LOCALISATION.md`, `/ARCHITECTURE.md`, `VALIDATION_AND_EVIDENCE.md` |
| ROUTE-012 | Security, privacy and source integrity | `SECURITY_PRIVACY_AND_RISK.md`, `OUTPUT_VERIFICATION_AND_HISTORY.md` |
| ROUTE-013 | Build, version, packaging and release | `REPOSITORY_AND_VERSIONING.md`, `STRUCTURE_NAMING_AND_TOOLS.md`, `IMPLEMENTATION_PLAN.md`, `/CHANGELOG.md` |
| ROUTE-014 | Debugging and maintenance | `DEBUGGING_AND_MAINTENANCE.md`, `SOURCE_EVIDENCE.md`, `/VERIFICATION.md` |
| ROUTE-015 | Governance and scope changes | `PROJECT_INDEX.md`, `PROJECT_FOUNDATION.md`, `STRUCTURE_NAMING_AND_TOOLS.md`, `DECISIONS_AND_CHANGE_HISTORY.md`, `TRACEABILITY.md` |

## Manifest and ownership

| Path | Status | Canonical ownership |
|---|---|---|
| /AGENTS.md | Complete | Runtime routing, core rules and stop conditions |
| /PROJECT_SETTINGS.md | Complete | Coding-assistant operating constraints |
| /README.md | Complete | Product/developer entry and status |
| /APP_OVERVIEW.md | Complete | Concise product overview |
| /APP_SCOPE.md | Complete | Scope summary and milestone boundaries |
| /PRODUCT_DIRECTION.md | Complete | Priority and non-goal direction |
| /ARCHITECTURE.md | Complete | Architecture and processing contracts |
| /CHANGELOG.md | Complete | Version/milestone history |
| /VERIFICATION.md | Complete | Evidence record and report template |
| project_docs/PROJECT_INDEX.md | Complete | Manifest, routing, ownership, phase pointer and ledger |
| project_docs/PROJECT_FOUNDATION.md | Complete | Scope and REQ definitions |
| project_docs/CURRENT_STATE_AND_GAP_ANALYSIS.md | Complete | Inspected state and assessment translation |
| project_docs/NEXT_BUILD_PLAN.md | Complete | Active PH-22 implementation handoff |
| project_docs/IMPLEMENTATION_PLAN.md | Complete | Historical PH-07–PH-19 plus active V2 PH-20–PH-31 roadmap |
| project_docs/PH08_IMPLEMENTATION_REPORT.md | Complete | Full PH-08 source implementation and evidence boundary |
| project_docs/PH09_IMPLEMENTATION_REPORT.md | Complete | PH-09 persistence/recovery implementation and evidence boundary |
| project_docs/PH10_IMPLEMENTATION_REPORT.md | Complete | PH-10 preset/queue implementation and evidence boundary |
| project_docs/PH20_CANONICAL_ADOPTION_REPORT.md | Complete | PH-20 governance adoption, merge evidence, boundaries and PH-22 handoff |
| project_docs/PH22_CLOSURE_READINESS_REPORT.md | Closure record; effective on merge | PH-22 scoped acceptance, retained evidence, DEC-039 not adopted and PH-23 handoff |
| project_docs/PROJECTS_AUTOSAVE_AND_RECOVERY.md | Complete | Project/recovery contracts |
| project_docs/PRESETS_AND_QUEUE_CONTROL.md | Complete | Preset/override/queue contracts |
| project_docs/CAPABILITY_COMPATIBILITY_AND_PROCESSING_PLAN.md | Complete | FFmpeg capability and plan contracts |
| project_docs/PREVIEW_AND_EDITOR.md | Complete | Preview/editor fidelity contracts |
| project_docs/STREAMS_METADATA_AND_LOSSLESS.md | Complete | Track/metadata/lossless contracts |
| project_docs/OUTPUT_VERIFICATION_AND_HISTORY.md | Complete | Verification/history/report contracts |
| project_docs/QUALITY_AUDIO_IMAGE_AND_FILTERS.md | Complete | Quality and advanced media scope |
| project_docs/AUTOMATION_AND_COMPLETION.md | Complete | Watch-folder/completion safeguards |
| project_docs/UI_UX_ACCESSIBILITY_AND_LOCALISATION.md | Complete | Desktop UX and accessibility |
| project_docs/STRUCTURE_NAMING_AND_TOOLS.md | Complete | Repository structure, naming, tools and scaffolding |
| project_docs/SECURITY_PRIVACY_AND_RISK.md | Complete | Risk catalogue and security/privacy rules |
| project_docs/VALIDATION_AND_EVIDENCE.md | Complete | Test strategy and release evidence |
| project_docs/TEST_FIXTURE_MATRIX.md | Complete | Canonical fixture coverage |
| project_docs/TRACEABILITY.md | Complete | REQ→scope→architecture→phase→files→AC→VAL→status |
| project_docs/REPOSITORY_AND_VERSIONING.md | Complete | Git/version/release/rollback |
| project_docs/DECISIONS_AND_CHANGE_HISTORY.md | Complete | Decision and delivery records |
| project_docs/DEBUGGING_AND_MAINTENANCE.md | Complete | Diagnostics and maintenance |
| project_docs/SETUP_AND_HANDOFF.md | Complete | Pack adoption and next commands |
| project_docs/SOURCE_EVIDENCE.md | Complete | Evidence sources and limitations |

## ID ownership

| Type | Canonical owner | Current range |
|---|---|---|
| `REQ-###` | `PROJECT_FOUNDATION.md` | `REQ-001`–`REQ-070` |
| `AC-###`, `VAL-###` | `TRACEABILITY.md` | `AC-001`–`AC-105`, `VAL-001`–`VAL-082` |
| `PH-##` | `IMPLEMENTATION_PLAN.md` | historical `PH-00`–`PH-19`; `PH-20` complete; `PH-21` satisfied bridge; active V2 roadmap `PH-22`–`PH-31` |
| `RISK-###` | `SECURITY_PRIVACY_AND_RISK.md` | `RISK-001`–`RISK-060` |
| `DEC-###`, `BR-YYYYMMDD-##` | `DECISIONS_AND_CHANGE_HISTORY.md` | `DEC-001`–`DEC-039` (`DEC-039` Not adopted), reports through `BR-20260924-01` (closure PR pending) |
| `ROUTE-###` | this index | `ROUTE-001`–`ROUTE-015` |

## Supersession

- V1 historical decisions, phases and evidence remain preserved.
- The earlier `PH-11`-next / proposed 1.2.0 sequencing is superseded by the MediaForge 2 roadmap.
- `PH-11` through `PH-19` are historical IDs and are never reused; useful scope is carried into V2 phases.
- The external MediaForge 2 planning pack remains design evidence, not a second canonical governance hierarchy.
- Canonical V2 governance does not claim that V2 runtime behaviour exists.
## Delivery ledger

| Report | Date | Scope | Status | Evidence |
|---|---|---|---|---|
| `BR-20260727-01` | 2026-07-27 | Historical 1.0.2 inspection/governance baseline | Historical | Prior source pack |
| `BR-20260727-02` | 2026-07-27 | 1.1.0 crop/trim/stitch source implementation | Source implemented; runtime unproven | Prior implementation report |
| `BR-20260727-03` | 2026-07-27 | 1.1.0 compile-token correction and source hardening | Source refined; runtime unproven | Refinement report/audit |
| `BR-20260728-01` | 2026-07-28 | Desktop product scope and complete next-build governance pack | Proposed file generation | Foundation pack and `FOUNDATION_AUDIT.json` |
| `BR-20260728-02` | 2026-07-28 | Adopt governance and start PH-07 reproducible baseline automation | Implemented source tooling; Linux-scoped checks passed; Windows gate pending | Scripts, `/VERIFICATION.md`, fixture/static evidence |
| `BR-20260728-03` | 2026-07-28 | PH-08A central process seam and package-free characterisation tests | Implemented in source; 14 Linux-scoped static checks passed; native compile/test pending | Runtime service files, test project/runner, source audit |
| `BR-20260728-04` | 2026-07-28 | ProcessRunner compile correction | Source corrected; later predecessor build passed | Explicit import and static guard |
| `BR-20260728-05` | 2026-07-28 | Settings-bitrate scope correction and predecessor build | User-reported Windows build passed | User build output |
| `BR-20260728-06` | 2026-07-28 | Full PH-08 seams, .NET 10 migration, expanded safety tests and large-image preflight | Source implemented; consolidated Windows validation pending | Source, fixtures, static checks and `/VERIFICATION.md` |
| `BR-20260728-07` | 2026-07-28 | Consolidated Windows compile evidence, CA2014 correction and isolated retrying publish | Compile passed; corrected publish/runtime validation pending | User build output, source guards and `/VERIFICATION.md` |
| `BR-20260728-08` | 2026-07-28 | PH-09 schema-v1 projects, atomic save/recovery, recent/relink/portable UI and multi-file Win64 publish | Implemented in source; 25 static checks passed; native validation pending | Source, PH-09 tests, static evidence and `PH09_IMPLEMENTATION_REPORT.md` |
| `BR-20260729-11` | 2026-07-29 | PH-10 presets, typed per-job precedence, immutable run snapshots and professional queue | Implemented in source; 31 static checks passed; native validation pending | Source, PH-10 tests, static evidence and `PH10_IMPLEMENTATION_REPORT.md` |
| `BR-20260729-13` | 2026-07-29 | PH-10 launch-smoke profile isolation and post-render recovery containment | Source corrected; 34 static checks passed; corrected native smoke pending | Source, regression test, schema-2 smoke evidence contract and `/VERIFICATION.md` |
| `BR-20260729-14` | 2026-07-29 | PH-10 read-only progress binding and nullable startup-diagnostics correction | Source corrected; 36 static checks passed; corrected native smoke pending | Windows startup log, source, verifier guards and `/VERIFICATION.md` |
| `BR-20260911-01` | 2026-09-11 | Consolidated PH-07 through PH-10 Windows x64 validation and harness correction | Native automated baseline passed; manual UI/filesystem/safety evidence pending | `artifacts/ph07-baseline-20260911-132855`, source/package audits and `/VERIFICATION.md` |
| `BR-20260919-01` | 2026-09-19 | PH-10 sequencing exception and V1 rollback-baseline handoff | Automated Windows evidence retained; remaining manual/native checks Skipped/Unproven and carried to V2 post-build validation | `/VERIFICATION.md`, `PH10_IMPLEMENTATION_REPORT.md`, `TRACEABILITY.md`, reviewed external V2 validation matrix |
| `BR-20260919-02` | 2026-09-19 | MediaForge 2 canonical governance adoption | Merged via PR #2; PH-20 complete for governance scope; PH-22 active; runtime implementation Unproven | Adoption commit `c8fdc552cca699bb4eae9169bdcf8e042fc57f93`, merge `d3c1c0958aae71afcc5462bfa2c4e66c6fd7ab55`, canonical audits and `PH20_CANONICAL_ADOPTION_REPORT.md` |
| `BR-20260924-01` | 2026-09-24 | PH-22 evidence-scoped closure, DEC-039 not adopted and PH-23 handoff | User-authorised; effective when closure PR is verified merged | Retained PH-22C Windows shell/Release evidence and `PH22-AC096-EVIDENCE-20260924`; `PH22_CLOSURE_READINESS_REPORT.md` |
