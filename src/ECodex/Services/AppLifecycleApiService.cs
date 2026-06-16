using System.Text.Json;
using ECodex.Core.IPC.V2;
using ECodex.Core.Services;

namespace ECodex.Services;

public sealed class AppLifecycleApiService
{
    private static readonly HashSet<string> Methods = new(StringComparer.Ordinal)
    {
        "app.exit",
    };

    private readonly Func<Task<DaemonSessionTerminationResult>> _terminateDaemonSessions;
    private readonly Action _requestShutdown;

    public AppLifecycleApiService(
        Func<Task<DaemonSessionTerminationResult>> terminateDaemonSessions,
        Action requestShutdown)
    {
        _terminateDaemonSessions = terminateDaemonSessions;
        _requestShutdown = requestShutdown;
    }

    public static bool CanHandle(string? method) => method != null && Methods.Contains(method);

    public async Task<V2Response> HandleRequestAsync(V2Request request)
    {
        if (!CanHandle(request.Method))
        {
            return V2Response.FromStableError(
                request.Id,
                V2ErrorCodes.NotSupported,
                $"Unsupported app lifecycle method: {request.Method}");
        }

        var terminateTerminals = GetBoolean(request.Params, "terminateTerminals");
        var termination = terminateTerminals
            ? await _terminateDaemonSessions().ConfigureAwait(false)
            : new DaemonSessionTerminationResult(0, 0);

        _requestShutdown();

        return V2Response.FromResult(request.Id, new
        {
            exiting = true,
            terminateTerminals,
            requestedDaemonSessions = termination.Requested,
            terminatedDaemonSessions = termination.Terminated,
        });
    }

    private static bool GetBoolean(JsonElement? parameters, string propertyName)
    {
        if (parameters is not { ValueKind: JsonValueKind.Object } element ||
            !element.TryGetProperty(propertyName, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false,
        };
    }
}
