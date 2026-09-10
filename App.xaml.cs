using System.Windows;
using System.Windows.Threading;
using MediaForge.Services.Runtime;

namespace MediaForge;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        try
        {
            base.OnStartup(e);
            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            ShowStartupFailure("main-window-startup", ex);
            Shutdown(-1);
        }
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        StartupDiagnostics.TryWrite("dispatcher-unhandled", e.Exception);
    }

    private static void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            StartupDiagnostics.TryWrite("appdomain-unhandled", exception);
        }
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        StartupDiagnostics.TryWrite("task-unobserved", e.Exception);
    }

    private static void ShowStartupFailure(string stage, Exception exception)
    {
        var diagnosticPath = StartupDiagnostics.TryWrite(stage, exception);
        var detail = string.IsNullOrWhiteSpace(diagnosticPath)
            ? "No diagnostic file could be written."
            : $"Diagnostic file:\n{diagnosticPath}";
        System.Windows.MessageBox.Show(
            $"MediaForge could not start.\n\n{FirstUsefulLine(exception)}\n\n{detail}",
            "MediaForge startup failure",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static string FirstUsefulLine(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
            {
                return current.Message.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim()
                    ?? "Unknown startup error.";
            }
        }
        return "Unknown startup error.";
    }
}
