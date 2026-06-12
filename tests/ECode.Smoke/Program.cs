using System.Diagnostics;
using System.Runtime.InteropServices;
using ECode.Core.Terminal;

namespace ECode.Smoke;

internal static class Program
{
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool FreeConsole();
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AllocConsole();

    private static StreamWriter _log = StreamWriter.Null;
    private static int _failures;
    private static int _outputEvents;
    private static int _outputBytes;

    private static void Check(string label, bool ok, string? detail = null)
    {
        var line = ok ? $"  PASS  {label}" : $"  FAIL  {label}{(detail is null ? "" : $"  -- {detail}")}";
        _log.WriteLine(line);
        if (!ok) _failures++;
    }

    private static void Log(string line) => _log.WriteLine(line);

    [STAThread]
    private static async Task<int> Main()
    {
        var logPath = Path.Combine(Path.GetTempPath(), "ecode-smoke.log");
        _log = new StreamWriter(logPath, append: false) { AutoFlush = true };
        Log($"=== ECode ConPTY smoke (log: {logPath}) ===");
        Log($"  [setup] FreeConsole result: {FreeConsole()}");

        try
        {
            await TestEnvironmentInjection();
        }
        finally
        {
            AllocConsole();
        }

        Log("");
        Log(_failures == 0 ? "ALL CHECKS PASSED" : $"{_failures} CHECK(S) FAILED");
        _log.Dispose();
        return _failures == 0 ? 0 : 1;
    }

    private static async Task TestEnvironmentInjection()
    {
        Log("\n[1] Environment injection (raw output event)");
        using var session = new TerminalSession("smoke-env", 120, 30);
        session.OutputReceived += () => Interlocked.Increment(ref _outputEvents);
        session.RawOutputReceived += bytes => Interlocked.Add(ref _outputBytes, bytes.Length);
        session.ProcessExited += () => Log("    [env] ProcessExited");
        session.Start();
        var pid = session.ProcessId;
        Log($"    [env] ProcessId={pid}");
        if (pid != null)
        {
            try { var p = Process.GetProcessById(pid.Value); Log($"    [env] process alive, hasExited={p.HasExited}"); }
            catch (Exception ex) { Log($"    [env] process lookup failed: {ex.Message}"); }
        }
        await Task.Delay(3000);
        if (pid != null)
        {
            try { var p = Process.GetProcessById(pid.Value); Log($"    [env] process after 3s, hasExited={p.HasExited}"); }
            catch (Exception ex) { Log($"    [env] process after 3s lookup failed: {ex.Message}"); }
        }
        var events = Interlocked.CompareExchange(ref _outputEvents, 0, 0);
        var bytes = Interlocked.CompareExchange(ref _outputBytes, 0, 0);
        Log($"    [env] events={events} bytes={bytes}");
        Check("ConPTY produced raw output", events > 0 && bytes > 0, $"events={events} bytes={bytes}");
    }

}
