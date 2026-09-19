# PH-20 canonical adoption report

**Report:** `BR-20260919-02`
**Phase:** `PH-20`
**Status:** Governance patch prepared for review; PH-20 exit is not claimed until exact-head commit/PR/merge and final governance audit complete.

## Objective

Adopt the reviewed MediaForge 2 direction into the existing canonical governance owners without duplicating definitions, erasing V1 history or changing application runtime source.

## Authority and rollback

- V1 rollback point: `5acaf88e751327eac47ca673178fbbd88a8603f1`.
- External V2 planning pack: design evidence only; 74-file SHA-256 manifest was verified before adoption work.
- Rollback for this governance-only patch: restore `5acaf88e751327eac47ca673178fbbd88a8603f1`.
- No application source, XAML, tests, scripts, installer, package or runtime dependency is intentionally changed.

## Canonical ranges

- `REQ-056`–`REQ-070` -> `PROJECT_FOUNDATION.md`.
- `AC-088`–`AC-105` and `VAL-066`–`VAL-081` -> `TRACEABILITY.md`.
- `DEC-029`–`DEC-037` -> `DECISIONS_AND_CHANGE_HISTORY.md`.
- `RISK-051`–`RISK-060` -> `SECURITY_PRIVACY_AND_RISK.md`.
- `PH-20`–`PH-31` -> `IMPLEMENTATION_PLAN.md`.

No normative V2 definition is intentionally owned by an external planning file.

## Adopted direction

- task-first Home/task workspaces;
- `Add Media` as the dominant Home command;
- Convert, Resize, Crop & Resize, Trim / Split and Combine primary tasks;
- batch as a property of applicable tasks;
- progressive disclosure;
- optional projects for quick work;
- queue as execution/status infrastructure;
- typed WorkflowIntent and immutable ProcessingPlan authority;
- accessibility from the shell foundation;
- truthful quality/geometry/estimate language.

## Retained V1 contracts

- Windows/WPF/local FFmpeg/FFprobe;
- source immutability;
- output containment and deterministic collision handling;
- temporary-output commit;
- owned process-tree cancellation/cleanup;
- typed persistence and separate recovery;
- evidence-based compatibility and release claims;
- non-NLE boundary.

## Phase reconciliation

`PH-11`–`PH-19` are historical superseded sequencing and remain immutable.

`PH-21` is retained as the V1 closure/rollback bridge from the reviewed design, but its required work was completed before canonical adoption by `BR-20260919-01` and rollback commit `5acaf88e751327eac47ca673178fbbd88a8603f1`.

Active order after this report:

`PH-20 -> PH-22 -> PH-23 -> ... -> PH-31`.

## Evidence-dependent items

| Item | Status |
|---|---|
| V1 `.mediaforge` final compatibility | Open until PH-28 migration fixtures |
| V1 queue compatibility | Open until PH-26/PH-28 evidence |
| direct V1 recovery migration | Open until PH-28 design/evidence |
| exact V2 product/package version | Open; resolve before first version/package-changing V2 delivery |

## Reuse evidence boundary

The external reuse matrix was inspected against application source at `b42bd93613e82b78c41c98948c329a53a3f373c4`. No application source changed between that commit and the V1 rollback point; only governance/validation-harness work changed. The reuse matrix therefore remains planning evidence, but each component is still characterised/regression-tested before actual V2 reuse.

Initial strong candidates include ProcessRunner, FFmpeg location/probing, source/output/collision/temp safety, atomic persistence and selected queue/project/preset services. V1 MainWindow presentation is replaced rather than preserved.

## Validation carry-forward

Inherited `VAL-001`–`VAL-065` and V2 `VAL-066`–`VAL-081` remain canonical in `TRACEABILITY.md`.

The PH-10 manual/native checks deferred under `BR-20260919-01` remain Unproven until executable V2 evidence covers them.

## Claim boundary

This report does not prove V2 runtime implementation, UX behaviour, persistence migration compatibility, the exact V2 version, PH-20 exit, a build, a package, an installer or a release.

## Next stop

Run the final canonical governance audit and inspect the reduced diff. If clean, stage exactly the intended governance files, commit/push/PR the exact head, merge only when repository requirements permit, sync main, record PH-20 exit, then begin PH-22.
