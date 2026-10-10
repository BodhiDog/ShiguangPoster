using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Shiguang
{
    public sealed partial class MainController
    {
        private StackPanel SettingsCard(string title, string description)
        {
            StackPanel content = new StackPanel();
            TextBlock heading = Ui.Text(title, 17, "#E5EDE1"); heading.FontWeight = FontWeights.SemiBold; content.Children.Add(heading);
            if (!String.IsNullOrEmpty(description)) { TextBlock detail = Ui.Text(description, 11, "#8FA58F"); detail.Margin = new Thickness(0, 7, 0, 8); content.Children.Add(detail); }
            Border card = new Border { Child = content }; card.SetResourceReference(FrameworkElement.StyleProperty, "SettingsCard"); Ui.Find<StackPanel>(Window, "SettingsPage").Children.Add(card); return content;
        }
        private void SettingRow(StackPanel card, string title, string description, FrameworkElement control)
        {
            Grid row = new Grid { Margin = new Thickness(0, 16, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            StackPanel label = new StackPanel { Margin = new Thickness(0, 0, 24, 0), VerticalAlignment = VerticalAlignment.Center };
            label.Children.Add(Ui.Text(title, 13, "#DBE6D6"));
            if (!String.IsNullOrEmpty(description)) { TextBlock detail = Ui.Text(description, 11, "#819884"); detail.Margin = new Thickness(0, 5, 0, 0); label.Children.Add(detail); }
            row.Children.Add(label); control.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(control, 1); row.Children.Add(control); card.Children.Add(row);
            System.Windows.Automation.AutomationProperties.SetName(control, title);
        }
        private CheckBox Switch(bool value, Action<bool> changed)
        {
            CheckBox toggle = new CheckBox { IsChecked = value };
            toggle.SetResourceReference(FrameworkElement.StyleProperty, "Switch");
            RoutedEventHandler handler = delegate { changed(toggle.IsChecked == true); Save(); };
            toggle.Checked += handler; toggle.Unchecked += handler; return toggle;
        }
        private CheckBox LoginStartupSwitch()
        {
            CheckBox toggle = new CheckBox { IsChecked = loginStartup.IsEnabled() };
            toggle.SetResourceReference(FrameworkElement.StyleProperty, "Switch");
            bool reverting = false;
            RoutedEventHandler handler = delegate
            {
                if (reverting) return;
                try
                {
                    if (toggle.IsChecked == true) loginStartup.Enable();
                    else if (!loginStartup.Disable()) throw new InvalidOperationException("已保留同名但不属于拾光画报的启动项。");
                    Status(toggle.IsChecked == true ? "已设置为登录 Windows 后自动打开" : "已关闭登录后自动打开");
                }
                catch (Exception ex)
                {
                    store.Log(ex);
                    reverting = true;
                    toggle.IsChecked = loginStartup.IsEnabled();
                    reverting = false;
                    Status("无法更新登录启动设置：" + ex.Message);
                }
            };
            toggle.Checked += handler;
            toggle.Unchecked += handler;
            return toggle;
        }
        private ComboBox Choice(string[] labels, int selected, Action<int> changed)
        {
            ComboBox choice = new ComboBox();
            foreach (string label in labels) choice.Items.Add(label);
            choice.SelectedIndex = Math.Max(0, selected);
            choice.SelectionChanged += delegate { if (choice.SelectedIndex >= 0) { changed(choice.SelectedIndex); Save(); } }; return choice;
        }
        private void BuildSettings()
        {
            Ui.Find<StackPanel>(Window, "SettingsPage").Children.Clear();
            StackPanel system = SettingsCard("让风景，在你离开时出现", "启用后由 Windows 在空闲时启动，无需让主窗口一直开着。");
            TextBlock state = Ui.Text(registration.IsEnabled() ? "●  已启用拾光屏保" : "○  拾光屏保尚未启用", 12, "#B8D7B4"); state.Margin = new Thickness(0, 9, 0, 0); system.Children.Add(state);
            SettingRow(system, "登录 Windows 时打开拾光画报", "登录当前账户后自动打开主窗口，无需管理员权限。", LoginStartupSwitch());
            int currentTimeout = registration.TimeoutMinutes();
            int[] waits = new[] { 1, 3, 5, 10, 15, 30, 60, currentTimeout }.Distinct().OrderBy(v => v).ToArray();
            ComboBox idleChoice = new ComboBox(); foreach (int wait in waits) idleChoice.Items.Add(wait + " 分钟"); idleChoice.SelectedIndex = Array.IndexOf(waits, currentTimeout);
            SettingRow(system, "电脑空闲多久后开始", "在下方点击“启用系统屏保”后生效。", idleChoice);
            StackPanel systemActions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 19, 0, 0) };
            Button enable = Ui.Button(registration.IsEnabled() ? "更新系统屏保" : "启用系统屏保", true);
            enable.Click += delegate
            {
                try
                {
                    if (!Save()) return;
                    registration.Enable(waits[idleChoice.SelectedIndex]);
                    try { loginStartup.Enable(); }
                    catch (Exception startupException) { store.Log(startupException); BuildSettings(); Status("屏保已启用，但无法设置登录启动：" + startupException.Message); return; }
                    BuildSettings();
                    Status("已启用 · 空闲 " + waits[idleChoice.SelectedIndex] + " 分钟后自动播放，并会在登录后打开拾光画报");
                }
                catch (Exception ex) { store.Log(ex); Status("无法启用系统屏保：" + ex.Message); }
            };
            systemActions.Children.Add(enable);
            Button disable = Ui.Button("停用拾光屏保", false); disable.Margin = new Thickness(10, 0, 0, 0); disable.IsEnabled = registration.IsInstalled();
            disable.Click += delegate { try { bool restored = registration.Disable(); BuildSettings(); Status(restored ? "已停用拾光屏保，并恢复此前的屏保设置" : "未停用屏保：找不到可安全恢复的设置备份"); } catch (Exception ex) { store.Log(ex); Status("无法停用屏保：" + ex.Message); } }; systemActions.Children.Add(disable);
            Button panel = Ui.Button("Windows 屏保设置 ↗", false); panel.SetResourceReference(FrameworkElement.StyleProperty, "GhostButton"); panel.Margin = new Thickness(7, 0, 0, 0);
            panel.Click += delegate { try { registration.OpenControlPanel(); } catch (Exception ex) { store.Log(ex); Status("无法打开 Windows 设置：" + ex.Message); } }; systemActions.Children.Add(panel);
            system.Children.Add(systemActions);
            TextBlock hint = Ui.Text("恢复时是否显示登录屏幕，可在 Windows 屏保设置中调整。", 11, "#819884"); hint.Margin = new Thickness(0, 13, 0, 0); system.Children.Add(hint);

            StackPanel playback = SettingsCard("播放的节奏", "一张画报，一个短暂而安静的停留。");
            int[] intervals = new[] { 15, 30, 45, 60, 120, 300, settings.IntervalSeconds }.Distinct().OrderBy(v => v).ToArray();
            SettingRow(playback, "每张画报停留", "到时自动切换下一张。", Choice(intervals.Select(v => v >= 60 && v % 60 == 0 ? (v / 60) + " 分钟" : v + " 秒").ToArray(), Array.IndexOf(intervals, settings.IntervalSeconds), i => settings.IntervalSeconds = intervals[i]));
            SettingRow(playback, "柔和过渡", "用淡入淡出衔接两处风景。", Switch(settings.Fade, v => settings.Fade = v));
            SettingRow(playback, "随机播放", "打乱顺序，完整播放后再开启下一轮。", Switch(settings.Shuffle, v => settings.Shuffle = v));
            SettingRow(playback, "所有显示器", "每块屏幕同步呈现同一张画报。", Switch(settings.AllMonitors, v => settings.AllMonitors = v));
            SettingRow(playback, "退出屏保方式", "适用于全屏预览和自动屏保；空白区域指图片、文字等非操作按钮区域。",
                Choice(new[] { "按退出键（Esc）退出", "鼠标点击屏保空白位置退出" }, settings.ExitMode == Settings.ExitWithBlankClick ? 1 : 0,
                    i => { settings.ExitMode = i == 1 ? Settings.ExitWithBlankClick : Settings.ExitWithEscape; UpdateExitHint(); }));

            StackPanel overlay = SettingsCard("画面里的小细节", "让时间和文字，以你喜欢的方式出现。");
            SettingRow(overlay, "显示时钟", "简洁大字，轻轻看一眼就好。", Switch(settings.ShowClock, v => settings.ShowClock = v));
            SettingRow(overlay, "显示日期", "公历、星期与农历。", Switch(settings.ShowDate, v => settings.ShowDate = v));
            SettingRow(overlay, "显示心情短句", "为眼前的风景，留一句话。", Switch(settings.ShowQuote, v => settings.ShowQuote = v));
            SettingRow(overlay, "12 小时制", "关闭时使用 24 小时制。", Switch(settings.Hour12, v => { settings.Hour12 = v; UpdateClock(); }));
            SettingRow(overlay, "时钟位置", "文字会轻微移动，避免长时间停在同一处。", Choice(new[] { "左下角", "居中" }, settings.ClockPosition == "center" ? 1 : 0, i => settings.ClockPosition = i == 1 ? "center" : "left"));
            StackPanel brightnessPanel = new StackPanel { Orientation = Orientation.Horizontal };
            TextBlock brightnessValue = Ui.Text(settings.Brightness + "%", 12, "#B7CBB1"); brightnessValue.Width = 42; brightnessValue.VerticalAlignment = VerticalAlignment.Center;
            Slider brightness = new Slider { Minimum = 30, Maximum = 100, TickFrequency = 5, IsSnapToTickEnabled = true, Value = settings.Brightness, Width = 135, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
            brightness.ValueChanged += delegate { settings.Brightness = (int)brightness.Value; brightnessValue.Text = settings.Brightness + "%"; Save(); };
            brightnessPanel.Children.Add(brightness); brightnessPanel.Children.Add(brightnessValue); SettingRow(overlay, "画面亮度", "夜晚可以调暗一些。", brightnessPanel);

            StackPanel sources = SettingsCard("想看的风景", "选择轮播来源，开启仅播放收藏时会进一步筛选。");
            SettingRow(sources, "拾光原创画报", "内置三张 AI 生成风景，随时离线欣赏。", Switch(settings.IncludeBuiltIn, v => settings.IncludeBuiltIn = v));
            SettingRow(sources, "我的本地相册", "在“我的相册”中添加照片文件夹。", Switch(settings.IncludeLocal, v => settings.IncludeLocal = v));
            SettingRow(sources, "读取相册子文件夹", "单次最多读取 1500 张照片。", Switch(settings.IncludeSubfolders, async v => { settings.IncludeSubfolders = v; await ReloadLibrary(); }));
            SettingRow(sources, "必应每日壁纸", "联网下载最近八天的图片，最多保留三十张缓存。", Switch(settings.OnlineEnabled, async v => { settings.OnlineEnabled = v; await ReloadLibrary(); if (v) await RefreshOnline(); }));
            SettingRow(sources, "仅播放我的收藏", "收藏为空或不可用时，使用内置画报。", Switch(settings.FavoritesOnly, v => settings.FavoritesOnly = v));
            TextBlock emptyHint = Ui.Text("所选来源没有可用图片时，会自动使用内置画报。", 11, "#819884"); emptyHint.Margin = new Thickness(0, 18, 0, 0); sources.Children.Add(emptyHint);

            StackPanel about = SettingsCard("拾光画报 1.0", "把忙碌交给时间，把片刻留给风景。");
            TextBlock shortcuts = Ui.Text("预览快捷键：← → 切换　空格 暂停 / 继续　F 收藏；退出方式由上方设置决定。", 12, "#ABBFA6"); shortcuts.Margin = new Thickness(0, 9, 0, 0); about.Children.Add(shortcuts);
            TextBlock local = Ui.Text("设置和缓存保存在当前 Windows 用户目录。默认离线运行；启用每日壁纸后才连接必应。", 11, "#819884"); local.Margin = new Thickness(0, 13, 0, 0); about.Children.Add(local);
            Button openData = Ui.Button("打开数据文件夹 ↗", false); openData.HorizontalAlignment = HorizontalAlignment.Left; openData.Margin = new Thickness(0, 14, 0, 0);
            openData.Click += delegate { try { Native.Start(store.Root, ""); } catch (Exception ex) { store.Log(ex); Status("无法打开文件夹：" + ex.Message); } }; about.Children.Add(openData);
        }
    }
}
