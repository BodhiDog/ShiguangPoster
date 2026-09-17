using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace Shiguang
{
    public static class Ui
    {
        public static T Resource<T>(string name)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Shiguang." + name)) return (T)XamlReader.Load(stream);
        }
        public static SolidColorBrush Brush(string hex)
        {
            SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); brush.Freeze(); return brush;
        }
        public static T Find<T>(Window window, string name) where T : class { return window.FindName(name) as T; }
        public static TextBlock Text(string text, double size, string color)
        {
            return new TextBlock { Text = text, FontSize = size, Foreground = Brush(color), TextWrapping = TextWrapping.Wrap };
        }
        public static Button Button(string text, bool primary)
        {
            Button button = new Button { Content = text };
            if (primary) button.SetResourceReference(FrameworkElement.StyleProperty, "PrimaryButton");
            return button;
        }
        public static void Chrome(Window window)
        {
            WindowChrome.SetWindowChrome(window, new WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new Thickness(6), CornerRadius = new CornerRadius(0), GlassFrameThickness = new Thickness(0), UseAeroCaptionButtons = false });
            DrawingGroup drawing = new DrawingGroup();
            drawing.Children.Add(new GeometryDrawing(Brush("#B8D7B4"), null, new RectangleGeometry(new Rect(0, 0, 32, 32), 8, 8)));
            drawing.Children.Add(new GeometryDrawing(null, new Pen(Brush("#243E2A"), 1.8), Geometry.Parse("M7,23 L7,10 Q12,7 16,11 Q20,7 25,10 L25,23 Q20,20 16,24 Q12,20 7,23 M16,11 L16,24")));
            drawing.Freeze(); window.Icon = new DrawingImage(drawing);
        }
        public static void Snapshot(FrameworkElement element, string path, int width, int height)
        {
            element.Measure(new Size(width, height)); element.Arrange(new Rect(0, 0, width, height)); element.UpdateLayout();
            RenderTargetBitmap render = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); render.Render(element);
            PngBitmapEncoder encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(render));
            using (FileStream output = File.Create(path)) encoder.Save(output);
        }
    }
}
