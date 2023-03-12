using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

class CustomCommandManager
{
    private MinecraftHandler MCSV;

    // Dictionary<command, description>;
    public Dictionary<string, string> BuiltInCommands = new Dictionary<string, string>()
    {
        { "help",       ".help           Show help message" },
        { "set",        ".set  [command] Set a custom message or command" },
        { "del",        ".del  [command] Delete a custom message or command" },
        { "printstat",  ".printstat      Print Players online statistics"},
        { "getmsg",     ".getmsg [line]  Get message list"},
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
            ExecuteCommand(command[2..], sender, false);
        }
        else
        {
            if (command.Length == 2) return;
            
            ExecuteCommand(command[1..], sender, true);
        }
    }

    public void ExecuteCommand(string command, string sender, bool isPublicCommand)
    {
        string flag = command[0..command.IndexOf(' ')].ToLower();
        
        string option = command[command.IndexOf(' ')..];

        if (flag == "set")
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
                JsonSerializer.Serialize(writer, PrivateCommands);

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
            using (FileStream fs = new FileStream("CustomCommands.json", FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(fs))
            {
                string json = reader.ReadToEnd();
                PrivateCommands = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json) ?? new Dictionary<string, Dictionary<string, string>>();
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Error occurred when trying to load time statistics.");
        }
    }
}

