using System.Globalization;
using System.Text;
using ECodex.Core.Models;

namespace ECodex.Core.Services;

public sealed class FailureLoopEvidencePreviewFormatter
{
    public string Format(FailureLoopEvidencePackage? package)
    {
        if (package is null)
            return "No failure loop evidence available.";

        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Failure Loop Evidence: {package.PackageId}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Captured: {package.CapturedAtUtc:O}");
        builder.Append("Scope: ");
        builder.Append(CultureInfo.InvariantCulture, $"workspace={package.WorkspaceId}");
        if (!string.IsNullOrWhiteSpace(package.SurfaceId))
            builder.Append(CultureInfo.InvariantCulture, $" surface={package.SurfaceId}");
        if (!string.IsNullOrWhiteSpace(package.PaneId))
            builder.Append(CultureInfo.InvariantCulture, $" pane={package.PaneId}");
        builder.AppendLine();

        if (package.FromUtc is not null || package.ToUtc is not null)
            builder.AppendLine(CultureInfo.InvariantCulture, $"Window: {FormatMaybeTime(package.FromUtc)} -> {FormatMaybeTime(package.ToUtc)}");

        AppendCommands(builder, package.Commands);
        AppendTranscripts(builder, package.Transcripts);
        AppendDaemonLogs(builder, package.DaemonLogs);
        AppendAgentMessages(builder, package.AgentMessages);

        return builder.ToString().TrimEnd();
    }

    private static void AppendCommands(StringBuilder builder, IReadOnlyList<FailureLoopCommandEvidence> commands)
    {
        builder.AppendLine();
        builder.AppendLine("Commands");
        if (commands.Count == 0)
        {
            builder.AppendLine("- none");
            return;
        }

        foreach (var command in commands)
        {
            builder.Append("- ");
            builder.Append(CultureInfo.InvariantCulture, $"{command.StartedAtUtc:O}");
            builder.Append(CultureInfo.InvariantCulture, $" exit {command.ExitCode}");
            if (!string.IsNullOrWhiteSpace(command.Command))
                builder.Append(CultureInfo.InvariantCulture, $" :: {command.Command}");
            if (!string.IsNullOrWhiteSpace(command.WorkingDirectory))
                builder.Append(CultureInfo.InvariantCulture, $" ({command.WorkingDirectory})");
            builder.AppendLine();
        }
    }

    private static void AppendTranscripts(StringBuilder builder, IReadOnlyList<FailureLoopTranscriptEvidence> transcripts)
    {
        builder.AppendLine();
        builder.AppendLine("Transcripts");
        if (transcripts.Count == 0)
        {
            builder.AppendLine("- none");
            return;
        }

        foreach (var transcript in transcripts)
        {
            builder.Append("- ");
            builder.Append(CultureInfo.InvariantCulture, $"{transcript.CapturedAtUtc:O}");
            builder.Append(CultureInfo.InvariantCulture, $" {transcript.FileName}");
            if (transcript.SummaryWasTruncated)
                builder.Append(" [truncated]");
            builder.AppendLine(CultureInfo.InvariantCulture, $" :: {transcript.Summary}");
        }
    }

    private static void AppendDaemonLogs(StringBuilder builder, IReadOnlyList<FailureLoopDaemonLogEvidence> daemonLogs)
    {
        builder.AppendLine();
        builder.AppendLine("Daemon Logs");
        if (daemonLogs.Count == 0)
        {
            builder.AppendLine("- none");
            return;
        }

        foreach (var log in daemonLogs)
        {
            builder.Append("- ");
            builder.Append(CultureInfo.InvariantCulture, $"{log.TimestampUtc:O}");
            if (!string.IsNullOrWhiteSpace(log.PaneId))
                builder.Append(CultureInfo.InvariantCulture, $" pane={log.PaneId}");
            if (log.LineWasTruncated)
                builder.Append(" [truncated]");
            builder.AppendLine(CultureInfo.InvariantCulture, $" :: {log.Line}");
        }
    }

    private static void AppendAgentMessages(StringBuilder builder, IReadOnlyList<FailureLoopAgentEvidence> agentMessages)
    {
        builder.AppendLine();
        builder.AppendLine("Agent Messages: planned source not connected");
        if (agentMessages.Count == 0)
        {
            builder.AppendLine("- none");
            return;
        }

        foreach (var message in agentMessages)
            builder.AppendLine(CultureInfo.InvariantCulture, $"- {message.CapturedAtUtc:O} {message.Role}: {message.Summary}");
    }

    private static string FormatMaybeTime(DateTimeOffset? value)
        => value is null ? "-" : value.Value.ToString("O", CultureInfo.InvariantCulture);
}
