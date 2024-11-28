using System.Diagnostics;
using System.Drawing;
using Tesseract;
public interface IOCRTextExtractor
{
    string ExtractText(Bitmap image);
}



public class OCRTextExtractor : IOCRTextExtractor
{

    TesseractEngine _engine;
    public OCRTextExtractor()
    {
        _engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
    }
    public string ExtractText(Bitmap image)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        using (var pix = PixConverter.ToPix(image))
        {
            using (var page = _engine.Process(pix))
            {                
                return page.GetText();
            }
        }
       

    }
}
