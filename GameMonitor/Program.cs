using System.Text.RegularExpressions;

public class Program
{
    public static string[] CounterTerroristsWinPatterns =
{
        "TS ELIMINATED",
        "TERRORISTS ELIMINATED",
        "BOMB DEFUSED",
        "COUNTER TERRORISTS WIN",
        "[T]COUNTER-TERRORISTS HAVE WON THE ROUND"
};
public static string[] TerroristsWinPatterns =
    {
        "CTS ELIMINATED",
        "COUNTER-TERRORISTS ELIMINATED",
        "BOMB DETONATED",
        "TERRORISTS WIN",
        "[T]TERRORISTS HAVE WON THE ROUND"
    
};

    public static Regex Regex = new Regex(
        $@"(?<terrorists>\b({string.Join("|", TerroristsWinPatterns)})\b)|(?<counterterrorists>\b({string.Join("|", CounterTerroristsWinPatterns)})\b)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    static void Main(string[] args)
    {
        var monitor = new GameMonitor(Regex, "Counter-Strike Source");

        monitor.MonitorGame();
    }
}
