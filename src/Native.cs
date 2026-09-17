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
    }

    public sealed class ScreensaverRegistration
    {
        private readonly AppStore store;
        private const string DesktopKey = @"Control Panel\Desktop";
        public ScreensaverRegistration(AppStore appStore) { store = appStore; }
        public string InstalledPath { get { return store.FilePath("bin\\ShiguangPoster.scr"); } }
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
        private void Restore(RegistrySnapshot snapshot)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(DesktopKey))
            {
                SetOrDelete(key, "SCRNSAVE.EXE", snapshot.Saver);
                SetOrDelete(key, "ScreenSaveActive", snapshot.Active);
                SetOrDelete(key, "ScreenSaveTimeOut", snapshot.Timeout);
            }
            uint seconds; if (UInt32.TryParse(snapshot.Timeout, out seconds)) Native.SetParameter(0x000F, seconds, IntPtr.Zero, 2);
            Native.SetParameter(0x0011, snapshot.Active == "1" ? 1u : 0u, IntPtr.Zero, 2);
        }
        public void Enable(int minutes)
        {
            RegistrySnapshot before = Capture();
            if (!IsInstalled()) Json.Write(store.FilePath("previous-screensaver.json"), before);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(InstalledPath));
            string source = Assembly.GetExecutingAssembly().Location;
            if (!String.Equals(source, InstalledPath, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(source, InstalledPath, true);
                if (File.Exists(source + ".config")) File.Copy(source + ".config", InstalledPath + ".config", true);
            }
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(DesktopKey)) key.SetValue("SCRNSAVE.EXE", InstalledPath, RegistryValueKind.String);
                uint timeout = (uint)(Math.Max(1, Math.Min(120, minutes)) * 60);
                if (!Native.SetParameter(0x000F, timeout, IntPtr.Zero, 3)) throw new Win32Exception(Marshal.GetLastWin32Error());
                if (!Native.SetParameter(0x0011, 1, IntPtr.Zero, 3)) throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            catch { Restore(before); throw; }
        }
        public void Disable()
        {
            if (!IsInstalled()) return;
            RegistrySnapshot snapshot = File.Exists(store.FilePath("previous-screensaver.json")) ? Json.Read<RegistrySnapshot>(store.FilePath("previous-screensaver.json")) : new RegistrySnapshot { Active = "0", Timeout = "300" };
            Restore(snapshot ?? new RegistrySnapshot { Active = "0", Timeout = "300" });
        }
        public void OpenControlPanel() { Native.Start("rundll32.exe", "shell32.dll,Control_RunDLL desk.cpl,,1"); }
    }
}
