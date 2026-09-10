using System.Buffers.Binary;
using System.IO;

namespace MediaForge.Services.Images;

public static class ImageHeaderProbe
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static ImageSourceInfo? TryRead(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            Span<byte> header = stackalloc byte[33];
            var read = stream.Read(header);
            if (read < 10) return null;

            if (read >= 29 && header[..8].SequenceEqual(PngSignature))
            {
                return ReadPng(header);
            }

            if (header[0] == 0xFF && header[1] == 0xD8)
            {
                stream.Position = 2;
                return ReadJpeg(stream);
            }

            if (read >= 10 && header[..6].SequenceEqual("GIF87a"u8) ||
                read >= 10 && header[..6].SequenceEqual("GIF89a"u8))
            {
                var width = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(6, 2));
                var height = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(8, 2));
                return Create(width, height, 4);
            }

            if (read >= 30 && header[0] == (byte)'B' && header[1] == (byte)'M')
            {
                var width = Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(header.Slice(18, 4)));
                var height = Math.Abs(BinaryPrimitives.ReadInt32LittleEndian(header.Slice(22, 4)));
                var bitsPerPixel = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(28, 2));
                return Create(width, height, Math.Max(1, (bitsPerPixel + 7) / 8));
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static ImageSourceInfo? ReadPng(ReadOnlySpan<byte> header)
    {
        if (!header.Slice(12, 4).SequenceEqual("IHDR"u8)) return null;
        var width = BinaryPrimitives.ReadInt32BigEndian(header.Slice(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(header.Slice(20, 4));
        var bitDepth = header[24];
        var colourType = header[25];
        var channels = colourType switch
        {
            0 => 1,
            2 => 3,
            3 => 1,
            4 => 2,
            6 => 4,
            _ => 4
        };
        var bytesPerChannel = Math.Max(1, (bitDepth + 7) / 8);
        return Create(width, height, checked(channels * bytesPerChannel));
    }

    private static ImageSourceInfo? ReadJpeg(Stream stream)
    {
        Span<byte> lengthBytes = stackalloc byte[2];
        Span<byte> frame = stackalloc byte[6];

        while (stream.Position < stream.Length)
        {
            var markerStart = stream.ReadByte();
            if (markerStart < 0) return null;
            if (markerStart != 0xFF) continue;

            int marker;
            do
            {
                marker = stream.ReadByte();
            } while (marker == 0xFF);

            if (marker < 0 || marker is 0xD8 or 0xD9) continue;
            if (marker is >= 0xD0 and <= 0xD7) continue;

            if (stream.Read(lengthBytes) != lengthBytes.Length) return null;
            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(lengthBytes);
            if (segmentLength < 2) return null;

            if (IsStartOfFrame(marker))
            {
                if (segmentLength < 8 || stream.Read(frame) != frame.Length) return null;
                var precision = frame[0];
                var height = BinaryPrimitives.ReadUInt16BigEndian(frame.Slice(1, 2));
                var width = BinaryPrimitives.ReadUInt16BigEndian(frame.Slice(3, 2));
                var components = frame[5];
                var bytesPerChannel = Math.Max(1, (precision + 7) / 8);
                return Create(width, height, Math.Max(1, components * bytesPerChannel));
            }

            stream.Seek(segmentLength - 2, SeekOrigin.Current);
        }

        return null;
    }

    private static bool IsStartOfFrame(int marker) => marker is
        0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF;

    private static ImageSourceInfo? Create(int width, int height, int bytesPerPixel) =>
        width > 0 && height > 0 ? new ImageSourceInfo(width, height, Math.Max(1, bytesPerPixel)) : null;
}
