using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public class WindowCapturer
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int width, int height, IntPtr hdcSrc, int xSrc, int ySrc, CopyPixelOperation rop);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private readonly IntPtr _hwnd;            // Window handle
    private Bitmap _tempBitmap;              // Temporary bitmap for raw capture
    private Bitmap _finalBitmap;             // Final output bitmap (800x600)
    private int _originalWidth;              // Original window width
    private int _originalHeight;             // Original window height
    private float _aspectRatio;              // Cached aspect ratio
    private int _scaledWidth;                // Cached scaled width
    private int _scaledHeight;               // Cached scaled height

    private const int TargetWidth = 1920;     // Fixed target width
    private const int TargetHeight = 1080;    // Fixed target height

    public WindowCapturer(string windowTitle)
    {
        // Retrieve the window handle by title
        _hwnd = FindWindow(null, windowTitle);
        if (_hwnd == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Window with title '{windowTitle}' not found.");
        }

        InitializeDimensionsAndScaling();    // Cache dimensions and scaling
        InitializeBitmaps();                 // Preallocate reusable bitmaps
    }

    private void InitializeDimensionsAndScaling()
    {
        // Retrieve window dimensions only once unless window is resized
        RECT rect;
        if (!GetWindowRect(_hwnd, out rect))
        {
            throw new InvalidOperationException("Failed to get window dimensions.");
        }

        _originalWidth = rect.Right - rect.Left;
        _originalHeight = rect.Bottom - rect.Top;
        _aspectRatio = (float)_originalWidth / _originalHeight;

        // Precompute scaled dimensions while maintaining aspect ratio
        if (_aspectRatio >= 1) // Landscape or square
        {
            _scaledWidth = TargetWidth;
            _scaledHeight = (int)(TargetWidth / _aspectRatio);
        }
        else // Portrait (unlikely, but handle gracefully)
        {
            _scaledHeight = TargetHeight;
            _scaledWidth = (int)(TargetHeight * _aspectRatio);
        }
    }

    private void InitializeBitmaps()
    {
        // Allocate temporary bitmap for raw capture
        if (_tempBitmap == null || _tempBitmap.Width != _originalWidth || _tempBitmap.Height != _originalHeight)
        {
            _tempBitmap?.Dispose();
            _tempBitmap = new Bitmap(_originalWidth, _originalHeight, PixelFormat.Format32bppArgb);
        }

        // Allocate final bitmap for scaled output
        if (_finalBitmap == null || _finalBitmap.Width != TargetWidth || _finalBitmap.Height != TargetHeight)
        {
            _finalBitmap?.Dispose();
            _finalBitmap = new Bitmap(TargetWidth, TargetHeight, PixelFormat.Format32bppArgb);
        }
    }

    public Bitmap Capture()
    {
        // Capture the window content into the temporary bitmap
        using (Graphics gTemp = Graphics.FromImage(_tempBitmap))
        {
            IntPtr hdcTemp = gTemp.GetHdc();
            IntPtr hdcWindow = GetDC(_hwnd);

            if (!BitBlt(hdcTemp, 0, 0, _originalWidth, _originalHeight, hdcWindow, 0, 0, CopyPixelOperation.SourceCopy))
            {
                ReleaseDC(_hwnd, hdcWindow);
                gTemp.ReleaseHdc(hdcTemp);
                throw new InvalidOperationException("Failed to capture the window content.");
            }

            ReleaseDC(_hwnd, hdcWindow);
            gTemp.ReleaseHdc(hdcTemp);
        }

        // Scale the temporary bitmap into the final bitmap while maintaining aspect ratio
        using (Graphics gFinal = Graphics.FromImage(_finalBitmap))
        {
            gFinal.Clear(Color.Black); // Fill any blank space
            gFinal.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            gFinal.DrawImage(_tempBitmap,
                (TargetWidth - _scaledWidth) / 2,
                (TargetHeight - _scaledHeight) / 2,
                _scaledWidth,
                _scaledHeight);
        }

        return _finalBitmap;
    }

    public void Dispose()
    {
        // Clean up bitmaps
        _tempBitmap?.Dispose();
        _finalBitmap?.Dispose();
        _tempBitmap = null;
        _finalBitmap = null;
    }
}
