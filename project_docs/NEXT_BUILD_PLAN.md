# Next build plan — PH-22 task-first application shell

**Target:** MediaForge 2 application shell and workflow navigation; exact package version unresolved.
**Active phase:** `PH-22`.
**Governance base:** `d3c1c0958aae71afcc5462bfa2c4e66c6fd7ab55`.
**V1 rollback:** `5acaf88e751327eac47ca673178fbbd88a8603f1`.
**Runtime status:** PH-22A task-first Home/menu/task routing and shell state source merged via PR #4 at `aedfb10117c7a95a199a9f7de4047b3567d3e26e`; PH-22 remains Active and keyboard/screen-reader/DPI interaction evidence remains Unproven.

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

Primary V2 acceptance: `AC-088`, `AC-091`, `AC-092`, `AC-096`, `AC-102`, `AC-103`, `AC-105`.

Primary V2 validation: `VAL-066`, `VAL-067`, `VAL-079`, `VAL-080`, plus Release build, XAML/name/handler checks and any directly affected existing regression tests.

A source implementation alone does not close PH-22. Evidence must demonstrate the implemented shell against the applicable acceptance/validation scope.

## Safety and non-goals

- Do not modify source media or weaken output/collision/temp/cancellation safeguards.
- Do not add cloud, telemetry, accounts, database, runtime AI, background service architecture or bundled FFmpeg.
- Do not change package/product version.
- Do not perform broad backend refactoring unrelated to the shell seam.
- Do not treat V1 visual layout as a compatibility requirement.

## Rollback

For PH-22B, rollback to merged PH-22A commit `aedfb10117c7a95a199a9f7de4047b3567d3e26e`; the phase-root rollback remains merged PH-20 governance commit `d3c1c0958aae71afcc5462bfa2c4e66c6fd7ab55`.

## Stop conditions

Stop if the shell requires duplicated processing authority, raw FFmpeg arguments in UI state, pointer-only primary navigation, inaccessible task selection, unrelated backend redesign, version/package changes, or weakening of inherited V1 safety contracts.

## Next phase

`PH-23` begins only after PH-22 exit evidence supports the shell/navigation contract or an explicit governance exception is recorded.
