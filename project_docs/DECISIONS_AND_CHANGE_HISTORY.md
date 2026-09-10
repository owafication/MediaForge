# Decisions and change history

**Purpose:** Canonical material decisions, change procedure and delivery reports.  
**Read when:** Revisiting scope, architecture, safety, version, persistence, update or release choices.  
**Owner:** Project maintainer.  
**Authority:** Canonical owner for `DEC-###` and `BR-YYYYMMDD-##`.  
**Update trigger:** Material choice, supersession, delivery, validation reversal or authority conflict.  
**Project rules:** Preserve history; supersede rather than rewrite silently.

## Decision register

| ID | Status | Decision |
|---|---|---|
| DEC-001 | Accepted | Project root `/MediaForge`; governance root `/project_docs`; root owns routing, architecture, verification and changelog. |
| DEC-002 | Accepted | Retain a single local WPF process with explicit models/services and external FFmpeg child processes. |
| DEC-003 | Superseded | 1.0.2 was the historical baseline; current inspected source target is 1.1.0 and remains runtime-unverified. |
| DEC-004 | Accepted | No telemetry, accounts, cloud processing or persisted media history without a new requirement and privacy decision. |
| DEC-005 | Superseded | Documentation-only delivery was superseded by the 1.1.0 source implementation. |
| DEC-006 | Open | FFmpeg acquisition and redistribution policy. Default: user-supplied tools plus an optional consented, hash-verified helper. |
| DEC-007 | Open | Output validation boundary. Default: tiered FFprobe/decode verification before final commit. |
| DEC-008 | Open | Supported Windows/architecture/codec/accessibility matrix must be declared from evidence. |
| DEC-009 | Accepted | Keep lightweight crop/trim/stitch editing in the existing WPF product rather than adding another UI framework. |
| DEC-010 | Accepted with limitation | MediaElement remains a fast fallback; it is not authority for FFmpeg export fidelity. |
| DEC-011 | Accepted | Store crop as clamped normalised coordinates and per-clip trim/source metadata. |
| DEC-012 | Accepted | Active crop/trim/stitch requires an explicit re-encode/processing plan; bypassing stream-copy paths are blocked. |
| DEC-013 | Proposed | Use 1.2.0 as the workflow-foundation milestone: projects/recovery, presets, per-job options and queue control. |
| DEC-014 | Accepted 2026-07-28 | User explicitly directed implementation of all PH-08. Migrate application/tests to `net10.0-windows`, pin the SDK, preserve settings/installer contracts and retain PH-08A.2 as rollback. Native migration evidence remains required before phase exit. |
| DEC-015 | Proposed accepted | Introduce incremental ViewModels and domain services without a DI framework or a wholesale rewrite. |
| DEC-016 | Proposed accepted | Use versioned UTF-8 JSON for `.mediaforge`, `.mediaforge-preset` and `.mediaforge-queue`; typed data only, no raw command fragments. |
| DEC-017 | Proposed accepted | Manual save is canonical; autosave writes separate recovery snapshots using temp/flush/atomic replace and bounded retention. |
| DEC-018 | Proposed accepted | Effective options resolve in one service: global default → selected preset → per-job overrides → edit plan. |
| DEC-019 | Proposed | Capability cache is keyed to the selected FFmpeg/FFprobe executable evidence and can be manually refreshed/exported. |
| DEC-020 | Proposed | One immutable ProcessingPlan is the authority for UI summary, preview, FFmpeg invocation, verification and diagnostics. |
| DEC-021 | Proposed | Build FFmpeg-backed still-frame/proxy preview through CLI processes first; retain MediaElement as labelled fallback; do not embed libav in the immediate roadmap. |
| DEC-022 | Proposed | Hardware control uses intent profiles and a local benchmark; no generic GPU checkbox. |
| DEC-023 | Proposed | Run selected verification against temporary output before commit; failed verification cannot silently replace the destination. |
| DEC-024 | Proposed | Use local-only bounded rolling JSONL history with retention/redaction controls first; introduce a database only if measured query/retention requirements exceed it. |
| DEC-025 | Proposed accepted | Watch folders never delete sources by default and all potentially destructive completion actions are disabled until explicitly configured. |
| DEC-026 | Decision required | Update checking and optional FFmpeg acquisition require a signed or hash-anchored release manifest, consent and rollback; no silent self-update. |
| DEC-027 | Proposed accepted | The product remains a converter/preparation/lightweight assembly app, not a full NLE, cloud service or AI application. |
| DEC-028 | Decision required | Choose whether PH-07 produces a public 1.1.1 stabilisation release or only an internal validated baseline before 1.2.0. |

## Open decisions before next milestone

- `DEC-006`: FFmpeg acquisition/redistribution
- `DEC-007`: exact output verification tolerances and failed-temp retention
- `DEC-008`: declared Windows/architecture/codec/accessibility matrix
- `DEC-014`: .NET 10 migration after baseline
- `DEC-026`: updater/acquisition integrity design
- `DEC-028`: public 1.1.1 versus internal checkpoint

## Governance change procedure

1. Identify canonical owner and affected IDs.
2. Inspect source/evidence.
3. Record material decision and same-level conflicts.
4. Update each definition once.
5. Update index, mapping, changelog and evidence.
6. Record rollback and unproven items.

## Delivery reports

### `BR-20260728-01` — Desktop scope and next-build pack

- **Mode:** File generation.
- **Inputs:** Inspected refined 1.1.0 source archive, prior refinement evidence and supplied A-class desktop assessment.
- **Produced:** Complete proposed governance pack for the existing desktop repository; no application code.
- **Ran:** Source inventory/metadata inspection, document generation, ID/link/limit/content/archive audit.
- **Unproven:** Every proposed implementation and all native Windows results.
- **Rollback:** Remove these proposed documents or restore the original governance from the refined source archive.

### `BR-20260728-02` — PH-07 baseline automation start

- **Objective:** Adopt the complete desktop governance pack and implement the smallest reproducible PH-07 baseline slice without changing media-processing behaviour.
- **Mode:** Source modification and local static/fixture execution in a Linux sandbox.
- **Inputs:** Refined 1.1.0 source SHA-256 `3ecf078e2f794231c71f763a7d9e229a497b21d0157b13aeb64025fa6cab8ed2`; foundation pack SHA-256 `7f5f3794fba92a0681b63a5fba774eab5904a9f0bf2f00421ef27eec774b08a3`.
- **Implemented:** Adopted root/project governance; deterministic source verifier; fixture generator; Windows baseline orchestrator; isolated settings launch smoke; package audit; versioned ZIP names, package manifest and SHA-256 output.
- **Ran:** Python compilation; static source audit; Linux FFmpeg fixture generation/probing; PowerShell lexical delimiter checks.
- **Passed:** 12 source checks over 68 non-generated files; 13 fixture/filesystem files generated and probed; seven PowerShell scripts passed the limited lexical delimiter check.
- **Unavailable:** .NET SDK, PowerShell, Windows/WPF runtime and Inno Setup.
- **Unproven:** C#/XAML compilation, Windows launch, actual conversion paths, source/output/collision/cancellation/temp-cleanup fixtures, package audit execution and installer.
- **Risks:** No source media or external paths were modified. Generated fixtures were isolated under ignored `artifacts/`. PowerShell execution correctness remains unproven until native execution.
- **Rollback:** Restore the input 1.1.0 archive, or revert the governance and script changes in this report. No user schema or media migration exists.
- **Next stop:** Run `scripts/validate-windows-baseline.ps1` on Windows and complete its manual safety checklist. Do not begin PH-08 until mandatory evidence passes.
### `BR-20260728-03` — PH-08A central process seam

- **Objective:** Start the smallest testable PH-08 architecture slice while retaining PH-07 native validation as pending.
- **Mode:** Source modification and Linux-scoped static verification.
- **Implemented:** `IProcessRunner`, typed request/result contracts and `ProcessRunner`; all production FFmpeg/FFprobe/PowerShell process lifecycle paths migrated; package-free `MediaForge.Tests` executable; guarded PowerShell test runner; baseline harness integration.
- **Behaviour contract:** `ProcessStartInfo.ArgumentList`, redirected stdout/stderr, non-zero exit handling, cancellation and entire-process-tree termination remain the intended behaviour. Output capture is bounded for FFmpeg stderr.
- **Ran:** Python compilation; 14 deterministic source checks over 75 non-generated files; direct child-process ownership audit; C# limited lexical inspection; existing Linux FFmpeg fixture generation/probing.
- **Passed:** Static source and fixture checks available in this environment.
- **Unavailable:** .NET SDK, PowerShell, Windows/WPF runtime.
- **Unproven:** C#/generated-XAML compile, characterisation executable runtime, process-tree cancellation on Windows, launch/conversion/package/installer.
- **Decision:** `DEC-014` remains unresolved; target framework stays `net8.0-windows`. ProjectSession, QueueCoordinator, EffectiveOptionsResolver and ViewModels remain pending.
- **Rollback:** Restore the PH-07 archive or revert the PH-08A runtime/test files and call-site migrations. No user data/schema change exists.
- **Next stop:** Run `scripts/validate-windows-baseline.ps1`; fix only evidenced compile/test regressions before any further PH-08 extraction.

### `BR-20260728-04` — PH-08A.1 ProcessRunner compile correction

- **Objective:** Resolve the user-observed Windows compiler error without changing runtime behaviour or public contracts.
- **Evidence:** User-run `scripts\build-release.ps1` restored successfully and failed with `CS0246` for `StreamReader` in `Services\Runtime\ProcessRunner.cs`.
- **Implemented:** Added explicit `using System.IO;`; added the `process-runner-system-io-import` static regression check; updated verification records.
- **Ran:** Linux-scoped source verification and archive integrity checks.
- **Unproven:** Corrected C#/WPF build, tests, publish and runtime behaviour remain pending Windows rerun.
- **Rollback:** Remove the explicit import and regression check, or restore the PH-08A archive. No data/schema migration exists.
- **Next stop:** Rerun `scripts\build-release.ps1`; address only newly evidenced compiler/test failures before further roadmap work.
### `BR-20260728-05` — PH-08A.2 build confirmation

- **Evidence:** User reported `scripts\build-release.ps1` restored and built successfully on Windows after the ProcessRunner import and settings-bitrate scope fixes.
- **Boundary:** User-reported predecessor-source evidence only; no launch or conversion safety claim.

### `BR-20260728-06` — Full PH-08 source implementation

- **Objective:** Implement all PH-08 architecture seams and the approved supported-platform migration without starting PH-09 persistence or PH-10 presets/queue features.
- **Implemented:** `EffectiveOptionsResolver`, `ProjectSession`, `QueueCoordinator`, `MainViewModel`, `IMediaConversionService`, .NET 10 target/global SDK pin, expanded architecture/safety tests and conservative large-image preflight.
- **Runtime evidence incorporated:** The user's 38,400 × 21,600 PNG FFmpeg failure is now converted into an actionable preflight/error path; backend conversion support is not claimed.
- **Contracts preserved:** Product version, `AppSettings`, installer identifiers, collision policies and conversion intent.
- **Available checks:** Linux-scoped static, XML/XAML, fixture, lexical and archive checks.
- **Unproven:** Clean Windows .NET 10 compile/tests/launch/settings/fixture/package/installer.
- **Rollback:** Restore the user-build-passing PH-08A.2 source archive. No data schema migration exists.
- **Next stop:** Run PH-08 tests and Windows build/evidence harness; do not begin PH-09 until failures are resolved or explicitly accepted.


### `BR-20260728-07` — Consolidated compile evidence and publish-lock correction

- **Evidence:** User-provided Windows output verifies .NET SDK 10.0.301 restore and x64 compilation of the consolidated PH-08 source.
- **Observed issues:** CA2014 in `ImageHeaderProbe.ReadJpeg`; `GenerateBundle` access denied for default `obj` `singlefilehost.exe`.
- **Implemented:** Hoisted JPEG probe stack buffers outside the loop; isolated publish intermediates under a unique temporary root; added three bounded fresh-path publish attempts.
- **Contracts preserved:** Conversion behaviour, settings, product version and package contents.
- **Unproven:** Corrected warning-free build, publish ZIP, tests, launch and installer.
- **Next stop:** Rerun `scripts\run-characterization-tests.ps1` and `scripts\build-release.ps1`; retain exact output as evidence.

### `BR-20260728-08` — PH-09 project persistence and multi-file release

- **Objective:** Implement PH-09 durable project workflows and change Win64 release publishing to `SingleFile=false` without beginning PH-10.
- **Authority:** User explicitly requested proceeding with PH-09 despite the PH-08 native evidence gap; that unresolved risk remains recorded.
- **Implemented:** Schema-v1 `.mediaforge` DTO/service/mapper; atomic canonical save and backup; separate bounded recovery/autosave; startup recovery UI; recent projects; source fingerprints and explicit relink; contained opt-in portable paths; project dirty/read-only UI; PH-09 tests; multi-file release and package-audit guards.
- **Contracts preserved:** Product version 1.1.0, global settings compatibility, source-media immutability, external FFmpeg process boundary and existing conversion option/collision behaviour.
- **Ran:** Python compilation and 25 deterministic static checks over 108 non-generated files.
- **Passed:** Static/XAML/ownership/persistence/package-guard checks available in the Linux sandbox.
- **Unavailable:** .NET SDK, PowerShell, Windows/WPF runtime and Inno Setup.
- **Unproven:** PH-09 compile/tests/UI, Windows atomic interruption behaviour, multi-file publish/launch/package/installer and conversion fixture regression.
- **Rollback:** Restore the PH-08 input archive. Preserve any schema-v1 project/recovery files because PH-08 cannot open them.
- **Next stop:** Run the PH-09 Windows gate and fix only evidenced regressions before PH-10.


### `BR-20260729-11` — PH-10 presets and professional queue

- **Objective:** Enact PH-10 on the corrected PH-09 source while preserving project compatibility and multi-file Win64 publishing.
- **Authority:** The user explicitly requested “Enact ph10”, providing the risk exception required by the active plan despite missing corrected PH-09 native evidence.
- **Implemented:** Schema-v1 preset/queue DTOs and atomic services; grouped read-only built-ins and user lifecycle/default/import/export; safe unknown-field retention and command/executable-path rejection; typed partial overrides and locked fields; one global → preset → job → edit resolver; project/queue preset snapshots; stable reorder/priority/enable/duplicate/retry controls; pause-after-current/resume; immutable source/edit/options run snapshots; advisory low-confidence size/time estimates; WPF workflow controls; characterisation tests and verifier guards.
- **Contracts preserved:** Product version 1.1.0, `.mediaforge` schema v1 compatibility, global settings, source-media immutability, external FFmpeg boundary and Win64 `PublishSingleFile=false`.
- **Ran:** Python verifier compilation, XML/XAML parsing and 31 deterministic static checks over 120 non-generated files.
- **Passed:** Static ownership, import-safety, persistence, precedence, immutable-run, queue/pause, estimate and multi-file package-guard checks.
- **Unavailable:** .NET SDK, PowerShell, Windows/WPF runtime and Inno Setup.
- **Unproven:** Native compile/tests/UI, preset and queue fixtures, pause/cancel races, FFmpeg conversion regression, publish/launch/package/installer.
- **Rollback:** Restore the PH-09 build-fix2 archive. Preserve schema-v1 preset/queue files; the prior app does not understand their workflow sections.
- **Next stop:** Run the consolidated PH-10 Windows gate and fix only evidenced regressions before PH-11.

### `BR-20260729-13` — PH-10 launch-smoke isolation and recovery rendering

- **Objective:** Fix the evidenced launch-smoke failure without weakening the release gate.
- **Evidence:** PH-10 compiles; the launch harness reported one or more failed cases. The supplied schema-v1 recovery snapshot is valid and has no canonical path, making it recoverable while present.
- **Implemented:** Central override-aware roaming/local storage paths; migrated settings, presets, recent projects, recovery, startup logs and local FFmpeg lookup; profile-isolated smoke environment; post-render one-shot recovery prompt; fail-soft recovery diagnostics; schema-2 per-case smoke evidence with window title/handle and startup logs; regression test and verifier guards.
- **Preserved:** Recovery data and choices, PH-09/PH-10 schemas, normal user storage locations when no override is set, and the mandatory multi-file release smoke gate.
- **Ran:** Python verifier compilation and 34 deterministic static checks over 123 non-generated files.
- **Unavailable:** Native .NET build/tests, PowerShell execution and Windows/WPF launch.
- **Unproven:** Which prior smoke case failed because its JSON evidence was not supplied; corrected Windows smoke outcome remains pending.
- **Next stop:** Rebuild on Windows and inspect `artifacts/MediaForge-1.1.0-win-x64-launch-smoke.json`; fix only any newly evidenced case.
### `BR-20260729-14` — PH-10 progress-binding startup correction

- **Objective:** Correct the exact startup exception captured by the Windows launch diagnostic without weakening the launch gate.
- **Evidence:** Windows restore/build succeeded, all 37 characterisation tests passed and multi-file publish completed. The launch diagnostic showed `InvalidOperationException`: WPF selected a TwoWay binding for read-only `MainViewModel.OverallProgress` while showing `MainWindow`; the build also reported CS8604 in startup diagnostics.
- **Implemented:** Explicit `Mode=OneWay` on the overall progress binding; `safeStage` passed into diagnostic report generation; verifier regression checks; evidence, traceability and handoff updates.
- **Preserved:** Read-only view-model projection, queue behaviour, startup diagnostics, profile isolation, recovery flow and mandatory launch-smoke packaging gate.
- **Ran:** Python verifier compilation and 36 deterministic static checks over 123 non-generated files.
- **Passed:** XAML parsing/handler checks, explicit OneWay binding guard, non-null diagnostic-stage guard and all prior PH-08–PH-10 static contracts.
- **Unavailable:** Native .NET/WPF rerun in this sandbox.
- **Unproven:** Corrected four-case Windows launch/close smoke and final package audit.
- **Next stop:** Rerun characterisation/build-release on Windows and inspect schema-2 smoke evidence only if a case remains red.

