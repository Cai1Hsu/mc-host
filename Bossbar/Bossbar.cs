using System.Text.Json;
using mchost.Server;

namespace mchost.Bossbar
{
    public class BossbarManager
    {
        private ServerHost? host;

        public Dictionary<Guid, Bossbar> Bossbars = new();

        public void UpdateAll()
        {
            if (!LoadBossbars())
            {
                Logging.Logger.Log("Failed to load bossbars.json");
                host?.TellRaw("@a", "[Server] Failed to load bossbars.json");
            }
            
            foreach (Bossbar bossbar in Bossbars.Values)
            {
                host?.SendCommand($"/bossbar set {bossbar.guid} name \"{bossbar.Name}\"");
                host?.SendCommand($"/bossbar set {bossbar.guid} color {bossbar.Color.ToString().ToLower()}");
                host?.SendCommand($"/bossbar set {bossbar.guid} max {bossbar.Max}");
                host?.SendCommand($"/bossbar set {bossbar.guid} value {bossbar.Value}");
                host?.SendCommand($"/bossbar set {bossbar.guid} style {bossbar.Style.ToString().ToLower()}");
                host?.SendCommand($"/bossbar set {bossbar.guid} visible {bossbar.Visible.ToString().ToLower()}");
            }
        }

        public void Update(Guid guid, BossbarProperty propertyName, string value)
        {
            Bossbar bossbar = Bossbars[guid];

            host?.SendCommand($"/bossbar set {bossbar.guid} {propertyName.ToString().ToLower()} \"{value}\"");
        }

        public Guid AddBossbar(string name)
        {
            Guid guid = Guid.NewGuid();
            Bossbars.Add(guid, new Bossbar(guid, name));

            host?.SendCommand($"/bossbar add {guid} {name}");

            return guid;
        }

        public void RemoveBossbar(string id)
        {
            Bossbars.Remove(new Guid(id));

            host?.SendCommand($"/bossbar remove {id}");
        }

        public void RemoveBossbar(Guid guid)
        {
            Bossbars.Remove(guid);

            RemoveBossbar(guid.ToString());
        }

        public Bossbar GetBossbar(Guid guid) => Bossbars[guid];

        public void SaveBossbarsAsync()
        {
            using (FileStream fs = File.Create("bossbars.json"))
            {
                JsonSerializer.SerializeAsync(fs, Bossbars);
            }
        }

        public bool LoadBossbars()
        {
            if (!File.Exists("bossbars.json"))
            {
                File.Create("bossbars.json");
                return false;
            }
            
            try
            {
                using (FileStream fs = File.OpenRead("bossbars.json"))
                {
                    Bossbars = JsonSerializer.DeserializeAsync<Dictionary<Guid, Bossbar>>(fs).Result ?? new();
                }
            }
            catch (Exception)
            {
                Logging.Logger.Log($"Failed to load bossbars. File may not exist or is empty");
                return false;
            }

            return true;
        }

        public BossbarManager()
        {
            this.host = ServerHost.GetServerHost();
        }
    }

    public class Bossbar
    {
        public Guid guid { get; set; }

        public string Name
        {
            get { return Name; }
            set
            {
                Name = value;
                bossbarManager?.Update(guid, BossbarProperty.Name, Name);
            }
        }

        public BossbarColor Color
        {
            get { return Color; }
            set
            {
                Color = value;
                bossbarManager?.Update(guid, BossbarProperty.Color, Color.ToString().ToLower());
            }
        }

        public int Max
        {
            get { return Max; }
            set
            {
                Max = value;
                bossbarManager?.Update(guid, BossbarProperty.Max, Max.ToString());
            }
        }

        public int Value
        {
            get { return Value; }
            set
            {
                Value = value;
                bossbarManager?.Update(guid, BossbarProperty.Value, Value.ToString());
            }
        }

        public BossbarStyle Style
        {
            get { return Style; }
            set
            {
                Style = value;
                bossbarManager?.Update(guid, BossbarProperty.Style, Style.ToString().ToLower());
            }
        }

        public bool Visible
        {
            get { return Visible; }
            set
            {
                Visible = value;
                bossbarManager?.Update(guid, BossbarProperty.Visible, Visible.ToString().ToLower());
            }
        }

        private BossbarManager? bossbarManager { get; set; }

        public Bossbar(Guid guid, string name)
        {
            this.guid = guid;
            this.Name = name;

            ServerHost.GetServerHost()?.bossbarManager?.Bossbars.Add(guid, this);
        }
    }

    public enum BossbarColor
    {
        Blue,
        Green,
        Pink,
        Purple,
        Red,
        White,
        Yellow
    }

    public enum BossbarStyle
    {
        Progress,
        Notched_6,
        Notched_10,
        Notched_12,
        Notched_20
    }

    public enum BossbarProperty
    {
        Color,
        Max,
        Value,
        Style,
        Visible,
        Name
    }
}