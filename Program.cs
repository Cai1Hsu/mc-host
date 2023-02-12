// See https://aka.ms/new-console-template for more information
string cwd = Environment.CurrentDirectory;

string jrePath = "java";

string jvmArgs = "";

string port = "8080";

foreach (string arg in args)
{
    string[] split = arg.Split('=');

    string key = split[0];
    string val = split.Length > 1 ? split[1] : string.Empty;

    switch (key)
    {
        case "-java":
            jrePath = val;
            if (!File.Exists(jrePath)){
                Console.WriteLine("Java Runtime Environment not found at " + jrePath);
                Console.WriteLine("Please specify the path to the java executable with the -java argument");
                Console.WriteLine("Falling back to default java executable");
                jrePath = "java";
            }
            break;

        case "-port":
            port = val == string.Empty ? "8080" : val;
            break;

        default:
            jvmArgs += arg + " ";
            break;
    }
}

MinecraftHandler minecraftServer = new MinecraftHandler(jrePath, jvmArgs);
HttpServer httpServer = new HttpServer(port, minecraftServer);

Console.WriteLine("Starting Minecraft Server with following arguments: " + jrePath + " " + jvmArgs);

while (File.Exists($"MinecraftServer-{minecraftServer.crashTimeAddition}.log"))
{
    minecraftServer.crashTimeAddition++;
}
Console.WriteLine("Log file will be saved as MinecraftServer-" + minecraftServer.crashTimeAddition + ".log");

minecraftServer.StartMinecraft();
Console.CancelKeyPress += (sender, args) => minecraftServer.StopMinecraft();

Console.WriteLine("Minecraft Server started");
Console.WriteLine("Starting http server on http://localhost:" + port);
httpServer.Run();
Console.WriteLine("--------------------\n");

int LastCyclePlayerCount = 0;

while (true){
    minecraftServer.Loop();
    LogCycle();
}

void LogCycle(){
    Console.WriteLine("Loop Count: " + minecraftServer.LoopCount + " | " + DateTime.Now.ToShortDateString());
    Console.WriteLine("--------------------");
    Console.WriteLine("Time: " + DateTime.Now.ToShortTimeString());
    Console.WriteLine("Status: " + (minecraftServer.IsDone ? "Done" : "Not Done"));

    if (minecraftServer.CrashTimes.Count > 0)
    {
        Console.WriteLine("Crash Times: " + minecraftServer.CrashTimes.Count);
        foreach (var crashTime in minecraftServer.CrashTimes)
        {
            Console.WriteLine("\tat " + crashTime.ToShortDateString());
        }
    }
    if (minecraftServer.OnlinePlayers.Count != LastCyclePlayerCount)
    {
        Console.WriteLine("Player Count: " + minecraftServer.OnlinePlayers.Count);
        foreach (var player in minecraftServer.OnlinePlayers)
        {
            Console.WriteLine("\t" + player);
        }
    }
    Console.WriteLine();
    LastCyclePlayerCount = minecraftServer.OnlinePlayers.Count;
}