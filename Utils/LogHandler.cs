using mchost.CustomCommand;
using mchost.Server;

namespace mchost.Utils;

public class LogHandler
{
    private ServerHost? host;

    private ServerProcessManager? serverProcess;

    private CustomCommandManager? customCommandManager;

    public LogHandler()
    {
        host = ServerHost.MainHost;
        serverProcess = host?.serverProcess;
        customCommandManager = host?.customCommandManager;
    }

    public void AnalyzeLog(string log)
    {

        // [07:28:21] [Server thread/INFO]: Done (2.958s)! For help, type "help"
        if (log[0] != '[') return;

        int logTypeEndIndex = log.IndexOf("]:");
        if (logTypeEndIndex == -1 || log.Length < 12) return;

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

    private void HandleWARN(string logContent)
    {
        if (!host?.IsDone ?? false && logContent.StartsWith("Perhaps a server is already running"))
        {
            Logging.Logger.Log("**** FAILED TO BIND TO PORT!");
            Logging.Logger.Log("Perhaps a server is already running on that port?");
        }
    }

    private void HandleERROR(string logContent)
    {
        if (logContent.Contains("Failed to start the minecraft server"))
        {
            Logging.Logger.Log("Minecraft Server failed to start. Maybe the lock file was occupied");
        }
        else
        {
            Logging.Logger.Log("Minecraft Server encountered an unexpected error.");
        }
    }

    private void HandleINFO(string logContent)
    {
        // message
        if (logContent.StartsWith("<"))
        {
            // Someone sent a message
            string sender = logContent[1..logContent.IndexOf(">")];
            string message = logContent[(logContent.IndexOf(">") + 2)..];
            host?.MessageList.Add(new PlayerMessage { Content = message, Sender = sender, Time = DateTime.Now });

            if (message.StartsWith('.'))
            {
                try
                {
                    customCommandManager?.Execute(message, sender);
                }
                catch (Exception)
                {

                }
            }
        }
        // Player joined the game
        else if (logContent.Contains("joined the game"))
        {
            string playerName = logContent[0..logContent.IndexOf(" ")];
            host?.OnlinePlayers.Add(playerName, DateTime.Now);

            // This is needed in case a new player joined the game
            host?.UpdateStoredPlayTime(playerName);
            host?.onlineBoardManager.Update();

            host?.GreetPlayer(playerName);
            Logging.Logger.Log($"Player {playerName} joined the game");
        }
        // Player left the game
        else if (logContent.Contains("left the game"))
        {
            string playerName = logContent[0..logContent.IndexOf(" ")];
            host?.OnlinePlayers.Remove(playerName);

            host?.UpdateStoredPlayTime(playerName);
            host?.onlineBoardManager.Update();

            Logging.Logger.Log($"Player {playerName} left the game");
        }
        else if (!host?.IsDone ?? false && logContent.Contains("You need to agree to the EULA"))
        {
            Logging.Logger.Log("You need to agree to the EULA in order to run the server.");
        }
    }
}