# Debugging and maintenance

**Purpose:** Canonical diagnostic order, evidence capture, maintenance and extension discipline.  
**Read when:** Investigating build, launch, persistence, queue, FFmpeg, preview, verification, automation, package or installer failures.  
**Owner:** Technical maintainer.  
**Authority:** Debugging/maintenance owner.  
**Update trigger:** New failure mode, diagnostic signal, maintenance task or extension procedure.

## Diagnostic order

1. Record app/source version, commit/hash, Windows build/architecture, .NET output, FFmpeg/FFprobe evidence and exact user action.
2. Preserve original error, job/project state, log tail and settings before editing.
3. Classify: build, launch, settings, project/recovery, preset/queue, import, capability, plan, preview, conversion, verification, watch, package or installer.
4. Reproduce with one generated fixture and one job before concurrency.
5. Locate boundary: state resolution → plan → process start → output → verification → commit → UI/history.
6. Inspect only routed governance and relevant source/tests.
7. Form one evidence-supported hypothesis.
8. Apply the smallest reversible change.
9. Run targeted check, then mapped regression.
10. Update evidence, risks, traceability, changelog and rollback.

## High-value evidence

- Project/preset/queue schema/version and redacted content
- Effective option sources and immutable run snapshot
- Capability cache key/report
- ProcessingPlan hash/version and per-stream summary
- Exact executable path/version/hash and ArgumentList copy
- Process exit/cancellation/cleanup state
- Temp/final path, source/output hashes, probe/verification result
- Preview fidelity state, source fingerprint and cache key

## Failure map

| Symptom | First checks |
|---|---|
| Build fails | selected SDK/global.json, first compiler/XAML error, clean obj/bin |
| Project will not open | schema, JSON location, newer/read-only state, recovery snapshot, migration log |
| Autosave corrupt/missing | dirty/debounce, temp/replace stage, permissions, retention, close race |
| Wrong job settings | resolver trace: global→preset→job→edit, lock state, run snapshot |
| Queue changes running job | stable job ID, state transition lock, immutable snapshot |
| Option unavailable | capability cache key, selected FFmpeg build, compatibility reason |
| Preview differs | plan version, orientation/SAR/colour, fidelity state, frame request, cache |
| Hardware slower/fails | driver/build, actual path, filter CPU transfer, benchmark expiry, fallback |
| Stream missing | probe identity, mapping policy, compiled `-map`, verification expected streams |
| Lossless output damaged | keyframe/time base/codec match, stream summary, decode verification |
| Verification failed | expected contract, tolerance, temp output, probe/decode sample, prior destination |
| Watch loop/incomplete | stability window, ignore paths, loop key, rule/output overlap, audit ledger |

## Maintenance cadence

For every change: mapped IDs, targeted tests, mandatory regression, docs/evidence, staged review.  
For every release: support status, FFmpeg provenance, full declared fixture matrix, accessibility, package/installer, licences, hashes, rollback.  
Periodically: .NET support state, FFmpeg helper/update integrity, settings/project/preset migrations, cache/history retention, ignored/generated files and open high risks.

## Extension rule

Add every media capability end-to-end. Never add only a ComboBox item or raw argument. Preserve source immutability and processing-plan authority. Unknown remains unknown.
