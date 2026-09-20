using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace Shiguang
{
    public sealed class SlideDeck
    {
        private readonly List<Photo> photos;
        private readonly bool shuffle;
        private readonly Random random;
        public int Index { get; private set; }
        public int Count { get { return photos.Count; } }
        public Photo Current { get { return photos[Index]; } }
        public SlideDeck(IEnumerable<Photo> source, bool randomOrder, Photo selected, Random rng)
        {
            photos = source.ToList(); if (photos.Count == 0) photos = PhotoCatalog.BuiltIn();
            shuffle = randomOrder; random = rng ?? new Random(); if (shuffle) Shuffle();
            Index = selected == null ? 0 : Math.Max(0, photos.FindIndex(p => p.Id == selected.Id));
        }
        private void Shuffle()
        {
            for (int i = photos.Count - 1; i > 0; i--) { int j = random.Next(i + 1); Photo temp = photos[i]; photos[i] = photos[j]; photos[j] = temp; }
        }
        public Photo Move(int direction)
        {
            if (direction > 0 && Index == photos.Count - 1)
            {
                string previous = Current.Id;
                if (shuffle)
                {
                    Shuffle();
                    if (photos.Count > 1 && photos[0].Id == previous) { Photo first = photos[0]; photos[0] = photos[1]; photos[1] = first; }
                }
                Index = 0;
            }
            else Index = (Index + (direction >= 0 ? 1 : -1) + photos.Count) % photos.Count;
            return Current;
        }
    }

    public sealed class SaverSession
    {
        private readonly AppStore store;
        private readonly Settings settings;
        private readonly bool interactive;
        private readonly Window owner;
        private readonly List<Window> windows = new List<Window>();
        private readonly List<PosterView> views = new List<PosterView>();
        private readonly HashSet<Window> backgroundPresses = new HashSet<Window>();
        private SlideDeck deck;
        private Photo displayedPhoto;
        private readonly DispatcherTimer timer;
        private readonly Stopwatch elapsed = new Stopwatch();
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private List<Photo> pendingPlaylist;
        private double lastTick, slideElapsed, lastMouseTime;
        private bool stopped, paused, changing;
        private bool ExitByBlankClick { get { return settings.ExitMode == Settings.ExitWithBlankClick; } }
        public event Action Finished;
        internal Photo DisplayedPhoto { get { return displayedPhoto; } }
        internal bool IsPaused { get { return paused; } }
        internal Window FirstWindow { get { return windows.FirstOrDefault(); } }
        public SaverSession(AppStore appStore, Settings appSettings, IEnumerable<Photo> photos, bool canInteract, Window parent, Photo selected)
        {
            store = appStore; settings = appSettings; interactive = canInteract; owner = parent;
            deck = new SlideDeck(photos, settings.Shuffle, selected, null);
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) }; timer.Tick += Tick;
        }
        public void Start()
        {
            try
            {
                Forms.Screen[] screens = settings.AllMonitors ? Forms.Screen.AllScreens : new[] { Forms.Screen.PrimaryScreen };
                foreach (Forms.Screen screen in screens)
                {
                    PosterView view = new PosterView(settings, interactive); views.Add(view);
                    Window window = new Window { Title = "拾光画报 · " + (interactive ? "沉浸预览" : "屏幕保护"), WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false, Topmost = true, Background = Brushes.Black, Content = view, Cursor = interactive || ExitByBlankClick ? Cursors.Arrow : Cursors.None, Left = screen.Bounds.Left, Top = screen.Bounds.Top, Width = screen.Bounds.Width, Height = screen.Bounds.Height };
                    window.PreviewKeyDown += OnKey;
                    window.MouseMove += delegate
                    {
                        if (interactive || ExitByBlankClick)
                        {
                            lastMouseTime = elapsed.Elapsed.TotalSeconds;
                            foreach (PosterView item in views) { item.Controls.Opacity = 1; item.Controls.IsHitTestVisible = true; }
                            foreach (Window item in windows) item.Cursor = Cursors.Arrow;
                        }
                    };
                    window.PreviewMouseDown += OnMouseDown;
                    window.PreviewMouseUp += OnMouseUp;
                    window.Deactivated += delegate { backgroundPresses.Remove(window); };
                    window.Closed += delegate { if (!stopped) Stop(); };
                    Forms.Screen target = screen;
                    window.SourceInitialized += delegate { Native.SetWindowPos(new WindowInteropHelper(window).Handle, new IntPtr(-1), target.Bounds.Left, target.Bounds.Top, target.Bounds.Width, target.Bounds.Height, 0x0010 | 0x0040); };
                    view.PreviousButton.Click += async delegate { await Change(-1, false); };
                    view.NextButton.Click += async delegate { await Change(1, false); };
                    view.PauseButton.Click += delegate { TogglePause(); };
                    view.FavoriteButton.Click += delegate { ToggleFavorite(); };
                    view.CloseButton.Click += delegate { Stop(); };
                    windows.Add(window);
                }
                if (owner != null) owner.Hide();
                foreach (Window window in windows) window.Show();
                if (windows.Count > 0) { windows[0].Activate(); windows[0].Focus(); }
                SystemEvents.DisplaySettingsChanged += DisplaysChanged;
                elapsed.Start(); timer.Start();
                ShowInitial();
                if (!interactive && settings.OnlineEnabled) RefreshDaily();
            }
            catch (Exception ex) { store.Log(ex); Stop(); if (interactive) MessageBox.Show("无法开始预览：" + ex.Message, "拾光画报"); }
        }
        private async void ShowInitial() { await Change(0, true); }
        private async void RefreshDaily()
        {
            try
            {
                PhotoCatalog catalog = new PhotoCatalog(store);
                if (catalog.ReadOnlineCache().Any(p => p.Date == DateTime.Now.ToString("yyyyMMdd"))) return;
                await catalog.RefreshOnline(lifetime.Token);
                if (stopped) return;
                List<Photo> next = await Task.Run(() => catalog.Load(settings));
                if (!stopped) pendingPlaylist = PhotoCatalog.Playlist(next, settings);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { store.Log(ex); }
        }
        private async Task Change(int direction, bool first)
        {
            if (changing || stopped) return;
            changing = true;
            try
            {
                if (pendingPlaylist != null)
                {
                    deck = new SlideDeck(pendingPlaylist, settings.Shuffle, displayedPhoto, null); pendingPlaylist = null;
                }
                if (!first) deck.Move(direction);
                Photo photo = deck.Current;
                BitmapSource image = null;
                int attempts = Math.Min(deck.Count, 30);
                for (int i = 0; i < attempts; i++)
                {
                    photo = deck.Current;
                    Photo candidate = photo;
                    image = await Task.Run(() => ImageLoader.Load(candidate, 2560));
                    if (stopped) return;
                    if (image != null) break;
                    deck.Move(direction == 0 ? 1 : direction);
                }
                if (image == null)
                {
                    deck = new SlideDeck(PhotoCatalog.BuiltIn(), settings.Shuffle, null, null);
                    photo = deck.Current; image = ImageLoader.Load(photo, 2560);
                }
                displayedPhoto = photo;
                foreach (PosterView view in views)
                {
                    view.Display(photo, image, deck.Index, deck.Count, !first);
                    view.SetFavorite(settings.Favorites.Contains(photo.Id, StringComparer.OrdinalIgnoreCase));
                }
                slideElapsed = 0;
            }
            catch (Exception ex) { store.Log(ex); }
            finally { changing = false; }
        }
        private async void Tick(object sender, EventArgs e)
        {
            if (stopped) return;
            double now = elapsed.Elapsed.TotalSeconds;
            double delta = now - lastTick; lastTick = now;
            if (!paused && !changing) slideElapsed += delta;
            foreach (PosterView view in views) { view.UpdateClock(DateTime.Now); view.SetProgress(slideElapsed / settings.IntervalSeconds); }
            if ((interactive || ExitByBlankClick) && now - lastMouseTime > 5)
            {
                foreach (PosterView view in views)
                    if (!view.Controls.IsMouseOver && !view.Controls.IsKeyboardFocusWithin) { view.Controls.Opacity = 0; view.Controls.IsHitTestVisible = false; }
                foreach (Window window in windows) window.Cursor = Cursors.None;
            }
            if (slideElapsed >= settings.IntervalSeconds && !paused && !changing) await Change(1, false);
        }
        private async void OnKey(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                if (!ExitByBlankClick) Stop();
                return;
            }
            if (!interactive) return;
            e.Handled = true;
            if (e.Key == Key.Right || e.Key == Key.Down) await Change(1, false);
            else if (e.Key == Key.Left || e.Key == Key.Up) await Change(-1, false);
            else if (e.Key == Key.Space) TogglePause();
            else if (e.Key == Key.F) ToggleFavorite();
            else e.Handled = false;
        }
        private bool IsBackgroundClick(Window window, MouseButtonEventArgs e)
        {
            PosterView view = window.Content as PosterView;
            return ExitByBlankClick && e.ChangedButton == MouseButton.Left && elapsed.Elapsed.TotalSeconds > 0.6
                && view != null && !view.IsControlInteraction(e.OriginalSource as DependencyObject);
        }
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Window window = (Window)sender;
            backgroundPresses.Remove(window);
            if (IsBackgroundClick(window, e)) backgroundPresses.Add(window);
        }
        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            Window window = (Window)sender;
            if (backgroundPresses.Remove(window) && IsBackgroundClick(window, e)) { e.Handled = true; Stop(); }
        }
        private void TogglePause() { paused = !paused; foreach (PosterView view in views) view.SetPaused(paused); }
        private void ToggleFavorite()
        {
            if (displayedPhoto == null) return;
            bool exists = settings.Favorites.Contains(displayedPhoto.Id, StringComparer.OrdinalIgnoreCase);
            if (exists) settings.Favorites.RemoveAll(id => String.Equals(id, displayedPhoto.Id, StringComparison.OrdinalIgnoreCase)); else settings.Favorites.Add(displayedPhoto.Id);
            try { store.Save(settings); foreach (PosterView view in views) view.SetFavorite(!exists); }
            catch (Exception ex) { store.Log(ex); if (exists) settings.Favorites.Add(displayedPhoto.Id); else settings.Favorites.Remove(displayedPhoto.Id); }
        }
        private void DisplaysChanged(object sender, EventArgs e)
        {
            if (!stopped) Application.Current.Dispatcher.BeginInvoke(new Action(Stop));
        }
        public void Stop()
        {
            if (stopped) return;
            stopped = true; timer.Stop(); elapsed.Stop(); lifetime.Cancel(); SystemEvents.DisplaySettingsChanged -= DisplaysChanged;
            backgroundPresses.Clear();
            foreach (Window window in windows.ToArray()) window.Close();
            if (Finished != null) Finished();
            if (owner != null) { owner.Show(); owner.Activate(); }
            else Application.Current.Shutdown();
        }
    }

    public sealed class NativePreview : IDisposable
    {
        private readonly HwndSource source;
        private readonly IntPtr parent;
        private readonly PosterView view;
        private readonly DispatcherTimer timer;
        private readonly List<Photo> photos;
        private readonly Settings settings;
        private DateTime lastSlide = DateTime.UtcNow;
        private int index;
        private bool disposed;
        public NativePreview(IntPtr parentWindow, Settings appSettings, List<Photo> playlist)
        {
            parent = parentWindow; settings = appSettings; photos = playlist;
            Native.RECT rect; Native.GetClientRect(parent, out rect);
            HwndSourceParameters parameters = new HwndSourceParameters("ShiguangPreview") { ParentWindow = parent, WindowStyle = 0x40000000 | 0x10000000, Width = Math.Max(1, rect.Right - rect.Left), Height = Math.Max(1, rect.Bottom - rect.Top), PositionX = 0, PositionY = 0 };
            source = new HwndSource(parameters); view = new PosterView(settings, false); source.RootVisual = view;
            ShowPhoto(); timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) }; timer.Tick += Tick; timer.Start();
        }
        private void ShowPhoto()
        {
            Photo photo = photos[index]; BitmapSource image = ImageLoader.Load(photo, 560);
            if (image == null) { photo = PhotoCatalog.BuiltIn()[0]; image = ImageLoader.Load(photo, 560); }
            view.Display(photo, image, index, photos.Count, false);
        }
        private void Tick(object sender, EventArgs e)
        {
            if (disposed) return;
            if (!Native.IsWindow(parent)) { Dispose(); Application.Current.Shutdown(); return; }
            Native.RECT rect;
            if (Native.GetClientRect(parent, out rect)) Native.SetWindowPos(source.Handle, IntPtr.Zero, 0, 0, Math.Max(1, rect.Right - rect.Left), Math.Max(1, rect.Bottom - rect.Top), 0x0004 | 0x0010);
            view.UpdateClock(DateTime.Now);
            if ((DateTime.UtcNow - lastSlide).TotalSeconds >= settings.IntervalSeconds) { index = (index + 1) % photos.Count; ShowPhoto(); lastSlide = DateTime.UtcNow; }
        }
        public void Dispose() { if (disposed) return; disposed = true; timer.Stop(); source.Dispose(); }
    }
}
