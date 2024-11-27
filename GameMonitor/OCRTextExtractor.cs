using System.Drawing;
using Tesseract;
// Single Responsibility Principle: Handles window capture
// Single Responsibility Principle: Handles OCR text extraction
public interface IOCRTextExtractor
{
    string ExtractText(Bitmap image);
}



public class OCRTextExtractor : IOCRTextExtractor
{
    public string ExtractText(Bitmap image)
    {
        using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
        {
            using (var pix = PixConverter.ToPix(image))
            {
                using (var page = engine.Process(pix))
                {
                    return page.GetText();
                }
            }
        }
    }
}
