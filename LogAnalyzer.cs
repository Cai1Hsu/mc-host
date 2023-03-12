
using System;
using System.Collections.Generic;

class LogAnalyzer
{
    private MinecraftHandler localMinecraftHandler;

    public Queue<string> LogAnalysisQueue = new Queue<string>();

    public LogAnalyzer(MinecraftHandler minecraftHandler)
    {
        localMinecraftHandler = minecraftHandler;
    }

    public void AnalyzeLog(string log)
    {
        
        // [07:28:21] [Server thread/INFO]: Done (2.958s)! For help, type "help"
        if (log[0] != '[') return;

        int logTypeEndIndex = log.IndexOf("]:");
        if (logTypeEndIndex == -1 || log.Length < 12) return;

        try
        {
            string time = log[1..9];
            string logType = log[12..logTypeEndIndex];
            string logContent = log[(logTypeEndIndex + 3)..];

            if (logType.Contains("INFO"))
            {
                HandleINFO(logContent);
            }
            else if (logType.Contains("ERROR"))
            {
                HandleERROR(logContent);
            }
            else if (logType.Contains("WARN"))
            {
                HandleWARN(logContent);
            }
            /* Current not used
            else if (logType.Contains("FATAL"))
            {
            }
            */
        }
        catch (System.IndexOutOfRangeException)
        {

        }
    }

    private void HandleWARN(string logContent)
    {
        if (!localMinecraftHandler.IsDone && logContent.StartsWith("Perhaps a server is already running"))
        {
            Console.WriteLine("**** FAILED TO BIND TO PORT!");
            Console.WriteLine("Perhaps a server is already running on that port?");
            localMinecraftHandler.TerminateServer();
        }
    }

    private void HandleERROR(string logContent)
    {
        if (logContent.Contains("Failed to start the minecraft server"))
        {
            localMinecraftHandler.WriteJavaLog();
            localMinecraftHandler.TerminateServer();

            Console.WriteLine("Minecraft Server failed to start. Maybe the lock file was occupied");
            Environment.Exit(1);
        }
        else
        {
            Console.WriteLine("Minecraft Server encountered an unexpected error.");
        }
        Console.WriteLine("Error information: " + logContent);
        Console.WriteLine("Log file: " + localMinecraftHandler.LogFileName);
    }

    private void HandleINFO(string logContent)
    {
        // message
        if (logContent.StartsWith("<"))
        {
            // Someone sent a message
            string sender = logContent[1..logContent.IndexOf(">")];
            string message = logContent[(logContent.IndexOf(">") + 2)..];
            localMinecraftHandler.MessageList.Add(new PlayerMessage { Content = message, Sender = sender, Time = DateTime.Now });
        }
        // Player joined the game
        else if (logContent.Contains("joined the game"))
        {
            string playerName = logContent[0..logContent.IndexOf(" ")];
            localMinecraftHandler.OnlinePlayers.Add(playerName, DateTime.Now);
            localMinecraftHandler.RecentConnectedPlayer.Enqueue(playerName);
            localMinecraftHandler.EventToLog = true;
        }
        // Player left the game
        else if (logContent.Contains("left the game"))
        {
            string playerName = logContent[0..logContent.IndexOf(" ")];
            localMinecraftHandler.OnlinePlayers.Remove(playerName);
            localMinecraftHandler.RecentLeftPlayer.Enqueue(playerName);
            localMinecraftHandler.EventToLog = true;
        }
        else if (!localMinecraftHandler.IsDone && logContent.Contains("You need to agree to the EULA"))
        {
            localMinecraftHandler.Quit = true;
            localMinecraftHandler.AutoRestart = false;
            Console.WriteLine("You need to agree to the EULA in order to run the server.");
        }
    }

    public void AnalyzeQueuedLog()
    {
        while (LogAnalysisQueue.Count > 0)
        {
            AnalyzeLog(LogAnalysisQueue.Dequeue());
        }
    }
}