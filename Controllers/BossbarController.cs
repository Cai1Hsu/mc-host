using Parallel.Server;
using Microsoft.AspNetCore.Mvc;

namespace Parallel.Controllers;

[ApiController]
[Route("api/bossbar/[controller]")]
public class BossbarController : ControllerBase
{
    private ServerHost? host = ServerHost.GetServerHost();

    private readonly ILogger<BossbarController> _logger;

    public BossbarController(ILogger<BossbarController> logger)
    {
        _logger = logger;
    }


    [HttpPost("CreateBossbar")]
    public bool CreateBossbar(string name)
    {
        if (name == null || host?.bossbarManager == null) return false;

        host?.bossbarManager.AddBossbar(name);
        host?.bossbarManager.UpdateAll();

        return true;
    }

    [HttpPost("RemoveBossbar")]
    public bool RemoveBossbar(string id)
    {
        if (host == null || id == null || host?.bossbarManager == null) return false;

        host?.bossbarManager.RemoveBossbar(id);

        return true;
    }

    [HttpPost("UpdateAllBossbar")]
    public bool UpdateAllBossbar()
    {
        if (host == null || host?.bossbarManager == null) return false;

        host.bossbarManager.UpdateAll();

        return true;
    }

    [HttpGet("GetBossbars")]
    public Dictionary<string, string> GetBossbars()
    {
        if (host == null || host?.bossbarManager == null) return new Dictionary<string, string>();

        return host.bossbarManager.Bossbars.ToDictionary(bossbar => bossbar.Key.ToString(), bossbar => bossbar.Value.Name);
    }
}