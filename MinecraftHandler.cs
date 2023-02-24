using System.Diagnostics;
using System.Text;

class MinecraftHandler
{
    private string jrePath;

    private string jvmArgs;

    public Process java { get;set; } = null!;

    private ProcessStartInfo psi;

    public List<DateTime> CrashTimes { get; set; }

    public Dictionary<string, DateTime> OnlinePlayers { get; set; }

    public Dictionary<string, TimeSpan> PlayerPlayTime { get; set; }

    public int LoopCount { get; set; } = 0;

    public List<string> StoragedLog { get; set; } = null!;

    public int crashTimeAddition = 0;

    private Queue<String> JavaLogWritingBuffer = new Queue<string>();

    public bool IsDone { get; set; } = false;

    private Queue<string> LogAnalysisQueue = new Queue<string>();

    private Queue<string> RecentConnectedPlayer = new Queue<string>();

    private Queue<string> RecentLeftPlayer = new Queue<string>();

    public string LogFileName = string.Empty;

    public bool IsInitialized { get; set; } = false;

    public List<MinecraftMessage> MessageList { get; set; } = new List<MinecraftMessage>();

    public StringBuilder ServerLogBuilder { get; set; } = new StringBuilder();

    public bool AutoRestart { get; set; } = true;

    public bool Quit { get; set; } = false;

    private bool EventToLog = true;

    public MinecraftHandler(string jrePath, string jvmArgs, bool autoRestart = true)
    {
        this.jrePath = jrePath;
        this.jvmArgs = jvmArgs;
        this.AutoRestart = autoRestart;

        psi = new ProcessStartInfo(this.jrePath, this.jvmArgs);
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardInput = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;

        CrashTimes = new List<DateTime>();
        OnlinePlayers = new Dictionary<string, DateTime>();
        PlayerPlayTime = new Dictionary<string, TimeSpan>();
        EventToLog = true;
    }
    
    public void StartMinecraft()
    {
        java = Process.Start(psi) ?? throw new Exception("Failed to start Minecraft Server. Are you sure you have Java installed?");
        
        if (java.HasExited)
            throw new Exception("Java exited before we could handle it. Please check the output for more information.");
        
        java.OutputDataReceived += (sender, args) => HandleNewLog(args.Data);
        java.BeginOutputReadLine();
        
        StoragedLog = new List<string>();
        IsDone = false;
        LogAnalysisQueue.Clear();
        LogFileName = $"MinecraftServer-{CrashTimes.Count + crashTimeAddition}.log";

        IsInitialized = true;
    }

    public void StopServer()
    {
        IsInitialized = false;
        java.Kill();
    }

    public void HandleNewLog(string? data)
    {
        if (data == null) return;


        if(!IsDone && data.Contains("Done"))
        {
            IsDone = true;
            EventToLog = true;
        }
            

        StoragedLog.Add(data);
        ServerLogBuilder.AppendLine(data);
        JavaLogWritingBuffer.Enqueue(data);
        LogAnalysisQueue.Enqueue(data);
    }

    private void AnalyzeQueuedLog(){
        while (LogAnalysisQueue.Count > 0){
            string log = LogAnalysisQueue.Dequeue();
            // [07:28:21] [Server thread/INFO]: Done (2.958s)! For help, type "help"
            if (log[0] != '[') continue;
            
            int logTypeEndIndex = log.IndexOf("]:");
            if (logTypeEndIndex == -1) continue;
            
            try
            {
                string time = log[1..9];
                string logType = log[12..logTypeEndIndex];
                string logContent = log[(logTypeEndIndex + 3)..];

                if (logType.Contains("INFO"))
                {
                    // Player joined the game
                    if (logContent.Contains("joined the game"))
                    {
                        string playerName = logContent[0..logContent.IndexOf(" ")];
                        OnlinePlayers.Add(playerName, DateTime.Now);
                        RecentConnectedPlayer.Enqueue(playerName);
                        EventToLog = true;
                    }
                    // Player left the game
                    else if (logContent.Contains("left the game"))
                    {
                        string playerName = logContent[0..logContent.IndexOf(" ")];
                        OnlinePlayers.Remove(playerName);
                        RecentLeftPlayer.Enqueue(playerName);
                        EventToLog = true;
                    }
                    // message
                    else if (logContent.StartsWith("<"))
                    {
                        // Someone sent a message
                        string sender = logContent[1..logContent.IndexOf(">")];
                        string message = logContent[(logContent.IndexOf(">") + 2)..];
                        MessageList.Append(new MinecraftMessage{ Content = message, Sender = sender, Time = DateTime.Now });
                    }
                }
                else if (logType.Contains("ERROR"))
                {
                    if (logContent.Contains("Failed to start the minecraft server")){
                        Quit = true;
                        AutoRestart = false;
                        WriteJavaLog();
                        StopServer();
                        
                        Console.WriteLine("Minecraft Server failed to start. Please check the log for more information.");
                    }
                    else
                    {
                        Console.WriteLine("Minecraft Server encountered an unexpected error.");
                    }
                    Console.WriteLine("Error information: " + logContent);
                    Console.WriteLine("Log file: " + LogFileName);

                    Environment.Exit(1);
                }
                /* Current not used
                else if (logType.Contains("WARN"))
                {
                }
                else if (logType.Contains("FATAL"))
                {
                }
                */
            }
            catch (System.IndexOutOfRangeException)
            {
                continue;
            }
        }
    }

    public string SendCommand(string command)
    {
        java.StandardInput.WriteLine(command);
        Task.Delay(100).Wait();
        return StoragedLog.Last();
    }

    public void SeverMessage(string message)
    {
        SendCommand($"/say {message}");
    }

    public void UpdatePlayerPlayTime()
    {
        foreach (string player in OnlinePlayers.Keys)
        {
            if (!PlayerPlayTime.ContainsKey(player))
                PlayerPlayTime.Add(player, TimeSpan.Zero);
            
            PlayerPlayTime[player] += DateTime.Now - OnlinePlayers[player];
            
            OnlinePlayers[player] = DateTime.Now;
        }
    }

    public void UpdateCrashTime()
    {
        if (java.HasExited){
            CrashTimes.Add(DateTime.Now);
            EventToLog = true;
        }
    }

    public void Restart()
    {
        StopServer();
        StartMinecraft();
    }

    public void RestartIfCrashed()
    {
        if (java.HasExited){
            WriteJavaLog();
            Restart();
        }
    }

    public void WriteJavaLog()
    { 
        using (StreamWriter LogWriter = new StreamWriter(LogFileName, true)){
            while (JavaLogWritingBuffer.Count > 0){
                LogWriter.WriteLine(JavaLogWritingBuffer.Dequeue());
            }
        }
        
        JavaLogWritingBuffer.Clear();
    }

    public void PrintOnlineStatistics()
    {
        if (PlayerPlayTime.Count == 0) return;
        SeverMessage("Online Time Statistics:");

        foreach (string player in PlayerPlayTime.Keys)
        {
            SeverMessage($"{player}: {PlayerPlayTime[player].TotalMinutes} minutes");
        }
    }

    private void HostLogCycle(){
        if (!EventToLog) return;

        Console.WriteLine("Loop Count: " + LoopCount + " | " + DateTime.Now.ToShortDateString());
        Console.WriteLine("--------------------");
        Console.WriteLine("Time: " + DateTime.Now.ToShortTimeString());
        Console.WriteLine("Status: " + (IsDone ? "Done" : "Not Done"));

        if (CrashTimes.Count > 0)
        {
            Console.WriteLine("Crash Times: " + CrashTimes.Count);
            foreach (var crashTime in CrashTimes)
            {
                Console.WriteLine($"  [{crashTime.ToShortDateString()}] [{crashTime.ToShortTimeString()}]");
            }
        }
        if (RecentConnectedPlayer.Count > 0)
        {
            Console.WriteLine("Player Count: " + OnlinePlayers.Count);
            Console.Write("Players: ");
            foreach (var player in OnlinePlayers)
            {
                Console.Write($"{player} ");
            }
            Console.WriteLine();

            Console.WriteLine("Recent Connected Players: " + RecentConnectedPlayer.Count);
            while (RecentConnectedPlayer.Count > 0)
            {
                Console.WriteLine($"  {RecentConnectedPlayer.Dequeue()} connected");
            }
        }
        if (RecentLeftPlayer.Count > 0)
        {
            Console.WriteLine("Recent Left Players: " + RecentLeftPlayer.Count);
            while (RecentLeftPlayer.Count > 0)
            {
                Console.WriteLine($"  {RecentLeftPlayer.Dequeue()} disconnected");
            }
        }
        Console.WriteLine();

        EventToLog = false;
    }

    public void Loop(){
        if (!IsInitialized) return;

        UpdateCrashTime();
        AnalyzeQueuedLog();

        if (LoopCount % 5 == 0) Task.Run(() => WriteJavaLog());
        
        // Update Player Play Time every 30 seconds
        if (IsDone && LoopCount % 30 == 0) UpdatePlayerPlayTime();

        if (IsDone && LoopCount % (60 * 20) == 0) SendCommand("save-all");

        if (IsDone && LoopCount % (60 * 20) == 0) Task.Run(() => PrintOnlineStatistics());

        if (AutoRestart && LoopCount % 10 == 0) RestartIfCrashed();

        HostLogCycle();

        LoopCount++;
        Thread.Sleep(1000);
    }
}