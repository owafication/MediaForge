# Output verification, history and support evidence

**Purpose:** Canonical verification tiers, commit gate, terminal states, history and support-report contracts.  
**Read when:** Implementing `REQ-042`–`REQ-043` or changing output commit.  
**Owner:** Reliability maintainer.  
**Authority:** Verification/history owner.  
**Update trigger:** Expected-output, tolerance, terminal state, retention or diagnostic field change.  
**Linked IDs:** `PH-15`, `AC-072`–`AC-074`, `VAL-048`–`VAL-049`, `RISK-003`, `RISK-036`–`RISK-037`.

## Verification contract

Verification runs against temporary output before final commit where technically possible.

- **Fast:** file exists/non-empty, FFprobe opens, expected basic stream types exist.
- **Standard:** Fast plus stream count/policy, dimensions, duration tolerance, frame-rate plausibility, audio sample rate/channels and metadata policy.
- **Thorough:** Standard plus bounded decode samples near beginning/middle/end and additional timestamp/discontinuity checks where measurable.

The exact expected output is part of `ProcessingPlan`. Tolerances are media/operation-specific and versioned.

## Terminal states

- Converted and verified
- Converted with verification warnings
- Conversion failed
- Verification failed
- Cancelled
- Skipped

A zero FFmpeg exit code alone is not verified. Verification failure preserves the prior destination. The user may explicitly retain or inspect the temporary output under a distinct unverified label; no automatic rename into the final destination.

## History

Initial storage is bounded rolling JSONL under local application data. Record job ID, timestamps, source/output redacted display, source fingerprint, preset/effective-option summary, plan hash/version, tool version, terminal state, warnings, duration, output size and verification result. Do not store media content or full logs by default.

Retention is configurable by age/count/size, with Clear and Export. A database is not justified until measured requirements exceed JSONL.

## Reports and support bundle

Per-job report: effective settings, stream plan, warnings, tool evidence, command evidence with redacted paths, exit code, verification and output hash when requested.

Support bundle may include app/version, environment, capability report, selected settings, redacted job reports/log excerpts and governance/release identifiers. Exclude projects, source files, media, secrets and full paths by default. Display an inventory before export.

## Limitations

Verification reduces risk but does not prove subjective visual/audio quality, every decoded frame, every player or archival integrity unless the declared tier performs those checks. The UI and release notes state the tier and boundary.
