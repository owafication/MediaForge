# Third-party notices

MediaForge invokes FFmpeg and FFprobe but does not include their binaries in the source archive.

FFmpeg is a separate project and is licensed under the LGPL 2.1 or later, or GPL 2 or later, depending on how a particular binary was configured. Windows builds may include GPL components. Consult the license files included by the binary distributor before redistribution.

The optional `scripts/download-ffmpeg.ps1` helper downloads a Gyan Windows essentials build and verifies the archive against the SHA-256 value published beside that archive. MediaForge does not control that third-party service or binary configuration.
