using System.Diagnostics;

class MinecraftHandler
{
    private string jrePath;

    private string jvmArgs;

    public Process java { get;set; } = null!;

    private ProcessStartInfo psi;

    public List<DateTime> CrashTimes { get; set; }

    public List<string> OnlinePlayers { get; set; }

    public Dictionary<string, long> PlayerPlayTime { get; set; }

    public int LoopCount { get; set; } = 0;

    public MinecraftHandler(string jrePath, string jvmArgs)
    {
        this.jrePath = jrePath;
        this.jvmArgs = jvmArgs;

        psi = new ProcessStartInfo(this.jrePath, this.jvmArgs);
        psi.RedirectStandardOutput = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        CrashTimes = new List<DateTime>();
        OnlinePlayers = new List<string>();
        PlayerPlayTime = new Dictionary<string, long>();

    }
    
    public void StartMinecraft()
    {
        java = Process.Start(psi) ?? throw new Exception("Failed to start Minecraft Server. Are you sure you have Java installed?");
        string output = java.StandardOutput.ReadToEnd();
    }

    public void StopMinecraft()
    {
        java.Kill();
    }

    public string GetLog()
    {
        return java.StandardOutput.ReadToEnd();
    }

    public void SendCommand(string command)
    {
        java.StandardInput.WriteLine(command);
    }

    public string GetOnlinePlayers()
    {
        SendCommand("list");
        string output = GetLog();
        string[] outputArray = output.Split(" ");
        string players = outputArray[outputArray.Length - 1];
        return players;
    }

    public void UpdateOnlinePlayers()
    {
        OnlinePlayers = GetOnlinePlayers().Split(",").ToList();
    }

    public void UpdatePlayerPlayTime()
    {
        foreach (string player in OnlinePlayers)
        {
            PlayerPlayTime[player] = PlayerPlayTime[player] + 1;
        }
    }

    public void UpdateCrashTime()
    {
        if (java.HasExited)
            CrashTimes.Add(DateTime.Now);
    }

    public void Restart()
    {
        StopMinecraft();
        StartMinecraft();
    }

    public void RestartIfCrashed()
    {
        if (java.HasExited)
            Restart();
    }

    public void Loop(){
        UpdateCrashTime();

        // Update Players every 30 seconds
        if (LoopCount % 3 == 0){
            UpdateOnlinePlayers();
            
            // Update Player Play Time every 1 minutes
            if(LoopCount % 6 == 0)  UpdatePlayerPlayTime();
        }
        
        RestartIfCrashed();
        LoopCount++;
        Task.Delay(1000 * 10).Wait();
    }
}