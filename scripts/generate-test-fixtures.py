#!/usr/bin/env python3
"""Generate small, non-sensitive PH-07 media and filesystem fixtures with FFmpeg."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path


def run(command: list[str]) -> None:
    completed = subprocess.run(command, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if completed.returncode != 0:
        raise RuntimeError(f"Command failed ({completed.returncode}): {' '.join(command)}\n{completed.stdout}")


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--ffmpeg", default="ffmpeg")
    parser.add_argument("--ffprobe", default="ffprobe")
    args = parser.parse_args()

    ffmpeg = shutil.which(args.ffmpeg) or (args.ffmpeg if Path(args.ffmpeg).is_file() else None)
    ffprobe = shutil.which(args.ffprobe) or (args.ffprobe if Path(args.ffprobe).is_file() else None)
    if not ffmpeg or not ffprobe:
        print("FFmpeg and FFprobe are required to generate fixtures.", file=sys.stderr)
        return 2

    output = args.output.resolve()
    if output.exists():
        shutil.rmtree(output)
    media = output / "media"
    filesystem = output / "filesystem"
    media.mkdir(parents=True)
    filesystem.mkdir(parents=True)

    commands: list[list[str]] = [
        [ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i", "testsrc2=size=320x180:rate=1", "-frames:v", "1", str(media / "image-landscape.png")],
        [ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i", "testsrc2=size=180x320:rate=1", "-frames:v", "1", str(media / "image-portrait.png")],
        [ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i", "color=c=red@0.5:size=321x241:rate=1", "-vf", "format=rgba", "-frames:v", "1", str(media / "image-odd-alpha.png")],
        [ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i", "sine=frequency=440:sample_rate=48000:duration=1.2", "-ac", "2", "-c:a", "pcm_s16le", str(media / "audio-stereo.wav")],
        [ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i", "testsrc2=size=320x180:rate=30:duration=1.2", "-f", "lavfi", "-i", "sine=frequency=880:sample_rate=48000:duration=1.2", "-shortest", "-c:v", "ffv1", "-level", "3", "-c:a", "pcm_s16le", str(media / "video-cfr-audio.mkv")],
        [ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i", "testsrc2=size=640x360:rate=24:duration=0.8", "-an", "-c:v", "ffv1", "-level", "3", str(media / "video-no-audio.mkv")],
    ]

    for command in commands:
        run(command)

    # Corrupt media and deterministic filesystem edge cases.
    (media / "corrupt-media.bin").write_bytes(b"not-media\x00\x01\x02")
    for relative in (
        Path("same-stem/a/clip.txt"),
        Path("same-stem/b/clip.txt"),
        Path("unicode/Åudio_日本語.txt"),
        Path("dot-names/.hidden-source.txt"),
        Path("input-tree/output-root/.keep"),
        Path("read-only/source.txt"),
    ):
        path = filesystem / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(f"MediaForge fixture: {relative.as_posix()}\n", encoding="utf-8")
    try:
        os.chmod(filesystem / "read-only/source.txt", 0o444)
    except OSError:
        pass

    probe_targets = [
        media / "image-landscape.png",
        media / "image-portrait.png",
        media / "image-odd-alpha.png",
        media / "audio-stereo.wav",
        media / "video-cfr-audio.mkv",
        media / "video-no-audio.mkv",
    ]
    probes: dict[str, object] = {}
    for path in probe_targets:
        completed = subprocess.run(
            [ffprobe, "-v", "error", "-show_streams", "-show_format", "-of", "json", str(path)],
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )
        if completed.returncode != 0:
            raise RuntimeError(f"FFprobe failed for {path.name}: {completed.stderr}")
        probes[path.name] = json.loads(completed.stdout)

    files = []
    for path in sorted(p for p in output.rglob("*") if p.is_file()):
        files.append({
            "path": path.relative_to(output).as_posix(),
            "size": path.stat().st_size,
            "sha256": sha256(path),
        })

    version_output = subprocess.run([ffmpeg, "-version"], text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, check=True).stdout.splitlines()
    manifest = {
        "schema": 1,
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "generator": "scripts/generate-test-fixtures.py",
        "ffmpeg": version_output[:3],
        "commands": commands,
        "probes": probes,
        "files": files,
        "limitations": [
            "These fixtures are non-sensitive and intentionally small.",
            "They do not represent the complete codec, metadata, rotation, VFR, long-GOP, low-space or filesystem matrix.",
            "A generated fixture proves only the generator command and recorded probe output.",
        ],
    }
    (output / "fixture-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(f"Generated {len(files)} fixture files under {output}")
    print(f"Manifest: {output / 'fixture-manifest.json'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
