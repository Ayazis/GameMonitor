using System.Drawing;

class ImageProcessor
{
    public static Bitmap IsolateRedOrBlueText(Bitmap image)
    {
        Bitmap result = new Bitmap(image.Width, image.Height);

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                Color pixel = image.GetPixel(x, y);

                // Check if the pixel is "red enough" or "blue enough"
                bool isRed = pixel.R > 150 && pixel.G < 80 && pixel.B < 80;  // Red filter
                bool isBlue = pixel.B > 150 && pixel.R < 80 && pixel.G < 80; // Blue filter

                if (isRed || isBlue)
                {
                    result.SetPixel(x, y, Color.White); // Keep red/blue text as white
                }
                else
                {
                    result.SetPixel(x, y, Color.Black); // Set everything else to black
                }
            }
        }

        return result;
    }
}
