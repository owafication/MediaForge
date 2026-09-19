# MediaForge desktop app scope

**Status:** Canonical scope summary. Requirement definitions remain in `project_docs/PROJECT_FOUNDATION.md`.

## V1 baseline

MediaForge is a local Windows WPF application using local FFmpeg/FFprobe child processes for image, video and audio conversion/preparation plus lightweight single-track assembly.

The durable V1 rollback point is `5acaf88e751327eac47ca673178fbbd88a8603f1`.

PH-08 through PH-10 source includes the .NET 10 architecture seam, projects/recovery, presets, deterministic per-job option resolution and professional queue controls. The automated Windows x64 baseline passed for its recorded scope; deferred manual/native interaction and real conversion-safety fixtures remain Skipped/Unproven under `BR-20260919-01`.

## MediaForge 2 active scope

Primary flow:

`Media -> Task -> Configure -> Review/Preview -> Output -> Process -> Result`

Primary tasks:

- Convert
- Resize
- Crop & Resize
- Trim / Split
- Combine

V2 keeps projects/presets/queue capability without making them prerequisites for ordinary one-off work. Batch is a property of applicable task workflows. WorkflowIntent and one immutable ProcessingPlan become the authority between UI intent and FFmpeg execution.

## Roadmap

- `PH-20`: active canonical governance/design adoption.
- `PH-21`: immutable V1 closure/rollback bridge, already satisfied before PH-20 adoption.
- `PH-22`–`PH-31`: active V2 implementation roadmap.
- `PH-11`–`PH-19`: historical superseded sequencing; identifiers are not reused and useful technical scope is redistributed into V2.

The exact V2 product/package version remains unresolved.

## Excluded unless scope is formally changed

- Unlimited multi-track timeline
- Complex compositing or keyframe animation engine
- Full colour-grading suite
- Cloud accounts, hosted processing or collaboration
- Plugin marketplace
- Generative AI
- DRM circumvention
- Telemetry or advertising
- Background service architecture
- Silent updater, unreviewed bundled FFmpeg or destructive source replacement
