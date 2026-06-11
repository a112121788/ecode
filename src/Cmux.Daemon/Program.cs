using Cmux.Core.IPC;
using Cmux.Daemon;

// 通过命名互斥体进行单实例检查
const string MutexName = "Global\\CmuxDaemon";
using var mutex = new Mutex(true, MutexName, out bool createdNew);
if (!createdNew)
{
    Log("cmux-daemon is already running (mutex exists). Exiting.");
    return 1;
}

Log($"[cmux-daemon] Starting (PID {Environment.ProcessId})...");

var sessionManager = new DaemonSessionManager();
var pipeServer = new DaemonPipeServer(sessionManager);

using var cts = new CancellationTokenSource();

// 空闲超时：在没有客户端连接且没有活动会话的情况下，24 小时后退出
var idleTimeout = TimeSpan.FromHours(24);
DateTime lastActivity = DateTime.UtcNow;

pipeServer.ClientConnected += () =>
{
    lastActivity = DateTime.UtcNow;
    Log($"[cmux-daemon] Client connected (total: {pipeServer.ConnectedClients})");
};

pipeServer.ClientDisconnected += () =>
{
    lastActivity = DateTime.UtcNow;
    Log($"[cmux-daemon] Client disconnected (total: {pipeServer.ConnectedClients}, sessions: {sessionManager.ActiveSessionCount})");
};

sessionManager.SessionCreated += paneId =>
{
    lastActivity = DateTime.UtcNow;
    Log($"[cmux-daemon] Session created: {paneId} (total: {sessionManager.ActiveSessionCount})");
};

sessionManager.SessionExited += (paneId, exitCode) =>
{
    Log($"[cmux-daemon] Session exited: {paneId} code={exitCode} (total: {sessionManager.ActiveSessionCount})");
};

Log("[cmux-daemon] Starting pipe server...");
// 在专用后台线程上运行管道服务器（同步 I/O）
var serverThread = new Thread(() =>
{
    try { pipeServer.Run(cts.Token); }
    catch (OperationCanceledException) { }
})
{
    IsBackground = true,
    Name = "PipeServer-Accept",
};
serverThread.Start();
Log("[cmux-daemon] Pipe server started, waiting for connections...");

// 空闲监控循环 —— 阻塞主线程直到关闭
while (!cts.Token.IsCancellationRequested)
{
    try
    {
        Thread.Sleep(TimeSpan.FromMinutes(5));
    }
    catch (ThreadInterruptedException) { break; }

    if (pipeServer.ConnectedClients == 0
        && sessionManager.ActiveSessionCount == 0
        && DateTime.UtcNow - lastActivity > idleTimeout)
    {
        Log("[cmux-daemon] Idle timeout reached. Shutting down.");
        cts.Cancel();
    }
}

Log($"[cmux-daemon] Shutting down (sessions: {sessionManager.ActiveSessionCount})...");
sessionManager.Dispose();
Log("[cmux-daemon] Stopped.");
return 0;

// 写入与 WPF 客户端共用的 daemon-debug.log
static void Log(string message) => DaemonClient.LogDaemon(message);
