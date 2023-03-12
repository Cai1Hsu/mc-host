using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

class HttpServer
{
    private string[] prefixes = new string[] {
        "http://localhost:",
        "http://+:"
    };
    private HttpListener listener;

    private MinecraftHandler MinecraftServer { get; set; }

    public string Title { get; set; }

    public HttpServer(string port, MinecraftHandler minecraftServer, string title)
    {
        port = port == null ? "8080" : port;
        this.MinecraftServer = minecraftServer;
        this.Title = title;

        listener = new HttpListener();
        listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
        listener.IgnoreWriteExceptions = true;
        foreach (string s in prefixes)
        {
            listener.Prefixes.Add($"{s}{port}/");
        }
    }

    private void ListenerCallback(IAsyncResult result)
    {
        HttpListener? listener = (HttpListener?)result.AsyncState;
        if (listener == null) return;

        HttpListenerContext context = listener.EndGetContext(result);
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        string responseString = HandleRequest(request, response);

        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.ContentEncoding = Encoding.UTF8;
        response.AppendHeader("Access-Control-Allow-Origin", "*");
        response.AppendHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.AppendHeader("Access-Control-Allow-Credentials", "true");
        response.AppendHeader("Server", "Minecraft Server");
        response.AppendHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
        response.AppendHeader("Access-Control-Max-Age", "1728000");
        System.IO.Stream output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
        listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
    }

    private String HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        if (request.Url == null) return string.Empty;
        string absolutePath = request.Url.AbsolutePath.ToLower();
        // return homepage if no path is specified or not found
        if (absolutePath == "/" || absolutePath == "/home")
        {
            response.StatusCode = 200;
            response.ContentType = "text/html";
            return Homepage();
        }
        // if path is /Restart
        if (absolutePath == "/restart")
        {
            response.StatusCode = 200;
            response.ContentType = "text/html";
            Task.Run(() => MinecraftServer.Restart());
            return $"<html><head><title>{Title}</title></head><body><h1>{Title}</h1><p>Restarting server...</p></body></html>";
        }

        // if path is /Stop
        if (absolutePath == "/stop")
        {
            response.StatusCode = 200;
            response.ContentType = "text/html";
            MinecraftServer.AutoRestart = false;
            MinecraftServer.Quit = true;
            Task.Run(() => MinecraftServer.TerminateServer());
            return $"<html><head><title>{Title}</title></head><body><h1>{Title}</h1><p>Stopping server...</p></body></html>";
        }

        // if path is /Log
        if (absolutePath == "/log")
        {
            response.StatusCode = 200;
            response.ContentType = "text/plain";
            return MinecraftServer.ServerLogBuilder.ToString();
        }

        // if path is /players
        if (absolutePath == "/players" || absolutePath == "/player")
        {
            response.StatusCode = 200;
            response.ContentType = "text/html";
            StringBuilder sb = new StringBuilder();
            sb.Append($"<html><head><title>{Title}</title></head><body><h1>{Title}</h1><p>Players:</p><ul>");
            foreach (string player in MinecraftServer.OnlinePlayers.Keys)
            {
                sb.Append($"<li>{player}</li>");
            }
            sb.Append("</ul></body></html>");
            return sb.ToString();
        }

        // if path is /Messages
        if (absolutePath == "/messages")
        {
            response.StatusCode = 200;
            response.ContentType = "text/html";
            StringBuilder sb = new StringBuilder();
            sb.Append($"<html><head><title>{Title}</title></head><body><h1>{Title}</h1><h2>Messages:</h2><ul>");
            foreach (PlayerMessage message in MinecraftServer.MessageList)
            {
                sb.Append($"<li>[{message.Time.ToShortTimeString()}] &lt;{message.Sender}&gt;: {message.Content}</li>");
            }
            sb.Append("</ul></body></html>");
            return sb.ToString();
        }

        // if path is /statistics
        if (absolutePath == "/statistics")
        {
            response.StatusCode = 200;
            response.ContentType = "text/html";
            StringBuilder sb = new StringBuilder();
            sb.Append($"<html><head><title>{Title}</title></head><body><h1>{Title}</h1><p>Statistics:</p><ul>");
            foreach (string player in MinecraftServer.OnlinePlayers.Keys)
            {
                sb.Append($"<li>{player}: {(int)Math.Ceiling(MinecraftServer.GetPlayerPlayTime(player).TotalMinutes)} minutes</li>");
            }
            sb.Append("</ul></body></html>");
            return sb.ToString();
        }

        // if path is /admin
        if (absolutePath == "/admin")
        {
            response.StatusCode = 200;
            response.ContentType = "text/html";
            return $"<html><head><title>{Title}</title></head><body><h1>{Title}</h1>"
                + "<p>Admin page</p><p>Exec Command</p><input type=\"text\"id=\"command\"name=\"command\"value=\"\"/><button type=\"button\"onclick=\"exec()\">send</button><p>Send Message</p><input type=\"text\"id=\"message\"name=\"message\"value=\"\"/><button type=\"button\"onclick=\"say()\">say</button><p>Control</p><button type=\"button\"onclick=\"window.location.href='/stop'\">stop</button><button type=\"button\"onclick=\"window.location.href='/restart'\">restart</button><button type=\"button\"onclick=\"window.location.href='/save'\">save world</button><button type=\"button\"onclick=\"window.location.href='/printstat'\">print stat</button><script>function exec(){var command=document.getElementById(\"command\").value;var xhr=new XMLHttpRequest();xhr.open(\"GET\",\"/exec?command=\"+command,true);xhr.send()}function say(){var message=document.getElementById(\"message\").value;var xhr=new XMLHttpRequest();xhr.open(\"GET\",\"/say?message=\"+message,true);xhr.send()}function stop(){var xhr=new XMLHttpRequest();xhr.open(\"GET\",\"/stop\",true);xhr.send()}function restart(){var xhr=new XMLHttpRequest();xhr.open(\"GET\",\"/restart\",true);xhr.send()}</script></body></html>";
        }

        // if path is /exec
        if (absolutePath == "/exec")
        {
            response.StatusCode = 200;
            response.ContentType = "text/html";

            string? command = request.QueryString["command"];
            if (command == null) return $"<html><head><title>{Title}</title></head><body><h1>{Title}</h1><p>Command not found</p></body></html>";

            Task.Run(() => MinecraftServer.SendCommand(command));
            return $"<html><head><title>{Title}</title></head><body><h1>{Title}</h1><p>Sent command:{command}</p></body></html>";
        }

        // if path is /say
        if (absolutePath == "/say")
        {
            response.StatusCode = 200;
            response.ContentType = "text/html";
            string? message = request.QueryString["message"];
            if (message == null) return $"<html><head><title>{Title}</title></head><body><h1>{Title}</h1><p>Message not found</p></body></html>";
            Task.Run(() => MinecraftServer.SendCommand($"say {message}"));
            return $"<html><head><title>{Title}</title></head><body><h1>{Title}</h1><p>Sent message: {message}</p></body></html>";
        }

        // if path is /save
        if (absolutePath == "/save")
        {
            response.StatusCode = 200;
            response.ContentType = "text/html";
            Task.Run(() => MinecraftServer.SendCommand("save-all"));
            return $"<html><head><title>{Title}</title></head><body><h1>{Title}</h1><p>World Saving</p></body></html>";
        }

        // if path is /printstat
        if (absolutePath == "/printstat")
        {
            response.StatusCode = 200;
            response.ContentType = "text/html";
            Task.Run(() => MinecraftServer.PublicPrintOnlineStatistics());
            return $"<html><head><title>{Title}</title></head><body><h1>{Title}</h1><p>Printed Statistics</p></body></html>";
        }

        // if path is not found
        response.StatusCode = 404;
        return $"<html><head><title>{Title}</title></head><body><h1>{Title}</h1><p>404 Not Found</p><p>Click <a href='/'>here</a> to return to homepage</p></body></html>";
    }

    private string Homepage()
    {
        //"<html><head><title>Minecraft Server</title></head><body><h1>MC-HOST Minecraft Server</h1><p>Online Players:</p><ul><li></li></ul><p>Statistics</p><ul><li></li></ul><p>Available commands:</p><ul><li><a href=\"/Log\">Log</a></li><li><a href=\"/Players\">Players</a></li><li><a href=\"/Messages\">Messages List</a></li><li><a href=\"/Statistics\">Statistics</a></li></ul><p>Find this this host on <a href=\"https://github.com/cai1hsu/mc-host\">GitHub</a></p></body></html>";
        StringBuilder sb = new StringBuilder($"<html><head><title>{Title}</title></head><body><h1>{Title}</h1>");
        sb.Append("<p>Server Status:</p><ul>");
        sb.Append($"<li>Server is {(MinecraftServer.IsDone ? "Running" : "Initializing")}</li>");
        sb.Append("</ul>");

        sb.Append("<p>Online Players:</p><ul>");
        foreach (string player in MinecraftServer.OnlinePlayers.Keys)
        {
            sb.Append($"<li>{player}</li>");
        }
        sb.Append("</ul>");

        sb.Append("<p>Statistics:</p><ul>");
        foreach (string player in MinecraftServer.OnlinePlayers.Keys)
        {
            sb.Append($"<li>{player}: {(int)Math.Ceiling(MinecraftServer.GetPlayerPlayTime(player).TotalMinutes)} minutes</li>");
        }
        sb.Append("</ul>");

        sb.Append("<p>Available commands:</p><ul><li><a href=\"/Log\">Server Log</a></li><li><a href=\"/Players\">Online Players</a></li><li><a href=\"/Messages\">Messages List</a></li><li><a href=\"/Statistics\">Time Statistics</a></li></ul><p>Find this host on <a href=\"https://github.com/cai1hsu/mc-host\">GitHub</a></p></body></html>");
        return sb.ToString();
    }

    private void Stop()
    {
        listener.Stop();
        listener.Close();
    }

    public void Run()
    {
        listener.Start();

        listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
    }
}