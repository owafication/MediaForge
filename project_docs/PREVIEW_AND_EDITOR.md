# Preview and editor direction

**Purpose:** Canonical preview fidelity, cache, timeline and editor boundary.  
**Read when:** Implementing `REQ-036`–`REQ-037` or changing current editor behaviour.  
**Owner:** Editor maintainer.  
**Authority:** Preview/editor contract owner.  
**Update trigger:** Decoder, frame request, proxy, cache, waveform, thumbnail, crop/timeline or fidelity change.  
**Linked IDs:** `PH-12`, `AC-065`–`AC-067`, `VAL-042`–`VAL-043`, `RISK-030`–`RISK-031`.

## Direction

The current `MediaElement` preview remains a fast convenience fallback. It is not the authority for orientation, sample aspect ratio, crop/scale/pad, colour, HDR or final filter output. The target preview renders from the canonical `ProcessingPlan` through FFmpeg CLI processes.

## Incremental implementation

1. On-demand exact transformed still frame at playhead.
2. Cached thumbnail strip and keyframe markers.
3. Audio waveform derived from a bounded analysis path.
4. Optional low-resolution proxy compiled from the same plan.
5. Before/after split, zoom/pan, safe-area and alpha checkerboard overlays.
6. Clip-boundary and transition previews only when corresponding product requirements exist.

No embedded libav/native decoder is required by the current roadmap. A later native integration needs a separate decision, ABI/licence/security plan and benchmark.

## Fidelity states

- `Matched`: declared transforms rendered by the same plan and parity fixtures pass within tolerance.
- `Approximate`: timing/frame selection or colour path has a documented tolerance.
- `Proxy`: transformed low-resolution proxy; geometry matches but quality/resolution differs.
- `Fallback`: MediaElement source playback; final filter graph is not rendered.
- `Unavailable`: no safe preview path; edit values remain inspectable.

The UI must show the active state and reason.

## Frame and timing rules

Frame accuracy is a measured property, not a label. CFR and VFR fixtures define seek tolerance. Keyframe indicators are evidence from probe/index data, not guesses. Trim bounds remain authoritative even when fallback playback overshoots visually.

## Resource contract

All preview/thumbnails/waveforms/proxies use a bounded queue, cancellable process runner and per-project cache. Cache keys include source fingerprint, plan version and relevant preview settings. Eviction is size/age bounded. Closing editor/project cancels owned work and reports cleanup failures.

## Editor boundary

MediaForge remains a single-track clip-list editor. Additions such as unlimited video/audio tracks, compositing layers, animation curves or a full timeline require a scope change and are not implied by waveform or before/after preview.
