# Capability, compatibility and canonical processing plan

**Purpose:** Canonical FFmpeg discovery, compatibility and per-job plan contracts.  
**Read when:** Implementing `REQ-033`–`REQ-035`, hardware, preview, streams or verification.  
**Owner:** Media pipeline maintainer.  
**Authority:** Capability/compatibility/plan owner.  
**Update trigger:** Tool parser, cache key, compatibility rule, plan field or FFmpeg argument change.  
**Linked IDs:** `PH-11`–`PH-16`, `AC-060`–`AC-064`, `VAL-039`–`VAL-041`, `RISK-028`–`RISK-030`.

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
