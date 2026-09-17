using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace Shiguang
{
    public sealed partial class MainController
    {
        public readonly Window Window;
        private readonly AppStore store;
        private readonly Settings settings;
        private readonly PhotoCatalog catalog;
        private readonly ScreensaverRegistration registration;
        private readonly bool diagnostic;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly DispatcherTimer clock;
        private List<Photo> photos;
        private Photo hero;
        private string page = "explore", category = "全部";
        private int visibleCount = 24, generation, loadGeneration;
        private bool refreshing, closed;
        public MainController(AppStore appStore, Settings appSettings, List<Photo> initialPhotos, bool diagnostics)
        {
            store = appStore; settings = appSettings; catalog = new PhotoCatalog(store); registration = new ScreensaverRegistration(store); diagnostic = diagnostics;
            photos = initialPhotos; hero = photos.First();
            Window = Ui.Resource<Window>("MainWindow.xaml"); Ui.Chrome(Window);
            B("CloseButton").Click += delegate { Window.Close(); };
            B("MinimizeButton").Click += delegate { Window.WindowState = WindowState.Minimized; };
            B("MaximizeButton").Click += delegate { ToggleMaximize(); };
            Ui.Find<Grid>(Window, "TitleBar").MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e) { if (e.ClickCount == 2) ToggleMaximize(); else if (e.LeftButton == MouseButtonState.Pressed) Window.DragMove(); };
            B("NavExplore").Click += delegate { Navigate("explore"); };
            B("NavAlbums").Click += delegate { Navigate("albums"); };
            B("NavFavorites").Click += delegate { Navigate("favorites"); };
            B("NavSettings").Click += delegate { Navigate("settings"); };
            B("PreviewButton").Click += delegate { Preview(null); };
            B("HeroOpen").Click += delegate { Preview(hero); };
            B("HeroFavorite").Click += delegate { ToggleFavorite(hero); };
            B("AddFolderButton").Click += delegate { AddFolder(); };
            B("RefreshButton").Click += async delegate { if (page == "explore" && settings.OnlineEnabled) await RefreshOnline(); else { await ReloadLibrary(); Status("画报已刷新" + (page == "explore" ? " · 可在屏保设置中开启每日壁纸" : "")); } };
            B("LoadMoreButton").Click += delegate { visibleCount += 24; RenderGallery(); };
            string[] filters = { "FilterAll", "FilterMountain", "FilterCoast", "FilterDesert", "FilterDaily" };
            foreach (string name in filters)
            {
                RadioButton radio = Ui.Find<RadioButton>(Window, name);
                radio.Checked += delegate { category = Convert.ToString(radio.Content) == "每日壁纸" ? "每日" : Convert.ToString(radio.Content); visibleCount = 24; RenderGallery(); };
            }
            Ui.Find<WrapPanel>(Window, "PhotoGrid").SizeChanged += delegate { SizeCards(); };
            clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            clock.Tick += delegate { UpdateClock(); }; if (!diagnostic) clock.Start();
            Window.Closed += delegate { closed = true; clock.Stop(); lifetime.Cancel(); };
            Window.ContentRendered += async delegate
            {
                if (diagnostic) return;
                await ReloadLibrary();
                if (settings.OnlineEnabled && !photos.Any(p => p.Source == "online" && p.Date == DateTime.Now.ToString("yyyyMMdd"))) await RefreshOnline();
            };
            Window.PreviewKeyDown += delegate(object sender, KeyEventArgs e) { if (e.Key == Key.F5) { e.Handled = true; Preview(null); } };
            SetHero(hero); Navigate("explore"); UpdateClock();
            if (!String.IsNullOrEmpty(store.Warning)) Status(store.Warning);
        }
        private Button B(string name) { return Ui.Find<Button>(Window, name); }
        private TextBlock T(string name) { return Ui.Find<TextBlock>(Window, name); }
        private void Show(string name, bool visible) { Ui.Find<UIElement>(Window, name).Visibility = visible ? Visibility.Visible : Visibility.Collapsed; }
        private void ToggleMaximize() { Window.WindowState = Window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; }
        public void Status(string text) { T("StatusText").Text = text; }
        public void Navigate(string destination)
        {
            page = destination; visibleCount = 24;
            string[] names = { "NavExplore", "NavAlbums", "NavFavorites", "NavSettings" };
            string[] pages = { "explore", "albums", "favorites", "settings" };
            for (int i = 0; i < names.Length; i++) B(names[i]).Tag = page == pages[i] ? "active" : "";
            bool explore = page == "explore";
            Show("GalleryPage", page != "settings"); Show("SettingsPage", page == "settings");
            Show("HeroSection", explore); Show("CategoryFilters", explore); Show("AlbumTools", page == "albums");
            T("PageEyebrow").Text = explore ? "EXPLORE THE QUIET" : page == "albums" ? "YOUR OWN LANDSCAPES" : page == "favorites" ? "KEEP WHAT YOU LOVE" : "MAKE IT YOURS";
            T("PageTitle").Text = explore ? "让屏幕，成为一扇窗。" : page == "albums" ? "让回忆，慢慢放映。" : page == "favorites" ? "总有风景，值得停留。" : "为你的留白，设定节奏。";
            T("PageSubtitle").Text = explore ? "慢下来，看见生活之外的风景。" : page == "albums" ? "旅途、日常、家人，把喜欢的照片留在眼前。" : page == "favorites" ? "那些让你按下爱心的瞬间，都在这里。" : "随心调整，设置会自动保存。";
            T("GalleryTitle").Text = explore ? "发现风景" : page == "albums" ? "相册里的瞬间" : "心之所向";
            T("GalleryFootnote").Text = explore ? "原创风景 · 离线可用    /    选一处风景，让思绪休息一下" : page == "albums" ? "移除文件夹只会将它移出画报，原始照片仍保留在原处。" : "收藏是一份播放清单；可在屏保设置中选择只播放收藏。";
            if (page == "settings") BuildSettings(); else { RenderFolders(); RenderGallery(); }
            Ui.Find<ScrollViewer>(Window, "ContentScroll").ScrollToTop();
        }
        private void UpdateClock()
        {
            DateTime now = DateTime.Now;
            T("HeroClock").Text = now.ToString(settings.Hour12 ? "hh:mm tt" : "HH:mm", CultureInfo.InvariantCulture);
            T("HeroDate").Text = now.ToString("dddd · M 月 d 日", CultureInfo.GetCultureInfo("zh-CN"));
        }
        private void SetHero(Photo photo)
        {
            hero = photo;
            BitmapSource image = ImageLoader.Load(photo, 1600);
            if (image != null) Ui.Find<Border>(Window, "HeroSection").Background = new ImageBrush(image) { Stretch = Stretch.UniformToFill };
            T("HeroTitle").Text = photo.Title;
            T("HeroDescription").Text = photo.Description;
            T("HeroCategory").Text = photo.Category + "  /  " + (photo.Source == "builtin" ? "拾光原创" : photo.Source == "online" ? "必应每日壁纸" : "我的相册");
            B("HeroFavorite").Content = IsFavorite(photo) ? "♥" : "♡";
        }
        private bool IsFavorite(Photo photo) { return settings.Favorites.Contains(photo.Id, StringComparer.OrdinalIgnoreCase); }
        private void ToggleFavorite(Photo photo)
        {
            bool wasFavorite = IsFavorite(photo);
            if (wasFavorite) settings.Favorites.RemoveAll(id => String.Equals(id, photo.Id, StringComparison.OrdinalIgnoreCase)); else settings.Favorites.Add(photo.Id);
            if (!Save()) { if (wasFavorite) settings.Favorites.Add(photo.Id); else settings.Favorites.Remove(photo.Id); return; }
            B("HeroFavorite").Content = IsFavorite(hero) ? "♥" : "♡";
            RenderGallery(); Status(wasFavorite ? "已取消收藏" : "已加入我的收藏");
        }
        private bool Save()
        {
            try { store.Save(settings); Status("设置已保存"); return true; }
            catch (Exception ex) { store.Log(ex); Status("无法保存设置：" + ex.Message); return false; }
        }
        private List<Photo> Filtered()
        {
            IEnumerable<Photo> selection = page == "albums" ? photos.Where(p => p.Source == "local") : page == "favorites" ? photos.Where(IsFavorite) : photos.Where(p => p.Source != "local");
            if (page == "explore" && category != "全部") selection = selection.Where(p => p.Category == category);
            return selection.ToList();
        }
        private void RenderGallery()
        {
            if (closed || page == "settings") return;
            int currentGeneration = ++generation;
            WrapPanel grid = Ui.Find<WrapPanel>(Window, "PhotoGrid"); grid.Children.Clear();
            List<Photo> visible = Filtered();
            T("GalleryCount").Text = visible.Count.ToString("D2");
            T("LibraryCount").Text = photos.Count + " 张风景，随时出发";
            Show("EmptyState", visible.Count == 0); Show("LoadMoreButton", visible.Count > visibleCount);
            if (visible.Count == 0)
            {
                T("EmptyTitle").Text = page == "albums" ? "把第一段回忆，放进来" : page == "favorites" ? "留一个位置，给喜欢的风景" : "这片风景，还在路上";
                T("EmptyDescription").Text = page == "albums" ? "添加照片文件夹；支持读取子文件夹里的照片。" : page == "favorites" ? "点击画报上的爱心，就能在这里找到它。" : category == "每日" ? "在屏保设置中开启必应每日壁纸，然后刷新画报。" : "换一个分类，发现新的风景。";
            }
            foreach (Photo photo in visible.Take(visibleCount)) grid.Children.Add(CreateCard(photo, currentGeneration));
            SizeCards();
        }
        private FrameworkElement CreateCard(Photo photo, int renderGeneration)
        {
            Border card = new Border { Background = Ui.Brush("#202C24"), BorderBrush = Ui.Brush("#334338"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(11), Margin = new Thickness(0, 0, 14, 16), Padding = new Thickness(7), Width = 270 };
            StackPanel body = new StackPanel(); card.Child = body;
            Grid visual = new Grid { Height = 143, ClipToBounds = true };
            Border photoSurface = new Border { CornerRadius = new CornerRadius(7), Background = Ui.Brush("#304B3A"), Cursor = Cursors.Hand, ToolTip = "点击预览 · " + photo.Title };
            photoSurface.MouseLeftButtonUp += delegate { SetHero(photo); Preview(photo); };
            visual.Children.Add(photoSurface);
            TextBlock caption = Ui.Text(photo.Category, 10, "#F0F3E6");
            Border pill = new Border { Child = caption, Background = Ui.Brush("#8018281E"), CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 4, 8, 4), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(9), IsHitTestVisible = false };
            visual.Children.Add(pill);
            Button favorite = Ui.Button(IsFavorite(photo) ? "♥" : "♡", false); favorite.Width = 29; favorite.Height = 29; favorite.Padding = new Thickness(0); favorite.FontSize = 18; favorite.Background = Ui.Brush("#8514241B"); favorite.BorderThickness = new Thickness(0); favorite.HorizontalAlignment = HorizontalAlignment.Right; favorite.VerticalAlignment = VerticalAlignment.Top; favorite.Margin = new Thickness(8); favorite.ToolTip = IsFavorite(photo) ? "取消收藏" : "收藏画报";
            favorite.Click += delegate { ToggleFavorite(photo); }; visual.Children.Add(favorite);
            body.Children.Add(visual);
            Button title = Ui.Button(photo.Title, false); title.SetResourceReference(FrameworkElement.StyleProperty, "GhostButton"); title.FontSize = 14; title.Padding = new Thickness(5, 12, 5, 3); title.HorizontalContentAlignment = HorizontalAlignment.Left; title.ToolTip = photo.Title; title.Click += delegate { Preview(photo); };
            title.Content = new TextBlock { Text = photo.Title, TextTrimming = TextTrimming.CharacterEllipsis }; body.Children.Add(title);
            TextBlock credit = Ui.Text(photo.Source == "builtin" ? "原创画报   /   AI 风景" : photo.Source == "online" ? "每日壁纸   /   必应" : "私人珍藏   /   本地照片", 10, "#8AA28E"); credit.Margin = new Thickness(5, 3, 5, 8); body.Children.Add(credit);
            Action<BitmapSource> apply = delegate(BitmapSource bitmap)
            {
                if (closed || generation != renderGeneration) return;
                if (bitmap != null) photoSurface.Background = new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
                else { photoSurface.Child = new TextBlock { Text = "图片暂时无法读取", Foreground = Ui.Brush("#B2C3B1"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }; }
            };
            if (diagnostic) apply(ImageLoader.Load(photo, 560));
            else Task.Run(() => ImageLoader.Load(photo, 560)).ContinueWith(task =>
            {
                if (!closed && !Window.Dispatcher.HasShutdownStarted)
                    Window.Dispatcher.BeginInvoke(new Action(delegate { if (task.IsFaulted) { store.Log(task.Exception); apply(null); } else if (!task.IsCanceled) apply(task.Result); }));
            });
            return card;
        }
        private void SizeCards()
        {
            WrapPanel grid = Ui.Find<WrapPanel>(Window, "PhotoGrid");
            double available = grid.ActualWidth > 0 ? grid.ActualWidth : 900;
            int columns = Math.Max(2, (int)Math.Floor((available + 14) / 265));
            double width = Math.Max(200, (available - (columns - 1) * 14) / columns);
            for (int i = 0; i < grid.Children.Count; i++)
            {
                Border card = (Border)grid.Children[i]; card.Width = width; card.Margin = new Thickness(0, 0, (i + 1) % columns == 0 ? 0 : 14, 16);
            }
        }
        private void RenderFolders()
        {
            StackPanel list = Ui.Find<StackPanel>(Window, "FolderList"); list.Children.Clear();
            foreach (string folder in settings.LocalFolders.ToArray())
            {
                Grid row = new Grid { Margin = new Thickness(0, 6, 0, 0) };
                row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                TextBlock path = Ui.Text(folder, 11, Directory.Exists(folder) ? "#A8BDA8" : "#E0BA91"); path.VerticalAlignment = VerticalAlignment.Center; path.TextTrimming = TextTrimming.CharacterEllipsis; path.TextWrapping = TextWrapping.NoWrap; path.ToolTip = Directory.Exists(folder) ? folder : "文件夹不可用：" + folder; row.Children.Add(path);
                Button remove = Ui.Button("移除", false); remove.SetResourceReference(FrameworkElement.StyleProperty, "GhostButton"); remove.Padding = new Thickness(10, 5, 10, 5); remove.FontSize = 11; Grid.SetColumn(remove, 1); row.Children.Add(remove);
                string captured = folder; remove.Click += async delegate { settings.LocalFolders.Remove(captured); if (Save()) { RenderFolders(); await ReloadLibrary(); } };
                list.Children.Add(row);
            }
        }
        private async void AddFolder()
        {
            using (Forms.FolderBrowserDialog dialog = new Forms.FolderBrowserDialog { Description = "选择要加入拾光画报的照片文件夹", ShowNewFolderButton = false })
            {
                if (dialog.ShowDialog(new WindowHandle(Window)) != Forms.DialogResult.OK) return;
                if (settings.LocalFolders.Contains(dialog.SelectedPath, StringComparer.OrdinalIgnoreCase)) { Status("这个文件夹已在相册中"); return; }
                if (settings.LocalFolders.Count >= 50) { Status("最多可添加 50 个文件夹"); return; }
                settings.LocalFolders.Add(dialog.SelectedPath);
                if (Save()) { RenderFolders(); await ReloadLibrary(); Status("相册已更新 · " + photos.Count(p => p.Source == "local") + " 张本地照片"); }
            }
        }
        private async Task ReloadLibrary()
        {
            int request = ++loadGeneration;
            Settings snapshot = new JavaScriptClone(settings).Value;
            try
            {
                List<Photo> next = await Task.Run(() => catalog.Load(snapshot));
                if (closed || request != loadGeneration) return;
                photos = next; T("LibraryCount").Text = photos.Count + " 张风景，随时出发";
                if (page != "settings") RenderGallery();
                if (photos.Count(p => p.Source == "local") >= PhotoCatalog.LocalLimit) Status("当前最多载入 1500 张本地照片；可缩小相册文件夹范围");
            }
            catch (Exception ex) { store.Log(ex); if (!closed) Status("部分照片无法读取：" + ex.Message); }
        }
        private async Task RefreshOnline()
        {
            if (refreshing || closed) return;
            refreshing = true; B("RefreshButton").IsEnabled = false; Status("正在获取必应每日壁纸…");
            try { int count = await catalog.RefreshOnline(lifetime.Token); await ReloadLibrary(); if (!closed) Status("已更新 " + count + " 张每日壁纸，离线时也可欣赏"); }
            catch (OperationCanceledException) { if (!closed) Status("连接超时，继续使用已有画报"); }
            catch (Exception ex) { store.Log(ex); if (!closed) Status("每日壁纸暂时无法连接，继续使用已有画报"); }
            finally { refreshing = false; if (!closed) B("RefreshButton").IsEnabled = true; }
        }
        private void Preview(Photo selected)
        {
            List<Photo> playlist = PhotoCatalog.Playlist(photos, settings);
            if (selected != null && !playlist.Any(p => p.Id == selected.Id)) playlist.Insert(0, selected);
            SaverSession session = new SaverSession(store, settings, playlist, true, Window, selected);
            session.Finished += delegate { if (!closed) { B("HeroFavorite").Content = IsFavorite(hero) ? "♥" : "♡"; RenderGallery(); } };
            session.Start();
        }
        private sealed class WindowHandle : Forms.IWin32Window
        {
            public IntPtr Handle { get; private set; }
            public WindowHandle(Window window) { Handle = new System.Windows.Interop.WindowInteropHelper(window).Handle; }
        }
        private sealed class JavaScriptClone
        {
            public readonly Settings Value;
            public JavaScriptClone(Settings settings) { var serializer = new System.Web.Script.Serialization.JavaScriptSerializer(); Value = serializer.Deserialize<Settings>(serializer.Serialize(settings)); }
        }
    }
}
