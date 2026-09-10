# Source inspection report — MediaForge 1.0.2

> **Historical baseline:** This report describes the supplied MediaForge 1.0.2 source before the 1.1.0 preview/edit implementation. See `IMPLEMENTATION_REPORT_1.1.0.md` for current generated-source changes and evidence.


**Purpose:** Evidence-grounded static inspection of the uploaded source and prioritised findings for implementation planning.  
**Read when:** Establishing current state, triaging hardening, or checking why a risk/phase exists.  
**Owner:** Technical maintainer.  
**Authority:** Inspection evidence for `BR-20260727-01`; canonical definitions remain in their owning files.  
**Update trigger:** A finding is disproved, fixed, superseded, or new build/runtime evidence becomes available.  
**Project rules:** Static inspection proves source structure only; do not promote risks to runtime defects without reproduction.  
**Linked IDs:** `BR-20260727-01`, `RISK-001`–`RISK-015`, `PH-01`–`PH-05`.  
**Open decisions:** `DEC-006`, `DEC-007`, `DEC-008`.

## Objective and mode

- **Mode:** File generation.
- **Objective:** Thoroughly inspect the uploaded app and create a complete governance/documentation baseline without changing application behaviour.
- **Original archive SHA-256:** `24ceaec042d30373364a38c5d0deb91564e74eebdc8db70d0eb01f5a2f960461`.
- **Inspected root:** `MediaForge/`.
- **File inventory:** 26 files, 2,655 text lines; 1,684 C# lines, 479 XAML lines, and 193 PowerShell lines.
- **Native execution:** Unavailable in this environment because `dotnet` and `pwsh` were not installed.

## Observed product

MediaForge is a compact native Windows converter:
- one `net8.0-windows` WPF executable;
- no NuGet package references;
- one mixed queue for extension-classified image/video/audio inputs;
- local JSON settings;
- FFmpeg/FFprobe child-process conversion;
- PowerShell build/download/package scripts;
- optional Inno Setup installer;
- version `1.0.2` in project and installer;
- no tests, CI configuration, Git metadata, telemetry, database, API, cloud service, or bundled FFmpeg binaries in the archive.

## Static checks run

- Parsed `App.xaml`, `MainWindow.xaml`, `app.manifest`, and `MediaForge.csproj` as XML.
- Compared XAML event-handler references with methods in `MainWindow.xaml.cs`.
- Checked duplicate `x:Name` values.
- Compared project and installer product versions.
- Scanned text files for TODO/FIXME/HACK/XXX markers.
- Inventoried all files and calculated original archive SHA-256.
- Checked availability of .NET and PowerShell runtimes.

Results and limitations are canonical in `/VERIFICATION.md`.

## Strengths observed

1. **Small, understandable architecture.** The current requirements fit one WPF process with models/services; no unnecessary service layer or external application dependency is present.
2. **Safer child-process argument handling.** FFmpeg and FFprobe arguments use `ProcessStartInfo.ArgumentList` with `UseShellExecute=false`, avoiding shell-command concatenation.
3. **Source immutability intent.** The conversion path writes to a unique destination-directory temp file and moves it only after zero exit and non-empty-file checks.
4. **Concurrent destination reservation.** A case-insensitive reserved-path set protects same-batch destination selection.
5. **Cancellation intent.** Active FFmpeg process trees are killed on cancellation.
6. **Bounded diagnostics.** The service retains a 35-line stderr tail while streaming logs to the UI.
7. **Settings recovery intent.** Invalid/unreadable JSON falls back to defaults; saves use a temp file and replacement move.
8. **Packaging failure guards.** Release and installer scripts check native exit codes and expected executable existence.
9. **Third-party separation.** FFmpeg binaries are absent from source and notices disclose configuration-dependent licensing.
10. **Documentation alignment.** README limitations generally match the observed first-frame, software-codec, one-pass-normalisation, and track-selection code.

## Prioritised findings

### High priority

#### `RISK-001` — Build and runtime status is unproven
The archive contains no compiled output or tests. `VERIFICATION.md` previously recorded an inability to compile. This inspection also could not run .NET or WPF. Any “working” or “release-ready” claim is unsupported until `PH-01`.

#### `RISK-002` — Recursive import has no explicit reparse-point policy
`MainWindow.xaml.cs:363-370` creates recursive `EnumerationOptions` and sets only `FileAttributes.System` to skip. Junctions/symlinks/reparse points are not explicitly excluded. Static inspection cannot prove looping, but the policy is ambiguous and can traverse unexpected trees. Define and test it before relying on folder containment.

#### `RISK-003` — Final-output validation is narrower than “valid media”
`MediaConversionService.cs:42-45` requires only file existence and length greater than zero after FFmpeg exits successfully. This is a reasonable baseline commit boundary but does not prove decode, expected streams, duration, dimensions, or semantic fidelity. Documentation now states the precise boundary.

#### `RISK-005` — Close-during-batch cleanup is not awaited
`MainWindow.xaml.cs:640-657` asks for confirmation, calls cancellation, saves settings, and allows window closure immediately. Conversion cleanup is asynchronous. The process may exit before all temp deletion/state updates finish. Reproduce on Windows before classifying the impact.

#### `RISK-006` — Settings service can fail before recovery logic
`SettingsService.cs:13-18` creates `%APPDATA%\MediaForge` in the constructor. If directory creation fails, the later `Load()` catch cannot recover because construction itself failed. Startup behaviour should be validated and likely guarded.

### Medium priority

#### `RISK-004` — FFmpeg acquisition is not reproducible provenance
`scripts/download-ffmpeg.ps1` uses mutable “release essentials” archive/checksum URLs. The SHA-256 is obtained from the same remote origin. This is useful corruption checking but not a pinned release or independent authenticity proof. The UI also starts PowerShell with `-ExecutionPolicy Bypass` after explicit user consent (`MainWindow.xaml.cs:268-328`). Decide the release dependency policy before distribution.

#### `RISK-007` — Codec/container preflight is partial
`MainWindow.xaml.cs:486-501` blocks video stream-copy with transforms and restricts WebM combinations. Other combinations are delegated to FFmpeg. This is acceptable if documented as runtime-dependent, but broad “supports” wording should be tied to a tested matrix.

#### `RISK-008` — Invalid numeric input silently falls back
`MainWindow.xaml.cs:438-445` and `675-680` parse text with fallback defaults. Non-numeric user input can become a valid default without field-level notice. This can produce output different from user intent.

#### `RISK-011` — Accessibility is unproven
Static XAML inspection found no explicit access keys, automation names, tooltips, tab indices, or keyboard-navigation metadata. Adjacent labels may be visually understandable, but programmatic associations and status announcements were not tested.

#### `RISK-012` — Diagnostic logs can disclose local paths
`MediaConversionService.cs:37` logs the full FFmpeg command and `:534` forwards stderr; `MainWindow.xaml.cs:636` displays it. Logs are in-memory, but copied logs/screenshots can expose filenames and directory structure. A support redaction policy is needed before log export is added.

#### `RISK-014` — Cancelled FFprobe is not explicitly killed
`MediaConversionService.cs:463-491` uses a cancellable wait for FFprobe but has no cancellation registration to kill the process tree. FFprobe is normally short-lived; the behaviour is still unproven for blocked/corrupt inputs.

### Lower priority / controlled

#### `RISK-009` — Manual version duplication
`MediaForge.csproj` and `installer/MediaForge.iss` both contain `1.0.2` and match now. Manual updates can drift; the release checklist controls this until automation exists.

#### `RISK-010` — No automated tests or CI
No test project or CI workflow is present. The current small codebase can begin with focused unit/integration tests rather than a large evaluation framework.

#### `RISK-013` — Relative containment test is broad
`MediaConversionService.cs:73-79` rejects relative strings starting with `..`, which safely blocks parent traversal but may flatten a legitimate directory name beginning with two dots. This is rare and should be covered by filesystem edge tests.

#### `RISK-015` — FFmpeg licence/provenance varies by build
The existing notices correctly state that build configuration affects terms. A release that bundles a binary requires exact build review, notices, and hashes.

## Consistency review

- Project and installer versions match at `1.0.2`.
- The manifest identity is `1.0.0.0`; no contradiction is asserted because manifest identity and semantic product version can serve different roles.
- XAML event references all have matching method names.
- No duplicate XAML names were found.
- No TODO/FIXME/HACK/XXX markers were found.
- Existing README feature descriptions are broadly consistent with inspected controls and argument paths.
- Prior verification claims that were not rerun are now retained as historical/unproven rather than current passes.

## Recommended sequence

1. `PH-01`: Windows build, launch, and mixed fixture baseline.
2. `PH-02`: reparse policy, graceful close/cancel, settings constructor recovery, FFprobe cancellation, output validation decision.
3. `PH-03`: numeric input, compatibility matrix, accessibility.
4. `PH-04`: version propagation, FFmpeg provenance/distribution, release artefacts.
5. `PH-05`: regression/CI and release readiness.

## Unproven items

Compilation, WPF runtime behaviour, actual codec availability, every format/container combination, source immutability under real FFmpeg execution, temp cleanup, process termination, settings permissions, performance, memory use, ARM64 operation, ZIP contents, installer behaviour, and third-party binary authenticity remain unproven.
