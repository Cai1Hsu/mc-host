using System.Collections.Generic;
using System;
using mchost.Server;

namespace mchost.Bossbar
{
    public class BossbarManager
    {
        private ServerHost? host;

        public Dictionary<Guid, Bossbar> Bossbars = new();

        public void UpdateAll()
        {
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

        public void RemoveBossbar(Guid guid)
        {
            Bossbars.Remove(guid);

            host?.SendCommand($"/bossbar remove {guid}");
        }

        public Bossbar GetBossbar(Guid guid) => Bossbars[guid];
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