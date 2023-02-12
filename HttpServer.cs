using System.Net;
using System.Text;

class HttpServer
{
    private string[] prefixes = new string[] { 
        "http://localhost:",
    };
    private HttpListener listener;

    private MinecraftHandler minecraftServer;

    public HttpServer(string port , MinecraftHandler minecraftServer)
    {
        port = port == null ? "8080" : port;
        this.minecraftServer = minecraftServer;

        listener = new HttpListener();
        foreach (string s in prefixes)
        {
            listener.Prefixes.Add($"{s}{port}/");
        }
    }

    private void ListenerCallback(IAsyncResult result)
    {
        HttpListener? listener = (HttpListener?)result.AsyncState;
        if(listener == null) return;
        HttpListenerContext context = listener.EndGetContext(result);
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;
        StringBuilder sb = new StringBuilder();
        foreach (string line in minecraftServer.StoragedLog) sb.AppendLine(line);
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        response.ContentLength64 = buffer.Length;
        System.IO.Stream output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
        listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
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