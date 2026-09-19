# Next build plan — MediaForge 2 canonical adoption

**Target:** Governance/design foundation for MediaForge 2; exact package version unresolved.
**Active phase:** `PH-20`.
**Rollback:** `5acaf88e751327eac47ca673178fbbd88a8603f1`.
**Runtime status:** V2 Unproven.

## Objective

Finish canonical V2 governance adoption without changing application source.

## Satisfied prerequisites

- PH-10 sequencing closure recorded by `BR-20260919-01`.
- V1 rollback point established and merged at `5acaf88e751327eac47ca673178fbbd88a8603f1`.
- external V2 planning pack reviewed and hash-verified;
- deferred V1 checks remain explicitly Skipped/Unproven.

`PH-21` is the immutable V1-closure/rollback bridge and is already satisfied; it is not future implementation work.

## PH-20 work

1. adopt V2 facts into existing canonical owners;
2. keep one definition per immutable ID;
3. preserve V1 history and safety contracts;
4. supersede PH-11-next/queue-first sequencing explicitly;
5. reconcile V1 later-scope requirements with V2 phases;
6. keep external planning files as design evidence only;
7. run immutable-ID, duplicate-definition, owner, route, link, phase and mapping audits;
8. confirm no source/XAML/test/script/installer/package changes;
9. inspect the full governance diff;
10. stage, commit, push and PR only the intended governance files;
11. merge only after exact-head checks permit it;
12. sync main and record PH-20 exit evidence.

## Next implementation phase

After PH-20 exits, begin `PH-22` task-first WPF shell/navigation. `PH-23` then establishes WorkflowIntent, capability/compatibility and ProcessingPlan seams before the first complete Resize/Crop vertical slice in `PH-24`.

## Validation boundary

Targeted validation remains mandatory during implementation. Deferred V1 safety validation remains Unproven until executable V2 evidence covers it. A future release requires the applicable full validation matrix; governance adoption alone satisfies none of those runtime checks.
