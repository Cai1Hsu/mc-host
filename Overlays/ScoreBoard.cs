using mchost.Server;

namespace mchost.Overlays
{
    public class OnlineBoardManager
    {
        private const string SCOREBOARD_TITLE = "Online Stats";

        private ServerHost? host;

        public ScoreBoard OnlineBoard { get; set; }

        public void Show()
        {
            host?.SendCommand($"/scoreboard objectives setdisplay sidebar {OnlineBoard.title}");

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
                host?.SendCommand($"/scoreboard players set {score.Key} {OnlineBoard.title} {score.Value}");
            }
        }

        public void Hide()
        {
            host?.SendCommand($"/scoreboard objectives setdisplay sidebar");
        }

        public OnlineBoardManager()
        {
            this.host = ServerHost.GetServerHost();
            OnlineBoard = new(SCOREBOARD_TITLE);
        }

        public void LoadScores() => Update();
    }

    public class ScoreBoard
    {
        public string title;

        public Dictionary<string, int> scores;

        public ScoreBoard(string title)
        {
            scores = new();
            this.title = title;
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