# Validation and work evidence

**Purpose:** Canonical validation strategy, evidence quality and release gates.
**Read when:** Planning checks, reviewing completion or changing test coverage.
**Owner:** Reliability maintainer.
**Authority:** Validation procedure owner; individual `VAL` definitions live in `TRACEABILITY.md`.
**Update trigger:** Requirement, risk, platform, fixture, tolerance or release claim change.
**Linked IDs:** `VAL-001`–`VAL-081`, `AC-001`–`AC-105`, `PH-07`–`PH-31`.

## Evidence levels

- **Static:** parse, names, handlers, lexical/source inventory. Does not prove compilation/runtime.
- **Build:** clean restore/compile/publish on declared environment.
- **Unit/characterisation:** deterministic model/service behaviour.
- **Integration:** real filesystem and FFmpeg/FFprobe child processes.
- **Fixture:** known media input and measurable output.
- **UI/accessibility:** interactive WPF behaviour, automation and DPI.
- **Package/install:** ZIP/installer/install/launch/uninstall and artefact hashes.
- **Release:** cumulative applicable checks plus rollback and limitation review.

## Mandatory cross-phase regression

Every processing or persistence phase runs:

- clean build and automated tests;
- source hashes before/after;
- collision/destination containment;
- cancellation/process-tree/temp cleanup;
- settings/project/preset migration where applicable;
- output verification for affected media;
- inventory/version/governance audit.

## Tolerances

Duration, frame, audio sync, colour, size and quality tolerances must be declared before a test can pass. Store the tolerance with the result. A manually viewed output without a criterion is exploratory evidence, not acceptance.

## Fixtures

Use generated or explicitly approved non-sensitive media. Record generator command/tool version and fixture hashes. Do not use irreplaceable media as the only test copy. See `TEST_FIXTURE_MATRIX.md`.

## Automated tests

Proposed first-party test project with no mocking framework requirement. Prefer pure model/service tests and process integration fixtures. UI automation is separate from unit tests. Tests must be runnable from a clean local copy; CI may be added only after a remote/host is approved.

## Reports

Every report records environment, source commit/hash, commands, exit codes, relevant output, artefact paths/hashes, mapped IDs, failures, skips, unproven items, risk changes and rollback. Preserve failed evidence. Redaction applies to exported copies, not the canonical local run record.

## Phase closure

A phase closes only when:

1. prerequisites and required reading were satisfied;
2. expected state is present;
3. all mapped mandatory AC/VAL pass;
4. no unresolved critical stop condition remains;
5. governance and evidence are updated;
6. rollback point is available and tested to its stated scope.

## Release boundary

Do not call a build a release candidate because it compiles. Release requires clean build, declared fixture matrix, accessibility baseline, package/installer evidence, licences/provenance, hashes, limitations and rollback for each claimed architecture.

## MediaForge 2 validation architecture

Individual `AC-###` and `VAL-###` definitions remain exclusively in `TRACEABILITY.md`.

### Implementation checks

During V2 development, affected changes run Release build plus relevant static, unit/service, XAML, persistence/schema, ProcessingPlan and targeted integration/safety checks.

### Fast Post-Build Smoke

A planned future entry point is `scripts\validate-post-build-smoke.ps1`. It will exercise isolated launch state, deterministic media fixtures, image/video/audio conversion, mixed batch, output probing, collision policies, cancellation/process cleanup, source hashes, project/queue/preset round-trips and orphan-process/temp-output cleanup.

The smoke suite is a regression detector, not a complete release gate.

### Full Release Validation

A planned future entry point is `scripts\validate-release.ps1`. It will orchestrate all applicable validation IDs, including inherited V1 contracts, V2 UI/ProcessingPlan/migration checks, packaging/version/hash/rollback and environment-dependent installer/architecture checks where claimed.

No release is Verified while an applicable mandatory validation is Failed, Skipped or Unproven unless a new explicitly scoped release risk exception exists.

### Evidence contract

Each execution records validation ID, status, commit, working tree, environment/tool provenance, fixture/source hashes, action/command, timestamps, exit code, output hashes/probe evidence, tolerances, cleanup evidence, failure details, residual risks and rollback point.

Target evidence root: `artifacts\validation-YYYYMMDD-HHMMSS\`.

### Automation boundary

Objectively measurable safety behaviour is automated wherever practical. Human review remains appropriate for actual screen-reader usability, subjective visual/DPI/multi-monitor clarity, wording/help usability, final privacy/support-bundle inspection and exploratory release review.

The reviewed execution architecture classifies the 81 validation IDs as 63 automated, 10 UI-automation-plus-human-spot-check, 7 environment-dependent automated and 1 human review. This is an execution classification, not a second definition catalogue.

### Deferred V1 carry-forward

The PH-10 checks deferred by `BR-20260919-01` remain Unproven: real mixed-media conversion; source immutability across success/failure/skip/overwrite/cancel; collisions; containment; cancellation/process/temp cleanup; close-during-work; projects/recovery/relink/portable mode; preset/queue persistence and races; immutable run snapshots; estimates; interrupted writes; accessibility; and installer/signing where later claimed.
