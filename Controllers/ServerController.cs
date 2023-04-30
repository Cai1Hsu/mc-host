using mchost.Server;
using mchost.Utils;
using Microsoft.AspNetCore.Mvc;

namespace mchost.Controllers;

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

        host?.StartServer(jre, args);

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

        host.SendCommand(command);

        return true;
    }

    [HttpPost("RunCommandAs")]
    public bool RunCommandAs(string player, string command)
    {
        if (host == null || player == null || command == null) return false;

        host.RunCommandAs(player, command);

        return true;
    }

    [HttpPost("TellRaw")]
    public bool TellRaw(string player, string msg)
    {
        if (host == null || player == null || msg == null) return false;

        host.TellRaw(player, msg);

        return true;
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
        if (host == null) return "Host offline";

        if (host.serverProcess == null) return "Process offline";

        if (!host.HasRunningInstence) return "Process offline";

        if (host.IsDone) return "Running";

        if (host.HasIntilizedInstence) return "Prepairing to run";

        return "Intilizing";
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

        if (host?.onlineBoardManager!= null) res.Add("OnlineBoardManager");

        if (host?.customCommandManager != null) res.Add("CustomCommandManager");

        if (host?.serverProcess != null) res.Add("ServerProcessManager");
    
        return res;
    }
}