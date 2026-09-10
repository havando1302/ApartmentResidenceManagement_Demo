using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ApartmentResidenceManagement.Wpf.Tests;

internal sealed class InvisibleHost : IDisposable
{
    private readonly HwndSource _source;
    private readonly Size _size;
    public Grid Root { get; }

    public InvisibleHost(UIElement content, double width = 1280, double height = 900)
    {
        _size = new Size(width, height);
        Root = new Grid { Width = width, Height = height };
        // Omitting WS_VISIBLE keeps the native host invisible while WPF still
        // receives a presentation source and normal Loaded/Unloaded events.
        _source = new HwndSource(new HwndSourceParameters("WPF regression tests")
        {
            Width = (int)Math.Ceiling(width),
            Height = (int)Math.Ceiling(height),
            WindowStyle = unchecked((int)0x80000000)
        });
        Root.Children.Add(content);
        _source.RootVisual = Root;
        Settle();
    }

    public void Settle()
    {
        for (var pass = 0; pass < 3; pass++)
        {
            Root.Measure(_size);
            Root.Arrange(new Rect(_size));
            Root.UpdateLayout();
            var frame = new DispatcherFrame();
            Root.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }
    }

    public void SaveScreenshot(string path)
    {
        var bitmap = new RenderTargetBitmap((int)_size.Width, (int)_size.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(Root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = System.IO.File.Create(path);
        encoder.Save(stream);
    }

    public void Dispose()
    {
        _source.RootVisual = null;
        Settle();
        _source.Dispose();
    }
}
