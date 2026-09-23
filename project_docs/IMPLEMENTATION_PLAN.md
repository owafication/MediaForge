# Complete implementation plan

## MediaForge 2 roadmap rule

`PH-11` through `PH-19` remain immutable historical phase IDs and are not reused.

`PH-21` remains the immutable V1-closure/rollback bridge from the reviewed V2 design, but the required work was completed before canonical adoption. Active execution order is therefore:

`PH-21 satisfied prerequisite -> PH-20 complete -> PH-22 active -> PH-23 -> ... -> PH-31`.

**Purpose:** Canonical phase sequence and phase contracts.
**Read when:** Starting, sequencing, reviewing or closing implementation work.
**Owner:** Technical maintainer.
**Authority:** Canonical owner for `PH-##`.
**Update trigger:** Phase scope, dependency, acceptance, validation, rollback or status change.
**Current pointer (effective on verified PH-22 closure PR merge):** `PH-22` evidence-scoped shell closure is recorded by `BR-20260924-01`, with `DEC-039` not adopted; `PH-23` is the next active implementation phase. Until that merge, PH-22 remains the last delivered phase.

## Historical phase boundary

`PH-00`–`PH-06` belong to the prior governance and 1.1.0 source implementation. This plan continues with immutable IDs `PH-07`–`PH-19`.

## `PH-07` — Native Windows baseline and safety gate

- **Status:** In progress / automated preparation implemented / blocked on native Windows execution
- **Objective:** Turn the refined 1.1.0 source into an evidenced baseline before new product work.
- **Prerequisites:** Extracted source in a local Git repository; Windows 10/11; Visual Studio or .NET SDK; FFmpeg/FFprobe; non-sensitive fixtures.
- **Required reading:** ROUTE-001, ROUTE-012, ROUTE-013.
- **Expected state:** Release build succeeds; app launches; mandatory source/output/cancellation fixtures pass; package hashes and full logs exist.
- **Tasks:** Inspect Git state; tag/branch baseline; restore/build; launch smoke; execute fixture matrix; publish x64; optionally installer; record failures before fixing; keep changes stabilisation-only.
- **Implemented preparation files:** Existing build scripts plus `scripts/verify-source.py`, `scripts/generate-test-fixtures.py`, `scripts/validate-windows-baseline.ps1`, `scripts/smoke-launch.ps1`, `scripts/verify-release-package.ps1`, `/VERIFICATION.md` and report `BR-20260728-02`.
- **Acceptance:** AC-004–AC-018, AC-021–AC-030, AC-044
- **Validation:** VAL-003–VAL-020, VAL-030–VAL-031, VAL-058–VAL-059, VAL-063, VAL-065
- **Governance update:** Update evidence, current-state status, decisions, risks and delivery ledger only from results.
- **Report:** `BR-YYYYMMDD-##` baseline report with commands, environment, outputs and hashes.
- **Rollback point:** Uploaded refined 1.1.0 ZIP SHA-256 plus local baseline tag.
- **Stop conditions:** Any compile error, source mutation, ambiguous destination, orphan process, failed mandatory fixture, missing tool provenance or unapproved installer/signing action.

## `PH-08` — Supported platform and architecture seam

- **Status:** Source implementation complete / native .NET 10 build, tests, launch and regression evidence pending
- **Objective:** Create testable state/process boundaries and decide/migrate the supported .NET baseline without changing user-visible intent.
- **Prerequisites:** PH-07 passed; characterisation tests exist; DEC-014 resolved.
- **Required reading:** ROUTE-001, ROUTE-004, ROUTE-013, `/ARCHITECTURE.md`.
- **Expected state:** Supported target framework builds; current workflows regress cleanly; process lifecycle, option resolution and session state have explicit interfaces.
- **Tasks:** Add test project; extract ProcessRunner, ProjectSession, QueueCoordinator and EffectiveOptionsResolver seams; introduce ViewModels incrementally; migrate target framework if approved; preserve settings and installer contracts. **Implemented:** all listed source tasks, `net10.0-windows` migration, expanded safety characterisation and large-image preflight. **Pending:** native clean build/test/launch, settings and fixture regression evidence.
- **Proposed files:** Project/solution, Models, Services, new ViewModels, tests, scripts and governance.
- **Acceptance:** AC-004–AC-018, AC-021, AC-026, AC-045, AC-086
- **Validation:** VAL-003–VAL-011, VAL-017–VAL-019, VAL-032, VAL-058, VAL-063, VAL-065
- **Governance update:** Record migration decision, architecture ownership, supported runtime and rollback.
- **Report:** Migration/architecture report with before/after fixture comparison.
- **Rollback point:** PH-07 validated tag and separate migration commit/branch.
- **Stop conditions:** Baseline behaviour changes without approved requirement; settings migration loses values; packaging/installer cannot be reproduced; test seams require a wholesale rewrite.

## `PH-09` — Saved projects, autosave and recovery

- **Status:** Implemented in source; native Windows validation pending
- **Objective:** Make queue and edit work durable and recoverable.
- **Prerequisites:** PH-08 passed; DEC-016/017 accepted; schema ownership assigned.
- **Required reading:** ROUTE-002, ROUTE-012, ROUTE-014.
- **Expected state:** Users can create/open/save/save-as projects; autosave and recovery work; missing sources can be relinked; forward schemas open read-only.
- **Tasks:** Define DTO/schema/migrations; ProjectService; dirty tracking; atomic manual save; separate recovery store; recent list; fingerprints; relink; portable root; recovery/startup UI; privacy/redaction. **Implemented:** schema v1, service/mapper, atomic save/backups, bounded recovery, recent list, fingerprint/relink, explicit portable paths, startup recovery UI and tests. **Pending:** native compile/tests/UI/filesystem/package evidence.
- **Proposed files:** Domain/persistence models, ProjectService, session/ViewModels, dialogs, tests and project docs.
- **Acceptance:** AC-046–AC-051, AC-074, AC-084, AC-086
- **Validation:** VAL-033–VAL-035, VAL-049, VAL-060–VAL-063, VAL-065
- **Governance update:** Freeze schema v1, recovery retention, privacy policy and migration procedure.
- **Report:** Project persistence report with corruption/recovery fixtures.
- **Rollback point:** Feature flag or revert phase commits; preserve user project files and document compatibility.
- **Stop conditions:** No atomic replacement on supported filesystem; ambiguous relink; destructive migration; project file can execute arbitrary commands; autosave can overwrite canonical manual save.

## `PH-10` — Presets, per-job options and professional queue

- **Status:** Implemented in source; native Windows build/test/UI/package evidence pending
- **Objective:** Make repeated workflows reusable and allow independent job settings within one batch.
- **Prerequisites:** PH-09 passed; effective-option hierarchy fixed by DEC-018.
- **Required reading:** ROUTE-003, ROUTE-012, ROUTE-014.
- **Expected state:** Preset CRUD/import/export/locking works; jobs expose effective settings; queue controls and queue save/load are durable and deterministic.
- **Tasks:** Implemented in source: preset/queue schemas, built-in/user catalogue, import safety, typed override resolver, stable job IDs, multi-select controls, reorder/priority/enable, duplicate/retry, pause state machine, queue persistence, immutable run snapshots and low-confidence size/time estimates.
- **Implemented files:** preset/queue models and services, resolver/session/coordinator extensions, `MainWindow.Workflow.cs`, preset dialog, queue UI, tests and docs.
- **Acceptance:** AC-052–AC-059, AC-074, AC-086
- **Validation:** VAL-036–VAL-038, VAL-049, VAL-060–VAL-063, VAL-065
- **Governance update:** Record built-in preset ownership, import safety, precedence and queue state transitions.
- **Report:** Workflow-foundation report; candidate 1.2.0 only after all gates.
- **Rollback point:** Keep projects loadable; ignore unsupported preset/queue sections with explicit warning.
- **Stop conditions:** Same job resolves differently in UI and run snapshot; imported preset can inject raw arguments; queue mutation can alter a running job; pause state is ambiguous.

## `PH-11` — FFmpeg capability and compatibility engine

- **Status:** Historical — original sequencing superseded by MediaForge 2; useful scope may be carried into PH-22–PH-31.
- **Objective:** Make UI and preflight truthful for the selected FFmpeg build.
- **Prerequisites:** PH-10 passed; supported fixture matrix declared.
- **Required reading:** ROUTE-004, ROUTE-012.
- **Expected state:** Capability report/cache exists; invalid combinations are blocked; each job has a canonical processing plan and per-stream summary.
- **Tasks:** Parse version/encoders/decoders/formats/filters/hwaccels/pix_fmts; cache/invalidate/export; build typed compatibility rules; micro-fixture probes; ProcessingPlan compiler; loss warnings; UI availability reasons.
- **Proposed files:** CapabilityService, CompatibilityService, ProcessingPlan models/compiler, UI, tests.
- **Acceptance:** AC-060–AC-064, AC-085–AC-086
- **Validation:** VAL-039–VAL-041, VAL-058, VAL-063–VAL-065
- **Governance update:** Define support claims, unknown state, cache evidence and compatibility rule ownership.
- **Report:** Capability/compatibility report for each tested FFmpeg build.
- **Rollback point:** Disable new engine behind compatibility fallback only if old path remains explicitly unverified.
- **Stop conditions:** Parser relies on one locale-specific message; unknown combinations are treated as supported; UI and run plan diverge.

## `PH-12` — FFmpeg-backed preview and editor evidence

- **Status:** Historical — original sequencing superseded by MediaForge 2; useful scope may be carried into PH-22–PH-31.
- **Objective:** Preview the canonical transform with measured fidelity.
- **Prerequisites:** PH-11 processing plan stable.
- **Required reading:** ROUTE-005, ROUTE-014.
- **Expected state:** Rendered frames/proxies, thumbnails and waveform are cancellable; preview fidelity is labelled; MediaElement fallback remains non-authoritative.
- **Tasks:** PreviewService; exact-frame render path; proxy/cache manager; thumbnail/waveform/keyframe extraction; overlay UI; zoom/pan/split; comparison fixtures; resource limits.
- **Proposed files:** Preview services/cache, editor ViewModel/views, tests.
- **Acceptance:** AC-064–AC-067, AC-086
- **Validation:** VAL-041–VAL-043, VAL-058, VAL-062–VAL-065
- **Governance update:** Define tolerances, cache retention, decoder boundary and unsupported state.
- **Report:** Preview parity and resource report.
- **Rollback point:** Retain MediaElement fallback and disable FFmpeg preview without losing edit plans.
- **Stop conditions:** Preview claims frame accuracy without measured tolerance; transform differs from export plan; process/cache cannot be bounded.

## `PH-13` — Hardware profiles, sample encode and quality targets

- **Status:** Historical — original sequencing superseded by MediaForge 2; useful scope may be carried into PH-22–PH-31.
- **Objective:** Improve speed and decision quality without hiding actual processing path.
- **Prerequisites:** PH-11/12 passed; test hardware available.
- **Required reading:** ROUTE-006, ROUTE-012.
- **Expected state:** Hardware paths are detected/benchmarked; intent profiles and software fallback work; sample/estimate results are labelled.
- **Tasks:** Benchmark harness; hardware profile catalogue; path planner; sample interval; target size/bitrate/two-pass/CQ; estimate model; optional SSIM/VMAF with capability gate.
- **Proposed files:** Benchmark/quality services, plan compiler, UI, tests.
- **Acceptance:** AC-068, AC-075, AC-085–AC-086
- **Validation:** VAL-044, VAL-050, VAL-058, VAL-062–VAL-065
- **Governance update:** Declare tested GPUs/drivers/builds, benchmark expiry and metric limitations.
- **Report:** Hardware/quality report by environment.
- **Rollback point:** Software path remains mandatory and selectable.
- **Stop conditions:** No software fallback; benchmark changes user settings silently; unsupported metric/filter is marketed.

## `PH-14` — Streams, subtitles, chapters, metadata and lossless plans

- **Status:** Historical — original sequencing superseded by MediaForge 2; useful scope may be carried into PH-22–PH-31.
- **Objective:** Add professional stream-level control and conservative smart processing.
- **Prerequisites:** PH-11 plan and PH-12 preview stable.
- **Required reading:** ROUTE-007, ROUTE-012.
- **Expected state:** Inspector and per-stream policy persist; explicit mappings compile; safe lossless/partial-copy operations are available only when proven compatible.
- **Tasks:** Expand probe model; inspector; stable stream IDs; mapping editor; external subtitles/burn; chapters/metadata/cover art; rewrap/extract/replace/keyframe trim/merge; timestamp safeguards.
- **Proposed files:** Probe/domain/plan models, inspector UI, conversion compiler, tests.
- **Acceptance:** AC-069–AC-071, AC-086
- **Validation:** VAL-045–VAL-047, VAL-058, VAL-060, VAL-063–VAL-065
- **Governance update:** Define stream identity, metadata policy, lossless claims and unsupported edge cases.
- **Report:** Stream/lossless compatibility report.
- **Rollback point:** Projects retain opaque unsupported stream policies read-only; default to no destructive mapping.
- **Stop conditions:** Any stream may be silently dropped; lossless claim lacks keyframe/timestamp evidence; subtitle/attachment mapping is ambiguous.

## `PH-15` — Output verification, history and support evidence

- **Status:** Historical — original sequencing superseded by MediaForge 2; useful scope may be carried into PH-22–PH-31.
- **Objective:** Make unattended results trustworthy and diagnosable.
- **Prerequisites:** PH-11 processing plan stable; DEC-023/024 accepted.
- **Required reading:** ROUTE-008, ROUTE-012, ROUTE-014.
- **Expected state:** Verification tiers run before commit; terminal statuses are accurate; bounded history/reports/support bundles exist.
- **Tasks:** VerificationService; expected-result contract; stream/duration/dimension/audio/timestamp checks; decode samples; retain-failed policy; history JSONL; report/export/redaction; UI states.
- **Proposed files:** Verification/history/report services, job model/UI, tests.
- **Acceptance:** AC-072–AC-074, AC-086
- **Validation:** VAL-048–VAL-049, VAL-058, VAL-062–VAL-065
- **Governance update:** Define tolerances, retention, redaction and claim language.
- **Report:** Verification false-positive/negative and privacy report.
- **Rollback point:** Fast pre-existing non-empty check remains only as explicitly insufficient fallback; no verified claim.
- **Stop conditions:** Verification occurs after destructive commit without rollback; warning/failure states collapse into success; support bundle leaks unapproved paths.

## `PH-16` — Professional audio, image and controlled filters

- **Status:** Historical — original sequencing superseded by MediaForge 2; useful scope may be carried into PH-22–PH-31.
- **Objective:** Add advanced workflows only through capability-aware, preset-driven plans.
- **Prerequisites:** PH-11/15 passed; fixture matrix expanded.
- **Required reading:** ROUTE-006, ROUTE-009, ROUTE-012.
- **Expected state:** Two-pass audio, advanced image preservation and controlled filters have explicit metadata/colour assumptions and verification.
- **Tasks:** Audio analysis/normalisation; image animation/page/ICC/alpha/metadata path; filter catalogue/order; HDR/SDR policy; presets; fixture coverage.
- **Proposed files:** Quality/filter/image/audio services and UI, tests.
- **Acceptance:** AC-076–AC-078, AC-086
- **Validation:** VAL-051–VAL-053, VAL-058, VAL-062–VAL-065
- **Governance update:** Define filter order, colour metadata, preservation/block rules and support matrix.
- **Report:** Quality workflow report.
- **Rollback point:** Advanced features disabled per capability; core conversion remains.
- **Stop conditions:** Animation/pages/colour metadata can be lost without warning; filter order is user-invisible; HDR transform lacks target metadata.

## `PH-17` — Watch folders and completion automation

- **Status:** Historical — original sequencing superseded by MediaForge 2; useful scope may be carried into PH-22–PH-31.
- **Objective:** Automate local workflows without destructive defaults.
- **Prerequisites:** PH-10 queue, PH-15 verification/history passed.
- **Required reading:** ROUTE-010, ROUTE-012.
- **Expected state:** Rules are auditable, restart-safe and dry-runnable; completion actions respect consent and terminal-state gates.
- **Tasks:** Rule schema; stable-file detector; ignore patterns; loop ledger; rule queue source; dry run; processed/review moves; notifications/sound/open folder; optional power/command/webhook with explicit warnings.
- **Proposed files:** Watch/automation services, settings/UI, tests.
- **Acceptance:** AC-079–AC-080, AC-084, AC-086
- **Validation:** VAL-054–VAL-055, VAL-058, VAL-061–VAL-065
- **Governance update:** Define destructive-action approvals, network boundary, retention and rollback.
- **Report:** Automation safety report.
- **Rollback point:** Disable all rules/actions; preserve audit/history.
- **Stop conditions:** Rule can delete source by default; incomplete-file guard absent; loop detection absent; command/webhook runs without explicit configuration.

## `PH-18` — Desktop polish, accessibility, localisation and updates

- **Status:** Historical — original sequencing superseded by MediaForge 2; useful scope may be carried into PH-22–PH-31.
- **Objective:** Reach release-quality Windows interaction and supportability.
- **Prerequisites:** Core workflows stable; DEC-026 resolved for updates.
- **Required reading:** ROUTE-011, ROUTE-013.
- **Expected state:** Themes/DPI/keyboard/accessibility/localisation pass; shell/portable/update/first-run paths are least-privilege and recoverable.
- **Tasks:** Resource dictionaries; theme/system/high contrast; queue views/search/drawer/context; AutomationProperties; localisation resources; shell/send-to/jump list; portable mode; first-run; updater/release notes; crash recovery diagnostics.
- **Proposed files:** Views/resources/services/installer/tests/docs.
- **Acceptance:** AC-081–AC-085, AC-087
- **Validation:** VAL-056–VAL-057, VAL-059, VAL-064–VAL-065
- **Governance update:** Declare accessibility baseline, locales, update provenance and privilege boundary.
- **Report:** Desktop quality and accessibility report.
- **Rollback point:** Disable integrations/update; retain portable/core app.
- **Stop conditions:** Accessibility regression blocks core flow; updater lacks integrity/rollback; shell integration requires unexpected elevation.

## `PH-19` — Release candidate and maintenance baseline

- **Status:** Historical — original sequencing superseded by MediaForge 2; useful scope may be carried into PH-22–PH-31.
- **Objective:** Produce a supportable release only from cumulative evidence.
- **Prerequisites:** All release-scoped phases passed; open critical decisions resolved.
- **Required reading:** All release routes; load only applicable docs per index.
- **Expected state:** Clean build, declared matrix, package, installer, licences, hashes, release notes, rollback and maintenance cadence are complete.
- **Tasks:** Cumulative audit; clean build/test; package/installer; sign only if authorised; install/uninstall; support bundle; limitations; rollback drill; archive evidence.
- **Proposed files:** Entire release surface and governance.
- **Acceptance:** AC-021–AC-030, AC-044–AC-045, AC-083–AC-087
- **Validation:** VAL-058–VAL-065 plus all validations mapped to release scope
- **Governance update:** Final statuses, release decision, delivery report and next maintenance review.
- **Report:** Final release report and artefact manifest.
- **Rollback point:** Prior validated installer/ZIP/settings/project schema plus tested procedure.
- **Stop conditions:** Any failed mandatory validation, unresolved critical risk/decision, missing licence/provenance, inconsistent version/hash or untested declared support claim.

## `PH-20` — MediaForge 2 product, UX and migration foundation

- **Status:** Complete — governance-only adoption merged via PR #2 at `d3c1c0958aae71afcc5462bfa2c4e66c6fd7ab55`; no V2 runtime claim.
- **Objective:** Canonically establish task-first behaviour, migration boundaries, requirements, decisions, risks, acceptance and validation ownership.
- **Prerequisites:** V1 rollback point `5acaf88e751327eac47ca673178fbbd88a8603f1`; reviewed V2 planning evidence; PH-10 sequencing exception recorded.
- **Required reading:** `PROJECT_INDEX.md`, `PROJECT_FOUNDATION.md`, `TRACEABILITY.md`, `DECISIONS_AND_CHANGE_HISTORY.md`, `SECURITY_PRIVACY_AND_RISK.md`.
- **Expected state:** One canonical owner per immutable definition; V1 history retained; external V2 pack remains evidence only; no runtime claim.
- **Work:** Reconcile product direction, architecture, UX, phase sequence, decisions, risks, acceptance, validation, migration and rollback boundaries.
- **Acceptance:** Existing governance `AC-001`–`AC-003` plus complete ownership/mapping consistency for `REQ-056`–`REQ-070`.
- **Validation:** `VAL-065` plus canonical-owner, duplicate-definition, route/link, phase-dependency, mapping, dirty-scope and `git diff --check` audits.
- **Governance update:** Adoption set finalised under `BR-20260919-02`; commit `c8fdc552cca699bb4eae9169bdcf8e042fc57f93` merged via PR #2 as `d3c1c0958aae71afcc5462bfa2c4e66c6fd7ab55`.
- **Rollback point:** `5acaf88e751327eac47ca673178fbbd88a8603f1`.
- **Stop conditions:** Duplicate/competing definitions, stale active sequencing, runtime/source changes, unsupported status inflation or unresolved safety contradiction.

## `PH-21` — V1 closure and V2 rollback bridge

- **Status:** Satisfied before PH-20 canonical adoption.
- **Objective:** Establish a durable V1 starting point while explicitly carrying unresolved validation forward.
- **Prerequisites:** PH-10 source and automated Windows evidence.
- **Required reading:** `PH10_IMPLEMENTATION_REPORT.md`, `VERIFICATION.md`, `TRACEABILITY.md`, `REPOSITORY_AND_VERSIONING.md`.
- **Expected state:** Merged rollback baseline exists; remaining V1 manual/native checks are explicitly Skipped/Unproven rather than silently treated as passed.
- **Work:** Historical only; no future implementation work is scheduled under PH-21.
- **Acceptance:** Rollback commit exists and sequencing exception preserves the evidence boundary.
- **Validation:** Evidence recorded by `BR-20260911-01` and `BR-20260919-01`; deferred checks remain Unproven.
- **Governance update:** Already recorded in V1 closure governance.
- **Rollback point:** `5acaf88e751327eac47ca673178fbbd88a8603f1`.
- **Stop conditions:** None for new work; reopening PH-21 requires an explicit governance change.

## `PH-22` — V2 application shell and workflow navigation

- **Status:** Closed for shell scope by `BR-20260924-01` (effective when closure PR is merged).
- **Objective:** Build the task-first WPF shell without recreating the V1 control wall.
- **Prerequisites:** PH-20 merged/exited; PH-21 satisfied bridge.
- **Required reading:** `UI_UX_ACCESSIBILITY_AND_LOCALISATION.md`, `PROJECT_FOUNDATION.md`, `TRACEABILITY.md`, `ARCHITECTURE.md`.
- **Expected state:** Conventional menu, Home, task navigation, shared workflow frame, media selection, task-state lifecycle, progressive disclosure and accessibility baseline.
- **Work:** Shell/menu commands; Home; task routing; shared workflow state; empty/loading/error states; Advanced disclosure; keyboard/focus/automation properties.
- **Acceptance:** `AC-088`, `AC-091`, `AC-092`, `AC-096`, `AC-105`, plus source inspection of task guidance. Full `AC-102` (including multi-file/batch-review guidance) remains mandatory with PH-26; `AC-103` remains mandatory with the end-to-end PH-24 workflow and later release validation. Neither is a PH-22 exit gate under `DEC-038`.
- **Validation:** Release build/XAML checks plus `VAL-066` and `VAL-082`. PH-22 retains structural evidence toward accessibility, while full `VAL-067`, `VAL-079` and `VAL-080` remain mandatory in their downstream workflow/release phases under `DEC-038`.
- **Closure evidence:** User-authorised PH-22 exit uses the retained `AC-096` projectless Home-to-Advanced native fixture plus the existing scoped Windows shell/build evidence. `DEC-039` was not adopted and `AC-096` remains unchanged. `AC-103`/`VAL-067`, full `AC-102`, release accessibility and deferred V1 safety validation remain downstream. Closure becomes effective on verified merge of `BR-20260924-01`.
- **Governance update:** Record actual shell files/state model and any changed navigation/accessibility contract.
- **Rollback point:** Merged PH-20 governance commit `d3c1c0958aae71afcc5462bfa2c4e66c6fd7ab55`.
- **Stop conditions:** Primary workflow requires unrelated technical controls, pointer-only operation, inaccessible navigation, or duplicated workflow authority in code-behind/services.

## `PH-23` — WorkflowIntent, compatibility and ProcessingPlan seam

- **Status:** Next active phase upon verified merge of `BR-20260924-01`.
- **Objective:** Establish one typed path from user intent to executable processing.
- **Prerequisites:** PH-22 shell state/navigation seam is stable enough for the first workflow.
- **Required reading:** `CAPABILITY_COMPATIBILITY_AND_PROCESSING_PLAN.md`, `ARCHITECTURE.md`, `TRACEABILITY.md`, `SECURITY_PRIVACY_AND_RISK.md`.
- **Expected state:** Typed WorkflowIntent, capability/compatibility resolution, immutable ProcessingPlan, plan summary, FFmpeg compilation seam and immutable execution snapshot.
- **Work:** Implement typed intent/plan models; reuse/refactor minimum V1 option/capability/conversion seams; wire estimate inputs and plan diagnostics.
- **Acceptance:** V2 `AC-095`, `AC-101` plus applicable legacy capability/plan criteria `AC-060`–`AC-064`.
- **Validation:** `VAL-039`–`VAL-041`, `VAL-071`, `VAL-077` plus Release build and targeted unit/service tests.
- **Governance update:** Record actual plan fields, compatibility authority and reused/refactored V1 seams.
- **Rollback point:** PH-22 merged state.
- **Stop conditions:** UI constructs raw FFmpeg arguments, option resolution is duplicated, plan is mutable after Start, or summary/execution can diverge.

## `PH-24` — Resize and Crop & Resize vertical slice

- **Status:** Planned.
- **Objective:** Prove the task-first architecture end-to-end on the first complete user workflow.
- **Prerequisites:** PH-23 plan/execution seam passes targeted tests.
- **Required reading:** `UI_UX_ACCESSIBILITY_AND_LOCALISATION.md`, `CAPABILITY_COMPATIBILITY_AND_PROCESSING_PLAN.md`, `TRACEABILITY.md`.
- **Expected state:** Add media -> Resize/Crop & Resize -> review/preview -> output -> estimate -> process -> result works for supported single and multi-file fixtures.
- **Work:** Source inspection; target dimensions; `Fit within`/`Fit in frame`/`Fill frame`/`Stretch`; crop geometry; normal quality; advanced disclosure; estimate; execution; output probe.
- **Acceptance:** `AC-089`, `AC-090`, `AC-095`, `AC-098`–`AC-101`, `AC-103`.
- **Validation:** `VAL-067`–`VAL-069`, `VAL-071`–`VAL-074`, `VAL-077`, `VAL-078` plus relevant inherited source/output safety fixtures.
- **Governance update:** Record implemented geometry/preview/estimate tolerances and first-slice reuse decisions.
- **Rollback point:** PH-23 merged state.
- **Stop conditions:** Preview/export geometry diverges, source hash changes, collision/temp/cancel safety regresses, or ambiguous geometry is hidden from the user.

## `PH-25` — Convert workflow

- **Status:** Planned.
- **Objective:** Provide an approachable general conversion workflow over the same plan authority.
- **Prerequisites:** PH-24 proves the task/plan pattern.
- **Required reading:** `CAPABILITY_COMPATIBILITY_AND_PROCESSING_PLAN.md`, `UI_UX_ACCESSIBILITY_AND_LOCALISATION.md`, `TRACEABILITY.md`.
- **Expected state:** Normal format, dimensions, quality, frame-rate and audio intents resolve visibly to supported technical output; advanced controls remain capability-aware.
- **Work:** Convert task UI; `Recommended` resolution; quality intent; frame-rate/audio intent; advanced supported codec controls; estimate/result projection.
- **Acceptance:** `AC-092`, `AC-095`, `AC-101`, `AC-102`, `AC-105` plus applicable inherited compatibility criteria.
- **Validation:** `VAL-066`, `VAL-067`, `VAL-071`, `VAL-077` plus representative image/video/audio conversion and output-probe fixtures.
- **Governance update:** Record normal-intent mappings and any capability-dependent defaults.
- **Rollback point:** PH-24 merged state.
- **Stop conditions:** `Recommended` hides resolved output, unsupported options appear supported, or advanced/normal paths use different execution authority.

## `PH-26` — Mixed-media batch workflow

- **Status:** Planned.
- **Objective:** Apply one task intent safely across technically different source files.
- **Prerequisites:** Convert/resize plan patterns are stable.
- **Required reading:** `CAPABILITY_COMPATIBILITY_AND_PROCESSING_PLAN.md`, `PRESETS_AND_QUEUE_CONTROL.md`, `TRACEABILITY.md`.
- **Expected state:** Explicit target policies, per-file plan review, exceptions/warnings, aggregate estimates and queue integration for mixed media.
- **Work:** Mixed-source policy resolution; per-file plan table; aggregate/free-space advisory; incompatibility handling; batch progress/results.
- **Acceptance:** `AC-093`, `AC-094`, `AC-095`, `AC-102` (complete Convert/Resize/Crop & Resize and multi-file/batch-review guidance) plus inherited queue/cancellation safety criteria where exercised.
- **Validation:** `VAL-070`, `VAL-071`, targeted task/batch-review guidance checks for `AC-102`, plus applicable `VAL-006`, `VAL-007`, `VAL-031`, `VAL-061`, `VAL-063`.
- **Governance update:** Record per-file override rules and final V1 queue-compatibility decision inputs.
- **Rollback point:** PH-25 merged state.
- **Stop conditions:** One incompatible item silently alters global intent, active plans mutate, destination reservations race, or batch cancellation leaves ambiguous outputs.

## `PH-27` — Trim, Split and Combine

- **Status:** Planned.
- **Objective:** Reintroduce lightweight temporal editing as task-specific workflows.
- **Prerequisites:** Shared task/plan/execution architecture is stable.
- **Required reading:** `CAPABILITY_COMPATIBILITY_AND_PROCESSING_PLAN.md`, editor-related traceability, `SECURITY_PRIVACY_AND_RISK.md`.
- **Expected state:** Trim/split/combine intent, ordering, duration and re-encode/copy consequences are explicit and represented in ProcessingPlan.
- **Work:** Trim ranges; split points/segments; combine order; A/V/timestamp normalisation; preview/review; explicit copy/re-encode policy.
- **Acceptance:** Applicable legacy `AC-039`–`AC-043`, `AC-069`–`AC-071`, plus V2 `AC-101`.
- **Validation:** Applicable `VAL-024`–`VAL-029`, `VAL-045`–`VAL-047`, `VAL-077`, `VAL-078`.
- **Governance update:** Record supported temporal-operation compatibility and tolerances.
- **Rollback point:** PH-26 merged state.
- **Stop conditions:** Streams are silently dropped, copy/lossless claims lack evidence, ordering is ambiguous, or timestamp/A-V integrity cannot be verified.

## `PH-28` — Projects, recovery and presets

- **Status:** Planned.
- **Objective:** Integrate durable V1 capabilities without making them mandatory for quick work.
- **Prerequisites:** V2 task/workflow state is stable enough to define persistence semantics.
- **Required reading:** `PROJECTS_AUTOSAVE_AND_RECOVERY.md`, `PRESETS_AND_QUEUE_CONTROL.md`, `TRACEABILITY.md`.
- **Expected state:** Save/Open/Save As, recovery, relink, recent projects and contextual presets preserve V2 workflow state; V1 compatibility classes are evidence-backed.
- **Work:** V2 schema/migration mapping; project session integration; recovery; relink; preset migration/management; queue compatibility decision where applicable.
- **Acceptance:** `AC-096`, `AC-097`, `AC-104` plus applicable legacy `AC-046`–`AC-058`.
- **Validation:** `VAL-033`–`VAL-038`, `VAL-060`, `VAL-061`, `VAL-075`, `VAL-076`.
- **Governance update:** Resolve OD-006, OD-007 and OD-008 from fixtures; document schema compatibility.
- **Rollback point:** PH-27 merged state plus preserved original V1 files.
- **Stop conditions:** Migration overwrites the only V1 copy, unknown semantics are silently discarded, recovery replaces canonical save implicitly, or raw executable data becomes trusted.

## `PH-29` — Results and output verification

- **Status:** Planned.
- **Objective:** Make terminal results trustworthy, inspectable and distinct from mere process exit.
- **Prerequisites:** Core conversion/task workflows produce stable ProcessingPlans.
- **Required reading:** output-verification/history requirements in `TRACEABILITY.md`, `CAPABILITY_COMPATIBILITY_AND_PROCESSING_PLAN.md`, `SECURITY_PRIVACY_AND_RISK.md`.
- **Expected state:** Expected-result contract, probing/verification levels, results/warnings/failures, output actions and failed-temp policy are explicit.
- **Work:** Verification service/result model; probe/decode checks; result page; reporting; privacy-aware diagnostics; failed-temp handling.
- **Acceptance:** Legacy `AC-072`–`AC-074` plus V2 `AC-098`–`AC-101`.
- **Validation:** `VAL-048`, `VAL-049`, `VAL-072`–`VAL-074`, `VAL-077`, `VAL-081` as applicable.
- **Governance update:** Resolve verification tolerances/retention decisions when evidence is sufficient.
- **Rollback point:** PH-28 merged state.
- **Stop conditions:** Verification occurs only after destructive commit, failure collapses into success, or support/result output leaks unapproved sensitive paths.

## `PH-30` — Advanced/professional tools

- **Status:** Planned.
- **Objective:** Restore technical depth only on top of the coherent V2 workflow/plan architecture.
- **Prerequisites:** Core task workflows, plan authority and result verification are stable.
- **Required reading:** capability/stream/quality/automation requirements and `SECURITY_PRIVACY_AND_RISK.md`.
- **Expected state:** Inspector, stream controls, hardware paths, metadata, advanced quality/audio/image/video tools and guarded automation expose only evidenced capability.
- **Work:** Implement only approved/supportable advanced surfaces and services; preserve one ProcessingPlan authority and explicit consent for automation/network/destructive actions.
- **Acceptance:** `AC-105` plus applicable legacy `AC-068`–`AC-080`, `AC-084`–`AC-086`.
- **Validation:** Applicable `VAL-044`–`VAL-055`, `VAL-058`, `VAL-062`–`VAL-065`, `VAL-077`.
- **Governance update:** Record capability support matrix, automation consent boundaries and any new supply-chain decision.
- **Rollback point:** PH-29 merged state.
- **Stop conditions:** No software fallback where required, unsupported capability is marketed, destructive/network action lacks explicit authorisation, or advanced controls bypass the canonical plan.

## `PH-31` — Onboarding, accessibility and release quality

- **Status:** Planned release-readiness phase.
- **Objective:** Complete V2 supportability and release-readiness without inflating unproven claims.
- **Prerequisites:** Release-scoped V2 workflows are integrated and open critical decisions for the claimed release are resolved.
- **Required reading:** `VALIDATION_AND_EVIDENCE.md`, `REPOSITORY_AND_VERSIONING.md`, UI/accessibility governance, release/supply-chain decisions.
- **Expected state:** Help/onboarding, keyboard/screen-reader/high-contrast/DPI/multi-monitor/localisation readiness, first-run/tool discovery, package evidence and rollback are complete for the declared release scope.
- **Work:** Getting Started/help; accessibility hardening; DPI/multi-monitor; localisation readiness; first-run/tool provenance; package/installer where claimed; final limitations/rollback.
- **Acceptance:** `AC-019`–`AC-030`, `AC-081`–`AC-087`, `AC-091`, `AC-102`, `AC-103` as applicable to the declared release.
- **Validation:** Applicable `VAL-013`–`VAL-020`, `VAL-056`–`VAL-065`, `VAL-079`–`VAL-081`, plus all mandatory validations mapped to claimed features.
- **Governance update:** Resolve OD-009 before any version/package-changing V2 delivery; record final support matrix, evidence, limitations and rollback.
- **Rollback point:** Last exact-head validated pre-release commit/package.
- **Stop conditions:** Any mandatory applicable validation is Failed/Skipped/Unproven without an explicit release risk exception, or version/provenance/hash/rollback evidence is inconsistent.

## MediaForge 2 sequencing

A V1 service existing in source does not allow a later V2 phase to bypass upstream WorkflowIntent, ProcessingPlan, safety or UX contracts.

Each phase runs affected Release build plus relevant unit/service/schema/plan and targeted safety tests. After substantial integration, implement/run the Fast Post-Build Smoke defined by canonical `VALIDATION_AND_EVIDENCE.md`. Before a release claim, run the applicable Full Release Validation programme.
