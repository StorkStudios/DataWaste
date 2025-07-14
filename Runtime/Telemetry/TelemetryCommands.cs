using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "Telemetry Commands", menuName = "Singletons/Commands/Telemetry Commands")]
public class TelemetryCommands : TerminalCommandBase
{
    private const string telemetryCommandName = "message";
    private const string serverStatusCommandName = "serverStatus";

    public override void RegisterCommands()
    {
        CommandTerminal.Terminal.Shell.AddCommand(telemetryCommandName, SendTelemetryMessageCommandWrapper, 1, 1, "Send telemetry message");
        CommandTerminal.Terminal.Shell.AddCommand(serverStatusCommandName, GetTelemetryServerStatusCommandWrapper, 0, 0, "Check telemetry server status");
    }

    public override void UnregisterCommands()
    {
        CommandTerminal.Terminal.Shell.RemoveCommand(telemetryCommandName);
    }

    private void SendTelemetryMessageCommandWrapper(CommandTerminal.CommandArg[] args)
    {
        SendTelemetryMessageCommand(args[0].String);
    }

    private void SendTelemetryMessageCommand(string content)
    {
        TelemetryMessage telemetryMessage = new TelemetryMessage("terminalMessage");
        telemetryMessage.AddProperty("content", content);
        Telemetry.Instance.SendTelemetryMessage(telemetryMessage);
    }

    private void GetTelemetryServerStatusCommandWrapper(CommandTerminal.CommandArg[] _) => GetTelemetryServerStatusCommand();

    private void GetTelemetryServerStatusCommand()
    {
        CommandTerminal.Terminal.AddLogEntry("Checking server status...");
        Task<string> task = Telemetry.Instance.GetServerStatus();
        task.ContinueWith(t =>
        {
            if (t.Result == "OK")
            {
                CommandTerminal.Terminal.AddLogEntry("Server is running");
            }
            else
            {
                CommandTerminal.Terminal.AddLogEntry("Server is not running");
            }
        });
    }
}
