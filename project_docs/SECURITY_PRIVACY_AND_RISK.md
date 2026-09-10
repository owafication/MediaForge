# Security, privacy and risk

**Purpose:** Canonical safety, privacy, supply-chain and risk controls.  
**Read when:** Work affects media/source paths, persistence, processes, logs, automation, tools, update, installer or network.  
**Owner:** Security/reliability maintainer.  
**Authority:** Canonical owner for `RISK-###`.  
**Update trigger:** Risk, control, boundary, third-party dependency or destructive action change.  
**Linked IDs:** `RISK-001`–`RISK-050`, `REQ-009`, `REQ-024`–`REQ-055`.

## Invariants

1. Source media is never intentionally modified or deleted.
2. Destination paths are canonicalised, contained and deterministically reserved.
3. Temporary output is unique and final commit occurs only after the declared verification gate.
4. Cancellation owns and terminates its child-process tree and reports cleanup failure.
5. Project/preset/queue files are typed data, not executable command sources.
6. Local media/path data remains local by default.
7. FFmpeg/updater/installer artefacts require provenance, integrity and licence evidence.
8. Destructive completion actions are off by default and explicitly authorised.
9. Unknown compatibility is not represented as supported.
10. A release claim requires the declared evidence matrix.

## Privacy

Potentially sensitive: source/output paths, project names, recent files, logs, history, command evidence, media metadata, watch roots and webhook payloads. Persist only what each feature requires. Provide retention/clear/export controls. Exported support copies show an inventory and redact full local paths by default. No telemetry, cloud upload or account exists without a new requirement and explicit privacy decision.

## Secrets and credentials

No credentials are required for current local conversion. Future code-signing, update, network webhook or release credentials must never enter source, projects, presets, logs or support bundles. Signing and publication remain explicit authorised actions.

## Supply chain

Record FFmpeg/FFprobe source, version, architecture, configuration, licence notices and hash. The helper cannot be treated as safe merely because TLS succeeded. Updater/tool acquisition needs an approved manifest/integrity/rollback design. Installer runs least-privilege by default.

## Risk register

| ID | Risk | Severity | Primary control |
|---|---|---|---|
| RISK-001 | Unverified Windows build/launch baseline | High | Do not start feature work until PH-07 evidence exists. |
| RISK-002 | Recursive filesystem traversal through reparse points | High | Retain skip policy and edge fixtures. |
| RISK-003 | Non-empty output accepted despite corrupt or wrong content | High | Introduce tiered pre-commit verification. |
| RISK-004 | Unreviewed FFmpeg download or redistribution supply chain | High | Record provenance, hash, licence and rollback. |
| RISK-005 | Cancellation/close leaves child processes or ambiguous outputs | High | Central process runner and lifecycle tests. |
| RISK-006 | Collision race overwrites wrong destination | High | Atomic reservation and deterministic policy tests. |
| RISK-007 | Output path escapes chosen root | High | Canonical containment checks and hostile paths. |
| RISK-008 | Source media mutation | Critical | Immutable-source invariant and hashes in every phase. |
| RISK-009 | Version/package inconsistency | Medium | Single version authority and release audit. |
| RISK-010 | Missing rollback artefact or settings compatibility | High | Tag/hash prior baseline and migration tests. |
| RISK-011 | Malformed numeric state changes intent | Medium | Typed validation and visible errors. |
| RISK-012 | Sensitive local paths leak through logs | Medium | Redaction and support-bundle review. |
| RISK-013 | Unsupported codec/container marketed as supported | High | Capability and compatibility evidence. |
| RISK-014 | FFprobe/helper lifecycle orphan | High | Central cancellation and process-tree termination. |
| RISK-015 | Installer or bundled tools violate licence/provenance policy | High | Decision and notices before distribution. |
| RISK-016 | Accessibility/DPI regression | Medium | Measured baseline and UI automation tests. |
| RISK-017 | Preview decoder differs from export decoder | High | Fidelity labels and FFmpeg-backed preview. |
| RISK-018 | Crop/orientation/SAR parity failure | High | Canonical geometry plus fixtures. |
| RISK-019 | Trim/stitch timestamp or A/V sync failure | High | Probe/decode tolerances and fixture matrix. |
| RISK-020 | Settings write corruption or unavailable AppData | Medium | Atomic writes and explicit degraded state. |
| RISK-021 | Large single-window code paths increase regression risk | High | Incremental state/service extraction before expansion. |
| RISK-022 | Project file corruption loses long editing work | Critical | Atomic manual saves, backups and recovery snapshots. |
| RISK-023 | Relinking chooses the wrong moved source | High | Fingerprint comparison and explicit user confirmation. |
| RISK-024 | Projects/history expose sensitive local paths | High | Local-only storage, redaction and retention controls. |
| RISK-025 | Imported presets inject unsafe or unsupported arguments | High | Typed schemas; no trusted raw command fragments. |
| RISK-026 | Override precedence is ambiguous | High | One resolver and visible effective settings. |
| RISK-027 | Queue mutation races with active jobs | High | Stable IDs, immutable run snapshots and locked transitions. |
| RISK-028 | Capability cache becomes stale | Medium | Key by path/version/size/mtime/hash and allow refresh. |
| RISK-029 | Compatibility engine false positive or false negative | High | Rules plus generated micro-fixtures; disclose unknown. |
| RISK-030 | Preview/export divergence misleads edits | High | Shared plan, fidelity status and parity tests. |
| RISK-031 | Preview/proxy processes or cache leak resources | High | Bounded jobs, cancellation, eviction and soak tests. |
| RISK-032 | Hardware path varies by GPU driver/build | High | Local benchmark and software fallback. |
| RISK-033 | Benchmark recommendation is misleading | Medium | Label environment/time and permit override. |
| RISK-034 | Track mapping silently removes or mislabels streams | Critical | Explicit per-stream plan and post-probe verification. |
| RISK-035 | Lossless trim/merge has timestamp or GOP damage | Critical | Conservative compatibility rules and decode checks. |
| RISK-036 | Verification produces false confidence | High | Tier labels, tolerances and stated limits. |
| RISK-037 | History/cache grows without bound | Medium | Retention limits, user clear/export and size monitoring. |
| RISK-038 | Size/time estimate is treated as a guarantee | Medium | Estimate labels, confidence and no hard dependency. |
| RISK-039 | Two-pass/sample temp artefacts persist | Medium | Unique workspaces and cleanup/recovery tests. |
| RISK-040 | Animated, colour or metadata image data is lost | High | Probe, warn, preserve or block by policy. |
| RISK-041 | Filter order or HDR transform damages output | High | Preset-controlled graph, source/target metadata and fixtures. |
| RISK-042 | Watch folder processes incomplete files or loops | High | Stable window, ignore rules, ledger and loop key. |
| RISK-043 | Completion action causes destructive system change | Critical | Off by default, confirmation and terminal-state gate. |
| RISK-044 | Updater or tool installer is compromised | Critical | Signed/integrity-checked manifest, consent and rollback. |
| RISK-045 | .NET 8 support ends during product maturation | High | Baseline first, then approve migration to .NET 10 LTS. |
| RISK-046 | Architecture refactor changes working behaviour | High | Characterisation tests and phase-sized extraction. |
| RISK-047 | Compatibility test matrix becomes unbounded | Medium | Declare supported matrix; unknown remains unproven. |
| RISK-048 | FFmpeg licence/configuration obligations are misstated | High | Record selected build configuration and legal review boundary. |
| RISK-049 | Shell integration or updater elevates privileges unexpectedly | High | Least privilege, user scope and installer tests. |
| RISK-050 | Localisation changes layout or accessibility names | Medium | Resource-based strings and locale UI fixtures. |

## Review rules

A risk is not closed by source presence. Reduce status only after mapped validation passes in the declared environment. Critical risks block phase/release closure unless explicitly accepted by the authorised owner with scope and expiry.
