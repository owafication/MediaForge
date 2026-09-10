using MediaForge.Models;

namespace MediaForge.Services;

public interface IMediaConversionService
{
    Task<ConversionResult> ConvertAsync(
        MediaJob job,
        ConversionOptions options,
        IProgress<double>? progress,
        Action<string>? log,
        CancellationToken cancellationToken);
}
