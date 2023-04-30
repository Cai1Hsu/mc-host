using System.Text.Json;
using mchost.Server;
using mchost.Utils;
using mchost.Logging;

namespace mchost.CustomCommand;

public class CustomCommandManager
{
    private ServerHost? host;

    public Dictionary<string, MyCommand> BuiltInCommands = new();

    // Dictionary<player, Dictionary<command, option>>
    public Dictionary<string, Dictionary<string, string>> PrivateCommands;

    public void SetHost(ServerHost host) => this.host = host;

    public void Execute(string cmd, string player)
    {
        int separator = cmd.IndexOf(' ');

        string cmd_name = "";
        string trimed = "";
        if (separator == -1)
        {
            cmd_name = cmd.Substring(1).Trim(' ').ToLower();
        }
        else
        {
            cmd_name = cmd.Substring(1, separator).Trim(' ').ToLower();
            trimed = cmd[separator..].Trim(' ');
        }

        Logging.Logger.Log($"params : {player} ,\'{cmd_name}\','{trimed}'");

        if (BuiltInCommands.ContainsKey(cmd_name))
        {
            BuiltInCommands[cmd_name]?.Action(player, trimed);

            host?.TellRaw(player, new RawJson($"[CustomCommand] Executed built-in command: {trimed}", "yellow"));
            return;
        }

        if (PrivateCommands.ContainsKey(player))
        {
            if (PrivateCommands[player].ContainsKey(cmd_name))
            {
                new Task(() =>
                {
                    string command = PrivateCommands[player][cmd_name];

                    if (command.StartsWith("/"))
                    {
                        host?.SendCommand(command);
                        host?.TellRaw(player, new RawJson($"[CustomCommand] Executed your command.", "yellow"));
                    }
                    else
                    {
                        host?.TellRaw(player, $"{cmd_name}: {command}");
                    }
                }).Start();

                return;
            }
        }

        host?.TellRaw(player, new RawJson($"[CustomCommand] Command not found: {trimed}", "red"));
    }

    public CustomCommandManager()
    {
        this.host = ServerHost.GetServerHost();

        LoadCommands();

        if (PrivateCommands == null)
        {
            PrivateCommands = new Dictionary<string, Dictionary<string, string>>();
        }

        BindBuiltInCommands();

        void BindBuiltInCommands()
        {
            // .help
            BuiltInCommands.Add("help", new MyCommand("help", ".help            Show help message", (player, _) =>
            {
                host?.TellRaw(player, "§a§lCustomCommand Help");
                host?.TellRaw(player, "========================");
                host?.TellRaw(player, "§a§lBuilt-in Commands:");
                foreach (var cmd in BuiltInCommands)
                {
                    host?.TellRaw(player, $"  §a{cmd.Value.Command}§r: {cmd.Value.Description}");
                }
                host?.TellRaw(player, "========================");
                if (PrivateCommands.ContainsKey(player))
                {
                    host?.TellRaw(player, "§a§lCustom Commands:");
                    foreach (var cmd in PrivateCommands[player])
                    {
                        host?.TellRaw(player, $"  §a{cmd.Key}§r: {cmd.Value}");
                    }
                }
                else
                {
                    host?.TellRaw(player, "You have no custom commands");
                }
            }));


            // .set
            BuiltInCommands.Add("set", new MyCommand("set", ".set [var] [msg]  Set a custom message or command", (player, input) =>
            {
                string[] inputs = input.Split(' ');
                if (inputs.Length < 2)
                {
                    host?.TellRaw(player, new RawJson("[!] Please specify the command or message", "red"));
                    return;
                }

                string var = inputs[0];
                string msg = input.Substring(var.Length).Trim(new char[] { ' ', '\"' });

                if (BuiltInCommands.ContainsKey(var))
                {
                    host?.TellRaw(player, new RawJson("[!] Can NOT define built-in commands", "red"));
                    host?.TellRaw(player, new RawJson("[!] Built-in commands has higher priority level", "red"));
                    return;
                }

                if (!PrivateCommands.ContainsKey(player))
                {
                    PrivateCommands.Add(player, new Dictionary<string, string>());
                }

                if (!PrivateCommands[player].ContainsKey(var))
                    PrivateCommands[player].Add(var, msg);
                else
                    PrivateCommands[player][var] = msg;

                Logging.Logger.Log($"set: {player}, {var}, {PrivateCommands[player][var]}");

                host?.TellRaw(player, new RawJson($"[+] Command §a{var}§r has been set to §a\"{msg}\"", "yellow"));

                SaveCommands();
            }));

            // .exec
            BuiltInCommands.Add("exec", new MyCommand("exec", ".exec  [command] Execute command on server", (player, input) =>
            {
                if (input.Length == 0)
                {
                    host?.TellRaw(player, new RawJson("[!] Please specify the command", "red"));
                    return;
                }

                string cmd = input.Trim(' ').Trim('\"');

                host?.SendCommand(cmd);
            }));

            // .del
            BuiltInCommands.Add("del", new MyCommand("del", ".del [command] Delete a custom command", (player, input) =>
            {
                if (input.Length == 0)
                {
                    host?.TellRaw(player, new RawJson("[!] Please specify the command", "red"));
                    return;
                }

                string cmd = input.Trim(' ').Trim('\"');

                if (PrivateCommands.ContainsKey(player))
                {
                    if (PrivateCommands[player].ContainsKey(cmd))
                    {
                        PrivateCommands[player].Remove(cmd);
                        host?.TellRaw(player, new RawJson($"[-] Command §a{cmd}§r has been deleted", "yellow"));
                        return;
                    }
                }

                host?.TellRaw(player, new RawJson("[!] Command not found", "red"));

                SaveCommands();
            }));

            // .getbars
            BuiltInCommands.Add("getbars", new MyCommand("getbars", ".getbars  Get the list of bossbars", (player, _) =>
            {
                var bossbars = host?.bossbarManager.Bossbars;

                if (bossbars == null)
                {
                    host?.TellRaw(player, new RawJson("[!] Bossbars not found", "red"));
                    return;
                }

                host?.TellRaw(player, new RawJson("§a§lBossbars:", "yellow"));

                foreach (var bar in bossbars.Keys)
                {
                    host?.TellRaw(player, $"  §a{bar}§r: {bossbars[bar].Name}");
                }
            }));

            // .updatebars
            BuiltInCommands.Add("updatebars", new MyCommand("updatebars", ".updatebars  Update the list of bossbars", (player, _) =>
            {
                host?.bossbarManager.UpdateAll();
                host?.TellRaw(player, new RawJson("[+] Bossbars updated", "yellow"));
            }));

            foreach (var cmd in BuiltInCommands)
            {
                cmd.Value.Action += (player, input) =>
                {
                    Logger.Log($"Command \'{input}\' executed by {player}");
                };
            }

            // .showallbars
            BuiltInCommands.Add("showallbars", new MyCommand("showallbars", ".showallbars  Show all bossbars", (player, _) =>
            {
                var bossbarManager = host?.bossbarManager;

                if (bossbarManager == null)
                {
                    host?.TellRaw(player, new RawJson("[!] bossbarManager not found", "red"));
                    return;
                }

                bossbarManager.ShowAll();

                host?.TellRaw(player, new RawJson("[+] Bossbars shown", "yellow"));
            }));

            // .showbar
            // show a specific bossbar
            BuiltInCommands.Add("showbar", new MyCommand("showbar", ".showbar [bar]  Show a specific bossbar", (player, input) =>
            {
                var bossbarManager = host?.bossbarManager;

                if (bossbarManager == null)
                {
                    host?.TellRaw(player, new RawJson("[!] bossbarManager not found", "red"));
                    return;
                }

                if (input.Length == 0)
                {
                    host?.TellRaw(player, new RawJson("[!] Please specify the bossbar", "red"));
                    return;
                }

                string bar = input.Trim(' ').Trim('\"');

                if (!bossbarManager.Bossbars.ContainsKey(new Guid(bar)))
                {
                    host?.TellRaw(player, new RawJson("[!] Bossbar not found", "red"));
                    return;
                }

                bossbarManager.Show(bar);

                host?.TellRaw(player, new RawJson($"[+] Bossbar §a{bar}§r shown", "yellow"));
            }));

            // .hidebar
            // hide a specific bossbar
            BuiltInCommands.Add("hidebar", new MyCommand("hidebar", ".hidebar [bar]  Hide a specific bossbar", (player, input) =>
            {
                var bossbarManager = host?.bossbarManager;

                if (bossbarManager == null)
                {
                    host?.TellRaw(player, new RawJson("[!] bossbarManager not found", "red"));
                    return;
                }

                if (input.Length == 0)
                {
                    host?.TellRaw(player, new RawJson("[!] Please specify the bossbar", "red"));
                    return;
                }

                string bar = input.Trim(' ').Trim('\"');

                if (!bossbarManager.Bossbars.ContainsKey(new Guid(bar)))
                {
                    host?.TellRaw(player, new RawJson("[!] Bossbar not found", "red"));
                    return;
                }

                bossbarManager.Hide(bar);

                host?.TellRaw(player, new RawJson($"[+] Bossbar §a{bar}§r hidden", "yellow"));
            }));
        }
    }

    public void SaveCommands()
    {
        Logging.Logger.Log("Saving custom commands");
        try
        {
            using (FileStream fs = File.Create("CustomCommands.json"))
            {
                JsonSerializer.Serialize(fs, PrivateCommands);
            }
        }
        catch (Exception)
        {
            Logger.Log("Error occurred when trying to save custom commands. File may not exist or is empty");
        }
    }

    public void LoadCommands()
    {
        try
        {
            // serialize JSON directly to a file
           using (FileStream fs = File.OpenRead("CustomCommands.json"))
           {
                PrivateCommands = JsonSerializer.DeserializeAsync<Dictionary<string, Dictionary<string, string>>>(fs).Result ?? new();
           }
        }
        catch (Exception)
        {
            Logger.Log("Error occurred when trying to load custom commands.");
        }
    }
}

public class MyCommand
{
    public string Command { get; set; }

    public string? SubCommond { get; set; }

    public string Description { get; set; }

    // Action<player, command>
    public Action<string, string> Action { get; set; }

    public MyCommand(string command, string description, Action<string, string> action)
    {
        string[] commands = command.Split(' ');
        Command = commands[0];

        if (commands.Length > 1)
        {
            SubCommond = commands[1];
        }
        else
        {
            SubCommond = null;
        }

        Description = description;
        Action = action;
    }
}
