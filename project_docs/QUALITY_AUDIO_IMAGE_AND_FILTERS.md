# Quality targeting, audio, image and controlled filters

**Purpose:** Canonical later scope for `REQ-044`–`REQ-047`.  
**Read when:** Adding quality targets, analysis, audio/image workflows or video filters.  
**Owner:** Media quality maintainer.  
**Authority:** Quality/filter catalogue owner.  
**Update trigger:** Quality mode, metric, audio analysis, image preservation or filter order change.  
**Linked IDs:** `PH-13`, `PH-16`, `AC-075`–`AC-078`, `VAL-050`–`VAL-053`.

## Quality targeting

Expose intent modes rather than raw flags: constant quality, target bitrate, target file size, one-pass/two-pass and bounded max bitrate/buffer where supported. A user-selected or default 10-second sample uses the canonical plan, shows interval/effective settings and is isolated from final output.

Estimated size/time/compression ratio are labelled estimates. Optional SSIM/VMAF requires detected filters/models, declared interpretation and source/output alignment; unavailable metrics are hidden with a reason.

## Audio

Later scope:

- two-pass EBU R128 loudness with measured values
- configurable LUFS, true peak and loudness range
- ReplayGain analysis/tagging
- silence trim, fade, delay correction
- channel mapping/downmix and per-track gain
- waveform, peak/clipping and analysis-only reports

Analysis and final pass use unique workspaces and cancellable processes. Achieved values and warnings are verified/reported.

## Images

Later scope:

- preserve supported animated GIF/WebP/AVIF and multi-page TIFF
- EXIF orientation normalisation
- ICC profile preservation/conversion
- alpha/depth detection and output warning
- lossless optimisation where capability permits
- metadata editor, contact sheets, overlays and rename templates
- selectable scaling algorithm
- RAW only after an explicit decoder/provenance strategy

Unsupported preservation must warn or block; never flatten animation/pages silently under a preservation preset.

## Video filters

Provide named, ordered controls/presets for deinterlace/decomb, denoise, sharpen, deband, deflicker, stabilisation, levels/white balance, LUT, SDR/HDR tone mapping, rotation/flip, speed, A/V delay, fades/crossfades and overlays.

The filter graph records order, pixel/colour transitions, hardware upload/download and CPU fallback. Avoid a wall of arbitrary FFmpeg switches. Advanced raw filter entry is excluded unless separately sandboxed and governed.
