using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace Shiguang
{
    public sealed class Settings
    {
        public const string ExitWithEscape = "escape";
        public const string ExitWithBlankClick = "blank-click";
        public int Version { get; set; }
        public int IntervalSeconds { get; set; }
        public bool ShowClock { get; set; }
        public bool ShowDate { get; set; }
        public bool ShowQuote { get; set; }
        public bool Hour12 { get; set; }
        public bool Fade { get; set; }
        public bool Shuffle { get; set; }
        public string ExitMode { get; set; }
        public bool AllMonitors { get; set; }
        public bool OnlineEnabled { get; set; }
        public int OnlineCacheDays { get; set; }
        public bool IncludeBuiltIn { get; set; }
        public bool IncludeLocal { get; set; }
        public bool FavoritesOnly { get; set; }
        public bool IncludeSubfolders { get; set; }
        public int Brightness { get; set; }
        public string ClockPosition { get; set; }
        public List<string> LocalFolders { get; set; }
        public List<string> Favorites { get; set; }
        public Settings()
        {
            Version = 3; IntervalSeconds = 45; Brightness = 90; OnlineCacheDays = 30;
            ShowClock = ShowDate = ShowQuote = Fade = AllMonitors = true;
            ExitMode = ExitWithEscape;
            IncludeBuiltIn = IncludeLocal = IncludeSubfolders = true;
            ClockPosition = "left";
            LocalFolders = new List<string>(); Favorites = new List<string>();
        }
        public void Normalize()
        {
            if (Version < 3) OnlineCacheDays = 30;
            Version = 3;
            if (ExitMode != ExitWithBlankClick) ExitMode = ExitWithEscape;
            IntervalSeconds = Math.Max(10, Math.Min(3600, IntervalSeconds));
            Brightness = Math.Max(30, Math.Min(100, Brightness));
            if (OnlineCacheDays != 0 && OnlineCacheDays != 7 && OnlineCacheDays != 30 && OnlineCacheDays != 90 && OnlineCacheDays != 180 && OnlineCacheDays != 365) OnlineCacheDays = 30;
            if (ClockPosition != "center") ClockPosition = "left";
            LocalFolders = (LocalFolders ?? new List<string>()).Where(p => !String.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).Take(50).ToList();
            Favorites = (Favorites ?? new List<string>()).Where(p => !String.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase).Take(10000).ToList();
        }
    }

    public sealed class Photo
    {
        public string Id { get; set; }
        public string Path { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Source { get; set; }
        public string Credit { get; set; }
        public string Date { get; set; }
    }

    public sealed class AppStore
    {
        public readonly string Root;
        public string Warning { get; private set; }
        public AppStore(string root) { Root = root; Directory.CreateDirectory(Root); }
        public string FilePath(string name) { return System.IO.Path.Combine(Root, name); }
        public Settings LoadSettings()
        {
            try
            {
                if (!File.Exists(FilePath("settings.json"))) return new Settings();
                Settings value = Json.Read<Settings>(FilePath("settings.json"));
                if (value == null) throw new InvalidDataException("Empty settings.");
                value.Normalize(); return value;
            }
            catch (Exception ex)
            {
                Warning = "设置文件无法读取，已使用默认设置。";
                Log(ex);
                try { File.Copy(FilePath("settings.json"), FilePath("settings.invalid-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".json"), true); } catch { }
                return new Settings();
            }
        }
        public void Save(Settings settings) { settings.Normalize(); Json.Write(FilePath("settings.json"), settings); }
        public void Log(Exception exception)
        {
            try
            {
                string path = FilePath("app.log");
                if (File.Exists(path) && new FileInfo(path).Length > 1024 * 1024) File.WriteAllText(path, "", Encoding.UTF8);
                File.AppendAllText(path, DateTime.Now.ToString("s") + " " + exception + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }
    }

    public static class Json
    {
        public static T Read<T>(string path) { return new JavaScriptSerializer { MaxJsonLength = 4 * 1024 * 1024 }.Deserialize<T>(File.ReadAllText(path, Encoding.UTF8)); }
        public static void Write<T>(string path, T value)
        {
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp, new JavaScriptSerializer().Serialize(value), new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }

    public sealed class LaunchOptions
    {
        public string Mode = "config";
        public IntPtr Parent;
        public string DataDirectory;
        public string OutputDirectory;
        public static LaunchOptions Parse(string[] args)
        {
            LaunchOptions options = new LaunchOptions();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLowerInvariant();
                if ((arg == "--data-dir" || arg == "--output-dir") && i + 1 < args.Length)
                {
                    if (arg == "--data-dir") options.DataDirectory = System.IO.Path.GetFullPath(args[++i]);
                    else options.OutputDirectory = System.IO.Path.GetFullPath(args[++i]);
                }
                else if (arg == "/s" || arg == "-s") options.Mode = "saver";
                else if (arg == "--preview") options.Mode = "interactive";
                else if (arg == "--verify") options.Mode = "verify";
                else if (arg == "--check-online") options.Mode = "online-check";
                else if (arg == "--smoke-test") options.Mode = "smoke";
                else if (arg == "--enable-login-startup") options.Mode = "enable-login-startup";
                else if (arg == "--disable-login-startup") options.Mode = "disable-login-startup";
                else if (arg == "--uninstall") options.Mode = "uninstall";
                else if (arg == "/p" || arg == "-p" || arg.StartsWith("/p:") || arg.StartsWith("-p:"))
                {
                    options.Mode = "preview";
                    string handle = arg.Length > 3 ? arg.Substring(3) : (i + 1 < args.Length ? args[++i] : "");
                    long parsed;
                    if (Int64.TryParse(handle, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) && parsed > 0 && (IntPtr.Size == 8 || parsed <= UInt32.MaxValue)) options.Parent = new IntPtr(parsed);
                }
                else if (arg == "/c" || arg == "-c" || arg.StartsWith("/c:") || arg.StartsWith("-c:")) options.Mode = "config";
            }
            return options;
        }
    }
}
