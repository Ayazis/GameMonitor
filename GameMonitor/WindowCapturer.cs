using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Linq;

public class WindowCapturer
{
    private const int TargetWidth = 1920;
    private const int TargetHeight = 1080;
    private Bitmap _cachedBitmap;
    private Graphics _graphics;
    private Texture2D _currentTexture;
    private SharpDX.Direct3D11.Device _device;
    private OutputDuplication _deskDupl;
    private IntPtr _windowHandle;
    private string _windowTitle;
    private float _dpiScaling;

    public WindowCapturer()
    {
        _windowHandle = FindWindow(null, "Counter-Strike Source");
        if (_windowHandle == IntPtr.Zero)
        {
            _windowHandle = FindWindow(null, "Counter-Strike 2");
            if (_windowHandle == IntPtr.Zero)
                throw new ArgumentException("Window not found.");
        }

        _dpiScaling = GetDpiScalingForMonitor(_windowHandle);

        _cachedBitmap = new Bitmap(TargetWidth, TargetHeight, PixelFormat.Format32bppArgb);
        _graphics = Graphics.FromImage(_cachedBitmap);
        _graphics.Clear(Color.Black);
        InitializeCapture();
    }

    private void InitializeCapture()
    {
        var factory = new Factory1();
        var adapter = factory.Adapters.First();
        _device = new SharpDX.Direct3D11.Device(adapter);

        // Create output duplication for the desktop
        var output = adapter.Outputs.First();
        var output1 = output.QueryInterface<Output1>();
        _deskDupl = output1.DuplicateOutput(_device);
    }

    public Bitmap Capture()
    {
        Bitmap bitmap = null;
        if (_deskDupl == null)
        {
            throw new InvalidOperationException("No valid duplication interface available.");
        }

        RECT windowRect;
        if (!GetClientRect(_windowHandle, out windowRect))
        {
            throw new InvalidOperationException("Failed to get client rectangle.");
        }

        POINT topLeft = new POINT { x = 0, y = 0 };
        if (!ClientToScreen(_windowHandle, ref topLeft))
        {
            throw new InvalidOperationException("Failed to convert client coordinates to screen coordinates.");
        }

        // Calculate the window's absolute position and size, applying DPI scaling correctly
        int windowLeft = topLeft.x;
        int windowTop = topLeft.y;
        int windowWidth = windowRect.Right - windowRect.Left;
        int windowHeight = windowRect.Bottom - windowRect.Top;

        float currentDpiScaling = GetDpiScalingForMonitor(_windowHandle);
        windowLeft = (int)(windowLeft * currentDpiScaling);
        windowTop = (int)(windowTop * currentDpiScaling);
        windowWidth = (int)(windowWidth * currentDpiScaling);
        windowHeight = (int)(windowHeight * currentDpiScaling);

        try
        {
            OutputDuplicateFrameInformation frameInfo;
            SharpDX.DXGI.Resource desktopResource;
            _deskDupl.TryAcquireNextFrame(500, out frameInfo, out desktopResource);

            using (desktopResource)
            using (var tempTexture = desktopResource.QueryInterface<Texture2D>())
            {
                // Copy resource into CPU accessible texture
                var textureDesc = tempTexture.Description;
                textureDesc.Usage = ResourceUsage.Staging;
                textureDesc.BindFlags = BindFlags.None;
                textureDesc.CpuAccessFlags = CpuAccessFlags.Read;
                textureDesc.OptionFlags = ResourceOptionFlags.None;

                _currentTexture = new Texture2D(_device, textureDesc);
                _device.ImmediateContext.CopyResource(tempTexture, _currentTexture);

                // Map the resource to access pixel data
                var dataBox = _device.ImmediateContext.MapSubresource(_currentTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                try
                {
                    using (var dataStream = new DataStream(dataBox.DataPointer, _currentTexture.Description.Height * dataBox.RowPitch, true, false))
                    {
                        // Create Bitmap from the data stream
                        using (Bitmap fullScreenBitmap = new Bitmap(_currentTexture.Description.Width, _currentTexture.Description.Height, dataBox.RowPitch, PixelFormat.Format32bppArgb, dataStream.DataPointer))
                        {
                            // Make sure windowWidth and windowHeight do not exceed the full screen bitmap dimensions
                            windowWidth = Math.Min(windowWidth, fullScreenBitmap.Width - windowLeft);
                            windowHeight = Math.Min(windowHeight, fullScreenBitmap.Height - windowTop);

                            // Use Graphics to efficiently crop the game window area
                            bitmap = new Bitmap(windowWidth, windowHeight, PixelFormat.Format32bppArgb);
                            using (Graphics graphics = Graphics.FromImage(bitmap))
                            {
                                graphics.DrawImage(fullScreenBitmap, new Rectangle(0, 0, windowWidth, windowHeight), windowLeft, windowTop, windowWidth, windowHeight, GraphicsUnit.Pixel);
                            }
                        }
                    }
                }
                finally
                {
                    // Unmap the resource
                    _device.ImmediateContext.UnmapSubresource(_currentTexture, 0);
                }
            }

            using (Graphics g = Graphics.FromImage(_cachedBitmap))
            {
                g.DrawImage(bitmap, 0, 0, TargetWidth, TargetHeight);
            }

            _deskDupl.ReleaseFrame();
            return _cachedBitmap;
        }
        catch (SharpDXException ex)
        {
            throw new InvalidOperationException("Failed to capture frame: " + ex.Message);
        }
        finally
        {
            bitmap?.Dispose();
            _currentTexture?.Dispose();
        }
    }

    public void Save(string filePath)
    {
        if (_cachedBitmap == null)
        {
            throw new InvalidOperationException("No captured bitmap available.");
        }

        try
        {
            _cachedBitmap.Save(filePath, ImageFormat.Png);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.IO.IOException)
        {
            throw new InvalidOperationException("Failed to save the file: " + ex.Message);
        }
    }

    ~WindowCapturer()
    {
        _graphics?.Dispose();
        _cachedBitmap?.Dispose();
        _deskDupl?.Dispose();
        _device?.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr hwnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ClientToScreen(IntPtr hwnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    private float GetDpiScalingForMonitor(IntPtr hwnd)
    {
        // Get monitor associated with the window handle
        IntPtr monitor = MonitorFromWindow(hwnd, 2 /*MONITOR_DEFAULTTONEAREST*/);

        // Get DPI for the monitor
        uint dpiX, dpiY;
        GetDpiForMonitor(monitor, 0 /*MDT_EFFECTIVE_DPI*/, out dpiX, out dpiY);

        return dpiX / 96.0f; // 96 is the default DPI
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, uint dpiType, out uint dpiX, out uint dpiY);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }
}
