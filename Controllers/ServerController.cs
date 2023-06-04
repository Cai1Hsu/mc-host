using Parallel.Server;
using Parallel.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Parallel.Controllers;

[ApiController]
[Route("api/server/[controller]")]
public class ServerController : ControllerBase
{
    private readonly ILogger<ServerController> _logger;

    public ServerHost? host { get; set; } = ServerHost.GetServerHost();

    public ServerController(ILogger<ServerController> logger)
    {
        _logger = logger;
    }

    [HttpPost("StartServer")]
    public bool StartServer(string jre, string args)
    {
        if (host == null || args == null) return false;

        try
        {
            host?.StartServer(jre, args);
        }
        catch (Exception e)
        {
            _logger.LogInformation(e.ToString());
            return false;
        }

        return true;
    }

    [HttpPost("StopServer")]
    public bool StopServer()
    {
        host?.StopServer();
        return true;
    }

    [HttpPost("TerminateServer")]
    public bool TerminateServer()
    {
        host?.serverProcess.Terminate();
        return true;
    }

    [HttpPost("SendCommand")]
    public bool SendCommand(string command)
    {
        if (host == null || command == null) return false;

        return host.SendCommand(command);
    }

    [HttpPost("RunCommandAs")]
    public bool RunCommandAs(string player, string command)
    {
        if (host == null || player == null || command == null) return false;

        return host.RunCommandAs(player, command);
    }

    [HttpPost("TellRaw")]
    public bool TellRaw(string player, string msg)
    {
        if (host == null || player == null || msg == null) return false;

        return host.TellRaw(player, msg);
    }

    [HttpGet("GetOnlinePlayerCount")]
    public int GetOnlinePlayerCount()
    {
        return host?.OnlinePlayers.Count ?? 0;
    }

    [HttpGet("GetOnlinePlayers")]
    public Dictionary<string, DateTime> GetOnlinePlayers()
    {
        return host?.OnlinePlayers ?? new Dictionary<string, DateTime>();
    }

    [HttpGet("GetPlayersPlayTime")]
    public Dictionary<string, TimeSpan> GetPlayersPlayTime()
    {
        return host?.PlayersPlayTime ?? new Dictionary<string, TimeSpan>();
    }

    [HttpGet("GetServerLog")]
    public string GetServerLog()
    {
        return host?.ServerLogBuilder.ToString() ?? string.Empty;
    }

    [HttpGet("GetServerStatus")]
    public string GetServerStatus()
    {
        return host?.GetStatus() ?? "Host offline";
    }

    [HttpGet("GetMessageList")]
    public List<PlayerMessage> GetMessageList()
    {
        return host?.MessageList ?? new List<PlayerMessage>();
    }

    [HttpGet("GetRunningManagers")]
    public List<string> GetRunningManagers()
    {
        var res = new List<string>();

        if (host == null) return res;

        if (host?.bossbarManager != null) res.Add("BossbarManager");

        if (host?.onlineBoardManager != null) res.Add("OnlineBoardManager");

        if (host?.customCommandManager != null) res.Add("CustomCommandManager");

        if (host?.serverProcess != null) res.Add("ServerProcessManager");

        return res;
    }
}