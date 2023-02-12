// using System.Net;

// class HttpServer
// {
//     private string[] prefixes = new string[] { 
//         "http://localhost:",
//     };
//     private HttpListener listener;

//     private MinecraftHandler minecraftServer;

//     public HttpServer(string port , MinecraftHandler minecraftServer)
//     {
//         this.minecraftServer = minecraftServer;

//         listener = new HttpListener();
//         foreach (string s in prefixes)
//         {
//             listener.Prefixes.Add($"{s}{port}/");
//         }
//     }

//     private void ListenerCallback(IAsyncResult result)
//     {
//         HttpListener listener = (HttpListener)result.AsyncState;
//         HttpListenerContext context = listener.EndGetContext(result);
//         HttpListenerRequest request = context.Request;
//         HttpListenerResponse response = context.Response;
//         string responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
//         byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
//         response.ContentLength64 = buffer.Length;
//         System.IO.Stream output = response.OutputStream;
//         output.Write(buffer, 0, buffer.Length);
//         output.Close();
//         listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
//     }

//     private void Stop()
//     {
//         listener.Stop();
//         listener.Close();
//     }

//     private void Run()
//     {
//         listener.Start();

//         listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
//     }
// } 