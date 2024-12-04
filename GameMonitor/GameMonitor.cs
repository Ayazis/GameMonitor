using System.Diagnostics;
using System.Text.RegularExpressions;

public class GameMonitor
{
    readonly Regex regex;
    readonly string windowTitle;
    WindowCapturer windowCapturer = new WindowCapturer("Counter-Strike Source");
    IOCRTextExtractor textExtractor = new OCRTextExtractor();
    readonly ISoundPlayer _soundPlayer;

    public GameMonitor(Regex regex, string windowTitle, ISoundPlayer soundPlayer)
    {
        this._soundPlayer = soundPlayer;
        this.windowTitle = windowTitle;
        this.regex = regex;
    }

    public void MonitorGame()
    {
        try
        {
            Stopwatch watch = new Stopwatch();

            while (true)
            {
                watch.Start();
              //  Console.WriteLine("checkking");

                string extractedText = ExtractTextFromGame();
              //  Console.Clear();
                //Console.WriteLine(watch.ElapsedMilliseconds);

                var match = regex.Match(extractedText);

                if (match.Success)
                {

                    if (match.Groups["terrorists"].Success)
                    {
                        TerroristsWin();
                    }
                    else if (match.Groups["counterterrorists"].Success)
                    {
                        CounterTerroristsWin();
                    }
                    // Log individual groups and their values
                    foreach (var groupName in match.Groups.Keys)
                    {
                        Console.WriteLine($"Group: {groupName}, Value: {match.Groups[groupName].Value}");
                    }
                    //Console.WriteLine(extractedText.Replace(" ", ""));
                    Task.Delay(10000).Wait();
                }
                else
                {
                 
                    WriteInProgress();
                }

                watch.Stop();
                watch.Reset();
                var timepassed = watch.ElapsedMilliseconds;
                if (timepassed < 500)
                    Task.Delay(500 - (int)watch.ElapsedMilliseconds).Wait();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void CounterTerroristsWin()
    {
        Console.WriteLine("Counter Terrorists win!");
        _soundPlayer.PlayCtWin();
    }

    private void TerroristsWin()
    {
        Console.WriteLine("Terrorists win!");
        _soundPlayer.PlayTwin();
    }

    private string ExtractTextFromGame()
    {
        string extractedText = string.Empty;

        // var capturedImage = new Bitmap(new MemoryStream(File.ReadAllBytes("./testimages/Terrorist win.png")));

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var capturedImage = windowCapturer.Capture();
        
            
            if (capturedImage == null)
                return "";

            extractedText = textExtractor.ExtractText(capturedImage);
        
        Console.WriteLine("extracted image in:"+stopwatch.ElapsedMilliseconds);   
        return extractedText;
    }

    static int nrOfDots = 1;
    private static void WriteInProgress()
    {
        int newNrOfDots = nrOfDots == 3 ? 0 : nrOfDots + 1;
        string stringToWrite = "Game in progress";
        for (int i = 0; i < newNrOfDots; i++)
        {
            stringToWrite += ".";
        }
        Console.WriteLine(stringToWrite);
        nrOfDots = newNrOfDots;
    }
}
