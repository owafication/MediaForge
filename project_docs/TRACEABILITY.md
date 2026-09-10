# Traceability and validation catalogue

**Purpose:** Canonical acceptance criteria, validation definitions and requirement mapping.  
**Read when:** Implementing, testing, reviewing completion or changing requirements.  
**Owner:** Technical maintainer.  
**Authority:** Canonical owner for `AC-###`, `VAL-###` and mapping.  
**Update trigger:** Requirement, phase, acceptance, validation, file ownership or status change.  
**Linked IDs:** `REQ-001`–`REQ-055`, `AC-001`–`AC-087`, `VAL-001`–`VAL-065`, `PH-07`–`PH-19`.

## Acceptance criteria

| ID | Criterion |
|---|---|
| AC-001 | Project root, governance root, routing, ownership, IDs, evidence and stop conditions are documented. |
| AC-002 | Canonical documents do not duplicate definitions or claim unrun runtime results. |
| AC-003 | The prior supplied source can be restored and the implementation change has a defined rollback. |
| AC-004 | The project restores and builds Release on the declared Windows/.NET environment with zero errors. |
| AC-005 | The app launches/closes without unhandled exception with absent, valid and malformed settings. |
| AC-006 | One image, video and audio file can be queued and processed together with declared tools. |
| AC-007 | Source hashes remain unchanged for successful, failed, skipped, overwrite-target and cancelled jobs. |
| AC-008 | Job state, message, output, progress, overall progress and log reflect actual results. |
| AC-009 | Settings persist/reload with documented defaults and failure behaviour. |
| AC-010 | Recursive import has an explicit tested reparse/junction/symlink policy and terminates. |
| AC-011 | Cancelled FFmpeg/FFprobe processes terminate and temp files are removed or reported. |
| AC-012 | Closing during work follows a defined cleanup contract without ambiguous final output. |
| AC-013 | Rename, Skip and Overwrite are deterministic for existing and concurrently targeted names. |
| AC-014 | Folder-tree preservation cannot escape output root and handles declared path cases. |
| AC-015 | Final commit validation is explicitly defined and claims only that boundary. |
| AC-016 | Invalid numeric input is rejected/corrected visibly; no silent fallback changes conversion intent. |
| AC-017 | Declared image/audio/video combinations pass fixtures; untested combinations are not marketed as supported. |
| AC-018 | Reliably detectable stream-copy/transform incompatibilities are blocked before conversion. |
| AC-019 | Core workflow is keyboard-operable with visible focus and labelled controls. |
| AC-020 | Approved screen-reader/automation and scaling baseline passes. |
| AC-021 | Product, installer, changelog and artefact versions agree. |
| AC-022 | FFmpeg acquisition/distribution records version, source, hash and notices. |
| AC-023 | x64 ZIP contains expected content and excludes secrets/media/generated build directories. |
| AC-024 | ARM64 is claimed only after ARM64 build/launch/fixture evidence. |
| AC-025 | Installer is claimed only after compile/install/launch/uninstall/privilege/residue checks. |
| AC-026 | Mandatory regression is repeatable from a clean checkout/copy. |
| AC-027 | Corrupt, locked, unwritable, long-path, low-space and missing-tool failures are actionable. |
| AC-028 | Shared logs/support artefacts follow approved path-redaction and retention policy. |
| AC-029 | Release documentation states limitations and unproven combinations. |
| AC-030 | A prior validated artefact/settings rollback procedure exists. |
| AC-031 | Image and video controls expose all declared common ratios plus positive custom ratios. |
| AC-032 | Opening one supported image/video creates an editor without changing the source or starting export. |
| AC-033 | The selection can move and resize from eight handles and remains inside visible media bounds. |
| AC-034 | Ratio lock preserves selection ratio within tolerance through resizing and boundary contact. |
| AC-035 | Applied crop is stored as clamped normalised coordinates and reproduces the intended source region at export. |
| AC-036 | Centre, fit, stretch, shrink and fill/crop produce documented target geometry. |
| AC-037 | Video preview provides play/pause, seek, skips and approximate frame stepping within trim bounds. |
| AC-038 | Unsupported Windows preview decoding displays a clear fallback and does not prevent FFmpeg export. |
| AC-039 | Trim start/end, reset and split preserve valid non-empty ranges and expected clip order. |
| AC-040 | Clips can be added, duplicated, removed, reordered and dropped while at least one clip remains. |
| AC-041 | Resolution mismatch is detected and target guidance explains Fit/Crop/Stretch trade-offs. |
| AC-042 | Edited video export normalises streams and concatenates retained ranges in order. |
| AC-043 | Audio-only extraction and stream-copy paths that bypass active edits are rejected before start. |
| AC-044 | The refined 1.1.0 source builds, launches, runs the mandatory safety fixture matrix and produces hashed reviewable artefacts on Windows. |
| AC-045 | A platform migration, when approved, preserves baseline behaviour, settings compatibility, packaging and rollback evidence. |
| AC-046 | A saved project round-trips queue order, source references, edit plans, options, outputs and referenced presets without silent loss. |
| AC-047 | Project and recovery writes are atomic; an interrupted write leaves either the prior valid file or a recoverable snapshot. |
| AC-048 | Dirty state and rate-limited autosave produce discoverable recovery without overwriting the last manual save. |
| AC-049 | Recent-project and relink workflows handle moved, missing, duplicate and changed source files explicitly. |
| AC-050 | A newer schema opens read-only with a clear warning; unknown fields are not silently destroyed during supported round-trips. |
| AC-051 | Portable projects resolve relative paths inside their root and never escape it through traversal. |
| AC-052 | Built-in and user presets support declared CRUD, grouping and default behaviour without mutating read-only built-ins. |
| AC-053 | Preset import/export validates schema and values and never accepts raw executable arguments as trusted configuration. |
| AC-054 | Locked fields and the global→preset→job→edit precedence chain are visible, deterministic and testable. |
| AC-055 | Two queue jobs may use different effective output options in the same batch and report those options independently. |
| AC-056 | Multi-select edit, reorder, enable/disable, priority, duplicate and retry preserve stable queue identity and state. |
| AC-057 | Pause after current and pause/resume do not abandon running work or start new jobs while paused. |
| AC-058 | Queue save/load round-trips enabled state, order, priority, overrides and source fingerprints independently of projects. |
| AC-059 | Time and size estimates are labelled estimates, update from evidence and never block processing solely because they are unavailable. |
| AC-060 | Capability discovery records version, architecture, encoders, decoders, formats, filters, hardware paths, pixel/subtitle formats and cache key. |
| AC-061 | Unavailable options are hidden or disabled with a reason; cache invalidates when the selected tool build changes. |
| AC-062 | Compatibility preflight rejects known invalid combinations and discloses alpha, HDR, bit-depth and metadata-loss risks. |
| AC-063 | Every job displays a stream-level processing summary: copied, encoded, removed, generated or unsupported. |
| AC-064 | Preview, export, verification and diagnostics consume the same immutable canonical processing plan. |
| AC-065 | FFmpeg-backed preview reproduces declared crop/scale/pad/orientation/colour transforms within measured tolerance. |
| AC-066 | Preview state is labelled Matched, Approximate, Proxy, Fallback or Unavailable and never implies unmeasured frame accuracy. |
| AC-067 | Thumbnail, waveform, keyframe and proxy generation is cancellable, bounded and leaves no unreported orphan process/cache. |
| AC-068 | Hardware profiles disclose actual decode/filter/encode path, benchmark evidence and software fallback. |
| AC-069 | The inspector represents every discovered stream, attachment and chapter with stable identity and technical metadata. |
| AC-070 | Track, subtitle, chapter and metadata changes compile to an explicit mapping and survive save/load. |
| AC-071 | Lossless or partial-copy operations run only after compatibility checks and show the exact per-stream plan before start. |
| AC-072 | Fast, Standard and Thorough verification apply their declared checks and produce distinct verified/warning/failed states. |
| AC-073 | Verification failure cannot silently replace a valid destination; retained failed output requires explicit user choice. |
| AC-074 | History, job reports, capability reports and support bundles are bounded, exportable and privacy-redacted. |
| AC-075 | Sample encode and quality targeting report selected interval, effective settings, estimated size and comparison evidence. |
| AC-076 | Two-pass loudness uses measured first-pass values and reports achieved LUFS, true peak and warnings. |
| AC-077 | Advanced image paths preserve or explicitly warn about animation, pages, alpha, orientation, ICC profile, depth and metadata. |
| AC-078 | Advanced video filters are ordered through named controls/presets and report CPU/GPU transitions and colour/HDR assumptions. |
| AC-079 | Watch-folder rules wait for stable files, ignore partials, prevent loops, support dry run and never delete sources by default. |
| AC-080 | Completion actions require explicit opt-in and run only after all jobs and required verification reach terminal states. |
| AC-081 | Declared themes, DPI scales, keyboard paths, accessible names, queue views, confirmation/undo behaviour and localisation resources pass the UI baseline. |
| AC-082 | Update checking and first-run tool acquisition require consent, provenance, integrity checks, rollback and clear release notes. |
| AC-083 | No delivered feature introduces an unlimited multi-track timeline, complex compositing, cloud dependency or generative AI by implication. |
| AC-084 | Local media and paths remain local unless the user explicitly invokes a separately approved network action. |
| AC-085 | The selected FFmpeg acquisition/distribution path records source, licence/configuration, version, architecture and cryptographic hash. |
| AC-086 | Automated tests and fixtures fail on regression in source immutability, collision, cancellation, project persistence, processing plans and verification. |
| AC-087 | A release claim is backed by clean-build, fixture, accessibility, package, installer and rollback evidence for each declared architecture. |

## Validation definitions

| ID | Validation |
|---|---|
| VAL-001 | Parse root XAML, manifest and project XML. |
| VAL-002 | Check XAML event handlers exist and `x:Name` values are unique. |
| VAL-003 | Run `dotnet restore` and Release build on declared Windows/.NET environment. |
| VAL-004 | Launch/close smoke test with absent, valid and malformed settings. |
| VAL-005 | Mixed queue/import/conversion fixture test. |
| VAL-006 | Cancellation, close-during-work, child-process and temp-cleanup test. |
| VAL-007 | Rename/Skip/Overwrite and concurrent destination test. |
| VAL-008 | Beside-source/output-root/tree-containment test. |
| VAL-009 | UI numeric and preflight validation test. |
| VAL-010 | Image/video/audio/extraction compatibility fixture matrix. |
| VAL-011 | Settings load/save/permission/corruption/concurrency test. |
| VAL-012 | FFmpeg discovery/test/helper download/hash/failure/cleanup check. |
| VAL-013 | x64 publish and ZIP content/hash check. |
| VAL-014 | ARM64 publish, launch and fixture check. |
| VAL-015 | Installer compile/install/launch/uninstall/privilege/residue check. |
| VAL-016 | Keyboard, focus, scaling, automation-name and screen-reader test. |
| VAL-017 | Source/archive inventory, excluded-file and checksum check. |
| VAL-018 | Version consistency across project, installer, changelog and artefact names. |
| VAL-019 | Filesystem edge cases: reparse, unicode, dot names, long paths, locked/unwritable/low-space. |
| VAL-020 | Log privacy/redaction and support-evidence review. |
| VAL-021 | Parse editor XAML; verify handlers, names and source inventory. |
| VAL-022 | Image editor load/move/eight-handle/ratio/centre/reset/apply/reopen fixture. |
| VAL-023 | Crop/placement export geometry fixture matrix. |
| VAL-024 | CFR/VFR preview controls and documented frame-step tolerance. |
| VAL-025 | Trim/split/stitch matrix with repeated clips, order, missing audio and mixed media properties. |
| VAL-026 | Probe edited output streams, dimensions, duration and A/V sync against tolerances. |
| VAL-027 | Preview fallback using media FFmpeg supports but MediaElement cannot decode. |
| VAL-028 | Rotation/orientation, SAR and odd-dimension preview/export parity fixtures. |
| VAL-029 | Edit-plan lifetime, clear-edits, non-persistence, cancellation and source-hash test. |
| VAL-030 | Native Windows 1.1.0 restore/build/launch/close baseline with complete logs. |
| VAL-031 | Mandatory source immutability, temp-commit, collision, cancellation and output-safety fixtures. |
| VAL-032 | Supported .NET migration clean build, launch, settings and fixture regression comparison. |
| VAL-033 | Project schema serialisation, round-trip, deterministic output and migration tests. |
| VAL-034 | Atomic save, interrupted-write, autosave throttling, snapshot retention and recovery tests. |
| VAL-035 | Recent project, relink, changed fingerprint, duplicate candidate, newer schema and portable-root tests. |
| VAL-036 | Preset CRUD, built-in immutability, import/export, invalid input, locked field and default tests. |
| VAL-037 | Effective-option precedence, multi-select, reorder, enable, priority, duplicate, retry and pause tests. |
| VAL-038 | Queue save/load and independent project/queue migration tests. |
| VAL-039 | FFmpeg/FFprobe discovery output parser, cache-key, invalidation and report export tests. |
| VAL-040 | Compatibility rule and generated micro-fixture matrix across declared containers/codecs/filters/hardware. |
| VAL-041 | Canonical processing-plan snapshot/golden tests and preview/export/verifier consumption checks. |
| VAL-042 | FFmpeg transformed-frame preview parity for crop, scale, pad, orientation, SAR, alpha and colour fixtures. |
| VAL-043 | Preview proxy/fallback/fidelity labels, seek tolerance, cancellation, cache cleanup and memory tests. |
| VAL-044 | Hardware benchmark on declared NVIDIA/Intel/AMD/software environments with failure and fallback cases. |
| VAL-045 | Probe and render media-inspector fixtures containing multiple streams, attachments, chapters, HDR and dispositions. |
| VAL-046 | Track selection/reorder, subtitle add/burn/remux, chapter and metadata round-trip fixtures. |
| VAL-047 | Rewrap/extract/replace/keyframe-trim/merge/metadata/partial-copy compatibility and timestamp tests. |
| VAL-048 | Fast/Standard/Thorough verification tests including corrupt, truncated, tiny, wrong-stream and discontinuity outputs. |
| VAL-049 | History retention, report content, privacy redaction and support-bundle inventory tests. |
| VAL-050 | Target-size, bitrate, two-pass, constant-quality, sample encode, estimate and optional metric fixtures. |
| VAL-051 | Two-pass loudness, ReplayGain, silence/fade/delay/channel/downmix and clipping-report fixtures. |
| VAL-052 | Animated/multi-page/ICC/alpha/depth/metadata/image-rename/overlay/scaler fixtures. |
| VAL-053 | Deinterlace/denoise/sharpen/deband/stabilise/LUT/tone-map/speed/delay/fade/overlay filter fixtures. |
| VAL-054 | Watch-folder stable-file, partial-extension, duplicate-loop, dry-run, audit and restart tests. |
| VAL-055 | Completion notification/sound/open-folder/power/command/webhook/report ordering and consent tests. |
| VAL-056 | Theme, DPI, multi-monitor, keyboard, focus, Accessibility Insights/screen reader, queue view and localisation tests. |
| VAL-057 | First-run, tool discovery/acquisition, portable mode, shell integration and updater provenance/rollback tests. |
| VAL-058 | Clean-copy automated unit/integration suite and deterministic fixture generation. |
| VAL-059 | Publish, package, installer, install/launch/uninstall, privilege, residue, version, hash and rollback tests. |
| VAL-060 | Project/preset/queue schema fuzz, unknown-field preservation and forward/backward compatibility tests. |
| VAL-061 | Queue concurrency, pause/cancel/close/restart and state-recovery stress tests. |
| VAL-062 | Long-batch performance, process-count, memory, disk-space and cache-retention soak test. |
| VAL-063 | Cross-phase source-hash, destination containment, temp cleanup and privacy regression suite. |
| VAL-064 | Manual support-bundle review against approved redaction and third-party notice policy. |
| VAL-065 | Governance manifest, immutable ID, link, version, changelog, evidence and archive checksum audit. |

## Requirement mapping

| Requirement | Scope | Architecture | Phase | Primary files | Acceptance | Validation | Status |
|---|---|---|---|---|---|---|---|
| REQ-001 | Current | WPF single process | PH-01/PH-07 | project, app, windows | AC-004, AC-005, AC-044 | VAL-003, VAL-004, VAL-030 | Source-present; unproven |
| REQ-002 | Current | Import/queue | PH-01/PH-07 | main window, classifier | AC-006, AC-010 | VAL-005, VAL-019, VAL-031 | Source-present; unproven |
| REQ-003 | Current | Classifier | PH-01/PH-07 | classifier | AC-006, AC-017 | VAL-005, VAL-010 | Source-present; unproven |
| REQ-004 | Current | Output/tree | PH-01/PH-07 | UI, conversion | AC-014 | VAL-008, VAL-019, VAL-031 | Source-present; unproven |
| REQ-005 | Current safety | Destination/collision | PH-02/PH-07 | conversion | AC-007, AC-013 | VAL-007, VAL-031 | Source-present; unproven |
| REQ-006 | Current | Image conversion | PH-03/PH-07 | UI/settings/conversion | AC-017, AC-036 | VAL-010, VAL-023, VAL-031 | Source-present; unproven |
| REQ-007 | Current | Video conversion | PH-03/PH-07 | UI/settings/conversion | AC-017, AC-018, AC-042 | VAL-009, VAL-010, VAL-025, VAL-026, VAL-031 | Source-present; unproven |
| REQ-008 | Current | Audio/extraction | PH-03/PH-07 | UI/settings/conversion | AC-017 | VAL-010, VAL-031 | Source-present; unproven |
| REQ-009 | Current safety | Temp/commit/cancel | PH-02/PH-07 | conversion | AC-007, AC-011, AC-012, AC-015 | VAL-006, VAL-007, VAL-029, VAL-031 | Source-present; unproven |
| REQ-010 | Current | Batch/state | PH-02/PH-07 | main window/job | AC-008, AC-011 | VAL-005, VAL-006, VAL-031 | Source-present; unproven |
| REQ-011 | Dependency | Tool discovery | PH-04/PH-07/PH-11 | locator/UI/script | AC-022, AC-060, AC-085 | VAL-012, VAL-039, VAL-057 | Source-present; decision open |
| REQ-012 | Current | Local settings | PH-02/PH-07 | settings/UI | AC-005, AC-009 | VAL-004, VAL-011 | Source-present; unproven |
| REQ-013 | Current | Metadata/timestamps | PH-03/PH-07 | conversion/UI | AC-006, AC-007 | VAL-005, VAL-010, VAL-031 | Source-present; unproven |
| REQ-014 | Quality | Errors/fallback | PH-03/PH-07 | UI/services | AC-008, AC-027, AC-038 | VAL-005, VAL-019, VAL-027, VAL-030 | Partial |
| REQ-015 | Delivery | Package/installer | PH-05/PH-07/PH-19 | project/scripts/installer | AC-021–AC-025, AC-087 | VAL-013–VAL-015, VAL-018, VAL-059 | Unproven |
| REQ-016 | Governance | Routing/evidence | PH-00/all | governance | AC-001–AC-003, AC-026, AC-029, AC-030 | VAL-001, VAL-002, VAL-017, VAL-018, VAL-065 | Implemented/proposed update |
| REQ-017 | Editor | Ratio controls | PH-06/PH-07 | XAML/code/settings | AC-031 | VAL-009, VAL-021, VAL-022 | Source-implemented; unproven |
| REQ-018 | Editor | Preview/crop model | PH-06/PH-07 | editor, edit plan | AC-032, AC-033, AC-035 | VAL-021–VAL-023 | Source-implemented; unproven |
| REQ-019 | Editor | Ratio-constrained selection | PH-06/PH-07 | editor | AC-034 | VAL-022 | Source-implemented; unproven |
| REQ-020 | Editor | Placement filters | PH-06/PH-07 | editor, conversion | AC-036 | VAL-023, VAL-028 | Source-implemented; unproven |
| REQ-021 | Editor | MediaElement preview | PH-06/PH-07/PH-12 | editor, probe | AC-037, AC-038, AC-066 | VAL-024, VAL-027, VAL-028, VAL-043 | Source-implemented; unproven |
| REQ-022 | Editor | Single-track clip list | PH-06/PH-07 | editor, edit plan | AC-039, AC-040 | VAL-024, VAL-025, VAL-029 | Source-implemented; unproven |
| REQ-023 | Editor safety | Normalise/guards | PH-06/PH-07 | editor, main, conversion | AC-041–AC-043 | VAL-009, VAL-025, VAL-026, VAL-031 | Source-implemented; unproven |
| REQ-024 | Next gate | Build/safety baseline | PH-07 | source, scripts, tests, evidence | AC-044 | VAL-030, VAL-031, VAL-058, VAL-059, VAL-063 | Proposed |
| REQ-025 | Next platform | Supported .NET + architecture seam | PH-08 | project, services, ViewModels, tests | AC-045, AC-086 | VAL-032, VAL-058, VAL-063 | Native .NET 10 build and 37 tests passed; launch rerun pending after binding fix |
| REQ-026 | Current validation | Project persistence | PH-09 | project DTO/service/session/UI | AC-046, AC-050 | VAL-033, VAL-060 | Implemented in source; native evidence pending |
| REQ-027 | Current safety validation | Atomic save/recovery | PH-09 | ProjectService, recovery store | AC-047, AC-048 | VAL-034, VAL-061 | Implemented in source; interruption evidence pending |
| REQ-028 | Current validation | Recent/relink/portable | PH-09 | project service/dialogs | AC-049–AC-051 | VAL-035, VAL-060 | Implemented in source; native UI evidence pending |
| REQ-029 | Current validation | Preset catalogue | PH-10 | preset model/service/UI | AC-052 | VAL-036 | Catalogue tests passed; native WPF UI/launch evidence pending |
| REQ-030 | Current validation | Preset lifecycle | PH-10 | preset service/UI | AC-052–AC-054 | VAL-036, VAL-060 | Import/compatibility characterisation passed; native UI evidence pending |
| REQ-031 | Current validation | Effective option resolver | PH-08/PH-10 | resolver, job snapshot, UI | AC-054, AC-055 | VAL-037, VAL-041 | Typed precedence/run-snapshot tests passed; native conversion batch evidence pending |
| REQ-032 | Current validation | Professional queue | PH-10 | queue coordinator/ViewModel/service | AC-056–AC-059 | VAL-037, VAL-038, VAL-061 | Queue characterisation passed; native UI/pause/race/package evidence pending |
| REQ-033 | Later core | Capability discovery | PH-11 | capability service/cache/report | AC-060, AC-061 | VAL-039 | Proposed |
| REQ-034 | Later core | Compatibility engine | PH-11 | compatibility rules/UI | AC-061–AC-063 | VAL-040 | Proposed |
| REQ-035 | Later architecture | Canonical plan | PH-11 | ProcessingPlan/compiler | AC-063, AC-064 | VAL-041 | Proposed |
| REQ-036 | Later core | FFmpeg preview | PH-12 | preview service/cache/editor | AC-065–AC-067 | VAL-042, VAL-043 | Proposed |
| REQ-037 | Later editor | Editor evidence surfaces | PH-12 | editor UI/preview cache | AC-067 | VAL-043 | Proposed |
| REQ-038 | Later performance | Hardware paths | PH-13 | benchmark/path planner/UI | AC-068 | VAL-044 | Proposed |
| REQ-039 | Later professional | Media inspector | PH-14 | probe/domain/inspector | AC-069 | VAL-045 | Proposed |
| REQ-040 | Later professional | Track and metadata editor | PH-14 | stream policy/compiler/UI | AC-070 | VAL-046 | Proposed |
| REQ-041 | Later professional | Lossless/partial-copy plan | PH-14 | compatibility/plan/compiler | AC-071 | VAL-047 | Proposed |
| REQ-042 | Later trust | Verification tiers | PH-15 | verification service/job states | AC-072, AC-073 | VAL-048 | Proposed |
| REQ-043 | Later trust | History/reports/support | PH-15 | history/report service/UI | AC-074 | VAL-049, VAL-064 | Proposed |
| REQ-044 | Later quality | Quality/sample encode | PH-13 | quality planner/sample service | AC-075 | VAL-050 | Proposed |
| REQ-045 | Later audio | Professional audio | PH-16 | audio analysis/plan/UI | AC-076 | VAL-051 | Proposed |
| REQ-046 | Later image | Professional image | PH-16 | image probe/plan/UI | AC-077 | VAL-052 | Proposed |
| REQ-047 | Later video | Controlled filters | PH-16 | filter catalogue/plan/UI | AC-078 | VAL-053 | Proposed |
| REQ-048 | Later automation | Watch folders | PH-17 | watch service/rules/UI | AC-079 | VAL-054 | Proposed |
| REQ-049 | Later automation | Completion actions | PH-17 | completion service/settings/UI | AC-080 | VAL-055 | Proposed |
| REQ-050 | Later polish | Desktop quality | PH-18 | views/resources/services/installer | AC-081, AC-082, AC-087 | VAL-056, VAL-057, VAL-059 | Proposed |
| REQ-051 | Boundary | Non-NLE scope | All | foundation/product direction | AC-083 | VAL-065 | Proposed accepted |
| REQ-052 | Boundary | Local-first privacy | All | architecture/security | AC-084 | VAL-063, VAL-064 | Proposed accepted |
| REQ-053 | Supply chain | Tool provenance | PH-07/PH-11/PH-18/PH-19 | locator/helper/installer/docs | AC-085 | VAL-012, VAL-039, VAL-057, VAL-059, VAL-064 | Open decision |
| REQ-054 | Quality | Automated evidence | PH-08 onward | tests/fixtures/scripts | AC-086, AC-087 | VAL-058–VAL-065 | Expanded source-implemented suite; native execution pending |
| REQ-055 | Safety invariant | Source/output/process integrity | All | conversion/process/project/watch paths | AC-007, AC-011–AC-015, AC-047, AC-073, AC-079 | VAL-031, VAL-034, VAL-048, VAL-054, VAL-061, VAL-063 | Proposed invariant |

## UI flow mapping

| Flow | Screen | Route | State | Permission | Fallback | Validation |
|---|---|---|---|---|---|---|
| Open/recover project | Start/recovery dialog | ROUTE-002 | recent/snapshot/newer schema | local file read/write | read-only or new project | VAL-033–VAL-035 |
| Relink sources | Relink dialog | ROUTE-002 | missing/changed/ambiguous fingerprint | local file/folder picker | leave unresolved/disable affected jobs | VAL-035 |
| Manage presets | Preset manager | ROUTE-003 | built-in/user/imported/locked | local file import/export | copy built-in or reject invalid | VAL-036 |
| Bulk queue edit | Main queue | ROUTE-003 | selected pending jobs | local state | disable command with reason | VAL-037–VAL-038 |
| Inspect compatibility | Job drawer | ROUTE-004 | capability cache + plan | FFmpeg/FFprobe process | Unknown/disabled reason | VAL-039–VAL-041 |
| Preview edit | Editor | ROUTE-005 | plan + preview fidelity | source read + child process | MediaElement fallback/unavailable | VAL-042–VAL-043 |
| Run/verify | Queue runner | ROUTE-008 | immutable run plan | output write + child process | failure preserves prior destination | VAL-048 |
| Watch folder | Automation settings | ROUTE-010 | authorised rule + ledger | folder read/write by rule | dry run/disable rule | VAL-054 |
| Update/tools | First-run/settings | ROUTE-011/013 | manifest/provenance/consent | network only after consent | manual tool selection/current version | VAL-057 |

## Runtime AI contract

Not applicable. Runtime AI is excluded by `REQ-052` and `DEC-027`. Adding it requires a new use case, prompt/schema/model/tool/grounding/failure/evaluation contract and privacy decision.
