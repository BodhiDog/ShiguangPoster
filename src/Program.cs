using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;

[assembly: AssemblyTitle("拾光画报")]
[assembly: AssemblyDescription("拾光画报 · 风景、时钟与片刻留白")]
[assembly: AssemblyProduct("Shiguang Poster")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace Shiguang
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            AppStore store = null;
            try
            {
                LaunchOptions options = LaunchOptions.Parse(args);
                if (options.Mode == "preview" && (options.Parent == IntPtr.Zero || !Native.IsWindow(options.Parent))) return 0;
                string root = options.DataDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShiguangPoster");
                store = new AppStore(root);
                if (options.Mode == "enable-login-startup")
                {
                    new LoginStartupRegistration(store).Enable();
                    return 0;
                }
                if (options.Mode == "disable-login-startup") return new LoginStartupRegistration(store).Disable() ? 0 : 2;
                if (options.Mode == "uninstall")
                {
                    new LoginStartupRegistration(store).Disable();
                    return new ScreensaverRegistration(store).Disable() ? 0 : 2;
                }
                Application app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                app.Resources.MergedDictionaries.Add(Ui.Resource<ResourceDictionary>("Theme.xaml"));
                AppStore log = store;
                app.DispatcherUnhandledException += delegate(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
                {
                    log.Log(e.Exception);
                    e.Handled = true;
                    // Never leave a failed saver covering the desktop.
                    app.Shutdown(1);
                };
                if (options.Mode == "verify") return Verification.Run(store, options.OutputDirectory ?? root);
                if (options.Mode == "online-check") return Verification.CheckOnline(store);
                Settings settings = store.LoadSettings();
                if (options.Mode == "smoke") return Verification.Smoke(app, store, settings, options.OutputDirectory ?? root);
                if (options.Mode == "preview")
                {
                    using (NativePreview preview = new NativePreview(options.Parent, settings, PhotoCatalog.Playlist(new PhotoCatalog(store).Load(settings), settings))) return app.Run();
                }
                if (options.Mode == "saver" || options.Mode == "interactive")
                {
                    bool created;
                    using (Mutex mutex = new Mutex(true, "Local\\ShiguangPoster.Saver", out created))
                    {
                        if (!created) return 0;
                        try
                        {
                            List<Photo> photos = new PhotoCatalog(store).Load(settings);
                            SaverSession session = new SaverSession(store, settings, PhotoCatalog.Playlist(photos, settings), options.Mode == "interactive", null, null);
                            session.Start(); return app.Run();
                        }
                        finally { mutex.ReleaseMutex(); }
                    }
                }
                MainController controller = new MainController(store, settings, PhotoCatalog.BuiltIn(), false);
                app.MainWindow = controller.Window;
                controller.Window.Closed += delegate { app.Shutdown(); };
                return app.Run(controller.Window);
            }
            catch (Exception ex)
            {
                if (store != null) store.Log(ex);
                bool quiet = args.Length > 0;
                if (!quiet) MessageBox.Show("拾光画报未能启动：" + ex.Message + "\n\n详情记录在本地 app.log 中。", "拾光画报", MessageBoxButton.OK, MessageBoxImage.Information);
                return 1;
            }
        }
    }
}
