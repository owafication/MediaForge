# Product direction

**Purpose:** Canonical product priorities, sequencing and non-goals.  
**Authority:** Product direction; requirement definitions remain in `project_docs/PROJECT_FOUNDATION.md`.

## Direction

MediaForge should become a reliable professional utility before it becomes broader. Codec options are useful only when the app can save work, explain availability, predict the actual processing path, preview the transform and verify the result.

## Five strategic capabilities

1. **Durable projects:** save, autosave, recover, relink and forward-open safely.
2. **Reusable workflows:** presets, per-job overrides and queue persistence/control.
3. **Capability truth:** selected FFmpeg build, compatibility rules and stream-level processing summary.
4. **Dependable preview:** FFmpeg-rendered transform evidence with explicit fallback/fidelity labels.
5. **Verified delivery:** tiered verification, history, reports and support bundles.

## Sequencing rules

- Safety and evidence precede feature claims.
- Establish the current native Windows baseline before refactoring or platform migration.
- Extract only the state/process seams required by the next feature; do not rewrite the WPF app wholesale.
- Build projects/recovery before adding more editor complexity.
- Build the canonical ProcessingPlan before FFmpeg preview, hardware or track-level features.
- Add hardware only after capability detection and software fallback.
- Add lossless modes conservatively; unknown compatibility means encode, block or request a decision—not “lossless”.
- Verify temporary output before final commit.
- Add automation only after durable queue/history/verification exist.

## Proposed milestone map

- **Baseline checkpoint:** `PH-07` — validated 1.1.0 source; public 1.1.1 is optional (`DEC-028`).
- **1.2.0 Workflow Foundation:** `PH-08`–`PH-10` — supported platform seam, projects/recovery, presets/per-job queue.
- **Capability and preview:** `PH-11`–`PH-13`.
- **Professional media control and trust:** `PH-14`–`PH-16`.
- **Automation and release quality:** `PH-17`–`PH-19`.

## Non-goals

- Unlimited multi-track timeline
- Complex compositing or keyframe animation engine
- Full colour-grading suite
- Cloud account, sync or hosted conversion
- Collaborative editing
- Plugin marketplace
- Generative AI
- Silent updater or unreviewed bundled FFmpeg

Any non-goal requires a new requirement, architecture decision, risk review and roadmap change.
