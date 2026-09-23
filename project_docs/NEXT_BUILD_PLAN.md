# Next build plan — PH-23 typed WorkflowIntent and ProcessingPlan (PH-22 closure handoff)

**Target:** PH-23 typed intent/compatibility/immutable-plan seam, pending verified closure PR merge; exact package version remains unresolved.
**Active phase:** `PH-23` (effective when PH-22 closure PR is verified merged; until then, PH-22 is the last delivered phase).
**Governance base:** `d3c1c0958aae71afcc5462bfa2c4e66c6fd7ab55`.
**V1 rollback:** `5acaf88e751327eac47ca673178fbbd88a8603f1`.
**Runtime status:** PH-22A/B merged through `637fc06be7135e90be47152d6054ec783c836cf0`. PH-22C was merged through PR #6 (`5f002af3434f519aa388e3ce8149655ab9e81b53`, merge `3d885760f9e91dd84866a7d61a044e6db643ce0f`); the reviewed Release build, four-case smoke and 12/12 `VAL-082` Windows checks are retained. The 2026-09-24 merged closure-readiness audit passed within this evidence scope. `AC-096` has one retained fixture-backed projectless Home-to-Advanced conversion pass; complete V2 task-first processing remains Unproven. `DEC-039` was not adopted in the user-authorised closure decision; PH-22 exit becomes effective on verified merge of `BR-20260924-01`.

PH22-AC096-EVIDENCE-20260924: Retained user-operated disposable projectless conversion via current PH-22C executable: Convert on Home, Add Media, then configure/process via Advanced. Source SHA-256 unchanged; one 160x120 output verified by FFprobe; zero saved .mediaforge files observed in the isolated test tree. JSON: artifacts/ph22-ac096-projectless-conversion-evidence-20260924.json; SHA-256 C515C4EDF35B5C9FCDE470BB611890969DA998B9130DB1BAA51DF18301296FF2. Full V2 task-first Configure/Review/Output/Process/Result and VAL-067 remain Unproven; this is not full AC-098/AC-099 safety validation. DEC-039 was not adopted; until the closure PR is independently verified merged, PH-22 remains the last delivered phase.

## PH-23 next implementation step (effective after PH-22 closure merge)

- **Goal:** Establish one typed `WorkflowIntent` to capability/compatibility resolution to immutable `ProcessingPlan` seam, shared by summary, FFmpeg compilation and execution snapshot. Preserve the existing V1 processing and source/collision/cancellation safety contracts.
- **Read first:** `CAPABILITY_COMPATIBILITY_AND_PROCESSING_PLAN.md`, root `ARCHITECTURE.md`, `TRACEABILITY.md`, `SECURITY_PRIVACY_AND_RISK.md`; inspect actual models/services/tests before changes.
- **First bounded work:** inventory current V1 effective option/queue/conversion seams, identify reusable paths and propose the smallest typed model and targeted test slice. Do not implement the complete PH-24 Resize UX or change package/version in PH-23.
- **Acceptance:** `AC-095`, `AC-101`, applicable `AC-060`-`AC-064`; `VAL-039`-`VAL-041`, `VAL-071`, `VAL-077` with Release build and targeted service tests. The PH-22 projectless fixture does not replace these checks.
- **Gate:** Do not start dependent PH-23 edits until the PH-22 closure PR is independently confirmed merged, local `main` is safely synchronised and a new guarded `agent/ph23-*` branch has been created from that verified base.

## PH-22 historical plan (closed on merge)

## Objective

Implement the task-first WPF shell without recreating the V1 control wall or weakening inherited processing, persistence or source-safety contracts.

## Satisfied prerequisites

- `PH-20` governance adoption merged via PR #2 at `d3c1c0958aae71afcc5462bfa2c4e66c6fd7ab55`.
- `PH-21` V1 closure/rollback bridge remains satisfied.
- `PH-22A` task-first Home/menu/task routing, progressive disclosure, live-status metadata and shell-state source merged via PR #4 at `aedfb10117c7a95a199a9f7de4047b3567d3e26e`.
- canonical V2 requirements, acceptance, validation, risks and phase contracts are adopted.
- deferred V1 manual/native safety checks remain explicitly Skipped/Unproven and carry forward.

## Required reading

- `/AGENTS.md`
- `PROJECT_INDEX.md`
- `IMPLEMENTATION_PLAN.md`
- `PROJECT_FOUNDATION.md`
- `UI_UX_ACCESSIBILITY_AND_LOCALISATION.md`
- `/ARCHITECTURE.md`
- `TRACEABILITY.md`
- `SECURITY_PRIVACY_AND_RISK.md`

## PH-22 scope

Implement only the shell/navigation foundation required by the PH-22 contract:

1. conventional `File | Edit | View | Project | Tools | Help` shell;
2. Home with `Add Media` as the dominant command and primary task choices;
3. task routing for Convert, Resize, Crop & Resize, Trim / Split and Combine;
4. shared workflow frame/state lifecycle;
5. normal controls separated from collapsed Advanced controls;
6. keyboard/focus/automation-property baseline;
7. empty/loading/error/needs-decision/review-ready state presentation;
8. minimum reuse/refactor required to keep workflow authority outside ad-hoc window code-behind.

Do not implement PH-23 ProcessingPlan/capability work beyond the minimum interfaces required to avoid architectural dead ends.

## Acceptance and validation

Primary PH-22 acceptance: `AC-088`, `AC-091`, `AC-092`, `AC-096`, `AC-105`, plus source-present task guidance. Full `AC-102` guidance (including actual multi-file/batch review) remains mandatory in PH-26 under `DEC-038`.

Primary PH-22 validation: `VAL-066`, `VAL-082`, Release build, XAML/name/handler checks and directly affected regression tests. `AC-103`/`VAL-067` remain mandatory downstream in PH-24/PH-31; full `VAL-079`/`VAL-080` remain release-quality accessibility evidence in PH-31. This split is governed by `DEC-038` and does not weaken those criteria.

A source implementation alone does not close PH-22. Evidence must demonstrate the implemented shell against the applicable acceptance/validation scope.

**Closure decision:** The user determined `DEC-039` unnecessary and authorised PH-22 closure with `AC-096` unchanged, using the retained Home-to-Advanced conversion evidence. This becomes effective only after verified merge of the closure record. Full `AC-102`/`AC-103`, `VAL-067`/`VAL-079`/`VAL-080` and deferred V1 checks remain mandatory downstream.

## Safety and non-goals

- Do not modify source media or weaken output/collision/temp/cancellation safeguards.
- Do not add cloud, telemetry, accounts, database, runtime AI, background service architecture or bundled FFmpeg.
- Do not change package/product version.
- Do not perform broad backend refactoring unrelated to the shell seam.
- Do not treat V1 visual layout as a compatibility requirement.

## Rollback

For PH-22C, rollback to merged PH-22B commit `637fc06be7135e90be47152d6054ec783c836cf0`; the phase-root rollback remains merged PH-20 governance commit `d3c1c0958aae71afcc5462bfa2c4e66c6fd7ab55`.

## Stop conditions

Stop if the shell requires duplicated processing authority, raw FFmpeg arguments in UI state, pointer-only primary navigation, inaccessible task selection, unrelated backend redesign, version/package changes, or weakening of inherited V1 safety contracts.

## Next phase

`PH-23` begins only after PH-22 exit evidence supports the shell/navigation contract or an explicit governance exception is recorded.
