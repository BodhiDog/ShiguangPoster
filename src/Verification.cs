using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Shiguang
{
    public static class Verification
    {
        private static readonly List<string> Results = new List<string>();
        private static void Check(string name, bool value) { if (!value) throw new InvalidOperationException("FAIL: " + name); Results.Add("PASS: " + name); }
        public static int Run(AppStore store, string output)
        {
            Directory.CreateDirectory(output);
            try
            {
                Check("no arguments open configuration", LaunchOptions.Parse(new string[0]).Mode == "config");
                Check("Windows /S and -s launch saver", LaunchOptions.Parse(new[] { "/S" }).Mode == "saver" && LaunchOptions.Parse(new[] { "-s" }).Mode == "saver");
                Check("preview handles support spaced and colon forms", LaunchOptions.Parse(new[] { "/p", "12345" }).Parent.ToInt64() == 12345 && LaunchOptions.Parse(new[] { "/P:54321" }).Parent.ToInt64() == 54321);
                Check("invalid preview handles are rejected", LaunchOptions.Parse(new[] { "/p", "bad" }).Parent == IntPtr.Zero && LaunchOptions.Parse(new[] { "/p:-1" }).Parent == IntPtr.Zero);
                Check("configuration handles are accepted", LaunchOptions.Parse(new[] { "/C:123" }).Mode == "config");
                Settings bad = new Settings { IntervalSeconds = -3, Brightness = 500, ClockPosition = "invalid", LocalFolders = null, Favorites = null }; bad.Normalize();
                Check("settings are bounded and normalized", bad.IntervalSeconds == 10 && bad.Brightness == 100 && bad.ClockPosition == "left" && bad.LocalFolders.Count == 0);
                AppStore testStore = new AppStore(Path.Combine(output, "test-data"));
                Settings saved = new Settings { IntervalSeconds = 120, ShowClock = false, Shuffle = true }; saved.Favorites.Add("builtin:mountain-dawn"); testStore.Save(saved);
                Settings reloaded = testStore.LoadSettings();
                Check("settings persist and round-trip", reloaded.IntervalSeconds == 120 && !reloaded.ShowClock && reloaded.Shuffle && reloaded.Favorites.Count == 1);
                saved.IntervalSeconds = 45; testStore.Save(saved);
                Check("atomic replacement updates existing settings", testStore.LoadSettings().IntervalSeconds == 45);
                File.WriteAllText(testStore.FilePath("settings.json"), "{broken");
                Check("corrupt settings fall back without crashing", testStore.LoadSettings().IntervalSeconds == 45 && testStore.Warning != null);
                Check("corrupt settings are backed up", Directory.GetFiles(testStore.Root, "settings.invalid-*.json").Length > 0);
                List<Photo> builtin = PhotoCatalog.BuiltIn();
                foreach (Photo photo in builtin)
                {
                    var image = ImageLoader.Load(photo, 560);
                    Check("bundled image decodes: " + photo.Id, image != null && image.PixelWidth == 560 && image.IsFrozen);
                }
                string local = Path.Combine(output, "fixture-photos"); Directory.CreateDirectory(Path.Combine(local, "nested"));
                using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream("Shiguang.Assets.quiet-coast.png"))
                using (Stream file = File.Create(Path.Combine(local, "sample.PNG"))) input.CopyTo(file);
                File.Copy(Path.Combine(local, "sample.PNG"), Path.Combine(local, "nested", "second.png"), true);
                File.WriteAllText(Path.Combine(local, "not-an-image.txt"), "ignored");
                File.WriteAllText(Path.Combine(local, "broken.jpg"), "not a jpeg");
                Check("local scanning recognizes formats and recursion", PhotoCatalog.EnumeratePhotos(local, false).Count() == 2 && PhotoCatalog.EnumeratePhotos(local, true).Count() == 3);
                Check("missing folder is skipped", !PhotoCatalog.EnumeratePhotos(Path.Combine(local, "missing"), true).Any());
                Settings localSettings = new Settings(); localSettings.LocalFolders.Add(local); localSettings.LocalFolders.Add(Path.Combine(local, "nested"));
                List<Photo> all = new PhotoCatalog(testStore).Load(localSettings);
                Check("overlapping album folders are deduplicated", all.Count(p => p.Source == "local") == 3);
                Check("damaged image is skipped safely", ImageLoader.Load(new Photo { Path = Path.Combine(local, "broken.jpg") }, 560) == null);
                CheckExifOrientation(output);
                Settings selection = new Settings { IncludeBuiltIn = false, IncludeLocal = true };
                Check("playlist respects photo sources", PhotoCatalog.Playlist(all, selection).Count == 3 && PhotoCatalog.Playlist(all, selection).All(p => p.Source == "local"));
                selection.FavoritesOnly = true;
                Check("empty favorite selection falls back to bundled photos", PhotoCatalog.Playlist(all, selection).All(p => p.Source == "builtin"));
                selection.IncludeBuiltIn = true; selection.Favorites.Add(builtin[1].Id);
                Check("favorites constrain the playlist", PhotoCatalog.Playlist(all, selection).Count == 1 && PhotoCatalog.Playlist(all, selection)[0].Id == builtin[1].Id);
                SlideDeck deck = new SlideDeck(builtin, false, null, new Random(7));
                Check("previous wraps and next wraps", deck.Move(-1).Id == builtin[2].Id && deck.Move(1).Id == builtin[0].Id);
                for (int seed = 0; seed < 30; seed++)
                {
                    SlideDeck shuffled = new SlideDeck(builtin, true, null, new Random(seed));
                    HashSet<string> cycle = new HashSet<string>(); cycle.Add(shuffled.Current.Id);
                    for (int i = 1; i < shuffled.Count; i++) cycle.Add(shuffled.Move(1).Id);
                    string last = shuffled.Current.Id;
                    if (cycle.Count != builtin.Count || shuffled.Move(1).Id == last) throw new InvalidOperationException("Shuffle repeated a photo.");
                }
                Check("shuffle covers every photo and avoids cycle-boundary repeats", true);
                Check("Chinese lunar new year is correct", PosterView.LunarDate(new DateTime(2026, 2, 17)) == "正月初一");
                Check("lunar calendar out-of-range dates have a fallback", PosterView.LunarDate(new DateTime(2200, 1, 1)) == "2200 年");
                Settings clean = new Settings();
                MainController controller = new MainController(store, clean, builtin, true);
                Ui.Snapshot((FrameworkElement)controller.Window.Content, Path.Combine(output, "home.png"), 1180, 840);
                Check("home XAML and image gallery render", Ui.Find<WrapPanel>(controller.Window, "PhotoGrid").Children.Count == 3);
                Ui.Find<Button>(controller.Window, "HeroFavorite").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                controller.Navigate("favorites");
                Check("favorite interaction updates the collection", Ui.Find<WrapPanel>(controller.Window, "PhotoGrid").Children.Count == 1);
                Ui.Snapshot((FrameworkElement)controller.Window.Content, Path.Combine(output, "favorites.png"), 1180, 840);
                controller.Navigate("albums"); Ui.Snapshot((FrameworkElement)controller.Window.Content, Path.Combine(output, "albums.png"), 1180, 840);
                Check("album empty state is visible", Ui.Find<Border>(controller.Window, "EmptyState").Visibility == Visibility.Visible);
                controller.Navigate("settings"); Ui.Snapshot((FrameworkElement)controller.Window.Content, Path.Combine(output, "settings.png"), 1180, 840);
                Check("settings page renders all groups", Ui.Find<StackPanel>(controller.Window, "SettingsPage").Children.Count == 5);
                controller.Navigate("explore"); Ui.Snapshot((FrameworkElement)controller.Window.Content, Path.Combine(output, "home-compact.png"), 980, 680);
                controller.Window.Close();
                PosterView poster = new PosterView(clean, true); poster.Display(builtin[0], ImageLoader.Load(builtin[0], 1920), 0, 3, false); poster.UpdateClock(new DateTime(2026, 9, 17, 16, 28, 0));
                Ui.Snapshot(poster, Path.Combine(output, "screensaver.png"), 1600, 900);
                Check("full-screen poster renders", File.Exists(Path.Combine(output, "screensaver.png")));
                Results.Add("All " + Results.Count + " checks passed. No system screen saver settings were changed.");
                File.WriteAllLines(Path.Combine(output, "verification.txt"), Results, new UTF8Encoding(false)); return 0;
            }
            catch (Exception ex) { store.Log(ex); Results.Add(ex.ToString()); File.WriteAllLines(Path.Combine(output, "verification.txt"), Results, new UTF8Encoding(false)); return 1; }
        }
        public static int CheckOnline(AppStore store)
        {
            try
            {
                int count = new PhotoCatalog(store).RefreshOnline(CancellationToken.None).GetAwaiter().GetResult();
                File.WriteAllText(store.FilePath("online-check.txt"), "PASS: downloaded and decoded " + count + " Bing images.", Encoding.UTF8); return 0;
            }
            catch (Exception ex) { store.Log(ex); File.WriteAllText(store.FilePath("online-check.txt"), ex.ToString(), Encoding.UTF8); return 1; }
        }
        private static void CheckExifOrientation(string output)
        {
            byte[] pixels = new byte[40 * 20 * 3];
            for (int y = 0; y < 20; y++) for (int x = 0; x < 40; x++)
            {
                int offset = (y * 40 + x) * 3;
                bool right = x >= 20, bottom = y >= 10;
                pixels[offset] = (byte)(bottom && !right ? 255 : 0);
                pixels[offset + 1] = (byte)(right ? 255 : 0);
                pixels[offset + 2] = (byte)(!right && !bottom || right && bottom ? 255 : 0);
            }
            BitmapSource bitmap = BitmapSource.Create(40, 20, 96, 96, PixelFormats.Bgr24, null, pixels, 120);
            foreach (int orientation in new[] { 5, 6, 7, 8 })
            {
                BitmapMetadata metadata = new BitmapMetadata("jpg"); metadata.SetQuery("/app1/ifd/{ushort=274}", (ushort)orientation);
                JpegBitmapEncoder encoder = new JpegBitmapEncoder { QualityLevel = 100 }; encoder.Frames.Add(BitmapFrame.Create(bitmap, null, metadata, null));
                string path = Path.Combine(output, "orientation-" + orientation + ".jpg"); using (Stream file = File.Create(path)) encoder.Save(file);
                BitmapSource loaded = ImageLoader.Load(new Photo { Path = path }, 560);
                if (loaded == null || loaded.PixelWidth != 20 || loaded.PixelHeight != 40) throw new InvalidOperationException("Orientation dimensions failed.");
                FormatConvertedBitmap converted = new FormatConvertedBitmap(loaded, PixelFormats.Bgra32, null, 0);
                byte[] topLeft = new byte[4]; converted.CopyPixels(new Int32Rect(3, 3, 1, 1), topLeft, 4, 0);
                bool expected = orientation == 5 ? topLeft[2] > 220 && topLeft[1] < 30 : orientation == 6 ? topLeft[0] > 220 && topLeft[2] < 30 : orientation == 7 ? topLeft[2] > 220 && topLeft[1] > 220 : topLeft[1] > 220 && topLeft[2] < 30;
                Check("EXIF rotation and reflection: orientation " + orientation, expected);
            }
        }
        private static void Press(Window window, Key key)
        {
            window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        }
        public static int Smoke(Application app, AppStore store, Settings settings, string output)
        {
            Directory.CreateDirectory(output);
            MainController controller = new MainController(store, settings, PhotoCatalog.BuiltIn(), true);
            app.MainWindow = controller.Window;
            DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            int stage = 0; NativePreview preview = null; HwndSource host = null; SaverSession session = null;
            controller.Window.ContentRendered += delegate { timer.Start(); };
            timer.Tick += delegate
            {
                try
                {
                    stage++;
                    if (stage == 1) { Ui.Snapshot((FrameworkElement)controller.Window.Content, Path.Combine(output, "live-home.png"), 1180, 840); controller.Navigate("settings"); }
                    else if (stage == 2)
                    {
                        Ui.Snapshot((FrameworkElement)controller.Window.Content, Path.Combine(output, "live-settings.png"), 1180, 840);
                        host = new HwndSource(new HwndSourceParameters("Shiguang preview test") { Width = 320, Height = 180, PositionX = 30, PositionY = 30, WindowStyle = 0x10000000 });
                        preview = new NativePreview(host.Handle, settings, PhotoCatalog.BuiltIn());
                    }
                    else if (stage == 3)
                    {
                        preview.Dispose(); preview = null; host.Dispose(); host = null;
                        settings.AllMonitors = true; settings.ExitOnMouseMove = false;
                        session = new SaverSession(store, settings, PhotoCatalog.BuiltIn(), true, controller.Window, null); session.Start();
                    }
                    else if (stage == 4)
                    {
                        if (session.DisplayedPhoto == null || session.DisplayedPhoto.Id != "builtin:mountain-dawn") throw new InvalidOperationException("Fullscreen did not render the first image.");
                        int monitorWindows = app.Windows.Cast<Window>().Count(w => w.Title == "拾光画报 · 沉浸预览");
                        if (monitorWindows != System.Windows.Forms.Screen.AllScreens.Length) throw new InvalidOperationException("Not every monitor was covered.");
                        Ui.Snapshot((FrameworkElement)session.FirstWindow.Content, Path.Combine(output, "live-screensaver.png"), 1600, 900);
                        Press(session.FirstWindow, Key.Right);
                    }
                    else if (stage == 5)
                    {
                        if (session.DisplayedPhoto.Id != "builtin:quiet-coast") throw new InvalidOperationException("Next-photo key did not change the image.");
                        Press(session.FirstWindow, Key.Space);
                        if (!session.IsPaused) throw new InvalidOperationException("Pause key did not pause playback.");
                        Press(session.FirstWindow, Key.Escape);
                    }
                    else if (stage == 6)
                    {
                        if (!controller.Window.IsVisible) throw new InvalidOperationException("ESC did not restore the main window.");
                        session = new SaverSession(store, settings, PhotoCatalog.BuiltIn(), false, controller.Window, null); session.Start();
                    }
                    else if (stage == 7)
                    {
                        if (session.DisplayedPhoto == null) throw new InvalidOperationException("System saver did not render.");
                        Press(session.FirstWindow, Key.A);
                    }
                    else if (stage == 8)
                    {
                        if (!controller.Window.IsVisible) throw new InvalidOperationException("System saver did not exit on keyboard input.");
                        File.WriteAllText(Path.Combine(output, "smoke-test.txt"), "PASS: native window, settings page, embedded preview, all monitor windows, fullscreen image rendering, next-photo key, pause key, ESC return, and system-saver keyboard exit. No screen saver registration was changed.", Encoding.UTF8);
                        timer.Stop(); controller.Window.Close(); app.Shutdown();
                    }
                }
                catch (Exception ex) { store.Log(ex); File.WriteAllText(Path.Combine(output, "smoke-test.txt"), ex.ToString()); timer.Stop(); if (preview != null) preview.Dispose(); if (host != null) host.Dispose(); if (session != null) session.Stop(); app.Shutdown(1); }
            };
            return app.Run(controller.Window);
        }
    }
}
