using System.Diagnostics;
using System.Text.RegularExpressions;

public class GameMonitor
{
    readonly Regex regex;
    readonly string windowTitle;
    IWindowCapturer windowCapturer = new WindowCapturer();
    IOCRTextExtractor textExtractor = new OCRTextExtractor();

    public GameMonitor(Regex regex, string windowTitle)
    {
        this.windowTitle = windowTitle;
        this.regex = regex;
    }

    public void MonitorGame()
    {
        try
        {

            while (true)
            {
                string extractedText = ExtractTextFromGame();
                var match = regex.Match(extractedText);

                if (match.Success)
                {
//                    Console.WriteLine(extractedText.Replace(" ", ""));

                    if (match.Groups["terrorists"].Success)
                    {
                        TerroristsWin();
                    }
                    else if (match.Groups["counterterrorists"].Success)
                    {
                        CounterTerroristsWin();
                    }
                    Task.Delay(10000).Wait();
                }
                else
                {
                    Console.Clear();
                    WriteInProgress();
                }

                Task.Delay(2000).Wait();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static void CounterTerroristsWin()
    {
        Console.WriteLine("Counter Terrorists win!");
        SoundPlayer.PlaySound(@"./sounds/Counter Terrorists Win - CS GO - QuickSounds.com.mp3");
   
        
    }

    private static void TerroristsWin()
    {
        Console.WriteLine("Terrorists win!");
        SoundPlayer.PlaySound(@"./sounds/Terrorists Win - CS GO - QuickSounds.com.mp3");       
    }

    private string ExtractTextFromGame()
    {
        string extractedText = string.Empty;

        // var capturedImage = new Bitmap(new MemoryStream(File.ReadAllBytes("./testimages/Terrorist win.png")));
        using (var capturedImage = windowCapturer.CaptureWindow(windowTitle))
        {
            extractedText = textExtractor.ExtractText(capturedImage);
        }
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
