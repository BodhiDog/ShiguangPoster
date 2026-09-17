using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace Shiguang
{
    public sealed class PosterView : Grid
    {
        private readonly Settings settings;
        private readonly Image previous = new Image { Stretch = Stretch.UniformToFill };
        private readonly Image current = new Image { Stretch = Stretch.UniformToFill };
        private readonly TextBlock time, date, title, quote, credit, number;
        private readonly StackPanel clockGroup;
        private readonly Border progress;
        private readonly Grid overlay;
        private readonly TranslateTransform drift = new TranslateTransform();
        public readonly StackPanel Controls;
        public readonly Button PreviousButton, PauseButton, NextButton, FavoriteButton, CloseButton;
        public PosterView(Settings appSettings, bool interactive)
        {
            settings = appSettings; Background = Brushes.Black; ClipToBounds = true;
            Children.Add(previous); Children.Add(current);
            Children.Add(new Border { Background = Brushes.Black, Opacity = 1 - settings.Brightness / 100.0, IsHitTestVisible = false });
            LinearGradientBrush shade = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            shade.GradientStops.Add(new GradientStop(Color.FromArgb(35, 5, 20, 15), 0));
            shade.GradientStops.Add(new GradientStop(Color.FromArgb(0, 5, 20, 15), 0.35));
            shade.GradientStops.Add(new GradientStop(Color.FromArgb(24, 5, 20, 15), 0.55));
            shade.GradientStops.Add(new GradientStop(Color.FromArgb(180, 5, 20, 15), 1)); shade.Freeze();
            Children.Add(new Border { Background = shade, IsHitTestVisible = false });
            overlay = new Grid { Margin = new Thickness(65, 45, 65, 40), RenderTransform = drift };
            overlay.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 18, Opacity = 0.32, ShadowDepth = 1 };
            TextBlock brand = Label("拾光画报   /   SHIGUANG", 14, "#E6F1E4"); brand.HorizontalAlignment = HorizontalAlignment.Left; brand.VerticalAlignment = VerticalAlignment.Top; brand.FontWeight = FontWeights.Medium; overlay.Children.Add(brand);
            number = Label("01 / 03", 13, "#E1EBDC"); number.FontFamily = new FontFamily("Segoe UI"); number.HorizontalAlignment = HorizontalAlignment.Right; number.VerticalAlignment = VerticalAlignment.Top; overlay.Children.Add(number);
            clockGroup = new StackPanel { HorizontalAlignment = settings.ClockPosition == "center" ? HorizontalAlignment.Center : HorizontalAlignment.Left, VerticalAlignment = settings.ClockPosition == "center" ? VerticalAlignment.Center : VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, settings.ClockPosition == "center" ? 40 : 168) };
            time = Label("", 112, "#FFFFFF"); time.FontFamily = new FontFamily("Segoe UI"); time.FontWeight = FontWeights.Light; time.Margin = new Thickness(-7, 0, 0, 0); time.Visibility = settings.ShowClock ? Visibility.Visible : Visibility.Collapsed;
            date = Label("", 17, "#EBF0E6"); date.Margin = new Thickness(0, 5, 0, 0); date.Visibility = settings.ShowDate ? Visibility.Visible : Visibility.Collapsed;
            if (settings.ClockPosition == "center") { time.HorizontalAlignment = HorizontalAlignment.Center; date.HorizontalAlignment = HorizontalAlignment.Center; }
            clockGroup.Children.Add(time); clockGroup.Children.Add(date); overlay.Children.Add(clockGroup);
            StackPanel story = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 45), MaxWidth = 940 };
            credit = Label("", 12, "#DAE7D6"); credit.TextWrapping = TextWrapping.Wrap;
            title = Label("", 36, "#FFFFFF"); title.FontWeight = FontWeights.SemiBold; title.Margin = new Thickness(0, 10, 0, 0); title.TextTrimming = TextTrimming.CharacterEllipsis;
            quote = Label("", 16, "#DAE5D8"); quote.Margin = new Thickness(0, 13, 0, 0); quote.TextWrapping = TextWrapping.Wrap; quote.Visibility = settings.ShowQuote ? Visibility.Visible : Visibility.Collapsed;
            story.Children.Add(credit); story.Children.Add(title); story.Children.Add(quote); overlay.Children.Add(story);
            Controls = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 0), Visibility = interactive ? Visibility.Visible : Visibility.Collapsed };
            PreviousButton = ControlButton("‹", "上一张（←）"); PauseButton = ControlButton("Ⅱ", "暂停 / 继续（空格）"); NextButton = ControlButton("›", "下一张（→）"); FavoriteButton = ControlButton("♡", "收藏（F）"); CloseButton = ControlButton("退出  Esc", "退出预览"); CloseButton.Width = 98; CloseButton.FontSize = 12;
            Controls.Children.Add(PreviousButton); Controls.Children.Add(PauseButton); Controls.Children.Add(NextButton); Controls.Children.Add(FavoriteButton); Controls.Children.Add(CloseButton); overlay.Children.Add(Controls);
            Viewbox content = new Viewbox { Stretch = Stretch.Uniform, Child = overlay };
            // A fixed logical canvas keeps text and controls proportional on all monitor sizes.
            overlay.Width = 1600; overlay.Height = 900; overlay.Margin = new Thickness(0);
            Grid frame = new Grid { Width = 1600, Height = 900 };
            content.Child = null; overlay.Width = Double.NaN; overlay.Height = Double.NaN; overlay.Margin = new Thickness(65, 45, 65, 35); frame.Children.Add(overlay); content.Child = frame;
            Children.Add(content);
            progress = new Border { Background = Ui.Brush("#BFE0B4"), Height = 2, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Opacity = 0.7, Width = 0 };
            Children.Add(progress); UpdateClock(DateTime.Now);
        }
        private static TextBlock Label(string text, double size, string color) { return new TextBlock { Text = text, FontSize = size, Foreground = Ui.Brush(color), FontFamily = new FontFamily("Microsoft YaHei UI, Segoe UI") }; }
        private static Button ControlButton(string text, string hint)
        {
            return new Button { Content = text, ToolTip = hint, Width = 42, Height = 38, FontSize = 23, Background = Ui.Brush("#7013221A"), BorderBrush = Ui.Brush("#507D9C7A"), Padding = new Thickness(0), Margin = new Thickness(7, 0, 0, 0), Foreground = Brushes.White };
        }
        public void Display(Photo photo, BitmapSource image, int index, int total, bool animate)
        {
            current.BeginAnimation(UIElement.OpacityProperty, null);
            previous.Source = current.Source; current.Source = image; current.Opacity = 1;
            if (animate && settings.Fade && previous.Source != null)
            {
                DoubleAnimation fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(950)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
                fade.Completed += delegate { previous.Source = null; };
                current.BeginAnimation(UIElement.OpacityProperty, fade);
            }
            else previous.Source = null;
            title.Text = photo.Title; quote.Text = photo.Description;
            credit.Text = (photo.Category ?? "风景") + "  /  " + photo.Credit;
            number.Text = (index + 1).ToString("D2") + " / " + total.ToString("D2");
        }
        public void UpdateClock(DateTime now)
        {
            time.Text = now.ToString(settings.Hour12 ? "hh:mm tt" : "HH:mm", CultureInfo.InvariantCulture);
            date.Text = now.ToString("M 月 d 日  dddd", CultureInfo.GetCultureInfo("zh-CN")) + "   ·   " + LunarDate(now);
            // Shift overlays a few pixels each minute while keeping the layout calm.
            drift.X = (now.Minute * 7 % 13) - 6; drift.Y = (now.Minute * 3 % 9) - 4;
        }
        public void SetProgress(double value) { progress.Width = Math.Max(0, ActualWidth * Math.Max(0, Math.Min(1, value))); }
        public void SetPaused(bool paused) { PauseButton.Content = paused ? "▷" : "Ⅱ"; }
        public void SetFavorite(bool favorite) { FavoriteButton.Content = favorite ? "♥" : "♡"; }
        public static string LunarDate(DateTime day)
        {
            try
            {
                ChineseLunisolarCalendar lunar = new ChineseLunisolarCalendar();
                int year = lunar.GetYear(day), month = lunar.GetMonth(day), leap = lunar.GetLeapMonth(year), date = lunar.GetDayOfMonth(day);
                bool isLeap = leap > 0 && month == leap;
                if (leap > 0 && month >= leap) month--;
                string[] months = { "正", "二", "三", "四", "五", "六", "七", "八", "九", "十", "冬", "腊" };
                string[] digits = { "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };
                string dayText = date <= 10 ? "初" + digits[date - 1] : date < 20 ? "十" + digits[date - 11] : date == 20 ? "二十" : date < 30 ? "廿" + digits[date - 21] : "三十";
                return (isLeap ? "闰" : "") + months[month - 1] + "月" + dayText;
            }
            catch (ArgumentOutOfRangeException) { return day.ToString("yyyy 年"); }
        }
    }
}
