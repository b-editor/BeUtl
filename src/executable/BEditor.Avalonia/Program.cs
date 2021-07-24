using System;
using System.Globalization;
using System.Linq;

using Avalonia;

namespace BEditor
{
    static class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            CultureInfo.CurrentUICulture = new(Settings.Default.Language);

            if (args.ElementAtOrDefault(0) == "package-install")
            {
                PackageInstaller.Program.Main(args.Skip(1).ToArray());
            }
            else
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
    }
}