using System.Text.Json;
using mchost.Server;
using mchost.Utils;
using mchost.Logging;
using System.ComponentModel;

namespace mchost.Tictactoe;

public class TictactoeManager
{
    private TimeSpan ROUNDTIMEOUT = TimeSpan.FromSeconds(60);
    private Task? roundTimeoutTask;

    private ServerHost? host;

    public void SetHost(ServerHost host) => this.host = host;

    public TictactoeRound? CurrentRound;

    public bool IsPlaying 
    {
        get
        {
            return CurrentRound != null;
        }
    }

    public bool IsFull
    {
        get
        {
            return IsPlaying && CurrentRound?.Owner != null && CurrentRound?.Participate != null;
        }
    }

    public TictactoeManager()
    {
        host = ServerHost.MainHost;
    }

    private void ResetTimeOut()
    {
        if (roundTimeoutTask == null) return;

        roundTimeoutTask.Dispose();

        roundTimeoutTask = Task.Delay(ROUNDTIMEOUT).ContinueWith((task) => {
           if (CurrentRound == null) return;
           if (CurrentRound.Owner == null) return;
           if (CurrentRound.Participate == null) return;

            var current_player = CurrentRound.Turn == TictactoeTurn.Owner ? CurrentRound.Owner : CurrentRound.Participate;

            host?.TellRaw("@a", $"[Tictactoe] The game has timed out! So the winner is {current_player}");
            CurrentRound = null; // End game
        });

    }

    private void EndTimer()
    {
        if (roundTimeoutTask == null) return;

        roundTimeoutTask.Dispose();
    }

    public void StartGame(string player)
    {
        if (IsPlaying)
        {
            host?.TellRaw(player, "[Tictactoe] There is already a game in progress!");
            return;
        }

        CurrentRound = new TictactoeRound();
        CurrentRound.Owner = player;

        host?.TellRaw("@a", $"[Tictactoe] {player} have started a game of Tictactoe!");
        host?.TellRaw("@a", "[Tictactoe] Waiting for players to join...");

        this.PrintBoard();
        
        host?.TellRaw(player, "[Tictactoe] Now it's your turn! Click or say [a,b,c][1,2,3] to place your mark!");
        
        // Set timeout
        ResetTimeOut();
    }

    public JoinResult isJoining(string player, string msg)
    {
        if (msg.Length != 2) return JoinResult.NotJoining;

        if (!this.IsPlaying) return JoinResult.NotPlaying;

        if (this.IsFull && !IsPlayerPlaying(player)) return JoinResult.Full;

        msg = msg.ToLower();

        var check_result = CheckType(msg[0]) * CheckType(msg[1]);

        if (check_result != -1) return JoinResult.InCorrectFormat;

        return JoinResult.Join;
    }

    public void Join(string player, string msg)
    {
        if (CurrentRound == null) return;

        var check_result = isJoining(player, msg);
        if (check_result != JoinResult.Join) return;

        if (!CurrentRound.IsPlayerTurn(player))
        {
            host?.TellRaw(player, "[Tictactoe] It's not your turn!");
            return;
        }

        var type_check = CheckType(msg[0]) * CheckType(msg[1]);

        if (type_check != -1)
        {
            host?.TellRaw(player, "[Tictactoe] Invalid format! Please click the mark or say [a,b,c][1,2,3]!");
            return;
        }

        int col = CheckType(msg[0]) == 1 ? msg[0] - 'a' : msg[1] - 'a';
        int row = CheckType(msg[0]) == -1 ? msg[0] - '1' : msg[1] - '1';

        if (CurrentRound.Board[row, col] != 0)
        {
            host?.TellRaw(player, "[Tictactoe] This position is already taken!");
            return;
        }

        // Reset timeout timer


        CurrentRound.Board[row, col] = CurrentRound.Owner == player ? TictactoeMark.X : TictactoeMark.O;

        this.PrintBoard();

        var turn = CurrentRound.Owner == player ? TictactoeTurn.Owner : TictactoeTurn.Participate;

        var turn_res = CheckWin(turn);

        if (!turn_res.IsEnded)
        {
            var next_turn = turn == TictactoeTurn.Owner ? TictactoeTurn.Participate : TictactoeTurn.Owner;
            CurrentRound.Turn = next_turn;
            var next_player = next_turn == TictactoeTurn.Owner ? CurrentRound.Owner : CurrentRound.Participate;
            
            host?.TellRaw(next_player, "[Tictactoe] Now it's your turn! Click or say [a,b,c][1,2,3] to place your mark!");
            return;
        }

        this.EndTimer();
        CurrentRound = null;
        // Draw
        if (turn_res.Winner == null)
        {
            host?.TellRaw("@a", "[Tictactoe] It's a draw!");

            return;
        }

        string winner = turn_res.Winner;

        host?.TellRaw("@a", $"[Tictactoe] {winner} has won the game!");

        host?.TellRaw("@a", "[Tictactoe] The game has ended! Type .tictactoe to start a new game!");
    }

    private TurnResult CheckWin(TictactoeTurn turn)
    {
        if (CurrentRound == null) return new TurnResult(false, null);

        var board = CurrentRound?.Board;
        var player1 = CurrentRound?.Owner;
        var player2 = CurrentRound?.Participate;
        
        if (board == null) return new TurnResult(false, null);
        
        // Check rows
        for (int i = 0; i < 3; i++)
        {
            if (board[i, 0] == board[i, 1] && board[i, 1] == board[i, 2] && board[i, 0] != 0)
            {
                return new TurnResult(true, turn == TictactoeTurn.Owner ? player1 : player2);
            }
        }

        // Check columns
        for (int i = 0; i < 3; i++)
        {
            if (board[0, i] == board[1, i] && board[1, i] == board[2, i] && board[0, i] != 0)
            {
                return new TurnResult(true, turn == TictactoeTurn.Owner ? player1 : player2);
            }
        }

        // Check diagonals
        if (board[0, 0] == board[1, 1] && board[1, 1] == board[2, 2] && board[0, 0] != 0)
        {
            return new TurnResult(true, turn == TictactoeTurn.Owner ? player1 : player2);
        }

        // Check Draw
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; i++)
            {
                // Still empty space
                if (board[i, j] == 0) return new TurnResult(false, null);
            }
        }

        // Draw
        return new TurnResult(true, null);
    }

    public bool IsPlayerPlaying(string player)
    {
        return CurrentRound?.Owner == player || CurrentRound?.Participate == player;
    }

    private int CheckType(char c)
    {
        if (c >= 'a' && c <= 'c') return 1;
        if (c >= '1' && c <= '3') return -1;
        return 0;
    }

    private void PrintBoard()
    {
        if (CurrentRound == null) return;

        var board = CurrentRound.Board;
 
        // [Tictactoe] §a§lCurrent Board:
        // [Tictactoe] | \ | A | B | C |
        // [Tictactoe] +---+---+---+---+
        // [Tictactoe] | 1 |___|___|___|
        // [Tictactoe] +---+---+---+---+
        // [Tictactoe] | 2 |___|___|___|
        // [Tictactoe] +---+---+---+---+
        // [Tictactoe] | 3 |___|___|___|

        host?.TellRaw("@a", @"[Tictactoe] §a§lCurrent Board:");
        host?.TellRaw("@a", @"[Tictactoe] | \ | A | B | C |");
        host?.TellRaw("@a", @"[Tictactoe] +---+---+---+---+");
        host?.TellRaw("@a", GetBoardLine(board, 1));
        host?.TellRaw("@a", @"[Tictactoe] +---+---+---+---+");
        host?.TellRaw("@a", GetBoardLine(board, 2));
        host?.TellRaw("@a", @"[Tictactoe] +---+---+---+---+");
        host?.TellRaw("@a", GetBoardLine(board, 3));
    }

    public RawJson GetBoardLine(TictactoeMark[,] board,int line)
    {
        RawJson res = new RawJson()
                            .WriteStartArray()
                            .WriteStartObject()
                            .WriteText($"[Tictactoe] | {line} |")
                            .WriteEndObject();

        for (int i = 0; i < 3; i++)
        {
            if (board[line - 1, i] != TictactoeMark.Empty)
            {
                res.WriteStartObject()
                        .WriteText($" {board[line - 1, i]} ")
                    .WriteEndObject();
            }
            else
            {
                res.WriteStartObject()
                        .WriteText("   ")
                    .WritePropertyName("clickEvent")
                        .WriteStartObject()
                            .WriteProperty("action", "run_command")
                            .WriteProperty("value", $"/say {line}{"abc"[i]}")
                        .WriteEndObject()
                    .WriteEndObject();
            }
            
            res.WriteStartObject()
                    .WriteText("|")
                .WriteEndObject();
        }

        return res.WriteEndArray();
    }
}

public class TictactoeRound
{
    public string Owner { get; set;} = null!;
    public string Participate { get; set;} = null!;

    public TictactoeMark[,] Board { get; set; } = new TictactoeMark[3, 3];

    public TictactoeTurn Turn { get; set; } = TictactoeTurn.Owner;

    public bool IsPlayerTurn(string player)
    {
        return (Turn == TictactoeTurn.Owner && Owner == player) || (Turn == TictactoeTurn.Participate && Participate == player);
    }
}

public struct TurnResult
{
    public bool IsEnded { get; set; }
    public string? Winner { get; set; }
    public TurnResult(bool isEnded, string? winner)
    {
        IsEnded = isEnded;
        Winner = winner;
    }

    public bool isDraw()
    {
        return !IsEnded && Winner == null;
    }
}

public enum TictactoeTurn
{
    Owner,
    Participate
}

public enum TictactoeMark
{
    [Description(" ")]
    Empty,
    [Description("X")]
    X,
    [Description("O")]
    O,
}

public enum JoinResult
{
    Join,
    Full,
    NotPlaying,
    InCorrectFormat,
    NotJoining,
    NotYourTurn,
}

public static class EnumExtensions
{
    public static string ToFriendlyString(this Enum value)
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);
        if (name != null)
        {
            var field = type.GetField(name);
            if (field != null)
            {
                var attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attr != null)
                {
                    return attr.Description;
                }
            }
            return name;
        }
        return " ";
    }
}