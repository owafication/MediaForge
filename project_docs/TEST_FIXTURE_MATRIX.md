# Test fixture matrix

**Purpose:** Canonical non-sensitive media and filesystem fixture categories.  
**Read when:** Implementing conversion, preview, persistence, verification or release tests.  
**Owner:** Reliability maintainer.  
**Authority:** Fixture scope owner.  
**Update trigger:** New supported media property, feature, risk or compatibility claim.

## Baseline media

| Category | Required variants | Primary validations |
|---|---|---|
| Still image | odd/even dimensions, portrait/landscape, alpha, EXIF rotation, ICC profile, corrupt | `VAL-010`, `VAL-022`, `VAL-023`, `VAL-028`, `VAL-052` |
| Animated/multi-page | GIF/WebP/AVIF animation, TIFF pages | `VAL-052` |
| Video | CFR/VFR, short/long GOP, rotated, SAR, odd size, with/without audio, mixed resolutions/FPS | `VAL-024`–`VAL-028`, `VAL-042`–`VAL-048` |
| Audio | mono/stereo/multichannel, clipping, silence, delay, known LUFS/sample rate | `VAL-010`, `VAL-051` |
| Multi-stream | multiple audio/subtitle tracks, chapters, cover art, attachments, dispositions, HDR | `VAL-045`, `VAL-046`, `VAL-048` |
| Lossless/remux | matching/mismatching codec parameters, keyframe/non-keyframe trims, timestamp offsets | `VAL-047`, `VAL-048` |

## Filesystem and lifecycle

- same stem from different folders
- existing destination under Rename/Skip/Overwrite
- output root inside import tree
- reparse/junction/symlink tree
- Unicode, dot-prefixed and long paths
- read-only/locked source; locked/unwritable destination
- low disk space and interrupted write
- cancellation during probe, preview, sample, first/second pass, conversion and verification
- close during batch/project save/recovery
- settings/AppData unavailable

## Persistence

- empty/small/large projects
- duplicate sources and repeated clips
- moved/renamed sources with matching and changed fingerprints
- multiple ambiguous relink candidates
- older/newer/corrupt/partially written schema
- unknown extension fields
- preset locked fields and invalid imported values
- queue state with disabled, failed, running-at-crash and prioritised jobs

## Hardware and compatibility

Declare each tested Windows version, CPU/GPU/driver, FFmpeg build/hash and encoder/filter path. Include software-only and failed hardware initialisation. No untested GPU family is implied supported.

## Verification faults

Generate non-empty but corrupt/truncated output, wrong dimensions, missing stream, wrong duration, wrong audio layout, unexpectedly tiny file, timestamp discontinuity and decode failure. Confirm terminal state and destination preservation.

## Fixture governance

Store generator scripts and small generated fixtures only when repository size/licence permits. Otherwise store reproducible commands and hashes. Never include user media or third-party copyrighted samples without permission and provenance.
