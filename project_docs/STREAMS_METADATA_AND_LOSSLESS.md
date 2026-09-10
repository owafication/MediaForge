# Streams, subtitles, chapters, metadata and lossless processing

**Purpose:** Canonical professional stream-inspector and conservative lossless-operation contracts.  
**Read when:** Implementing `REQ-039`–`REQ-041`.  
**Owner:** Media pipeline maintainer.  
**Authority:** Stream identity/mapping/lossless owner.  
**Update trigger:** Probe schema, stream policy, metadata, subtitle, chapter or copy/merge behaviour change.  
**Linked IDs:** `PH-14`, `AC-069`–`AC-071`, `VAL-045`–`VAL-047`, `RISK-034`–`RISK-035`.

## Inspector model

Represent all probed streams and container structures: video, audio, subtitle, data, attachments, chapters and cover art. Record stable per-probe identity, input index, codec, profile, dimensions, pixel format, depth, colour primaries/transfer/matrix/range, HDR metadata, frame rate/time base, language, title, bitrate, channel layout, sample rate, default/forced/hearing/visual disposition and chapter timing.

A stream index alone is not stable across replaced sources. Project reopening re-probes and reconciles by source fingerprint plus type/index/codec/language/title; ambiguity requires review.

## User policies

Per stream: copy, encode, remove, set default/forced, rename title/language, reorder where the container supports it. Add external subtitle or cover art; burn subtitle only through an explicit video filter plan. Preserve, remove or edit chapters and metadata by typed policy.

The preflight summary must list each output stream and its source/action.

## Conservative lossless modes

- Rewrap/remux container
- Extract selected tracks
- Replace/add audio or subtitles
- Metadata/chapter/disposition changes
- Keyframe-aligned trim
- Merge only when required stream parameters/time bases are compatible
- Copy unchanged streams while encoding changed streams
- Rotation metadata change where container/player behaviour is declared

`Lossless` means no encoded media samples are changed for copied streams; it does not guarantee identical container bytes, timestamps, metadata or player behaviour. The UI states those limits.

## Smart cut

Encoding only GOP boundaries while copying the middle is deferred beyond the current roadmap. It requires codec-specific timestamp, keyframe and concatenation evidence and must not be represented as a minor extension of keyframe trim.

## Safety

Unknown compatibility blocks or falls back to an explicit full encode; it does not silently copy. Any stream omission requires visible policy. Post-output verification checks the expected stream mapping, duration and metadata policy before commit.
