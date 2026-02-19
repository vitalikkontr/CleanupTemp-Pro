using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
            catch { return null; }
        }

        private void CloseBorder_Click(object sender, MouseButtonEventArgs e) => Close();
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
