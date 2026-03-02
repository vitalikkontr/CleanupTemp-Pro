using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

// Алиасы: разрешаем конфликты WPF ↔ WinForms (UseWindowsForms=true в .csproj)
using Color          = System.Windows.Media.Color;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Brushes        = System.Windows.Media.Brushes;

namespace CleanupTemp_Pro
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            LoadLogo();
        }

        private void LoadLogo()
        {
            var bmp = TryLoad(new Uri("pack://application:,,,/app_icon.png", UriKind.Absolute));

            if (bmp == null)
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                foreach (var name in new[] { "app_icon.png", "CleanupTempPro_Logo.png",
                                             "Cleanup.png", "logo.png" })
                {
                    string p = Path.Combine(dir, name);
                    if (File.Exists(p))
                    {
                        bmp = TryLoad(new Uri(p, UriKind.Absolute));
                        if (bmp != null) break;
                    }
                }
            }

            if (bmp != null) LogoImage.Source = bmp;
        }

        private static BitmapImage? TryLoad(Uri uri)
        {
            try
            {
                var b = new BitmapImage();
                b.BeginInit();
                b.UriSource   = uri;
                b.CacheOption = BitmapCacheOption.OnLoad;
                b.EndInit();
                b.Freeze();
                return b;
            }
            catch (Exception ex)
            {
                AppLog.Warn($"AboutWindow.TryLoad failed: {uri} | {ex.Message}");
                return null;
            }
        }

        private void CloseBorder_Click(object sender, MouseButtonEventArgs e) => Close();

        // ── Shimmer / Glow при наведении ─────────────────────────────

        private DoubleAnimation? _shimmerAnim;
        private DoubleAnimation? _shimmerAnim2;
        private DoubleAnimation? _logoGlowAnim;

        private void Title_MouseEnter(object sender, MouseEventArgs e)
        {
            // Shimmer на «CleanupTemp»
            var b1 = new LinearGradientBrush();
            b1.StartPoint = new System.Windows.Point(0, 0.5);
            b1.EndPoint   = new System.Windows.Point(1, 0.5);
            b1.GradientStops.Add(new GradientStop(Color.FromRgb(0xE8, 0xE8, 0xFF), 0.0));
            b1.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xFF, 0xFF), 0.5));
            b1.GradientStops.Add(new GradientStop(Color.FromRgb(0xA0, 0xC8, 0xFF), 1.0));
            AboutRunCleanup.Foreground = b1;

            _shimmerAnim = new DoubleAnimation(-0.5, 1.5, TimeSpan.FromMilliseconds(950))
            {
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            b1.GradientStops[1].BeginAnimation(GradientStop.OffsetProperty, _shimmerAnim);

            // Shimmer на «Pro» — радужный переход
            var b2 = new LinearGradientBrush();
            b2.StartPoint = new System.Windows.Point(0, 0.5);
            b2.EndPoint   = new System.Windows.Point(1, 0.5);
            b2.GradientStops.Add(new GradientStop(Color.FromRgb(0x5A, 0xBA, 0xFF), 0.0));
            b2.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xFF, 0xFF), 0.5));
            b2.GradientStops.Add(new GradientStop(Color.FromRgb(0x00, 0xE5, 0xD0), 1.0));
            AboutRunPro.Foreground = b2;

            _shimmerAnim2 = new DoubleAnimation(-0.5, 1.5, TimeSpan.FromMilliseconds(950))
            {
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime      = TimeSpan.FromMilliseconds(200),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            b2.GradientStops[1].BeginAnimation(GradientStop.OffsetProperty, _shimmerAnim2);
        }

        private void Title_MouseLeave(object sender, MouseEventArgs e)
        {
            _shimmerAnim?.SetValue(Timeline.BeginTimeProperty, null);
            _shimmerAnim2?.SetValue(Timeline.BeginTimeProperty, null);

            AboutRunCleanup.Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xFF));

            var origPro = new LinearGradientBrush();
            origPro.StartPoint = new System.Windows.Point(0, 0);
            origPro.EndPoint   = new System.Windows.Point(1, 0);
            origPro.GradientStops.Add(new GradientStop(Color.FromRgb(0x5A, 0xBA, 0xFF), 0.0));
            origPro.GradientStops.Add(new GradientStop(Color.FromRgb(0xB0, 0x6E, 0xFF), 0.5));
            origPro.GradientStops.Add(new GradientStop(Color.FromRgb(0x00, 0xE5, 0xD0), 1.0));
            origPro.Freeze();
            AboutRunPro.Foreground = origPro;
        }

        private void Logo_MouseEnter(object sender, MouseEventArgs e)
        {
            // Пульсирующий glow на иконке
            _logoGlowAnim = new DoubleAnimation(10, 28, TimeSpan.FromMilliseconds(700))
            {
                AutoReverse    = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            var glow = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color       = Color.FromRgb(0x4A, 0x9E, 0xFF),
                ShadowDepth = 0,
                Opacity     = 0.9,
                BlurRadius  = 10,
            };
            LogoImage.Effect = glow;
            glow.BeginAnimation(
                System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty,
                _logoGlowAnim);

            // Анимируем вращение градиента рамки — меняем цвет первого стопа
            if (LogoBorder.BorderBrush is LinearGradientBrush borderBrush)
            {
                var colorAnim = new ColorAnimation(
                    Color.FromRgb(0x00, 0xE5, 0xD0),
                    Color.FromRgb(0x4A, 0x9E, 0xFF),
                    TimeSpan.FromMilliseconds(800))
                {
                    AutoReverse    = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                };
                borderBrush.GradientStops[0].BeginAnimation(
                    GradientStop.ColorProperty, colorAnim);
            }
        }

        private void Logo_MouseLeave(object sender, MouseEventArgs e)
        {
            LogoImage.Effect = null;

            // Сброс анимации рамки
            if (LogoBorder.BorderBrush is LinearGradientBrush borderBrush)
                borderBrush.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, null);
        }
        private void CloseBorder_Enter(object sender, MouseEventArgs e)
        {
            CloseBtnBorder.Background = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0x4A, 0x6A));
            CloseBtnBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x6A, 0x85));
            CloseBtnBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Color.FromRgb(0xFF, 0x4A, 0x6A),
                BlurRadius = 28, ShadowDepth = 0, Opacity = 1.0
            };
        }
        private void CloseBorder_Leave(object sender, MouseEventArgs e)
        {
            CloseBtnBorder.Background = Brushes.Transparent;
            CloseBtnBorder.BorderBrush = Brushes.Transparent;
            CloseBtnBorder.Effect = null;
        }
    }
}
