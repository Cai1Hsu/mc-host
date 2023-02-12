using System.Diagnostics;
using System.Text;

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

    public StreamReader LogReader { get; set; } = null!;

    public StreamWriter LogWriter { get; set; } = null!;

    public int crashTimeAddition = 0;

    private StringBuilder JavaLogBuffer = new StringBuilder();

    public bool IsDone { get; set; } = false;

    public MinecraftHandler(string jrePath, string jvmArgs)
    {
        this.jrePath = jrePath;
        this.jvmArgs = jvmArgs;

        psi = new ProcessStartInfo(this.jrePath, this.jvmArgs);
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardInput = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        CrashTimes = new List<DateTime>();
        OnlinePlayers = new List<string>();
        PlayerPlayTime = new Dictionary<string, long>();

    }
    
    public void StartMinecraft()
    {
        java = Process.Start(psi) ?? throw new Exception("Failed to start Minecraft Server. Are you sure you have Java installed?");
        if (java.HasExited)
            throw new Exception("Java exited before we could handle it. Please check the output for more information.");
        java.OutputDataReceived += (sender, args) => UpdateStoragedLog(args.Data);
        java.BeginOutputReadLine();
        StoragedLog = new List<string>();
        LogWriter = new StreamWriter($"MinecraftServer-{CrashTimes.Count + crashTimeAddition}.log", new FileStreamOptions() { Access = FileAccess.Write, Mode= FileMode.Append, Share= FileShare.ReadWrite});
        IsDone = false;
    }

    public void StopMinecraft()
    {
        java.Kill();
    }

    public void UpdateStoragedLog(string? data)
    {
        if (data == null) return;


        if(!IsDone && data.Contains("Done"))
            IsDone = true;

        StoragedLog.Add(data);
        JavaLogBuffer.AppendLine(data);
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
        OnlinePlayers.Remove("");
    }

    public void UpdatePlayerPlayTime()
    {
        foreach (string player in OnlinePlayers)
        {
            if (!PlayerPlayTime.ContainsKey(player))
                PlayerPlayTime.Add(player, 0);
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
        LogWriter.WriteLine(JavaLogBuffer.ToString());
        JavaLogBuffer.Clear();
    }

    public void Loop(){
        UpdateCrashTime();
        WriteJavaLog();

        // Update Players every 30 seconds
        if (IsDone && LoopCount % 3 == 0){
            Console.WriteLine("Updating Players...");
            UpdateOnlinePlayers();

            // Update Player Play Time every 1 minutes
            if(LoopCount % 6 == 0)  UpdatePlayerPlayTime();
        }

        RestartIfCrashed();
        LoopCount++;
        Thread.Sleep(1000 * 10);
    }
}