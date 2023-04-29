using System;
using System.Collections.Generic;
using System.Diagnostics;
using mchost.Bossbar;
using mchost.Utils;
using mchost.Overlays;

namespace mchost.Server
{
    public class ServerHost
    {
        public ServerProcess serverProcess;

        public Dictionary<string, DateTime> OnlinePlayers;

        public Dictionary<string, TimeSpan> PlayerPlayTime;

        public OnlineBoardManager onlineBoardManager;

        public BossbarManager bossbarManager;

        
        public void SendCommand(string command) => serverProcess.SendCommand(command);

        public void TellRaw(string player, RawJson json) => SendCommand($"/tellraw {player} {json}");

        public void TellRaw(string player, string msg) => SendCommand($"/tellraw {player} {{\"text\":\"{msg}\"}}");

        public void RunAs(string player, string command) => SendCommand($"/execute as {player} run {command.TrimStart('/')}");
    
        public void HandleLog(object sender, DataReceivedEventArgs args)
        {
            string? data = args.Data;

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


    }
}