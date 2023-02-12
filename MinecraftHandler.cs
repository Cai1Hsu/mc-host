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

    public List<string> StoragedLog { get; set; } = null!;

    public int LogReadPosition = 0;

    public StreamReader LogReader { get; set; } = null!;

    public StreamWriter LogWriter { get; set; } = null!;

    public MinecraftHandler(string jrePath, string jvmArgs)
    {
        this.jrePath = jrePath;
        this.jvmArgs = jvmArgs;

        psi = new ProcessStartInfo(this.jrePath, this.jvmArgs);
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardInput =true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        CrashTimes = new List<DateTime>();
        OnlinePlayers = new List<string>();
        PlayerPlayTime = new Dictionary<string, long>();

    }
    
    public void StartMinecraft()
    {
        java = Process.Start(psi) ?? throw new Exception("Failed to start Minecraft Server. Are you sure you have Java installed?");
        Task.Delay(100).Wait();
        if (java.HasExited)
            throw new Exception("Java exited before we could handle it. Please check the output for more information.");
        StoragedLog = new List<string>();
        LogReadPosition = 0;
        LogReader = java.StandardOutput;
        LogWriter = new StreamWriter($"MinecraftServer-{CrashTimes.Count}.log", true);
    }

    public void StopMinecraft()
    {
        java.Kill();
    }

    public void UpdateStoragedLog()
    {
        java.StandardOutput.ReadToEnd().Split("\n").ToList().ForEach(line => StoragedLog.Add(line));
    }

    public void SendCommand(string command)
    {
        java.StandardInput.WriteLine(command);
    }

    public string GetOnlinePlayers()
    {
        SendCommand("list");
        string output = StoragedLog.Last();
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

    public void WriteJavaLog()
    {
        for (int i = LogReadPosition; i < StoragedLog.Count; i++)
        {
            LogWriter.WriteLine(StoragedLog[i]);
        }
    }

    public void Loop(){
        UpdateStoragedLog();
        UpdateCrashTime();
        WriteJavaLog();

        // Update Players every 30 seconds
        if (LoopCount % 6 == 0){
            UpdateOnlinePlayers();

            // Update Player Play Time every 1 minutes
            if(LoopCount % 12 == 0)  UpdatePlayerPlayTime();
        }

        RestartIfCrashed();
        LoopCount++;
        Task.Delay(1000 * 5).Wait();
    }
}