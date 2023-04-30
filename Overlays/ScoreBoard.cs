using mchost.Server;
using mchost.Utils;

namespace mchost.Overlays
{
    public class OnlineBoardManager
    {
        private const string SCOREBOARD_TITLE = "Online Stats";

        private const string SCOREBOARD_NAME = "OnlineStats";

        private ServerHost? host;

        public ScoreBoard OnlineBoard { get; set; }

        public void Show()
        {
            host?.SendCommand($"/scoreboard objectives add {OnlineBoard.name} dummy \"{OnlineBoard.title}\"");

            host?.SendCommand($"/scoreboard objectives setdisplay sidebar {OnlineBoard.name}");

            this.Update();
        }

        public void Update()
        {
            var PlayersPlayTime = host?.PlayersPlayTime;

            if (PlayersPlayTime == null) return;

            foreach (var player in PlayersPlayTime)
            {
                OnlineBoard.SetScore(player.Key, (int)player.Value.TotalMinutes);
            }

            foreach (var score in OnlineBoard.scores)
            {
                host?.SendCommand($"/scoreboard players set {score.Key} {OnlineBoard.name} {score.Value}");
            }
        }

        public void Hide()
        {
            // Hide the scoreboard
            host?.SendCommand($"/scoreboard objectives remove {OnlineBoard.name}");
        }

        public OnlineBoardManager()
        {
            this.host = ServerHost.GetServerHost();
            OnlineBoard = new(SCOREBOARD_TITLE, SCOREBOARD_NAME);
        }

        public void LoadScores() => Update();
    }

    public class ScoreBoard
    {
        public string title;

        public string name;

        public Dictionary<string, int> scores;

        public ScoreBoard(string title, string name = "")
        {
            scores = new();
            this.title = title;
            if (name.Contains(' ')) throw new InvalidOperationException("The name of a scoreboard CANNOT contain spaces");
            this.name = (name == "" || name == null) ? "board" : name;
        }

        public void SetScore(string name, int score)
        {
            scores[name] = score;
        }

        public void RemovePlayer(string name)
        {
            scores.Remove(name);
        }
    }
}