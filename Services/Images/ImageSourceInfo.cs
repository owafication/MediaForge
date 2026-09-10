namespace MediaForge.Services.Images;

public sealed record ImageSourceInfo(
    int Width,
    int Height,
    int EstimatedBytesPerPixel)
{
    public long PixelCount => (long)Width * Height;
    public long EstimatedDecodedBytes
    {
        get
        {
            var bytesPerPixel = Math.Max(1, EstimatedBytesPerPixel);
            return PixelCount > long.MaxValue / bytesPerPixel
                ? long.MaxValue
                : PixelCount * bytesPerPixel;
        }
    }
}
