using System.Drawing;
using System.Runtime.InteropServices;
public interface IWindowCapturer
{
    Bitmap CaptureWindow();
}
public class WindowCapturer : IWindowCapturer
{
    IntPtr hdcWindow;
    IntPtr hwnd;
    public WindowCapturer(string windowTitle)
    {
        hwnd = FindWindow(null, windowTitle);
        if (hwnd == IntPtr.Zero)
            throw new InvalidOperationException("Window not found.");

        hdcWindow = GetWindowDC(hwnd);
        if (hdcWindow == IntPtr.Zero)
            throw new InvalidOperationException("Failed to get device context.");
    }
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    public Bitmap CaptureWindow()
    {
      

        return CreateImageFromWindow(hwnd, hdcWindow);

    }

    private static Bitmap CreateImageFromWindow(nint hwnd, nint hdcWindow)
    {
        // Get the DPI scale for the window
        float dpiScale = GetDpiForWindow(hwnd) / 96f; // Default DPI is 96        
        // Get the actual window dimensions, adjusted for DPI
        RECT windowRect;
        GetWindowRect(hwnd, out windowRect);
        int width = (int)((windowRect.Right - windowRect.Left) * dpiScale);
        int height = (int)((windowRect.Bottom - windowRect.Top) * dpiScale);

        // Create a bitmap with adjusted size
        Bitmap bmp = new Bitmap(width, height);

        // Use PrintWindow to capture the window content
        using (Graphics gBmp = Graphics.FromImage(bmp))
        {
            IntPtr hdcBmp = gBmp.GetHdc();
            bool success = PrintWindow(hwnd, hdcBmp, 0);
            gBmp.ReleaseHdc(hdcBmp);

            if (!success)
            {
                bmp.Dispose();
                throw new InvalidOperationException("Failed to capture the window content.");
            }
        }

        // Release the device context
        ReleaseDC(hwnd, hdcWindow);

        // Save the bitmap to a file
        string filePath = "captured_window.png"; // Change the file name and extension if needed
        bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

        return bmp;
    }
    // Native function for Windows 10+ DPI scaling
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hwnd);

    // Helper to define RECT structure
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint hwnd, out RECT lpRect);

}
