using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

class CustomCommandManager
{
    private MinecraftHandler MCSV;

    // Dictionary<command, description>;
    public Dictionary<string, string> BuiltInCommands = new Dictionary<string, string>()
    {
        { "help",       ".help            Show help message" },
        { "set",        ".set   [command] Set a custom message or command" },
        { "alias",      ".alias [command] Set a custom message or command" },
        { "del",        ".del   [command] Delete a custom message or command" },
        { "printstat",  ".printstat       Print Players online statistics"},
        { "getmsg",     ".getmsg [line]   Get message list"},
        { "exec",       ".exec  [command] Execute command on server"}
    };

    // Dictionary<player, Dictionary<command, option>>
    public Dictionary<string, Dictionary<string, string>> PrivateCommands;

    public CustomCommandManager(MinecraftHandler minecraftHandler)
    {
        MCSV = minecraftHandler;

        LoadCommands();

        if (PrivateCommands == null)
        {
            PrivateCommands = new Dictionary<string, Dictionary<string, string>>();
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
        string flag = command[0..command.IndexOf(' ')].ToLower();
        
        string option = command[command.IndexOf(' ')..];

        if (flag == "set" || flag == "alias")
        {
            string[] options = option.Split(' ');
            if (options.Length < 2)
            {
                MCSV.ServerPrivateRawMessage("[!] Please specify the command", sender);
                return;
            }

            string key = options[0];
            string val = options[1].Trim('"');
            
            if (BuiltInCommands.ContainsKey(key))
            {
                MCSV.ServerPrivateRawMessage("[!] Can NOT redefine built-in commands", sender);
                MCSV.ServerPrivateRawMessage("[!] Use \".set\" to set your private commands", sender);
            }
            else
            {
                if (PrivateCommands.ContainsKey(sender))
                {
                    PrivateCommands[sender].Add(key, val);
                }
                else
                {
                    PrivateCommands.Add(sender, new Dictionary<string, string>());
                    PrivateCommands[sender].Add(key, val);
                }
            }

            SaveCommands();
        }
        else if (flag == "del")
        {
            string[] options = option.Split(' ');
            if (options.Length == 0)
            {
                MCSV.ServerPrivateRawMessage("[!] Please specify the command", sender);
                return;
            }

            string key = options[0];

            if (PrivateCommands.ContainsKey(sender))
            {
                if (PrivateCommands[sender].ContainsKey(key))
                {
                    PrivateCommands[sender].Remove(key);
                }
                else
                {
                    MCSV.ServerPrivateRawMessage("[!] Can NOT find the command", sender);
                }
            }
            else
            {
                MCSV.ServerPrivateRawMessage("[!] Can NOT find the command", sender);
            }
        }
        else if (flag == "help")
        {
            MCSV.ServerPrivateRawMessage("Built-in commands:", sender);
            
            foreach (var public_command in BuiltInCommands.Keys)
            {
                string description = BuiltInCommands[public_command];
                MCSV.ServerPrivateRawMessage($"  {description}", sender);
            }
            
            if (PrivateCommands.ContainsKey(sender))
            {
                MCSV.ServerPrivateRawMessage("Your private commands:", sender);

                foreach (var private_command in PrivateCommands[sender].Keys)
                {
                    MCSV.ServerPrivateRawMessage($"  .{private_command}", sender);
                }
            }
            else
            {
                MCSV.ServerPrivateRawMessage("You have no private commands", sender);
            }
        }
        else if (flag == "printstat")
        {
            MCSV.PrivatePrintOnlineStatistics(sender);
        }
        else if (flag == "getmsg")
        {
            string[] options = option.Split(' ');
            if (options.Length == 0)
            {
                PlayerMessage msg = MCSV.MessageList[MCSV.MessageList.Count - 1];
                MCSV.ServerPrivateRawMessage($"[!] [{msg.Time.ToShortTimeString()}] <{msg.Sender}>: {msg.Content}", sender);
                return;
            }

            string line = options[0];

            if (!int.TryParse(line, out int l))
            {
                MCSV.ServerPrivateRawMessage($"[!] \"{line}\" is a invalid line number", sender);
                return;
            }
            else
            {
                if (l > MCSV.MessageList.Count || l < 1)
                {
                    MCSV.ServerPrivateRawMessage($"[!] \"{line}\" is a invalid line number", sender);
                    return;
                }

                PlayerMessage msg = MCSV.MessageList[MCSV.MessageList.Count - l - 1];
                MCSV.ServerPrivateRawMessage($"[!] [{msg.Time.ToShortTimeString()}] <{msg.Sender}>: {msg.Content}", sender);
            }
        }
        else if (flag == "exec")
        {
            MCSV.SendCommand(option);
        }
        // parse command
        else
        {
            if (BuiltInCommands.ContainsKey(flag))
            {
                MCSV.ServerPrivateRawMessage($"[!] {BuiltInCommands[flag]}", sender);
            }
            else
            {
                if (PrivateCommands.ContainsKey(sender))
                {
                    if (PrivateCommands[sender].ContainsKey(flag))
                    {
                        if (PrivateCommands[sender][flag].StartsWith("/"))
                        {
                            MCSV.SendCommand(PrivateCommands[sender][flag]);
                        }
                        else
                        {
                            if (isPublicCommand)
                                MCSV.ServerPublicRawMessage($"{flag}: {PrivateCommands[sender][flag]}");
                            else
                                MCSV.ServerPrivateRawMessage($"{flag}: {PrivateCommands[sender][flag]}", sender);
                        }
                    }
                    else
                    {
                        MCSV.ServerPrivateRawMessage($"[!] Unknown command: {flag}", sender);
                    }
                }
                else
                {
                    MCSV.ServerPrivateRawMessage($"[!] Unknown command: {flag}", sender);
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

