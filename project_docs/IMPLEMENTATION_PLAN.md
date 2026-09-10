# Complete implementation plan

**Purpose:** Canonical phase sequence and phase contracts.  
**Read when:** Starting, sequencing, reviewing or closing implementation work.  
**Owner:** Technical maintainer.  
**Authority:** Canonical owner for `PH-##`.  
**Update trigger:** Phase scope, dependency, acceptance, validation, rollback or status change.  
**Current pointer:** PH-08–PH-10 source implementation complete; consolidated native Windows evidence remains pending and no PH-10 phase-exit claim is permitted.

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

- **Status:** Later
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

- **Status:** Later
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

- **Status:** Later
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

- **Status:** Later
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

- **Status:** Later
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

- **Status:** Later
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

- **Status:** Later
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

- **Status:** Later
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

- **Status:** Later
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
