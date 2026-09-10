# Watch folders and completion actions

**Purpose:** Canonical guarded automation contracts for `REQ-048`–`REQ-049`.  
**Read when:** Adding filesystem monitoring or post-batch actions.  
**Owner:** Automation maintainer.  
**Authority:** Rule/action owner.  
**Update trigger:** Rule trigger, stable-file policy, loop ledger, move/delete or completion action change.  
**Linked IDs:** `PH-17`, `AC-079`–`AC-080`, `VAL-054`–`VAL-055`, `RISK-042`–`RISK-043`.

## Watch rule

A rule contains ID/name/enabled state, authorised input root, include/exclude patterns, partial extensions, stability window, preset/overrides, output rule, verification tier, success/failure action and audit settings.

## Safeguards

- Combine watcher events with periodic rescan; events alone are not authoritative.
- A file is eligible only after size and last-write remain stable for the configured window and it can be opened according to policy.
- Ignore temporary/partial extensions and output roots.
- Use a loop key from canonical path, fingerprint and rule version.
- Dry run shows intended jobs/actions without conversion or move.
- Never delete source by default.
- Moving successful sources to Processed or failed sources to Review is explicit and occurs only after terminal verification state.
- Record an audit line for detection, deferral, queueing, result and action.

## Completion actions

Low-risk: Windows notification, sound, open output folder, create report.  
High-risk: shutdown/sleep/hibernate, local command, local-network webhook.

High-risk actions are off by default, require explicit configuration and confirmation, display their exact condition, and run only after all jobs and required verification finish. Commands use typed executable/argument fields, no shell interpolation. Webhooks are a deliberate network boundary and never include paths/media unless configured and previewed.

## Recovery

Rules and audit state survive restart. In-flight conversion recovery follows queue/project contracts; the watcher does not assume an interrupted job succeeded. Actions are idempotent where possible and never repeat after an ambiguous restart without review.
