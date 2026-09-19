# Capability, compatibility and canonical processing plan

**Purpose:** Canonical FFmpeg discovery, compatibility and per-job plan contracts.
**Read when:** Implementing `REQ-033`–`REQ-035`, hardware, preview, streams or verification.
**Owner:** Media pipeline maintainer.
**Authority:** Capability/compatibility/plan owner.
**Update trigger:** Tool parser, cache key, compatibility rule, plan field or FFmpeg argument change.
**Linked IDs:** legacy `PH-11`–`PH-16`, `AC-060`–`AC-078`, `VAL-039`–`VAL-053`, `RISK-028`–`RISK-041`; V2 `REQ-060`–`REQ-065`, `PH-23`–`PH-30`, `AC-093`–`AC-101`, `VAL-068`–`VAL-078`, `RISK-051`–`RISK-055`.

## Capability evidence

Collect with the selected executable pair:

- FFmpeg/FFprobe path, size, last-write UTC, architecture, version and configuration line
- encoders, decoders and codecs
- muxers/demuxers
- filters
- hardware accelerators/devices discoverable by CLI
- pixel formats and relevant subtitle formats
- protocols only for diagnostics; network protocols are not enabled as product inputs by default

Use machine-readable or stable tabular output where possible. Parsing must tolerate whitespace and not depend on a single localised error sentence.

Cache key includes executable canonical path, file size, last-write UTC, version/configuration and optionally binary hash. Cache is invalidated on mismatch and can be manually refreshed. Capability report export is privacy-reviewed.

## Compatibility layers

1. **Component availability:** encoder/filter/muxer exists.
2. **Static rule:** known container/codec/pixel/subtitle/alpha/HDR constraints.
3. **Source-specific rule:** input streams, edit plan and metadata policy.
4. **Generated micro-fixture:** optional proof for combinations that cannot be determined safely from listings.
5. **Unknown:** visible as unknown; never promoted to supported.

The engine must explain disabled options and distinguish unavailable, incompatible, risky, untested and supported-by-current-evidence.

## Canonical `ProcessingPlan`

Minimum sections:

```text
planVersion / jobId / source fingerprints
input streams and selected policies
trim/crop/placement/filter intent
target container and output path policy
video/audio/subtitle/data/attachment stream actions
metadata/chapter/cover-art policy
hardware decode/filter/encode path
expected output contract
verification tier and tolerances
estimated resources and warnings
compiled FFmpeg invocation evidence (generated at run time)
```

Stream actions are `Copy`, `Encode`, `Remove`, `Generate`, `Burn`, `Attach` or `Unsupported`. The UI summary and diagnostic report are derived from these actions.

## Compilation

The plan builder is pure/testable. FFmpeg compilation uses `ArgumentList`, not shell strings. Preview/export/verifier may compile different commands from the same plan but cannot reinterpret user intent. Golden/snapshot tests cover plan and argument changes. Sensitive paths are redacted only in exported copies, never in the executable invocation.

## Loss warnings

Preflight surfaces expected alpha loss, HDR/colour changes, bit-depth reduction, metadata stripping, attachment/chapter removal, subtitle conversion and forced CPU transfers. Warnings require acknowledgement only where risk is material; routine informational notices remain non-blocking.

## MediaForge 2 WorkflowIntent contract

V2 resolves user intent through one path:

`WorkflowIntent -> capability/compatibility -> immutable ProcessingPlan`

The ProcessingPlan is the authority for:

- visible technical summary;
- compatibility/loss warnings;
- estimates;
- preview transform intent;
- executable FFmpeg compilation;
- active-job snapshot;
- verification expectation;
- diagnostics.

The UI never constructs raw FFmpeg command fragments.

Normal quality intents are `Smaller file`, `Balanced`, `Higher quality`, `Custom`; their technical mapping is codec/capability-dependent.

Normal geometry intents are:

- **Fit within:** proportional bounding-box resize; no crop; no padding.
- **Fit in frame:** proportional exact-frame output with explicit padding/background.
- **Fill frame:** proportional exact-frame output with crop.
- **Stretch:** exact dimensions with distortion, explicit only.

Mixed batches use explicit target policies for dimensions, aspect and frame rate. Every file has a pre-run plan summary; incompatible items show a reason and declared skip/block/decision state rather than silently altering global intent.

Output-size estimates are advisory. Fixed-bitrate paths may use narrow approximations; CRF/CQ/content-dependent paths use ranges/confidence unless stronger evidence exists.

Changes to pending UI state after Start cannot mutate the immutable active ProcessingPlan.
