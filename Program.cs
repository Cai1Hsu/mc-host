using System;
using System.IO;
using System.Diagnostics;

#if DEBUG
Console.WriteLine("Running under *Debug* mode");

if (args.Length == 0)
    args = "-rm-lock -kill-jvm -Xmx1G -Xms1G -jar server.jar nogui".Split(' ');
#endif

if (args.Length == 0)
{
    PrintUsage();
    return;
}

string cwd = Environment.CurrentDirectory;

string jrePath = "java";

string jvmArgs = "";

string port = "8080";

string title = "Minecraft Server";

string jar = ".jar";

bool AutoRestart = true;

foreach (string arg in args)
{
    string[] split = arg.Split('=');

    string key = split[0];
    string val = split.Length > 1 ? split[1] : string.Empty;

    switch (key)
    {
        case "-java":
        case "--java":
            jrePath = val;
            if (!File.Exists(jrePath))
            {
                Console.WriteLine("Java Runtime Environment not found at " + jrePath);
                Console.WriteLine("Please specify the path to the java executable with the -java argument");
                Console.WriteLine("Falling back to default java executable");
                jrePath = "java";
            }
            break;

        case "-p":
        case "-port":
        case "--port":
            port = val == string.Empty ? "8080" : val;
            break;

        case "-t":
        case "--title":
            title = val == string.Empty ? "Minecraft Server" : val;
            break;

        case "--no-autorestart":
            AutoRestart = false;
            break;

        case "-?":
        case "-h":
        case "-help":
        case "--help":
            PrintUsage();
            break;

        case "-rm-lock":
        case "--rm-lock":
            RemoveLock();
            break;

        case "-kill-jvm":
        case "-kill-java":
        case "--kill-jvm":
        case "--kill-java":
            KillJava();
            break;

        default:
            jvmArgs += arg + " ";
            break;
    }

    if (arg.EndsWith(".jar")) jar = arg;
}

MinecraftHandler minecraftServer = new MinecraftHandler(jrePath, jvmArgs, AutoRestart);
HttpServer httpServer = new HttpServer(port, minecraftServer, title);

Console.WriteLine("Starting Minecraft Server with following arguments: " + jrePath + " " + jvmArgs);

while (File.Exists($"MinecraftServer-{minecraftServer.crashTimeAddition}.log"))
{
    minecraftServer.crashTimeAddition++;
}
Console.WriteLine("Log file will be saved as MinecraftServer-" + minecraftServer.crashTimeAddition + ".log");

minecraftServer.StartMinecraft();
Console.CancelKeyPress += (sender, args) => minecraftServer.TerminateServer();

Console.WriteLine("Minecraft Server started");
Console.WriteLine("Starting http server on http://localhost:" + port);
httpServer.Run();
Console.WriteLine("--------------------\n");

while (!minecraftServer.Quit)
{
    minecraftServer.Loop();
}

void PrintUsage()
{
    Console.WriteLine("Minecraft Server Wrapper");
    Console.WriteLine("Usage: ./mc-host [options]");
    Console.WriteLine("Options:");
    Console.WriteLine("  -? -h -help --help Display this help message");
    Console.WriteLine("  --java=<path>      Path to the java executable");
    Console.WriteLine("  --port/-p=<port>   Port to run the http server on");
    Console.WriteLine("  --no-autorestart   Disable automatic server restarts");
    Console.WriteLine("  -t --title=<title>  Set the title of the http server");
    Console.WriteLine("  -rm-lock --rm-lock Remove the session.lock file");
    Console.WriteLine("  -kill-jvm --kill-jvm Kill all java processes");
    Console.WriteLine("  <jvm args>         Arguments to pass to the java executable");
    Console.WriteLine("                     (e.g. -Xmx2G -Xms2G -jar server.jar nogui)");
    Environment.Exit(0);
}

void KillJava()
{
    Process[] processes = Process.GetProcessesByName("java");
    foreach (var p in processes)
    {
        try
        {
            if (p.StartInfo.Arguments.Contains(".jar") && p.StartInfo.Arguments.Contains(jar))
                p.Kill();
        }
        catch (Exception)
        {

        }
    }
}

void RemoveLock()
{
    string lock_file = Path.Combine(cwd, "world/session.lock");

    try
    {
        if (File.Exists(lock_file))
            File.Delete(lock_file);
    }
    catch (Exception)
    {

    }
}