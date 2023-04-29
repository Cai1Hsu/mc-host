using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using mchost.Server;
using mchost.Utils;

namespace mchost.CustomCommand
{
    class CustomCommandManager
    {
        private ServerHost? host;

        private ServerProcess MinecraftServer;

        // Dictionary<command, description>;
        // public Dictionary<string, MyCommand> BuiltInCommands = new Dictionary<string, string>()
        // {
        //     { "help",       ".help            Show help message" },
        //     { "set",        ".set   [command] Set a custom message or command" },
        //     { "alias",      ".alias [command] Set a custom message or command" },
        //     { "del",        ".del   [command] Delete a custom message or command" },
        //     { "printstat",  ".printstat       Print Players online statistics"},
        //     { "getmsg",     ".getmsg [line]   Get message list"},
        //     { "exec",       ".exec  [command] Execute command on server"}
        // };

        public Dictionary<string, MyCommand> BuiltInCommands = new();

        // Dictionary<player, Dictionary<command, option>>
        public Dictionary<string, Dictionary<string, string>> PrivateCommands;

        public CustomCommandManager(ServerProcess minecraftHandler)
        {
            MinecraftServer = minecraftHandler;

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


                // TODO
                // .set
                BuiltInCommands.Add("set", new MyCommand("set", ".set [command]   Set a custom message or command", (player, input) =>
                {
                    string[] options = input.Split(' ');
                    if (options.Length < 2)
                    {
                        host?.TellRaw(player, new RawJson("[!] Please specify the comman or message", "red"));
                        return;
                    }

                    string key = options[0];
                    string val = input[(input.IndexOf(' ') + 1)..].Trim('"');

                    if (BuiltInCommands.ContainsKey(key))
                    {
                        host?.TellRaw(player, new RawJson("[!] Can NOT define built-in commands", "red"));
                        host?.TellRaw(player, new RawJson("[!] Use \".set\" to set your private commands", "red"));
                    }
                    else
                    {
                        if (PrivateCommands.ContainsKey(player))
                        {
                            if (PrivateCommands[player].ContainsKey(key))
                            {
                                PrivateCommands[player][key] = val;
                            }
                            else
                            {
                                PrivateCommands[player].Add(key, val);
                            }
                        }
                        else
                        {
                            PrivateCommands.Add(player, new Dictionary<string, string>());
                            PrivateCommands[player].Add(key, val);
                        }

                        host?.TellRaw(player, new RawJson($"[+] Command §a{key}§r has been set to §a{val}", "yellow"));
                    }
                }));


                // TODO
                // .exec
                BuiltInCommands.Add("exec", new MyCommand("exec", ".exec  [command] Execute command on server", (player, input) =>
                {
                    string[] options = input.Split(' ');
                    if (options.Length == 0)
                    {
                        host?.TellRaw(player, new RawJson("[!] Please specify the command", "red"));
                        return;
                    }

                    string cmd = input.Trim('"');

                    MinecraftServer.ServerExecuteCommand(cmd);
                }));
            }
        }

        public void ExecuteCustomCommand(string command, string sender)
        {
            if (command.Length == 1) return;

            if (command[1] != '.')
            {
                ExecuteCommand(command[1..], sender, false);
            }
            else
            {
                if (command.Length == 2) return;

                ExecuteCommand(command[2..], sender, true);
            }
        }

        public void ExecuteCommand(string command, string sender, bool isPublicCommand)
        {
            string globalCommond = command[0..command.IndexOf(' ')].ToLower();

            string subCommond = command[command.IndexOf(' ')..];

            if (globalCommond == "set" || globalCommond == "alias")
            {
                string[] options = subCommond.Split(' ');
                if (options.Length < 2)
                {
                    MinecraftServer.ServerTargetRawMessage("[!] Please specify the command", sender);
                    return;
                }

                string key = options[0];
                string val = options[1].Trim('"');

                if (BuiltInCommands.ContainsKey(key))
                {
                    MinecraftServer.ServerTargetRawMessage("[!] Can NOT define built-in commands", sender);
                    MinecraftServer.ServerTargetRawMessage("[!] Use \".set\" to set your private commands", sender);
                }
                else
                {
                    if (PrivateCommands.ContainsKey(sender))
                    {
                        if (PrivateCommands[sender].ContainsKey(key))
                        {
                            PrivateCommands[sender][key] = val;
                        }
                        else
                        {
                            PrivateCommands[sender].Add(key, val);
                        }
                    }
                    else
                    {
                        PrivateCommands.Add(sender, new Dictionary<string, string>());
                        PrivateCommands[sender].Add(key, val);
                    }
                }

                SaveCommands();
            }
            else if (globalCommond == "del")
            {
                string[] options = subCommond.Split(' ');
                if (options.Length == 0)
                {
                    MinecraftServer.ServerTargetRawMessage("[!] Please specify the command", sender);
                    return;
                }

                string key = options[0];

                if (PrivateCommands.ContainsKey(sender) && PrivateCommands[sender].ContainsKey(key))
                {
                    PrivateCommands[sender].Remove(key);
                }
                else
                {
                    MinecraftServer.ServerTargetRawMessage("[!] Can NOT find the command", sender);
                }
            }
            else if (globalCommond == "help")
            {
                MinecraftServer.ServerTargetRawMessage("Built-in commands:", sender);

                foreach (var public_command in BuiltInCommands.Keys)
                {
                    string description = BuiltInCommands[public_command];
                    MinecraftServer.ServerTargetRawMessage($"  {description}", sender);
                }

                if (PrivateCommands.ContainsKey(sender))
                {
                    MinecraftServer.ServerTargetRawMessage("Your private commands:", sender);

                    foreach (var private_command in PrivateCommands[sender].Keys)
                    {
                        MinecraftServer.ServerTargetRawMessage($"  .{private_command}", sender);
                    }
                }
                else
                {
                    MinecraftServer.ServerTargetRawMessage("[!] You have no private commands", sender);
                }
            }
            else if (globalCommond == "printstat")
            {
                MinecraftServer.PrivatePrintOnlineStatistics(sender);
            }
            else if (globalCommond == "getmsg")
            {
                string[] options = subCommond.Split(' ');
                if (options.Length == 0)
                {
                    PlayerMessage msg = MinecraftServer.MessageList[MinecraftServer.MessageList.Count - 1];
                    MinecraftServer.ServerTargetRawMessage($"[!] [{msg.Time.ToShortTimeString()}] <{msg.Sender}>: {msg.Content}", sender);
                    return;
                }

                string line = options[0];

                if (!int.TryParse(line, out int l))
                {
                    MinecraftServer.ServerTargetRawMessage($"[!] \"{line}\" is a invalid line number", sender);
                    return;
                }
                else
                {
                    if (l > MinecraftServer.MessageList.Count || l < 1)
                    {
                        MinecraftServer.ServerTargetRawMessage($"[!] \"{line}\" is a invalid line number", sender);
                        return;
                    }

                    PlayerMessage msg = MinecraftServer.MessageList[MinecraftServer.MessageList.Count - l - 1];
                    MinecraftServer.ServerTargetRawMessage($"[!] [{msg.Time.ToShortTimeString()}] <{msg.Sender}>: {msg.Content}", sender);
                }
            }
            else if (globalCommond == "exec")
            {
                MinecraftServer.SendCommand(subCommond);
            }
            // TODO
            // parse command
            else
            {
                if (BuiltInCommands.ContainsKey(globalCommond))
                {
                    MinecraftServer.ServerTargetRawMessage($"[!] {BuiltInCommands[globalCommond]}", sender);
                }
                else
                {
                    if (PrivateCommands.ContainsKey(sender))
                    {
                        if (PrivateCommands[sender].ContainsKey(globalCommond))
                        {
                            if (PrivateCommands[sender][globalCommond].StartsWith("/"))
                            {
                                MinecraftServer.SendCommand(PrivateCommands[sender][globalCommond]);
                            }
                            else
                            {
                                if (isPublicCommand)
                                    MinecraftServer.ServerPublicRawMessage($"{globalCommond}: {PrivateCommands[sender][globalCommond]}");
                                else
                                    MinecraftServer.ServerTargetRawMessage($"{globalCommond}: {PrivateCommands[sender][globalCommond]}", sender);
                            }
                        }
                        else
                        {
                            MinecraftServer.ServerTargetRawMessage($"[!] Unknown command: {globalCommond}", sender);
                        }
                    }
                    else
                    {
                        MinecraftServer.ServerTargetRawMessage($"[!] Unknown command: {globalCommond}", sender);
                    }
                }
            }
        }

        public void SaveCommands()
        {
            try
            {
                using (FileStream fs = new FileStream("CustomCommands.json", FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                using (Utf8JsonWriter writer = new Utf8JsonWriter(fs))
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("CustomCommands");
                    writer.WriteStartObject();
                    foreach (string player in PrivateCommands.Keys)
                    {
                        writer.WritePropertyName(player);
                        writer.WriteStartObject();
                        foreach (string command in PrivateCommands[player].Keys)
                        {
                            writer.WriteString(command, PrivateCommands[player][command]);
                        }
                        writer.WriteEndObject();
                    }
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error occurred when trying to save time statistics.");
            }
        }

        public void LoadCommands()
        {
            try
            {
                using (FileStream fs = new FileStream("TimeStatistics.json", FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                using (JsonDocument document = JsonDocument.Parse(fs))
                {
                    JsonElement root = document.RootElement;
                    JsonElement playerPlayTime = root.GetProperty("CustomCommands");
                    foreach (JsonProperty player in playerPlayTime.EnumerateObject())
                    {
                        string playerName = player.Name;
                        PrivateCommands.Add(playerName, new Dictionary<string, string>());
                        foreach (JsonProperty command in player.Value.EnumerateObject())
                        {
                            string commandName = command.Name;
                            string commandValue = command.Value.GetString() ?? "";
                            PrivateCommands[playerName].Add(commandName, commandValue);
                        }
                    }

                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error occurred when trying to load time statistics.");
            }
        }
    }

    public class MyCommand
    {
        public string Command { get; set; }

        public string? SubCommond { get; set; }
        
        public string Description { get; set; }

        // Action<player, command>
        public Action<string, string>? Action { get; set; }

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
}