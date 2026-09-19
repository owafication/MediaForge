# UI, UX, accessibility and localisation

**Purpose:** Canonical Windows interaction and release-quality UI baseline.
**Read when:** Changing views, commands, navigation, resources or shell/first-run experience.
**Owner:** UX maintainer.
**Authority:** UI/accessibility owner.
**Update trigger:** Flow, screen, state, keyboard, theme, DPI, accessible name or locale change.
**Linked IDs:** legacy `PH-18`, `AC-081`–`AC-082`, `VAL-056`–`VAL-057`, `RISK-016`, `RISK-049`–`RISK-050`; V2 `REQ-056`–`REQ-068`, `PH-22`–`PH-31`, `AC-088`–`AC-105`, `VAL-066`–`VAL-080`, `RISK-051`–`RISK-059`.

## Information architecture

Proposed main surfaces:

- Queue workspace with compact/detailed views, customisable columns, search/filter and multi-select commands
- Job details drawer for source, effective settings, edit summary, stream plan, compatibility, progress, output and diagnostics
- Project/recovery/relink dialogs
- Preset manager
- Media inspector/editor
- Capability/benchmark report
- History/report view
- Settings/first-run/tools/update

Do not place every technical field in the queue cell.

## Flow contract

`flow → screen → command/route → state → permission → fallback → validation`

Commands are exposed through ViewModels where stateful. UI disablement always has a reason where the user may not understand the constraint. Destructive actions support consistent confirmation and, where feasible, undo or recovery. Undo never implies rollback of a completed external FFmpeg write; that boundary is stated clearly.

## Accessibility baseline

- Full keyboard path for import, queue selection/actions, editor apply/cancel, project save/recovery, presets and start/cancel/pause
- Logical tab order, visible focus and no keyboard traps
- `AutomationProperties.Name`, HelpText and relationships for unlabeled/custom controls
- Screen-reader announcements for progress and terminal results without excessive noise
- High contrast and system theme support
- DPI/multi-monitor tests at declared scales
- Minimum target sizes and non-colour-only status
- Accessibility Insights plus at least one screen-reader smoke baseline

## Themes and localisation

Use resource dictionaries and semantic resources; avoid hard-coded colours/strings in logic. Support Light, Dark, System and High Contrast behaviour. Externalise user-visible strings and accessible names from the beginning of PH-18. Locale changes must not alter persisted enum identifiers or FFmpeg arguments.

## Shell, portable and update UX

File association, Send To, jump lists and portable mode remain optional and least-privilege. First run locates existing FFmpeg before offering acquisition. Update checks are opt-in and show current/new version, notes, source, integrity and rollback; no silent update.

## MediaForge 2 task-first shell

Top-level menu:

`File | Edit | View | Project | Tools | Help`

Home answers **What do you want to do?** with `Add Media` as the dominant command and these primary tasks:

- Convert
- Resize
- Crop & Resize
- Trim / Split
- Combine

Batch is not a separate processing architecture. A Batch Processor shortcut may route into normal multi-file task workflows.

A task workspace normally contains:

1. task title/purpose;
2. selected-media summary;
3. task-specific normal controls;
4. review/preview when relevant;
5. output summary;
6. estimate;
7. collapsed Advanced settings;
8. primary Process action.

Normal geometry terms are `Fit within`, `Fit in frame`, `Fill frame`, and explicit `Stretch`. Do not label an ambiguous geometry policy simply `Fit`.

Workflow states include Empty, MediaReady, Configuring, NeedsDecision, ReviewReady, Running, Paused, Cancelling, Completed, CompletedWithWarning, Failed and VerificationFailed.

Projects remain optional for quick work. Queue is a running/status surface, not the default conceptual model. Presets are contextual in normal workflows and managed through advanced Tools surfaces.

From PH-22 onward require keyboard access, logical tab order, visible focus, accessible names/help text, high contrast, DPI resilience, screen-reader-compatible status updates and no required pointer-only workflow.
