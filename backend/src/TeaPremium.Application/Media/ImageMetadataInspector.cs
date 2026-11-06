using System.Buffers.Binary;

namespace TeaPremium.Application.Media;

public sealed record InspectedImage(string MimeType, int Width, int Height);

public static class ImageMetadataInspector
{
    public static bool TryInspect(ReadOnlySpan<byte> bytes, out InspectedImage? image)
    {
        if (TryInspectPng(bytes, out image) ||
            TryInspectJpeg(bytes, out image) ||
            TryInspectWebP(bytes, out image))
        {
            return true;
        }

        image = null;
        return false;
    }

    private static bool TryInspectPng(ReadOnlySpan<byte> bytes, out InspectedImage? image)
    {
        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (bytes.Length < 24 || !bytes[..8].SequenceEqual(signature))
        {
            image = null;
            return false;
        }

        var width = BinaryPrimitives.ReadInt32BigEndian(bytes[16..20]);
        var height = BinaryPrimitives.ReadInt32BigEndian(bytes[20..24]);
        return Complete("image/png", width, height, out image);
    }

    private static bool TryInspectJpeg(ReadOnlySpan<byte> bytes, out InspectedImage? image)
    {
        if (bytes.Length < 4 || bytes[0] != 0xff || bytes[1] != 0xd8)
        {
            image = null;
            return false;
        }

        var offset = 2;
        while (offset + 4 <= bytes.Length)
        {
            if (bytes[offset] != 0xff)
            {
                offset++;
                continue;
            }

            while (offset < bytes.Length && bytes[offset] == 0xff)
            {
                offset++;
            }

            if (offset >= bytes.Length)
            {
                break;
            }

            var marker = bytes[offset++];
            if (marker is 0xd8 or 0xd9 || marker is >= 0xd0 and <= 0xd7)
            {
                continue;
            }

            if (offset + 2 > bytes.Length)
            {
                break;
            }

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(bytes[offset..(offset + 2)]);
            if (segmentLength < 2 || offset + segmentLength > bytes.Length)
            {
                break;
            }

            if (IsStartOfFrame(marker) && segmentLength >= 7)
            {
                var height = BinaryPrimitives.ReadUInt16BigEndian(bytes[(offset + 3)..(offset + 5)]);
                var width = BinaryPrimitives.ReadUInt16BigEndian(bytes[(offset + 5)..(offset + 7)]);
                return Complete("image/jpeg", width, height, out image);
            }

            offset += segmentLength;
        }

        image = null;
        return false;
    }

    private static bool TryInspectWebP(ReadOnlySpan<byte> bytes, out InspectedImage? image)
    {
        if (bytes.Length < 30 ||
            !bytes[..4].SequenceEqual("RIFF"u8) ||
            !bytes[8..12].SequenceEqual("WEBP"u8))
        {
            image = null;
            return false;
        }

        var chunk = bytes[12..16];
        if (chunk.SequenceEqual("VP8X"u8))
        {
            var width = 1 + ReadUInt24LittleEndian(bytes[24..27]);
            var height = 1 + ReadUInt24LittleEndian(bytes[27..30]);
            return Complete("image/webp", width, height, out image);
        }

        if (chunk.SequenceEqual("VP8L"u8) && bytes.Length >= 25 && bytes[20] == 0x2f)
        {
            var bits = BinaryPrimitives.ReadUInt32LittleEndian(bytes[21..25]);
            var width = (int)(bits & 0x3fff) + 1;
            var height = (int)((bits >> 14) & 0x3fff) + 1;
            return Complete("image/webp", width, height, out image);
        }

        if (chunk.SequenceEqual("VP8 "u8) &&
            bytes.Length >= 30 &&
            bytes[23] == 0x9d &&
            bytes[24] == 0x01 &&
            bytes[25] == 0x2a)
        {
            var width = BinaryPrimitives.ReadUInt16LittleEndian(bytes[26..28]) & 0x3fff;
            var height = BinaryPrimitives.ReadUInt16LittleEndian(bytes[28..30]) & 0x3fff;
            return Complete("image/webp", width, height, out image);
        }

        image = null;
        return false;
    }

    private static bool IsStartOfFrame(byte marker) =>
        marker is 0xc0 or 0xc1 or 0xc2 or 0xc3 or 0xc5 or 0xc6 or 0xc7 or
            0xc9 or 0xca or 0xcb or 0xcd or 0xce or 0xcf;

    private static int ReadUInt24LittleEndian(ReadOnlySpan<byte> bytes) =>
        bytes[0] | bytes[1] << 8 | bytes[2] << 16;

    private static bool Complete(
        string mimeType,
        int width,
        int height,
        out InspectedImage? image)
    {
        if (width <= 0 || height <= 0)
        {
            image = null;
            return false;
        }

        image = new InspectedImage(mimeType, width, height);
        return true;
    }
}
