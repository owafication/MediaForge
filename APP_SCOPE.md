# MediaForge desktop app scope

**Status:** Proposed scope summary. Canonical requirement definitions are in `project_docs/PROJECT_FOUNDATION.md`.

## Current product

- Local Windows WPF application
- Mixed image, video and audio queue
- Batch conversion, output rules, collisions, progress and cancellation
- Image/video crop, resize and placement
- Single-track video trim, split, reorder and stitch
- Local FFmpeg/FFprobe and persistent global settings

Current source presence does not prove native Windows behaviour. `PH-07` is the release/safety gate.

## Next milestone — proposed 1.2.0

- Supported platform and testable architecture seam
- Versioned `.mediaforge` projects
- Manual save/save-as, autosave, recovery, recent projects and relinking
- Optional portable project root
- Built-in and user presets with import/export and field locking
- Deterministic global → preset → job → edit option resolution
- Multi-select queue editing, reorder, enable/disable, priority, duplicate, retry, pause and queue persistence

## Later core

- FFmpeg capability/compatibility engine and canonical processing plan
- FFmpeg-backed preview, thumbnails, waveform and proxies
- Hardware profiles, local benchmark, sample encoding and quality targeting
- Stream, subtitle, chapter and metadata management
- Conservative remux/lossless/partial-copy operations
- Tiered verification, history, reports and support bundles

## Later workflow and polish

- Two-pass audio and analysis
- Animated/multi-page/colour-aware image workflows
- Controlled video filters and HDR/tone-map paths
- Guarded watch folders and completion actions
- Themes, DPI, accessibility, localisation, shell/portable/update/first-run experience

## Excluded unless scope is formally changed

- Unlimited multi-track timeline
- Complex compositing or keyframe animation engine
- Full colour-grading suite
- Cloud accounts, hosted processing or collaboration
- Plugin marketplace
- Generative AI
- DRM circumvention
- Silent updater, unreviewed bundled FFmpeg or destructive source replacement
