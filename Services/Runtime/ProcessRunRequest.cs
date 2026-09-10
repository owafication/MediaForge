namespace MediaForge.Services.Runtime;

public sealed record ProcessRunRequest
{
    public required string FileName { get; init; }
    public IReadOnlyList<string> Arguments { get; init; } = Array.Empty<string>();
    public int StandardOutputTailLineLimit { get; init; } = int.MaxValue;
    public int StandardErrorTailLineLimit { get; init; } = int.MaxValue;
    public Action<string>? StandardOutputLineReceived { get; init; }
    public Action<string>? StandardErrorLineReceived { get; init; }
}
