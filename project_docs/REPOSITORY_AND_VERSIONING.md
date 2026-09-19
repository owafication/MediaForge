# Repository and versioning

**Linked IDs:** `PH-07`, `PH-08`, `PH-18`, `PH-19`, `DEC-014`, `DEC-028`.

## Current contracts

- Product/source version remains `1.1.0` until the workflow milestone is release-ready.
- Application and tests target `net10.0-windows`.
- `global.json` pins SDK `10.0.100` with `latestFeature` roll-forward.
- Supported release runtime remains Windows x64 by default; arm64 remains a build-script option, not a verified support claim.
- Settings JSON and Inno Setup product/version identifiers remain unchanged by PH-08.

## Version intent

- 1.1.x: validation/stabilisation checkpoints where needed.
- 1.2.0: workflow foundation after PH-08–PH-10 pass their gates.

## Packaging

Release and source archives must be versioned, exclude generated/source-control state, include inventories or checksums, and be reproducible from a clean tree. Do not claim signing or installer validation without evidence.

## Rollback

The current durable V1-to-V2 rollback baseline is `5acaf88e751327eac47ca673178fbbd88a8603f1`.

The older PH-08A.2 archive remains historical fallback evidence but is not the current V1/V2 transition boundary.

V2 governance adoption and later implementation use feature branches and pull requests. No published history is rewritten. Persistent-data migration must preserve the original V1 copy until the migration succeeds and is explicitly saved/adopted.

## MediaForge 2 transition policy

- V2 application work branches from merged canonical governance, never from the external planning directory.
- The external planning pack remains design evidence only.
- The exact 2.x product/package version is unresolved and must not be changed merely because governance is adopted.
- Commit, PR head, validation evidence and rollback points must correspond exactly before merge/release claims.
