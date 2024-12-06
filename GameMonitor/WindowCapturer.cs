using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SharpDX;
using Device = SharpDX.Direct3D11.Device;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using System.Linq;
using EasyHook;
using SharpDX.Direct3D;

public class WindowCapturer
{
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

    private const int TargetWidth = 1920;
    private const int TargetHeight = 1080;
    private static Bitmap _cachedBitmap;
    private IntPtr _windowHandle;
    private string _windowTitle;
    private Device _device;
    private SwapChain _swapChain;
    private Texture2D _stagingTexture;
    private DeviceContext _context;
    private static IDXGISwapChain_PresentDelegate _presentDelegate;
    private static LocalHook _hook;
    private static bool _captureNextFrame = false;

    public WindowCapturer()
    {
        _windowHandle = FindWindow(null, "Counter-Strike Source");
        if (_windowHandle == IntPtr.Zero)
        {
            _windowHandle = FindWindow(null, "Counter-Strike 2");
            if (_windowHandle == IntPtr.Zero)
                throw new ArgumentException("Window not found.");
        }

        InitializeDirectX();
        HookPresentMethod();
    }

    private void InitializeDirectX()
    {
        var swapChainDescription = new SwapChainDescription()
        {
            BufferCount = 1,
            ModeDescription = new ModeDescription(TargetWidth, TargetHeight, new Rational(60, 1), Format.R8G8B8A8_UNorm),
            IsWindowed = true,
            OutputHandle = _windowHandle,
            SampleDescription = new SampleDescription(1, 0),
            SwapEffect = SwapEffect.Discard,
            Usage = Usage.RenderTargetOutput
        };

        Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.BgraSupport, swapChainDescription, out _device, out _swapChain);
        _context = _device.ImmediateContext;

        var textureDesc = new Texture2DDescription
        {
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None,
            Format = Format.B8G8R8A8_UNorm,
            Width = TargetWidth,
            Height = TargetHeight,
            ArraySize = 1,
            MipLevels = 1,
            OptionFlags = ResourceOptionFlags.None,
            Usage = ResourceUsage.Staging,
            SampleDescription = new SampleDescription(1, 0)
        };

        _stagingTexture = new Texture2D(_device, textureDesc);
    }

    private void HookPresentMethod()
    {
        IntPtr presentAddress = Marshal.GetFunctionPointerForDelegate((IDXGISwapChain_PresentDelegate)PresentHooked);
        _hook = LocalHook.Create(presentAddress, new IDXGISwapChain_PresentDelegate(PresentHooked), this);
        _hook.ThreadACL.SetExclusiveACL(new int[] { 0 });
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int IDXGISwapChain_PresentDelegate(IntPtr swapChainPtr, uint syncInterval, uint flags);

    private static int PresentHooked(IntPtr swapChainPtr, uint syncInterval, uint flags)
    {
        try
        {
            if (_captureNextFrame)
            {
                var swapChain = new SwapChain(swapChainPtr);
                using (var backBuffer = swapChain.GetBackBuffer<Texture2D>(0))
                {
                    var device = backBuffer.Device;
                    var context = device.ImmediateContext;

                    // Copy the back buffer to a staging texture
                    var textureDesc = backBuffer.Description;
                    textureDesc.Usage = ResourceUsage.Staging;
                    textureDesc.CpuAccessFlags = CpuAccessFlags.Read;
                    textureDesc.BindFlags = BindFlags.None;

                    using (var stagingTexture = new Texture2D(device, textureDesc))
                    {
                        context.CopyResource(backBuffer, stagingTexture);
                        var dataBox = context.MapSubresource(stagingTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                        Bitmap bitmap = new Bitmap(textureDesc.Width, textureDesc.Height, PixelFormat.Format32bppArgb);
                        var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, textureDesc.Width, textureDesc.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                        Utilities.CopyMemory(bitmapData.Scan0, dataBox.DataPointer, textureDesc.Height * dataBox.RowPitch);
                        bitmap.UnlockBits(bitmapData);

                        // Store the captured bitmap
                        _cachedBitmap = bitmap;

                        context.UnmapSubresource(stagingTexture, 0);
                    }
                }
                _captureNextFrame = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in Present Hook: " + ex.Message);
        }

        // Call original Present function
        return _presentDelegate(swapChainPtr, syncInterval, flags);
    }

    public Bitmap Capture()
    {
        _captureNextFrame = true;

        // Wait for the next frame to be captured
        while (_captureNextFrame)
        {
            // Busy wait, could be improved with proper signaling or timeout
        }

        if (_cachedBitmap == null)
        {
            throw new InvalidOperationException("Failed to capture the frame.");
        }

        return (Bitmap)_cachedBitmap.Clone();
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
        _cachedBitmap?.Dispose();
        _stagingTexture?.Dispose();
        _swapChain?.Dispose();
        _context?.Dispose();
        _device?.Dispose();
    }
}
