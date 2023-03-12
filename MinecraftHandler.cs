using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class MinecraftHandler
{
    private string jrePath;

    private string jvmArgs;

    public Process java { get; set; } = null!;

    private ProcessStartInfo psi;

    public List<DateTime> CrashTimes { get; set; }

    public Dictionary<string, DateTime> OnlinePlayers { get; set; }

    public Dictionary<string, TimeSpan> PlayerPlayTime { get; set; }

    public int LoopCount { get; set; } = 0;

    public List<string> StoragedLog { get; set; } = null!;

    public int crashTimeAddition = 0;

    private Queue<String> JavaLogWritingBuffer = new Queue<string>();

    public bool IsDone { get; set; } = false;

    public Queue<string> RecentConnectedPlayer = new Queue<string>();

    public Queue<string> RecentLeftPlayer = new Queue<string>();

    public string LogFileName = string.Empty;

    public bool IsInitialized { get; set; } = false;

    public List<PlayerMessage> MessageList { get; set; } = new List<PlayerMessage>();

    public StringBuilder ServerLogBuilder { get; set; } = new StringBuilder();

    public bool AutoRestart { get; set; } = true;

    public bool Quit { get; set; } = false;

    public bool EventToLog = true;

    public DateTime StartTime { get; set; } = DateTime.Now;

    private LogAnalyzer logAnalyzer;

    public CustomCommandManager customCommandManager;

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
        logAnalyzer = new LogAnalyzer(this);
        customCommandManager = new CustomCommandManager(this);
        EventToLog = true;

        LoadMessageList();
    }

    public void StartMinecraft()
    {
        java = Process.Start(psi) ?? throw new Exception("Failed to start Minecraft Server. Are you sure you have Java installed?");
        StartTime = DateTime.Now;

        if (java.HasExited)
            throw new Exception("Java exited before we could handle it. Please check the output for more information.");

        java.OutputDataReceived += (sender, args) => HandleNewLog(args.Data);
        java.BeginOutputReadLine();

        StoragedLog = new List<string>();
        IsDone = false;
        logAnalyzer.LogAnalysisQueue.Clear();
        LogFileName = $"MinecraftServer-{CrashTimes.Count + crashTimeAddition}.log";

        IsInitialized = true;

        if (PlayerPlayTime.Count == 0) ReadStoredTimeStatistics();
    }

    public void TerminateServer()
    {
        SendCommand("/stop", true);
        IsInitialized = false;
        Quit = true;
        AutoRestart = false;
        java.Kill();
    }

    public void StopServer()
    {
        SendCommand("/stop", true);
        IsInitialized = false;
        java.Kill();
    }

    public void HandleNewLog(string? data)
    {
        if (data == null) return;

        if (!IsDone && data.Contains("Done"))
        {
            IsDone = true;
            EventToLog = true;
        }

        StoragedLog.Add(data);
        ServerLogBuilder.AppendLine(data);
        JavaLogWritingBuffer.Enqueue(data);
        
        try
        {
            logAnalyzer.AnalyzeLog(data);
        }
        catch(Exception)
        {
            logAnalyzer.LogAnalysisQueue.Enqueue(data);
        }
    }

    public bool SendCommand(string command, bool force = false)
    {
        if (!IsDone && !force) return false;

        try
        {
            java.StandardInput.WriteLine(command);
            return true;
        }
        catch (Exception)
        {

        }

        return false;
    }

    public void SeverMessage(string message)
    {
        SendCommand($"/say {message}");
    }

    public void ServerPrivateRawMessage(string message, string player)
    {
         SendCommand($"/tellraw {player} {{\"text\":\"{message}\"}}");
    }

    public void ServerAnnounceMessage(string message)
    {
        SendCommand($"/title @a title {message}");
    }

    public void ServerPublicRawMessage(string message)
    {
        SendCommand($"/tellraw @a {{\"text\":\"{message}\"}}");
    }

    public void UpdatePlayerPlayTime(string player)
    {
        if (!PlayerPlayTime.ContainsKey(player))
            PlayerPlayTime.Add(player, TimeSpan.Zero);

        PlayerPlayTime[player] += DateTime.Now - OnlinePlayers[player];
    }

    public void UpdateAllPlayerPlayTime()
    {
        foreach (string player in OnlinePlayers.Keys)
        {
            if (!PlayerPlayTime.ContainsKey(player))
                PlayerPlayTime.Add(player, TimeSpan.Zero);

            PlayerPlayTime[player] += DateTime.Now - OnlinePlayers[player];
        }
    }

    public TimeSpan GetPlayerPlayTime(string player)
    {
        if (!PlayerPlayTime.ContainsKey(player))
            PlayerPlayTime.Add(player, TimeSpan.Zero);

        return PlayerPlayTime[player] + (OnlinePlayers.ContainsKey(player) ? DateTime.Now - OnlinePlayers[player] : TimeSpan.Zero);
    }

    public void Restart()
    {
        StopServer();
        StartMinecraft();
    }

    public void RestartIfCrashed()
    {
        if (java.HasExited)
        {
            if (java.ExitCode == 0)
            {
                Quit = true;
                AutoRestart = false;
                
                // TODO ?
                Environment.Exit(0);
            }

            if (java.ExitTime == null || (DateTime.Now - java.ExitTime).TotalSeconds < 3)
            {
                Console.WriteLine("Minecraft Server crashed too fast. Is there any error in the arguments?");
                Console.WriteLine("We will not restart the server to prevent infinite loop.");
                Quit = true;
                AutoRestart = false;
                Environment.Exit(1);
            }

            WriteJavaLog();
            if (AutoRestart && !Quit) Restart();
            CrashTimes.Add(DateTime.Now);
            EventToLog = true;
        }
    }

    public void WriteJavaLog()
    {
        try
        {
            using (StreamWriter LogWriter = new StreamWriter(LogFileName, true))
            {
                while (JavaLogWritingBuffer.Count > 0)
                {
                    LogWriter.WriteLine(JavaLogWritingBuffer.Dequeue());
                }
            }
            JavaLogWritingBuffer.Clear();
        }
        catch (Exception)
        {
            Console.WriteLine("Error occurred when trying to write log. Some logs may lost.");
        }

    }

    public void SaveTimeStatistics()
    {
        try
        {
            using (FileStream fs = new FileStream("TimeStatistics.json", FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            using (Utf8JsonWriter writer = new Utf8JsonWriter(fs))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("PlayerPlayTime");
                writer.WriteStartObject();
                foreach (string player in PlayerPlayTime.Keys)
                {
                    writer.WriteNumber(player, (int)Math.Ceiling(GetPlayerPlayTime(player).TotalMinutes));
                }
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Error occurred when trying to save time statistics.");
        }
    }

    public void ReadStoredTimeStatistics()
    {
        try
        {
            using (FileStream fs = new FileStream("TimeStatistics.json", FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
            using (JsonDocument document = JsonDocument.Parse(fs))
            {
                JsonElement root = document.RootElement;
                JsonElement playerPlayTime = root.GetProperty("PlayerPlayTime");
                foreach (JsonProperty property in playerPlayTime.EnumerateObject())
                {
                    PlayerPlayTime.Add(property.Name, TimeSpan.FromMinutes(property.Value.GetInt32()));
                }
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Error occurred when trying to read time statistics.");
        }
    }

    public void PublicPrintOnlineStatistics()
    {
        ServerPublicRawMessage("------------------------- " + DateTime.Now.ToShortTimeString());
        ServerPublicRawMessage("Online Time Statistics:");

        foreach (string player in PlayerPlayTime.Keys)
        {
            ServerPublicRawMessage($"{player}: {(int)Math.Ceiling(GetPlayerPlayTime(player).TotalMinutes)} minutes");
        }

        ServerPublicRawMessage("-------------------------");
    }

    public void PrivatePrintOnlineStatistics(string caller)
    {
        ServerPrivateRawMessage("------------------------- " + DateTime.Now.ToShortTimeString(), caller);
        ServerPrivateRawMessage("Online Time Statistics:", caller);

        foreach (string player in PlayerPlayTime.Keys)
        {
            ServerPrivateRawMessage($"{player}: {(int)Math.Ceiling(GetPlayerPlayTime(player).TotalMinutes)} minutes", caller);
        }

        ServerPrivateRawMessage("-------------------------", caller);
    }

    private void HostLogCycle()
    {
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

    private void LoadMessageList()
    {
        try
        {
            using (FileStream fs = new FileStream("MessageList.json", FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
            using (JsonDocument document = JsonDocument.Parse(fs))
            {
                JsonElement root = document.RootElement;
                JsonElement messageList = root.GetProperty("MessageList");
                foreach (JsonElement message in messageList.EnumerateArray())
                {
                    PlayerMessage playerMessage = new PlayerMessage();
                    playerMessage.Time = new DateTime(long.Parse(message.GetProperty("Time").GetString() ?? "0"));
                    playerMessage.Sender = message.GetProperty("Player").GetString() ?? "Unknown";
                    playerMessage.Content = message.GetProperty("Message").GetString() ?? "Unknown";
                    MessageList.Add(playerMessage);
                }
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Error occurred when trying to load message list.");
        }
    }

    private void SaveMessageList()
    {
        try
        {
            using (FileStream fs = new FileStream("MessageList.json", FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            using (Utf8JsonWriter writer = new Utf8JsonWriter(fs))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("MessageList");
                writer.WriteStartArray();
                foreach (PlayerMessage message in MessageList)
                {
                    writer.WriteStartObject();
                    writer.WriteString("Time", message.Time.Ticks.ToString());
                    writer.WriteString("Player", message.Sender);
                    writer.WriteString("Message", message.Content);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Error occurred when trying to save message list.");
        }    
    }

    public void Loop()
    {
        if (!IsInitialized) return;

        logAnalyzer.AnalyzeQueuedLog();

        if (LoopCount % 5 == 0) Task.Run(() => WriteJavaLog());

        // Update Player Play Time every 30 seconds
        // if (IsDone && LoopCount % 30 == 0) UpdatePlayerPlayTime();

        if (IsDone && LoopCount % 60 == 0) SaveMessageList();

        if (IsDone && LoopCount % 60 == 0 && PlayerPlayTime.Count > 0) Task.Run(() => SaveTimeStatistics());

        if (IsDone && LoopCount % 1200 == 0) SendCommand("save-all");

        if (IsDone && LoopCount % 1200 == 0 && OnlinePlayers.Count > 0) Task.Run(() => PublicPrintOnlineStatistics());

        RestartIfCrashed();

        HostLogCycle();

        LoopCount++;
        Thread.Sleep(1000);
    }

}