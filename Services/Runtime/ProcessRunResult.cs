namespace MediaForge.Services.Runtime;

public sealed record ProcessRunResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);
