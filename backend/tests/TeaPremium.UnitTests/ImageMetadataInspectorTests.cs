using System.Buffers.Binary;
using TeaPremium.Application.Media;

namespace TeaPremium.UnitTests;

public sealed class ImageMetadataInspectorTests
{
    [Fact]
    public void ReadsPngSignatureAndDimensions()
    {
        var bytes = CreatePngHeader(1200, 900);

        Assert.True(ImageMetadataInspector.TryInspect(bytes, out var image));
        Assert.NotNull(image);
        Assert.Equal("image/png", image.MimeType);
        Assert.Equal(1200, image.Width);
        Assert.Equal(900, image.Height);
    }

    [Fact]
    public void RejectsSvgAndArbitraryContent()
    {
        var bytes = "<svg xmlns='http://www.w3.org/2000/svg'><script /></svg>"u8.ToArray();

        Assert.False(ImageMetadataInspector.TryInspect(bytes, out var image));
        Assert.Null(image);
    }

    internal static byte[] CreatePngHeader(int width, int height)
    {
        var bytes = new byte[32];
        byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10];
        signature.CopyTo(bytes, 0);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(16, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(20, 4), height);
        return bytes;
    }
}
