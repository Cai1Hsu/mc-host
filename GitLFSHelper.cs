using System.Diagnostics;

static class GitLFSHelper
{
    const string Git = "git";

    private static string Execute(string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Git,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();

        string result = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return result;
    }

    public static void AddAll()
    {
        Execute("lfs track");
    }

    public static void Add(string file)
    {
        Execute("lfs track " + file);
    }

    public static void Remove(string file)
    {
        Execute("lfs untrack " + file);
    }

    public static void RemoveAll()
    {
        Execute("lfs untrack");
    }

    public static void Push()
    {
        Execute("lfs push");
    }

    public static void Pull()
    {
        Execute("lfs pull");
    }

    public static void Fetch()
    {
        Execute("lfs fetch");
    }

    public static void Clone(string url)
    {
        Execute("lfs clone " + url);
    }

    public static void Install()
    {
        Execute("lfs install");
    }

    public static void Uninstall()
    {
        Execute("lfs uninstall");
    }

    public static void Update()
    {
        Execute("lfs update");
    }

    public static void Commit(string message)
    {
        Execute("commit -m \"" + message + "\"");
    }

    public static string GetUserName()
    {
        return Execute("config user.name").Trim();
    }

    public static string GetUserEmail()
    {
        return Execute("config user.email").Trim();
    }

    public static bool CheckNameAndEmail()
    {
        return GetUserName() != string.Empty && GetUserEmail() != string.Empty;
    }

    public static void SetName(string name)
    {
        Execute("config user.name \"" + name + "\"");
    }

    public static void SetEmail(string email)
    {
        Execute("config user.email \"" + email + "\"");
    }

    public static void Initialize()
    {
        Execute("init");
    }
}