#if HAS_UNO_SKIA
using System;
using Uno.UI.Hosting;
using Microsoft.Extensions.Hosting;

namespace E6Viewer;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }
}
#endif
