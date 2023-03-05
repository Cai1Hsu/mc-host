static class CustomCommandManager
{
    // Dictionary<command, option>;
    public Dictionary<string, string> PublicCommands = new Dictionary<string, string>();

    // Dictionary<player, Dictionary<command, option>>
    public Dictionary<string, Dictionary<string, string>> PrivateCommands = new Dictionary<string, Dictionary<string, string>>();

    public void ExecuteCustomCommand(string command, string sender)
    {
        if (command.Length == 1) return;
        
        if (command[1] != '.')
        {
            ExecutePublicCommand(command[1..]);
        }
        else
        {
            if (command.Length == 2) return;
            
            ExecutePrivateCommand(command[2..], sender);
        }
    }

    public void ExecutePublicCommand(string command)
    {
        string flag = command[0..command.IndexOf(' ')].ToLower();
    }

    public void ExecutePrivateCommand(string command, bool isPublicCommand, string sender)
    {
        string flag = command[0..command.IndexOf(' ')].ToLower();
        
        string option = command[command.IndexOf(' ')..];

        if (flag == "set")
        {
            string[] options = option.Split(' ');
            string key = options[0];
            string val = option[1].Trim('"');
            
            if (isPublicCommand)
            {
                PublicCommands[key] = val;
                MinecraftHander.SendMessage("");
            }
            else
            {
                PrivateCommands[sender][key] = val;
                MinecraftHander.SendPrivateMessage("");
            }

            SaveCommands();
        }
        else if (flag == "help")
        {
            MinecraftHander.SendPrivateMessage("");
            
            foreach (var public_command in PublicCommands.Keys)
            {

            }
            
            MinecraftHander.SendPrivateMessage("Your private commands:");

            foreach (var private_command in PrivateCommands.keys)
            {

            }
        }
        // parse command
        else
        {
            if (isPublicCommand)
            {

            }
            else
            {
                
            }
        }
    }

    public void SaveCommands()
    {

    }
}

