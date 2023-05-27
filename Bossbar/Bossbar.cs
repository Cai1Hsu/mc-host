using System.Text.Json;
using mchost.Server;
using mchost.Utils;

namespace mchost.Bossbar
{
    public class BossbarManager
    {
        private ServerHost? host;

        public Dictionary<Guid, Bossbar> Bossbars = new();

        public void ShowAll()
        {
            foreach (Bossbar bossbar in Bossbars.Values)
            {
                host?.SendCommand($"/bossbar set minecraft:{bossbar.guid} visible true");
                host?.SendCommand($"/bossbar set minecraft:{bossbar.guid} players @a");
            }
        }

        public void Show(string id)
        {
            host?.SendCommand($"/bossbar set minecraft:{id} visible true");
            host?.SendCommand($"/bossbar set minecraft:{id} players @a");
        }

        public void Show(Guid guid)
        {
            host?.SendCommand($"/bossbar set minecraft:{guid} visible true");
            host?.SendCommand($"/bossbar set minecraft:{guid} players @a");
        }

        public void HideAll()
        {
            foreach (Bossbar bossbar in Bossbars.Values)
            {
                host?.SendCommand($"/bossbar set minecraft:{bossbar.guid} visible false");
            }
        }

        public void Hide(string id)
        {
            host?.SendCommand($"/bossbar set minecraft:{id} visible false");
        }

        public void Hide(Guid guid)
        {
            host?.SendCommand($"/bossbar set minecraft:{guid} visible false");
        }

        public void UpdateAll()
        {
            if (!LoadBossbars())
            {
                Logging.Logger.Log("Failed to load Bossbars.json");
                host?.TellRaw("@a", "[Server] Failed to load Bossbars.json");
            }

            foreach (Bossbar bossbar in Bossbars.Values)
            {
                RawJson jsonName = new RawJson($"\"{bossbar.Name}\"");
                host?.SendCommand($"/bossbar set minecraft:{bossbar.guid} name \"{jsonName}\"");
                host?.SendCommand($"/bossbar set minecraft:{bossbar.guid} color {bossbar.Color.ToString().ToLower()}");
                host?.SendCommand($"/bossbar set minecraft:{bossbar.guid} max {bossbar.Max}");
                host?.SendCommand($"/bossbar set minecraft:{bossbar.guid} value {bossbar.Value}");
                host?.SendCommand($"/bossbar set minecraft:{bossbar.guid} style {bossbar.Style.ToString().ToLower()}");
                host?.SendCommand($"/bossbar set minecraft:{bossbar.guid} visible {bossbar.Visible.ToString().ToLower()}");
            }
        }

        public void Update(Guid guid, BossbarProperty propertyName, string value)
        {
            Bossbar bossbar = Bossbars[guid];

            host?.SendCommand($"/bossbar set minecraft:{bossbar.guid} {propertyName.ToString().ToLower()} \"{value}\"");
        }

        public Guid AddBossbar(string name)
        {
            Guid guid = Guid.NewGuid();
            Bossbars.Add(guid, new Bossbar(guid, name));

            host?.SendCommand($"/bossbar add {guid} {name}");

            Show(guid);

            SaveBossbars();

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

        public void SaveBossbars()
        {
            try
            {
                using (FileStream fs = File.Create("Bossbars.json"))
                {
                    JsonSerializer.Serialize(fs, Bossbars);
                }
            }
            catch (Exception e)
            {
                Logging.Logger.Log($"Failed to Save Bossbars. The message : " + e.Message);
            }
        }

        public bool LoadBossbars()
        {
            if (!File.Exists("Bossbars.json"))
            {
                File.Create("Bossbars.json");
                return false;
            }

            try
            {
                using (FileStream fs = File.OpenRead("Bossbars.json"))
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

        public string Name { get; set; }

        public BossbarColor Color { get; set; }

        public int Max { get; set; }

        public int Value { get; set; }

        public BossbarStyle Style { get; set; }

        public bool Visible { get; set; }

        private BossbarManager? bossbarManager { get; set; }

        public Bossbar(Guid guid, string name)
        {
            this.guid = guid;
            this.Name = name;
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