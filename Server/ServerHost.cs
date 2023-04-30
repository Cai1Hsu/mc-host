using System.Diagnostics;
using mchost.Bossbar;
using mchost.Utils;
using mchost.Overlays;
using System.Text.Json;
using System.Text;
using mchost.CustomCommand;

namespace mchost.Server
{
    public class ServerHost
    {
        public ServerProcessManager serverProcess;

        public Dictionary<string, DateTime> OnlinePlayers = new Dictionary<string, DateTime>();

        public Dictionary<string, TimeSpan> StoredPlayersPlayTime = new Dictionary<string, TimeSpan>();
        public Dictionary<string, TimeSpan> PlayersPlayTime
        {
            get
            {
                Dictionary<string, TimeSpan> result = new();

                foreach (var player in StoredPlayersPlayTime)
                {
                    result.Add(player.Key, player.Value);
                }

                foreach (var player in OnlinePlayers)
                {
                    TimeSpan time = OnlinePlayers.ContainsKey(player.Key) ? DateTime.Now - OnlinePlayers[player.Key] : TimeSpan.Zero;

                    if (result.ContainsKey(player.Key))
                        result[player.Key] += time;
                    else
                        result.Add(player.Key, time);
                }

                return result;
            }
        }

        public List<DateTime> CrashTimes { get; set; } = new List<DateTime>();

        public List<PlayerMessage> MessageList = new List<PlayerMessage>();

        public OnlineBoardManager onlineBoardManager = null!;

        public BossbarManager bossbarManager = null!;

        public CustomCommandManager customCommandManager = null!;

        public LogHandler logHandler = null!;

        public StringBuilder ServerLogBuilder { get; private set; } = new StringBuilder();

        public Thread HostThread { get; set; } = null!;

        public bool IsDone
        {
            get
            {
                return serverProcess.IsDone;
            }
        }

        public bool HasRunningInstence
        {
            get
            {
                return serverProcess.IsRunning;
            }
        }

        public bool HasIntilizedInstence
        {
            get
            {
                return serverProcess.IsIntilized;
            }
        }

        public bool AutoRestart { get; set; }

        public ServerHost()
        {
            serverProcess = new ServerProcessManager();
            // customCommandManager = new CustomCommandManager();
            // bossbarManager = new BossbarManager();
            // onlineBoardManager = new OnlineBoardManager();

            // bossbarManager.LoadBossbars();
            // onlineBoardManager.LoadScores();
            LoadMessageList();
            LoadTimeStatistics();
        }

        public void UpdateStoredPlayTime(string player)
        {
            if (!PlayersPlayTime.ContainsKey(player))
                PlayersPlayTime.Add(player, TimeSpan.Zero);

            PlayersPlayTime[player] += DateTime.Now - OnlinePlayers[player];

            try
            {
                SaveTimeStatistics();
            }
            catch (Exception e)
            {
                Logging.Logger.Log(e.Message);
            }
        }

        /// <summary>
        /// Get a player's play time. This method costs O(n) time. It's better to use <see cref="PlayersPlayTime"/> and cope with it yourself.
        /// </summary>
        public TimeSpan GetPlayerPlayTime(string player) => this.PlayersPlayTime[player];

        public bool SendCommand(string command) => serverProcess.SendCommand(command);

        public bool TellRaw(string player, RawJson json) => SendCommand($"/tellraw {player} {json.ToString()}");

        public bool TellRaw(string player, string msg) => SendCommand($"/tellraw {player} {{\"text\":\"{msg}\"}}");

        public bool RunCommandAs(string player, string command) => SendCommand($"/execute as {player} run {command.TrimStart('/')}");

        public void HandleLog(object sender, DataReceivedEventArgs args)
        {
            string? data = args.Data;

            if (data == null) return;

            if (!IsDone && data.Contains("Done")) SetDone();

            ServerLogBuilder.AppendLine(data);

            Logging.Logger.Log("[Minecraft] " + data);

            try
            {
                logHandler.AnalyzeLog(data);
            }
            catch (Exception)
            {
                Logging.Logger.Log($"Error occurred when trying to analyze log. The log \'{data}\'");
            }
        }

        public void OnServerProcessExit()
        {
            Process? java = serverProcess.java;

            if (java == null) return;

            if (java.HasExited)
            {
                if (java.ExitCode == 0) return;

                if ((DateTime.Now - java.ExitTime).TotalSeconds < 3)
                {
                    Logging.Logger.Log("Minecraft Server crashed too fast. Is there any error in the arguments?");
                    Logging.Logger.Log("We will not restart the server to prevent infinite loop.");

                    AutoRestart = false;
                }

                if (AutoRestart) StartServerUsingOldArguments();
                CrashTimes.Add(DateTime.Now);
            }
        }

        public void StopServer()
        {
            serverProcess.StopServer();
        }

        public void StartServerUsingOldArguments()
        {
            serverProcess.StartServerProcess();

            if (serverProcess.java == null) return;

            AfterStartServerHook();
        }

        public void StartServer(string jre, string args)
        {
            serverProcess.psi = new ProcessStartInfo(jre, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            serverProcess.StartServerProcess();

            if (serverProcess.java == null) return;

            AfterStartServerHook();
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
                    foreach (string player in PlayersPlayTime.Keys)
                    {
                        writer.WriteNumber(player, (int)Math.Ceiling(GetPlayerPlayTime(player).TotalMinutes));
                    }
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }
            }
            catch (Exception)
            {
                Logging.Logger.Log("Error occurred when trying to save time statistics.");
            }
        }

        public void LoadTimeStatistics()
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
                        PlayersPlayTime.Add(property.Name, TimeSpan.FromMinutes(property.Value.GetInt32()));
                    }
                }
            }
            catch (Exception)
            {
                Logging.Logger.Log("Error occurred when trying to load time statistics. File may not exist or is empty");
            }
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
                Logging.Logger.Log("Error occurred when trying to load message list. File may not exist or is empty");
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
                Logging.Logger.Log("Error occurred when trying to save message list.");
            }
        }

        public void GreetPlayer(string player)
        {
            RawJson greet = new RawJson()
                .WriteStartArray()
                .WriteStartObject()
                .WriteText($"Hello {player}, welcome to ")
                .WriteEndObject()
                .WriteStartObject()
                .WriteText("Tanghu Esports Minecraft Server")
                .WriteColor("green")
                .WriteEndObject()
                .WriteStartObject()
                .WriteText("!")
                .WriteEndObject()
                .WriteEndArray();

            TellRaw(player, greet);
            TellRaw(player, new RawJson("This is a technical preview of mchost(github.com/cai1hsu/mc-host), if you find any bugs, please report to the server owner.", "yellow"));
            TellRaw(player, new RawJson("Our server supports custom command, type .help for more info.", "yellow"));
        }

        public string GetStatus()
        {
            if (HasIntilizedInstence) return "Running";

            if (HasRunningInstence) return "Prepairing to run";

            return "Process offline";
        }

        public void AfterStartServerHook()
        {
            logHandler = new LogHandler();

            if (serverProcess.java == null) return;

            serverProcess.java.OutputDataReceived += HandleLog;
            serverProcess.java.BeginOutputReadLine();

            HostThread = new Thread(() =>
            {
                int LoopCount = 0;

                while (true)
                {
                    Thread.Sleep(1000);
                    LoopCount++;

                    if (!HasIntilizedInstence) continue;

                    if (IsDone && LoopCount % 60 == 0) SaveMessageList();

                    if (IsDone && LoopCount % 1200 == 0) SendCommand("save-all");

                    if (IsDone && LoopCount % 60 == 0) onlineBoardManager?.Update();
                }
            });

            HostThread.Start();
        }

        public void SetDone()
        {
            serverProcess.SetDone();

            customCommandManager = new CustomCommandManager();
            bossbarManager = new BossbarManager();
            onlineBoardManager = new OnlineBoardManager();

            bossbarManager.LoadBossbars();
            onlineBoardManager.LoadScores();
            bossbarManager.ShowAll();
            onlineBoardManager.Show();

            // TODO: REMOVE THIS ON RELEASE
            if (bossbarManager.Bossbars.Count == 0)
                bossbarManager.AddBossbar(new RawJson("Tanghu Esports Technical Preview").ToString());
        }

        public static void SetServerHost(ServerHost host)
        {
            MainHost = host;
        }

        public static ServerHost? GetServerHost() => MainHost;

        public static ServerHost? MainHost;
    }
}