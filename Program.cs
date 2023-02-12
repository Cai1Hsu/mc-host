// See https://aka.ms/new-console-template for more information
string cwd = Environment.CurrentDirectory;

string jrePath = "";

string jvmArgs = "";

foreach (string arg in args)
{
    string[] split = arg.Split('=');

    string key = split[0];
    string val = split.Length > 1 ? split[1] : string.Empty;

    switch (key)
    {
        case "-java":
            jrePath = val != "" ? val : "java";
            if (!File.Exists(jrePath)){
                Console.WriteLine("Java Runtime Environment not found at " + jrePath);
                Console.WriteLine("Please specify the path to the java executable with the -java argument");
                Console.WriteLine("Falling back to default java executable");
                jrePath = "java";
            }
            break;

        default:
            jvmArgs += arg + " ";
            break;
    }
}

MinecraftHandler minecraftServer = new MinecraftHandler(jrePath, jvmArgs);

Console.WriteLine("Starting Minecraft Server with following arguments:\n" + jrePath + " " + jvmArgs);
Console.WriteLine("--------------------\n");
minecraftServer.StartMinecraft();

int LastCyclePlayerCount = 0;

while (true){
    minecraftServer.Loop();
    LogCycle();
}

void LogCycle(){
    Console.WriteLine("Loop Count: " + minecraftServer.LoopCount + " | " + DateTime.Now.ToShortTimeString());
    Console.WriteLine("--------------------\n");
    if (minecraftServer.CrashTimes.Count > 0)
    {
        Console.WriteLine("Crash Times: " + minecraftServer.CrashTimes.Count);
        foreach (var crashTime in minecraftServer.CrashTimes)
        {
            Console.WriteLine(crashTime.ToShortTimeString());
        }
    }
    if (minecraftServer.OnlinePlayers.Count != LastCyclePlayerCount)
    {
        Console.WriteLine("Player Count: " + minecraftServer.OnlinePlayers.Count);
        foreach (var player in minecraftServer.OnlinePlayers)
        {
            Console.WriteLine(player);
        }
    }
    LastCyclePlayerCount = minecraftServer.OnlinePlayers.Count;
}