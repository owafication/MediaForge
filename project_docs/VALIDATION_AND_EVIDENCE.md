# Validation and work evidence

**Purpose:** Canonical validation strategy, evidence quality and release gates.  
**Read when:** Planning checks, reviewing completion or changing test coverage.  
**Owner:** Reliability maintainer.  
**Authority:** Validation procedure owner; individual `VAL` definitions live in `TRACEABILITY.md`.  
**Update trigger:** Requirement, risk, platform, fixture, tolerance or release claim change.  
**Linked IDs:** `VAL-001`–`VAL-065`, `AC-001`–`AC-087`, `PH-07`–`PH-19`.

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
