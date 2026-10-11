using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Shiguang
{
    public sealed class PhotoCatalog
    {
        private readonly AppStore store;
        private static readonly HashSet<string> Extensions = new HashSet<string>(new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff" }, StringComparer.OrdinalIgnoreCase);
        public const int LocalLimit = 1500;
        public PhotoCatalog(AppStore appStore) { store = appStore; }
        public static List<Photo> BuiltIn()
        {
            return new List<Photo> {
                new Photo { Id="builtin:mountain-dawn", Path="resource:mountain-dawn.png", Title="山有回响", Description="把片刻留给远山，把心事交给云海。", Category="山野", Source="builtin", Credit="拾光原创 · AI 生成风景" },
                new Photo { Id="builtin:quiet-coast", Path="resource:quiet-coast.png", Title="听，海的呼吸", Description="潮汐不赶时间，你也可以慢一点。", Category="海岸", Source="builtin", Credit="拾光原创 · AI 生成风景" },
                new Photo { Id="builtin:desert-light", Path="resource:desert-light.png", Title="风经过的地方", Description="在辽阔的寂静里，找回自己的节奏。", Category="旷野", Source="builtin", Credit="拾光原创 · AI 生成风景" }
            };
        }
        public List<Photo> Load(Settings settings)
        {
            PruneOnlineCache(settings);
            List<Photo> photos = BuiltIn();
            if (settings.OnlineEnabled) photos.AddRange(ReadOnlineCache());
            HashSet<string> found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string folder in settings.LocalFolders)
            {
                foreach (string file in EnumeratePhotos(folder, settings.IncludeSubfolders))
                {
                    if (!found.Add(file)) continue;
                    photos.Add(new Photo { Id = "local:" + file.ToLowerInvariant(), Path = file, Title = System.IO.Path.GetFileNameWithoutExtension(file), Description = "熟悉的风景，也值得再看一遍。", Category = "相册", Source = "local", Credit = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(file)) + " · 本地照片" });
                    if (found.Count >= LocalLimit) break;
                }
                if (found.Count >= LocalLimit) break;
            }
            return photos;
        }
        public static IEnumerable<string> EnumeratePhotos(string folder, bool recursive)
        {
            Stack<string> pending = new Stack<string>();
            HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            pending.Push(folder);
            int folders = 0;
            while (pending.Count > 0 && folders++ < 10000)
            {
                string current = pending.Pop();
                string[] files;
                try
                {
                    current = System.IO.Path.GetFullPath(current);
                    if (!visited.Add(current) || (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) continue;
                    files = Directory.GetFiles(current);
                }
                catch (Exception ex) { if (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is System.Security.SecurityException || ex is NotSupportedException) continue; throw; }
                foreach (string file in files) if (Extensions.Contains(System.IO.Path.GetExtension(file))) yield return file;
                if (!recursive) continue;
                try { foreach (string child in Directory.GetDirectories(current)) pending.Push(child); }
                catch (Exception ex) { if (!(ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException)) throw; }
            }
        }
        public static List<Photo> Playlist(IEnumerable<Photo> photos, Settings settings)
        {
            List<Photo> result = photos.Where(p => (p.Source == "builtin" && settings.IncludeBuiltIn) || (p.Source == "local" && settings.IncludeLocal) || (p.Source == "online" && settings.OnlineEnabled)).ToList();
            if (settings.FavoritesOnly) result = result.Where(p => settings.Favorites.Contains(p.Id, StringComparer.OrdinalIgnoreCase)).ToList();
            if (result.Count == 0) result = BuiltIn();
            return result;
        }
        public List<Photo> ReadOnlineCache()
        {
            try
            {
                string manifest = store.FilePath("bing.json");
                if (!File.Exists(manifest)) return new List<Photo>();
                return (Json.Read<List<Photo>>(manifest) ?? new List<Photo>()).Where(p => p != null && p.Source == "online" && IsCachePath(p.Path) && File.Exists(p.Path)).Take(30).ToList();
            }
            catch (Exception ex) { store.Log(ex); return new List<Photo>(); }
        }
        private bool IsCachePath(string path)
        {
            if (String.IsNullOrEmpty(path)) return false;
            string cache = System.IO.Path.GetFullPath(store.FilePath("cache")) + System.IO.Path.DirectorySeparatorChar;
            try { return System.IO.Path.GetFullPath(path).StartsWith(cache, StringComparison.OrdinalIgnoreCase); } catch { return false; }
        }
        public void PruneOnlineCache(Settings settings)
        {
            if (settings.OnlineCacheDays <= 0) return;
            try
            {
                string cacheDirectory = store.FilePath("cache");
                if (!Directory.Exists(cacheDirectory)) return;
                DateTime cutoff = DateTime.UtcNow.AddDays(-settings.OnlineCacheDays);
                foreach (string file in Directory.GetFiles(cacheDirectory, "*.jpg"))
                {
                    if (IsCachePath(file) && File.GetLastWriteTimeUtc(file) < cutoff)
                    {
                        try { File.Delete(file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                    }
                }
                string manifest = store.FilePath("bing.json");
                if (!File.Exists(manifest)) return;
                List<Photo> remaining = (Json.Read<List<Photo>>(manifest) ?? new List<Photo>())
                    .Where(p => p != null && p.Source == "online" && IsCachePath(p.Path) && File.Exists(p.Path)).ToList();
                Json.Write(manifest, remaining);
            }
            catch (Exception ex) { store.Log(ex); }
        }
        public async Task<int> RefreshOnline(CancellationToken cancellationToken)
        {
            string cacheDirectory = store.FilePath("cache"); Directory.CreateDirectory(cacheDirectory);
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            using (HttpClient client = new HttpClient())
            using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                client.Timeout = TimeSpan.FromSeconds(12);
                timeout.CancelAfter(TimeSpan.FromSeconds(40));
                client.DefaultRequestHeaders.UserAgent.ParseAdd("ShiguangPoster/1.0");
                string json;
                using (HttpResponseMessage response = await client.GetAsync("https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=8&mkt=zh-CN", timeout.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                BingFeed feed = new JavaScriptSerializer { MaxJsonLength = 1024 * 1024 }.Deserialize<BingFeed>(json);
                if (feed == null || feed.images == null || feed.images.Count == 0) throw new InvalidDataException("必应没有返回图片。");
                List<Photo> cached = ReadOnlineCache();
                List<Photo> downloaded = new List<Photo>();
                foreach (BingPhoto remote in feed.images.Take(8))
                {
                    timeout.Token.ThrowIfCancellationRequested();
                    if (String.IsNullOrEmpty(remote.urlbase) || !remote.urlbase.StartsWith("/th?id=OHR.", StringComparison.Ordinal)) continue;
                    string id;
                    using (SHA256 hash = SHA256.Create()) { id = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(remote.urlbase))).Replace("-", "").Substring(0, 24); }
                    string destination = System.IO.Path.Combine(cacheDirectory, id + ".jpg");
                    try
                    {
                        if (!File.Exists(destination))
                        {
                            byte[] bytes;
                            using (HttpResponseMessage response = await client.GetAsync("https://www.bing.com" + remote.urlbase + "_1920x1080.jpg", HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
                            {
                                response.EnsureSuccessStatusCode();
                                if (response.Content.Headers.ContentLength > 20 * 1024 * 1024) continue;
                                using (Stream input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                                using (MemoryStream buffer = new MemoryStream())
                                {
                                    byte[] block = new byte[32768]; int read;
                                    while ((read = await input.ReadAsync(block, 0, block.Length, timeout.Token).ConfigureAwait(false)) > 0)
                                    {
                                        if (buffer.Length + read > 20 * 1024 * 1024) throw new InvalidDataException("图片超过大小限制。");
                                        buffer.Write(block, 0, read);
                                    }
                                    bytes = buffer.ToArray();
                                }
                            }
                            using (MemoryStream imageStream = new MemoryStream(bytes)) { ImageLoader.Decode(imageStream, 64); }
                            string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
                            try
                            {
                                File.WriteAllBytes(temporary, bytes);
                                try { File.Move(temporary, destination); }
                                catch (IOException) { if (!File.Exists(destination)) throw; }
                            }
                            finally { if (File.Exists(temporary)) File.Delete(temporary); }
                        }
                        downloaded.Add(new Photo { Id = "bing:" + id, Path = destination, Title = String.IsNullOrWhiteSpace(remote.title) ? "今日远方" : remote.title, Description = "每一天，都有一处值得停留的风景。", Source = "online", Category = "每日", Credit = remote.copyright, Date = remote.startdate });
                    }
                    catch (OperationCanceledException) { if (downloaded.Count == 0) throw; break; }
                    catch (Exception ex) { store.Log(ex); }
                }
                if (downloaded.Count == 0) throw new IOException("暂时无法获取必应图片。");
                List<Photo> merged = downloaded.Concat(cached).GroupBy(p => p.Id).Select(g => g.First()).OrderByDescending(p => p.Date).ToList();
                Json.Write(store.FilePath("bing.json"), merged);
                PruneOnlineCache(settings);
                return downloaded.Count;
            }
        }
        private sealed class BingFeed { public List<BingPhoto> images { get; set; } }
        private sealed class BingPhoto
        {
            public string urlbase { get; set; }
            public string title { get; set; }
            public string copyright { get; set; }
            public string startdate { get; set; }
        }
    }

    public static class ImageLoader
    {
        private static readonly Dictionary<string, BitmapSource> Cache = new Dictionary<string, BitmapSource>();
        private static readonly Queue<string> Order = new Queue<string>();
        private static readonly object Gate = new object();
        public static BitmapSource Load(Photo photo, int width)
        {
            string key = photo.Path + "|" + width;
            lock (Gate) { BitmapSource value; if (Cache.TryGetValue(key, out value)) return value; }
            BitmapSource image;
            try
            {
                if (photo.Path.StartsWith("resource:", StringComparison.Ordinal))
                {
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Shiguang.Assets." + photo.Path.Substring(9))) image = Decode(stream, width);
                }
                else
                {
                    using (FileStream stream = new FileStream(photo.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) image = Decode(stream, width);
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is NotSupportedException || ex is ArgumentException || ex is InvalidOperationException || ex is UnauthorizedAccessException || ex is System.Runtime.InteropServices.COMException || ex is FileFormatException) return null;
                throw;
            }
            lock (Gate)
            {
                if (!Cache.ContainsKey(key)) { Cache[key] = image; Order.Enqueue(key); }
                // Bound the total decoded pixel count (about 100 MB), not only the image count.
                long pixels = Cache.Values.Sum(p => (long)p.PixelWidth * p.PixelHeight);
                while ((pixels > 24000000 || Cache.Count > 64) && Order.Count > 1)
                {
                    string oldest = Order.Dequeue(); BitmapSource old;
                    if (Cache.TryGetValue(oldest, out old)) { pixels -= (long)old.PixelWidth * old.PixelHeight; Cache.Remove(oldest); }
                }
            }
            return image;
        }
        public static BitmapSource Decode(Stream stream, int width)
        {
            if (stream == null) throw new FileNotFoundException("Missing image resource.");
            BitmapDecoder decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            BitmapFrame frame = decoder.Frames[0];
            int decodeWidth = Math.Min(frame.PixelWidth, Math.Max(64, Math.Min(3840, width)));
            if ((long)frame.PixelHeight * decodeWidth > 3840L * frame.PixelWidth)
                decodeWidth = Math.Max(1, (int)(3840.0 * frame.PixelWidth / frame.PixelHeight));
            int orientation = 1;
            try
            {
                BitmapMetadata metadata = frame.Metadata as BitmapMetadata;
                if (metadata != null)
                {
                    if (metadata.ContainsQuery("/app1/ifd/{ushort=274}")) orientation = Convert.ToInt32(metadata.GetQuery("/app1/ifd/{ushort=274}"));
                    else if (metadata.ContainsQuery("/ifd/{ushort=274}")) orientation = Convert.ToInt32(metadata.GetQuery("/ifd/{ushort=274}"));
                }
            }
            catch { }
            stream.Position = 0;
            BitmapImage bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = decodeWidth; bitmap.StreamSource = stream; bitmap.EndInit(); bitmap.Freeze();
            TransformGroup transform = new TransformGroup();
            if (orientation == 2 || orientation == 4 || orientation == 5 || orientation == 7) transform.Children.Add(new ScaleTransform(-1, 1));
            int angle = orientation == 3 || orientation == 4 ? 180 : orientation == 6 || orientation == 7 ? 90 : orientation == 5 || orientation == 8 ? 270 : 0;
            if (angle != 0) transform.Children.Add(new RotateTransform(angle));
            if (transform.Children.Count == 0) return bitmap;
            TransformedBitmap result = new TransformedBitmap(bitmap, transform); result.Freeze(); return result;
        }
    }
}
