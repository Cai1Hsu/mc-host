static class SaveSync
{
    public static MinecraftHandler minecraftServer = null!;
    
    public static void SaveWorld()
    {
        minecraftServer.SendCommand("save-all");

        GitLFSHelper.AddAll();
    }

    public static bool IsInitialized()
    {
        return File.Exists(".git");
    }

    public static void Initialize()
    {
        if (IsInitialized())
        {
            Console.WriteLine("Git repository already initialized");
            return;
        }

        GitLFSHelper.Initialize();
        GitLFSHelper.Install();
        GitLFSHelper.Add(".gitattributes");
        GitLFSHelper.Commit("Initialize Git LFS");
    }
}