# Project foundation

**Purpose:** Canonical identity, users, outcomes, scope, requirements, constraints and acceptance boundary.  
**Read when:** Planning, changing behaviour or resolving scope.  
**Owner:** Product maintainer.  
**Authority:** Canonical owner for `REQ-###`.  
**Update trigger:** Approved outcome, feature, platform, dependency, constraint or boundary change.  
**Project rules:** Local-first, least-complex, evidence-based and non-NLE.  
**Linked IDs:** `REQ-001`–`REQ-055`, `PH-07`–`PH-19`, `DEC-001`–`DEC-028`.

## Identity and users

MediaForge is a Windows WPF application for local image, video and audio conversion, preparation and lightweight single-track assembly. Primary users are Windows users and creators who value private local processing, repeatable workflows and transparent technical control without a full editor.

## Current observation

The inspected 1.1.0 source targets `net8.0-windows`, invokes FFmpeg/FFprobe child processes, stores global settings in JSON and retains edit plans only in memory. It has no application NuGet dependency, database, cloud service, account, telemetry or runtime AI. Static/archive checks exist; native Windows runtime remains unproven.

## Scope classes

- **Current:** functionality represented in 1.1.0 source; runtime claims remain evidence-dependent.
- **Next milestone:** `PH-07`–`PH-10`; baseline, platform seam, projects/recovery, presets/per-job queue.
- **Later core/professional:** capability, preview, hardware, streams, verification, quality and automation phases.
- **Excluded:** full NLE, complex compositing, cloud/collaboration, plugin marketplace, DRM circumvention and generative AI.

## Requirements

| ID | Requirement | Scope | Evidence status |
|---|---|---|---|
| REQ-001 | Run as a native local Windows desktop application using the inspected WPF project. | Current | Source-present; build/launch unproven |
| REQ-002 | Import supported files by picker, recursive folder selection and drag-drop into one duplicate-suppressed queue. | Current | Source-present; runtime unproven |
| REQ-003 | Classify supported image, video and audio paths and ignore unsupported paths without aborting import. | Current | Source-present |
| REQ-004 | Write beside source or to a selected output root, optionally preserving imported folder structure. | Current | Source-present; runtime unproven |
| REQ-005 | Apply suffix and Rename/Overwrite/Skip collision policies without intentionally overwriting source media. | Current safety | Source-present; edge cases unproven |
| REQ-006 | Convert still images using the exposed format, quality and placement/resize controls. | Current | Source-present; compatibility matrix unproven |
| REQ-007 | Convert video using exposed container, codec, quality, resolution, frame-rate and audio controls. | Current | Source-present; compatibility matrix unproven |
| REQ-008 | Convert audio and extract audio from video using exposed format and one-pass loudness controls. | Current | Source-present; compatibility matrix unproven |
| REQ-009 | Keep sources immutable; use unique same-directory temporary output; commit only after declared validation; clean up on failure/cancel. | Current safety | Source-present; Windows proof incomplete |
| REQ-010 | Run bounded parallel jobs with per-file/overall progress, cancellation, status, logs and retry/reset. | Current | Source-present; runtime unproven |
| REQ-011 | Discover and test FFmpeg plus sibling FFprobe and provide an optional consented acquisition helper. | Current dependency | Source-present; provenance decision open |
| REQ-012 | Persist global settings locally and recover to defaults after unreadable JSON or unavailable settings storage. | Current | Source-present; runtime failures unproven |
| REQ-013 | Optionally strip metadata and preserve source timestamps, with timestamp preservation explicitly best-effort. | Current | Source-present |
| REQ-014 | Provide actionable per-file tool, permission, media, path and encoder failures. | Current quality | Partial |
| REQ-015 | Build and package Windows artefacts without presenting failed commands as successful releases. | Current delivery | Scripts present; execution unproven |
| REQ-016 | Maintain traceable governance, validation, debugging, versioning and rollback procedures. | Current governance | Implemented in source pack |
| REQ-017 | Offer common image/video aspect presets and positive custom ratios. | Current editor | Source-implemented; runtime unproven |
| REQ-018 | Preview a selected image/video with a movable eight-handle crop box stored as normalised coordinates. | Current editor | Source-implemented; runtime unproven |
| REQ-019 | Keep ratio-locked crop resizing within media bounds. | Current editor | Source-implemented; runtime unproven |
| REQ-020 | Export centre, fit, stretch, shrink and fill/crop placement at declared dimensions. | Current editor | Source-implemented; fixture evidence limited |
| REQ-021 | Provide play/pause, seek, ±1 s, ±5 s and approximate frame-step controls with a disclosed preview fallback. | Current editor | Source-implemented; Windows decoder behaviour unproven |
| REQ-022 | Trim, split, add, duplicate, remove, reorder and stitch clips in a single-track clip list. | Current editor | Source-implemented; runtime/export unproven |
| REQ-023 | Detect mixed resolutions, suggest output dimensions, normalise edited clips and block paths that bypass active edits. | Current safety | Source-implemented; runtime/export unproven |
| REQ-024 | Establish a native Windows build, launch, fixture-conversion and output-safety baseline before feature expansion. | Next gate | Proposed |
| REQ-025 | Move to a supported long-term .NET/WPF baseline through a measured migration that preserves the validated 1.1.0 behaviour. | Next platform | Proposed; decision gate |
| REQ-026 | Save and load a versioned `.mediaforge` project containing queue, edit, conversion, output and referenced-preset state. | Next milestone | Proposed |
| REQ-027 | Use atomic project writes, dirty tracking, rate-limited autosave and recoverable snapshots. | Next milestone safety | Proposed |
| REQ-028 | Provide recent projects, missing-file relinking, newer-schema read-only handling and optional portable projects. | Next milestone | Proposed |
| REQ-029 | Provide named built-in and user presets grouped by workflow and media purpose. | Next milestone | Proposed |
| REQ-030 | Support preset create, duplicate, rename, organise, delete, default, import/export and field locking. | Next milestone | Proposed |
| REQ-031 | Resolve effective job options deterministically as global default → selected preset → per-job override → edit plan. | Next milestone | Proposed |
| REQ-032 | Provide professional queue control: multi-select bulk edit, reorder, enable/disable, priority, duplicate, retry, pause and independent queue save/load. | Next milestone | Proposed |
| REQ-033 | Discover and cache the selected FFmpeg/FFprobe build capabilities, architecture and version; export a support report. | Later core | Proposed |
| REQ-034 | Preflight container, codec, filter, pixel-format, subtitle and hardware compatibility with user-visible reasons and loss warnings. | Later core | Proposed |
| REQ-035 | Represent each job as one canonical processing plan shared by preview, export, verification and diagnostics. | Later architecture | Proposed |
| REQ-036 | Render FFmpeg-backed transformed preview frames and optional low-resolution proxies while retaining MediaElement as a labelled fast fallback. | Later core | Proposed |
| REQ-037 | Provide thumbnail strip, waveform, clip boundaries, keyframe indicators, zoom/pan, safe areas, alpha checkerboard and before/after comparison. | Later editor | Proposed |
| REQ-038 | Detect hardware encode/decode paths, offer intent-based profiles, benchmark locally, disclose actual path and fall back safely. | Later performance | Proposed |
| REQ-039 | Inspect all video, audio, subtitle, attachment and chapter streams with technical metadata. | Later professional | Proposed |
| REQ-040 | Select, reorder, copy, remove or encode tracks; manage subtitles, chapters, dispositions, language, titles, cover art and metadata policy. | Later professional | Proposed |
| REQ-041 | Plan rewrap, extraction, replacement, keyframe trim, compatible merge, metadata-only and partial stream-copy operations without claiming unsafe losslessness. | Later professional | Proposed |
| REQ-042 | Verify outputs in Fast, Standard and Thorough modes and distinguish conversion success from verification success. | Later trust | Proposed |
| REQ-043 | Maintain privacy-safe conversion history, detailed job reports, capability reports and exportable support bundles. | Later trust | Proposed |
| REQ-044 | Support target size/bitrate, one/two-pass, constant quality, limits, sample encodes, estimates and optional SSIM/VMAF analysis. | Later quality | Proposed |
| REQ-045 | Provide two-pass EBU R128 normalisation, ReplayGain analysis, silence/fade/delay/channel/downmix controls, waveform and clipping reports. | Later audio | Proposed |
| REQ-046 | Preserve supported animated/multi-page image content and manage colour profiles, alpha, metadata, rename templates, overlays and scaling algorithms. | Later image | Proposed |
| REQ-047 | Provide controlled preset-driven deinterlace, denoise, sharpen, deband, stabilisation, colour, LUT, tone-map, speed, delay, fade and overlay filters. | Later video | Proposed |
| REQ-048 | Provide guarded watch-folder rules with stable-file detection, dry run, loop prevention, audit log and no source deletion by default. | Later automation | Proposed |
| REQ-049 | Provide explicit opt-in completion actions only after all jobs and required verification finish. | Later automation | Proposed |
| REQ-050 | Reach desktop release quality through themes, DPI, keyboard, accessibility, queue views, search, consistent confirmation/undo, shell integration, portable mode, update checks, localisation and first-run guidance. | Later polish | Proposed |
| REQ-051 | Remain a conversion, preparation and lightweight single-track assembly tool; exclude a full multi-track NLE and complex compositing. | Product boundary | Proposed accepted boundary |
| REQ-052 | Remain local-first with no account, cloud processing, telemetry, advertising or runtime AI by default. | Product boundary | Proposed accepted boundary |
| REQ-053 | Adopt a recorded FFmpeg acquisition, provenance, licence, update and rollback policy before bundling or automatic updates. | Supply chain | Open decision |
| REQ-054 | Add proportionate automated unit, integration, fixture and release checks before professional features are represented as supported. | Cross-cutting quality | Proposed |
| REQ-055 | Preserve source immutability, deterministic collision handling, process-tree cancellation and verified temporary-output commit through every phase. | Cross-cutting safety | Proposed invariant |

## Constraints

- Windows-only WPF product.
- FFmpeg build and hardware determine actual codec/filter support.
- `MediaElement` decoding is not FFmpeg export evidence.
- No destructive source replacement.
- Unknown compatibility remains unsupported or unproven.
- Project/preset files are data, never executable command sources.
- .NET 8 is in maintenance and ends support on 10 November 2026; migration is a decision-gated phase after baseline.
- Release and installer claims require Windows evidence.

## Success criteria

- Users can preserve and recover long work.
- Effective settings and processing paths are deterministic and visible.
- Preview and export share a canonical plan with measured fidelity.
- Outputs are verified to a declared tier before commit.
- Source files remain unchanged across success, failure and cancellation.
- Every requirement maps to architecture, phase, files, acceptance, validation and status.
