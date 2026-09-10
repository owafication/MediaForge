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

Retain the PH-08A.2 build-passing source archive as the pre-migration fallback. The PH-08 .NET 10 and architecture changes should remain one reviewable source slice. No persistence schema or user-data migration was introduced, so rollback is source/package replacement rather than data conversion.
