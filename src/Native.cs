using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Shiguang
{
    public static class Native
    {
        [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
        [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT point);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr window, out RECT rect);
        [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetParent(IntPtr child, IntPtr parent);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] public static extern int GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] public static extern int SetWindowLong(IntPtr window, int index, int value);
        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)] public static extern bool SetParameter(uint action, uint value, IntPtr unused, uint flags);
        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)] public static extern bool GetParameter(uint action, uint unused, out uint value, uint flags);
        public static void Start(string file, string arguments)
        {
            Process.Start(new ProcessStartInfo(file, arguments) { UseShellExecute = true });
        }
    }

    public sealed class RegistrySnapshot
    {
        public string Saver { get; set; }
        public string Active { get; set; }
        public string Timeout { get; set; }
        public string AppliedActive { get; set; }
        public string AppliedTimeout { get; set; }
    }

    public sealed class LoginStartupRegistration
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "ShiguangPoster";
        private readonly AppStore store;

        public LoginStartupRegistration(AppStore appStore) { store = appStore; }
        public string InstalledPath { get { return Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".exe"); } }
        // Start without a visible configuration window when Windows signs in.
        private string CommandLine { get { return "\"" + InstalledPath + "\" --background"; } }
        // This exact command was created by releases before background startup existed.
        private string LegacyCommandLine { get { return "\"" + InstalledPath + "\""; } }

        private bool IsOurCommandLine(string value)
        {
            return String.Equals(value, CommandLine, StringComparison.OrdinalIgnoreCase)
                || String.Equals(value, LegacyCommandLine, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey))
            {
                string value = key == null ? null : Convert.ToString(key.GetValue(ValueName, null));
                return IsOurCommandLine(value);
            }
        }

        public void Enable()
        {
            if (CommandLine.Length > 260) throw new InvalidOperationException("The Windows startup command is too long.");
            if (!File.Exists(InstalledPath)) throw new FileNotFoundException("ShiguangPoster.exe was not found.", InstalledPath);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey)) key.SetValue(ValueName, CommandLine, RegistryValueKind.String);
        }

        // Leave a value alone if another program has replaced it after we created it.
        public bool Disable()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (key == null) return true;
                object raw = key.GetValue(ValueName, null);
                if (raw == null) return true;
                string value = Convert.ToString(raw);
                if (!IsOurCommandLine(value)) return false;
                key.DeleteValue(ValueName, false);
                return true;
            }
        }
    }

    public sealed class ScreensaverRegistration
    {
        private readonly AppStore store;
        private const string DesktopKey = @"Control Panel\Desktop";
        public ScreensaverRegistration(AppStore appStore) { store = appStore; }
        public string InstalledPath
        {
            get
            {
                string source = Assembly.GetExecutingAssembly().Location;
                return String.Equals(Path.GetExtension(source), ".scr", StringComparison.OrdinalIgnoreCase) ? source : Path.ChangeExtension(source, ".scr");
            }
        }
        public bool IsInstalled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(DesktopKey))
            {
                string current = key == null ? "" : Convert.ToString(key.GetValue("SCRNSAVE.EXE", "")).Trim('"');
                return String.Equals(current, InstalledPath, StringComparison.OrdinalIgnoreCase);
            }
        }
        public bool IsEnabled()
        {
            if (!IsInstalled()) return false;
            uint active; return Native.GetParameter(0x0010, 0, out active, 0) && active != 0;
        }
        public int TimeoutMinutes()
        {
            uint seconds; return Native.GetParameter(0x000E, 0, out seconds, 0) && seconds > 0 ? Math.Max(1, (int)seconds / 60) : 5;
        }
        private RegistrySnapshot Capture()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(DesktopKey))
                return new RegistrySnapshot { Saver = Read(key, "SCRNSAVE.EXE"), Active = Read(key, "ScreenSaveActive"), Timeout = Read(key, "ScreenSaveTimeOut") };
        }
        private static string Read(RegistryKey key, string name) { return key == null || key.GetValue(name) == null ? null : Convert.ToString(key.GetValue(name)); }
        private static void SetOrDelete(RegistryKey key, string name, string value)
        {
            if (value == null) key.DeleteValue(name, false); else key.SetValue(name, value, RegistryValueKind.String);
        }
        private RegistrySnapshot ReadSnapshot()
        {
            string path = store.FilePath("previous-screensaver.json");
            return File.Exists(path) ? Json.Read<RegistrySnapshot>(path) : null;
        }
        private void Restore(RegistrySnapshot snapshot, bool restoreActive, bool restoreTimeout)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(DesktopKey))
            {
                SetOrDelete(key, "SCRNSAVE.EXE", snapshot.Saver);
                if (restoreActive) SetOrDelete(key, "ScreenSaveActive", snapshot.Active);
                if (restoreTimeout) SetOrDelete(key, "ScreenSaveTimeOut", snapshot.Timeout);
            }
            uint seconds; if (restoreTimeout && UInt32.TryParse(snapshot.Timeout, out seconds)) Native.SetParameter(0x000F, seconds, IntPtr.Zero, 2);
            if (restoreActive) Native.SetParameter(0x0011, snapshot.Active == "1" ? 1u : 0u, IntPtr.Zero, 2);
        }
        public void Enable(int minutes)
        {
            RegistrySnapshot before = Capture();
            bool wasInstalled = IsInstalled();
            RegistrySnapshot restorePoint = wasInstalled ? ReadSnapshot() : before;
            if (!wasInstalled) Json.Write(store.FilePath("previous-screensaver.json"), restorePoint);
            if (!File.Exists(InstalledPath)) throw new FileNotFoundException("ShiguangPoster.scr was not found.", InstalledPath);
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(DesktopKey)) key.SetValue("SCRNSAVE.EXE", InstalledPath, RegistryValueKind.String);
                uint timeout = (uint)(Math.Max(1, Math.Min(120, minutes)) * 60);
                if (!Native.SetParameter(0x000F, timeout, IntPtr.Zero, 3)) throw new Win32Exception(Marshal.GetLastWin32Error());
                if (!Native.SetParameter(0x0011, 1, IntPtr.Zero, 3)) throw new Win32Exception(Marshal.GetLastWin32Error());
                if (restorePoint != null)
                {
                    restorePoint.AppliedActive = "1";
                    restorePoint.AppliedTimeout = timeout.ToString();
                    Json.Write(store.FilePath("previous-screensaver.json"), restorePoint);
                }
            }
            catch { Restore(before, true, true); throw; }
        }
        // Restore only values that still match what this app applied. Later Windows edits win.
        public bool Disable()
        {
            if (!IsInstalled()) return true;
            RegistrySnapshot snapshot = ReadSnapshot();
            if (snapshot == null) return false;
            bool restoreActive;
            bool restoreTimeout;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(DesktopKey))
            {
                restoreActive = !String.IsNullOrEmpty(snapshot.AppliedActive) && String.Equals(Read(key, "ScreenSaveActive"), snapshot.AppliedActive, StringComparison.OrdinalIgnoreCase);
                restoreTimeout = !String.IsNullOrEmpty(snapshot.AppliedTimeout) && String.Equals(Read(key, "ScreenSaveTimeOut"), snapshot.AppliedTimeout, StringComparison.OrdinalIgnoreCase);
            }
            Restore(snapshot, restoreActive, restoreTimeout);
            return !IsInstalled();
        }
        public void OpenControlPanel() { Native.Start("rundll32.exe", "shell32.dll,Control_RunDLL desk.cpl,,1"); }
    }
}
