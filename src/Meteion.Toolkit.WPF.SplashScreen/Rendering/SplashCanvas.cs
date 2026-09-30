using System.Drawing;
using System.Drawing.Imaging;
using Meteion.Toolkit.WPF.SplashScreen.Native;

namespace Meteion.Toolkit.WPF.SplashScreen.Rendering;

/// <summary>
/// A top-down 32bpp premultiplied-ARGB DIB section that GDI+ draws into directly (through a <see cref="Bitmap"/>
/// over the DIB's memory) and that <c>UpdateLayeredWindow</c> can consume without any per-frame conversion.
/// Thread-affine: create, use and dispose on the splash thread.
/// </summary>
internal sealed unsafe class SplashCanvas : IDisposable
{
    private readonly nint _previousBitmap;
    private nint _hbitmap;

    public SplashCanvas(int width, int height)
    {
        Width = width;
        Height = height;

        Hdc = NativeMethods.CreateCompatibleDC(0);
        if (Hdc == 0)
        {
            throw new InvalidOperationException("CreateCompatibleDC failed.");
        }

        try
        {
            var info = new NativeMethods.BITMAPINFOHEADER
            {
                BiSize = (uint)sizeof(NativeMethods.BITMAPINFOHEADER),
                BiWidth = width,
                BiHeight = -height, // negative = top-down, matching GDI+'s scan order
                BiPlanes = 1,
                BiBitCount = 32,
                BiCompression = NativeMethods.BI_RGB,
            };

            void* bits;
            _hbitmap = NativeMethods.CreateDIBSection(Hdc, &info, NativeMethods.DIB_RGB_COLORS, &bits, 0, 0);
            if (_hbitmap == 0 || bits == null)
            {
                throw new InvalidOperationException("CreateDIBSection failed.");
            }

            Bits = (byte*)bits;
            _previousBitmap = NativeMethods.SelectObject(Hdc, _hbitmap);
            Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, (nint)bits);
            Graphics = Graphics.FromImage(Bitmap);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public int Width { get; }

    public int Height { get; }

    public nint Hdc { get; private set; }

    public byte* Bits { get; }

    public int ByteCount => Width * Height * 4;

    public Bitmap Bitmap { get; } = null!;

    public Graphics Graphics { get; } = null!;

    public void Dispose()
    {
        Graphics?.Dispose();
        Bitmap?.Dispose();

        if (Hdc != 0)
        {
            if (_previousBitmap != 0)
            {
                NativeMethods.SelectObject(Hdc, _previousBitmap);
            }

            if (_hbitmap != 0)
            {
                NativeMethods.DeleteObject(_hbitmap);
                _hbitmap = 0;
            }

            NativeMethods.DeleteDC(Hdc);
            Hdc = 0;
        }
    }
}
