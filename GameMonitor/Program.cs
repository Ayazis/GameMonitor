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
        $@"(?<counterterrorists>(?<=^|\s)({string.Join("|", CounterTerroristsWinPatterns.Select(Regex.Escape))})(?=$|\s))|
            (?<terrorists>(?<=^|\s)({string.Join("|", TerroristsWinPatterns.Select(Regex.Escape))})(?=$|\s))",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace
    );
    static void Main(string[] args)
    {
        var monitor = new GameMonitor(Regex, "Counter-Strike Source", new SoundPlayer());

        monitor.MonitorGame();
    }
}
