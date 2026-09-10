using System.Diagnostics;
using System.IO;
using System.Text;

namespace MediaForge.Services.Runtime;

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(
        ProcessRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException("A process executable path is required.", nameof(request));
        }

        ValidateTailLimit(request.StandardOutputTailLineLimit, nameof(request.StandardOutputTailLineLimit));
        ValidateTailLimit(request.StandardErrorTailLineLimit, nameof(request.StandardErrorTailLineLimit));
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = request.FileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException($"Could not start {Path.GetFileName(request.FileName)}.");
        }

        using var cancellationRegistration = cancellationToken.Register(
            static state => TryKillProcessTree((Process)state!),
            process);

        var standardOutput = new TailLineBuffer(request.StandardOutputTailLineLimit);
        var standardError = new TailLineBuffer(request.StandardErrorTailLineLimit);
        var standardOutputTask = ReadLinesAsync(
            process.StandardOutput,
            standardOutput,
            request.StandardOutputLineReceived);
        var standardErrorTask = ReadLinesAsync(
            process.StandardError,
            standardError,
            request.StandardErrorLineReceived);

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            await Task.WhenAll(standardOutputTask, standardErrorTask).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryKillProcessTree(process);
            await WaitForTerminationAsync(process).ConfigureAwait(false);
            await ObserveReaderTasksAsync(standardOutputTask, standardErrorTask).ConfigureAwait(false);
            throw;
        }
        catch
        {
            TryKillProcessTree(process);
            await WaitForTerminationAsync(process).ConfigureAwait(false);
            await ObserveReaderTasksAsync(standardOutputTask, standardErrorTask).ConfigureAwait(false);
            throw;
        }

        return new ProcessRunResult(
            process.ExitCode,
            standardOutput.ToText(),
            standardError.ToText());
    }

    private static async Task ReadLinesAsync(
        StreamReader reader,
        TailLineBuffer buffer,
        Action<string>? lineReceived)
    {
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            buffer.Add(line);
            lineReceived?.Invoke(line);
        }
    }

    private static void ValidateTailLimit(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Tail line limits cannot be negative.");
        }
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process exited between the state check and termination request.
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Termination is best-effort here; the caller still waits and reports cancellation/failure.
        }
    }

    private static async Task WaitForTerminationAsync(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                await process.WaitForExitAsync(CancellationToken.None)
                    .WaitAsync(TimeSpan.FromSeconds(5))
                    .ConfigureAwait(false);
            }
        }
        catch (InvalidOperationException)
        {
            // The process was already disposed or never reached a waitable state.
        }
        catch (TimeoutException)
        {
            // Do not replace the original cancellation/failure with cleanup timeout noise.
        }
    }

    private static async Task ObserveReaderTasksAsync(params Task[] tasks)
    {
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch
        {
            // Preserve the original cancellation/process failure.
        }
    }

    private sealed class TailLineBuffer
    {
        private readonly int _limit;
        private readonly Queue<string> _lines = new();

        public TailLineBuffer(int limit) => _limit = limit;

        public void Add(string line)
        {
            if (_limit == 0) return;
            _lines.Enqueue(line);
            if (_limit == int.MaxValue) return;
            while (_lines.Count > _limit)
            {
                _lines.Dequeue();
            }
        }

        public string ToText()
        {
            if (_lines.Count == 0) return string.Empty;
            var builder = new StringBuilder();
            foreach (var line in _lines)
            {
                if (builder.Length > 0) builder.AppendLine();
                builder.Append(line);
            }
            return builder.ToString();
        }
    }
}
