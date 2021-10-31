using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;

using BEditor.Data;
using BEditor.Data.Property.Easing;
using BEditor.Drawing;
using BEditor.Extensions;
using BEditor.Graphics.Platform;
using BEditor.LangResources;
using BEditor.Models;
using BEditor.Models.ManagePlugins;
using BEditor.Packaging;
using BEditor.Plugin;
using BEditor.Primitive;
using BEditor.ViewModels.Settings;
using BEditor.Views;
using BEditor.Views.DialogContent;
using BEditor.Views.Setup;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Reactive.Bindings;

namespace BEditor
{
    public sealed class App : Application
    {
        public static readonly ILogger Logger = AppModel.Current.LoggingFactory.CreateLogger<App>();
        public static readonly DispatcherTimer BackupTimer = new()
        {
            Interval = TimeSpan.FromMinutes(Settings.Default.BackUpInterval)
        };

        public static ValueTask StartupTask { get; set; }

        public static void Shutdown(int exitCode)
        {
            ((IClassicDesktopStyleApplicationLifetime)Current.ApplicationLifetime).Shutdown(exitCode);
        }

        public static Window GetMainWindow()
        {
            if (Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }

            throw new Exception();
        }

        public static void SetMainWindow(Window window)
        {
            if (Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = window;
            }
        }

        public override void RegisterServices()
        {
            if (OperatingSystem.IsLinux())
            {
                AvaloniaLocator.CurrentMutable.Bind<IFontManagerImpl>().ToConstant(new CustomFontManagerImpl());
            }

            IPlatform.Current = Settings.Default.GraphicsProfile switch
            {
                ProjectViewModel.OPENGL => new Graphics.OpenGL.OpenGLPlatform(),
                ProjectViewModel.SKIA => new Graphics.Skia.SkiaPlatform(),
                ProjectViewModel.METAL => new Graphics.Veldrid.VeldridMetalPlatform(),
                ProjectViewModel.DIRECT3D11 => new Graphics.Veldrid.VeldridDirectXPlatform(),
                ProjectViewModel.VULKAN => new Graphics.Veldrid.VeldridVulkanPlatform(),
                _ => new Graphics.OpenGL.OpenGLPlatform(),
            };

            base.RegisterServices();
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            UIDispatcherScheduler.Initialize();
            var baseuri = new Uri("avares://beditor/App.axaml");
            var style = new StyleInclude(baseuri)
            {
                Source = Settings.Default.UseDarkMode ? new("avares://beditor/Controls/DarkTheme.axaml") : new("avares://beditor/Controls/LightTheme.axaml")
            };

            Styles.Insert(0, style);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                RegisterPrimitive();

                desktop.MainWindow = Settings.Default.ShowStartWindow ? new StartWindow() : new MainWindow();
                AppModel.Current.UIThread = SynchronizationContext.Current;

                StartupTask = new(Task.Run(async () =>
                {
                    await InitialPluginsAsync();
                    ServicesLocator.Current = new(AppModel.Current.ServiceProvider);

                    AppModel.Current.User = await Tool.LoadFromAsync(
                        Path.Combine(ServicesLocator.GetUserFolder(), "token"),
                        AppModel.Current.ServiceProvider.GetRequiredService<IAuthenticationProvider>());

                    await SetupAudioContextAsync();
                    await SetupAsync();
                    await ArgumentsContext.ExecuteAsync();

                    // �t�H���g�̓ǂݍ���
                    _ = FontManager.Default;

                    AppModel.Current.RestoreDisplayedMenus();
                }));
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                RunBackup();

                desktop.Exit += Desktop_Exit;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            // ���ݎg���Ă��錾�ꂪ�A���C���X�g�[������v���O�C���ɂ���Ē񋟂���Ă���ꍇ�A
            // �C���X�g�[���[�̋N���O�Ɍ���ݒ���f�t�H���g�ɕύX����B
            for (var i = 0; i < Settings.Default.SupportedLanguages.Count; i++)
            {
                var lang = Settings.Default.SupportedLanguages[i];
                var asmName = lang.Assembly.GetName().Name;

                if (PluginChangeSchedule.Uninstall.Any(item => item.AssemblyName == asmName) ||
                    PluginChangeSchedule.UpdateOrInstall.Any(item => item.Type == PluginChangeType.Update && item.Target.MainAssembly == asmName))
                {
                    i--;
                    Settings.Default.SupportedLanguages.Remove(lang);
                    Settings.Default.Language = Settings.LanguageProperty.Initializer!.Create();
                }
            }

            // �ݒ��ۑ�
            Settings.Default.Save();
            AppModel.Current.SaveDisplayedMenus();
            KeyBindingModel.Save();

            BackupTimer.Stop();

            var app = AppModel.Current;

            app.RaiseExit();
            app.ServiceProvider.GetService<HttpClient>()?.Dispose();

            app.Project?.Unload();
            app.Project = null;
            AppModel.Current.User?.Save(Path.Combine(ServicesLocator.GetUserFolder(), "token"));

            if (PluginChangeSchedule.Uninstall.Count is not 0 || PluginChangeSchedule.UpdateOrInstall.Count is not 0)
            {
                var jsonfile = Path.Combine(ServicesLocator.GetUserFolder(), "package-install.json");
                PluginChangeSchedule.CreateJsonFile(jsonfile);

                Process.Start(new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "beditor"), $"package-install {jsonfile}")
                {
                    UseShellExecute = true
                });
            }

            if (app.AudioContext is IDisposable disposable)
                disposable.Dispose();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            AppModel.Current.Message.Snackbar(
                string.Format(Strings.ExceptionWasThrown, e.ExceptionObject.ToString()),
                string.Empty,
                icon: IMessage.IconType.Error);

            AppModel.Current.User?.Save(Path.Combine(ServicesLocator.GetUserFolder(), "token"));
            Logger?.LogError(e.ExceptionObject as Exception, "UnhandledException was thrown.");
        }

        private static void RunBackup()
        {
            BackupTimer.Tick += (s, e) =>
            {
                Task.Run(() =>
                {
                    var proj = AppModel.Current.Project;
                    if (proj is not null && Settings.Default.AutoBackUp)
                    {
                        var dir = Path.Combine(proj.DirectoryName, "backup");
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                        proj.Save(Path.Combine(dir, DateTime.Now.ToString("HH:mm:ss").Replace(':', '_')) + ".backup");

                        var files = Directory.GetFiles(dir).Select(i => new FileInfo(i)).ToArray();
                        Array.Sort(files, (x, y) => y.LastWriteTime.CompareTo(x.LastWriteTime));
                        if (files.Length is > 10)
                        {
                            foreach (var file in files.Skip(10))
                            {
                                if (file.Exists) file.Delete();
                            }
                        }
                    }
                });
            };

            Settings.Default.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is nameof(Settings.BackUpInterval))
                {
                    BackupTimer.Interval = TimeSpan.FromMinutes(Settings.Default.BackUpInterval);
                }
            };

            BackupTimer.Start();
        }

        private static void RegisterPrimitive()
        {
            foreach (var obj in PrimitiveTypes.EnumerateAllObjectMetadata())
            {
                ObjectMetadata.LoadedObjects.Add(obj);
            }

            foreach (var effect in PrimitiveTypes.EnumerateAllEffectMetadata())
            {
                EffectMetadata.LoadedEffects.Add(effect);
            }

            foreach (var ease in PrimitiveTypes.EnumerateAllEasingMetadata())
            {
                EasingMetadata.LoadedEasingFunc.Add(ease);
            }
            EasingMetadata.LoadedEasingFunc.Add(EasingMetadata.Create<PrimitiveEasing>(Strings.PrimitiveEasing));
        }

        private static async ValueTask InitialPluginsAsync()
        {
            PluginBuilder.Config = new PluginConfig(AppModel.Current);
            // ���ׂ�
            var all = PluginManager.Default.GetNames();
            var app = AppModel.Current;

            try
            {
                PluginManager.Default.Load(all);
            }
            catch (AggregateException e)
            {
                var msg = string.Format(Strings.FailedToLoad, Strings.Plugins);
                Logger.LogError(e, "Failed to load plugins.");
                var sb = new StringBuilder(msg);

                foreach (var item in e.InnerExceptions)
                {
                    if (item is PluginException ex)
                    {
                        sb.Append('\n');
                        sb.Append("- ");
                        sb.Append(ex.PluginName);
                    }
                }

                await app.Message.DialogAsync(sb.ToString());
            }

            if (PluginManager.Default.Tasks.Count is not 0)
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    var dialog = new ProgressDialog();
                    dialog.Maximum.Value = 100;
                    _ = dialog.ShowDialog(GetMainWindow());
                    foreach (var (plugin, tasks) in PluginManager.Default.Tasks)
                    {
                        for (var i = 0; i < tasks.Count; i++)
                        {
                            var task = tasks[i];
                            dialog.Text.Value = $"{string.Format(Strings.IsLoading, plugin.PluginName)}  :{task.Name}";

                            await task.RunTaskAsync(dialog);
                            dialog.Report(0);
                        }
                    }

                    dialog.Close();
                });
            }

            app.ServiceProvider = app.Services.BuildServiceProvider();
        }

        private static async Task SetupAudioContextAsync()
        {
            if (Settings.Default.AudioProfile is "XAudio2" && OperatingSystem.IsWindows())
            {
                AppModel.Current.AudioContext ??= new Audio.XAudio2.XAudioContext();
            }
            else
            {
                try
                {
                    AppModel.Current.AudioContext ??= new Audio.AudioContext();
                }
                catch
                {
                    // Windows�̏ꍇXAudio2���g��
                    if (OperatingSystem.IsWindows())
                    {
                        AppModel.Current.AudioContext ??= new Audio.XAudio2.XAudioContext();
                        Settings.Default.AudioProfile = "XAudio2";
                        AppModel.Current.Message.Snackbar(Strings.XAudio2IsUsedAsAudioProfile);
                        return;
                    }

                    await AppModel.Current.Message.DialogAsync(Strings.OpenALNotFound);
                    App.Shutdown(1);
                }
            }
        }

        private static async Task SetupAsync()
        {
            var flagPath = Path.Combine(ServicesLocator.GetUserFolder(), "SETUP_FLAG");
            if (!File.Exists(flagPath))
            {
                await Dispatcher.UIThread.InvokeAsync(async () => await new SetupWindow().ShowDialog(GetMainWindow()));
                using (new FileStream(flagPath, FileMode.Create))
                {
                }
            }
        }
    }
}