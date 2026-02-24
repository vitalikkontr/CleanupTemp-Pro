using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace CleanupTemp_Pro
{
    public enum DialogKind { Info, Success, Warning, Confirm, Error }

    public class StatRow
    {
        public string Label { get; set; } = "";
        public string Value { get; set; } = "";
        public string Color { get; set; } = "#4A9EFF";
    }

    public partial class CustomDialog : Window
    {
        public bool Result { get; private set; } = false;

        // Исходные цвета кнопки OK — запоминаем для корректного восстановления в Leave
        private Color _okBtnC1;
        private Color _okBtnC2;

        // Исходные цвета кнопки Отмена
        private static readonly Color CancelC1 = Color.FromRgb(0x29, 0x79, 0xFF);
        private static readonly Color CancelC2 = Color.FromRgb(0x15, 0x65, 0xC0);
        private static readonly Color CancelHoverC1 = Color.FromRgb(0x30, 0x88, 0xFF);
        private static readonly Color CancelHoverC2 = Color.FromRgb(0x20, 0x70, 0xEE);

        public CustomDialog(string title, string message,
                            DialogKind kind = DialogKind.Info,
                            List<StatRow>? stats = null,
                            bool showCancel = false)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            TitleText.Text   = title;
            MessageText.Text = message;

            // Перетаскивание окна
            MouseLeftButtonDown += (s, e) => {
                if (e.LeftButton == MouseButtonState.Pressed) DragMove();
            };

            // ── Цвета и иконка по типу диалога ──
            Color iconBg1, iconBg2;

            switch (kind)
            {
                case DialogKind.Success:
                    IconText.Text  = "✅";
                    iconBg1        = Color.FromRgb(0x0A, 0x3A, 0x2A);
                    iconBg2        = Color.FromRgb(0x1A, 0x4A, 0x3A);
                    _okBtnC1       = Color.FromRgb(0x00, 0xE6, 0x76);
                    _okBtnC2       = Color.FromRgb(0x00, 0xB0, 0xFF);
                    OkBtnText.Text = "Отлично!";
                    break;

                case DialogKind.Warning:
                    IconText.Text  = "⚠️";
                    iconBg1        = Color.FromRgb(0x3A, 0x2A, 0x00);
                    iconBg2        = Color.FromRgb(0x5A, 0x3A, 0x00);
                    _okBtnC1       = Color.FromRgb(0xFF, 0x8C, 0x00);
                    _okBtnC2       = Color.FromRgb(0xFF, 0xA5, 0x00);
                    OkBtnText.Text = "Понятно";
                    break;

                case DialogKind.Confirm:
                    IconText.Text  = "❓";
                    iconBg1        = Color.FromRgb(0x1A, 0x2A, 0x5A);
                    iconBg2        = Color.FromRgb(0x2A, 0x1A, 0x5A);
                    _okBtnC1       = Color.FromRgb(0xFF, 0x2E, 0x95);
                    _okBtnC2       = Color.FromRgb(0x9D, 0x37, 0xFF);
                    OkBtnText.Text = "Очистить";
                    break;

                case DialogKind.Error:
                    IconText.Text  = "❌";
                    iconBg1        = Color.FromRgb(0x3A, 0x0A, 0x15);
                    iconBg2        = Color.FromRgb(0x5A, 0x1A, 0x25);
                    _okBtnC1       = Color.FromRgb(0xFF, 0x3D, 0x00);
                    _okBtnC2       = Color.FromRgb(0xCC, 0x00, 0x44);
                    OkBtnText.Text = "Закрыть";
                    break;

                default: // Info
                    IconText.Text  = "ℹ️";
                    iconBg1        = Color.FromRgb(0x1A, 0x2A, 0x5A);
                    iconBg2        = Color.FromRgb(0x2A, 0x1A, 0x5A);
                    _okBtnC1       = Color.FromRgb(0x29, 0x79, 0xFF);
                    _okBtnC2       = Color.FromRgb(0xAA, 0x00, 0xFF);
                    OkBtnText.Text = "ОК";
                    break;
            }

            IconBorder.Background  = MakeGradient(iconBg1, iconBg2);
            OkBtnBorder.Background = MakeGradient(_okBtnC1, _okBtnC2);

            // ── Статистика ──
            if (stats is { Count: > 0 })
            {
                StatsPanel.Visibility = Visibility.Visible;
                foreach (var row in stats)
                {
                    var color = TryParseColor(row.Color, Color.FromRgb(0x4A, 0x9E, 0xFF));
                    var grid  = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var label = new System.Windows.Controls.TextBlock
                    {
                        Text              = row.Label,
                        FontSize          = 12,
                        Foreground        = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xBB)),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    var val = new System.Windows.Controls.TextBlock
                    {
                        Text              = row.Value,
                        FontSize          = 12,
                        FontWeight        = FontWeights.Bold,
                        Foreground        = new SolidColorBrush(color),
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    Grid.SetColumn(val, 1);
                    grid.Children.Add(label);
                    grid.Children.Add(val);
                    StatsStack.Children.Add(grid);
                }
            }

            // ── Видимость кнопок ──
            if (showCancel)
            {
                CancelBtnBorder.Visibility = Visibility.Visible;
                Grid.SetColumn(OkBtnBorder, 2);
                Grid.SetColumnSpan(OkBtnBorder, 1);
            }
            else
            {
                CancelBtnBorder.Visibility = Visibility.Collapsed;
                Grid.SetColumn(OkBtnBorder, 0);
                Grid.SetColumnSpan(OkBtnBorder, 3);
            }
        }

        // ── Вспомогательные методы ──────────────────────────────────────────

        private static LinearGradientBrush MakeGradient(Color c1, Color c2)
            => new(c1, c2, new Point(0, 0.5), new Point(1, 0.5));

        private static DropShadowEffect MakeGlow(Color color, double radius = 35, double opacity = 0.9)
            => new() { Color = color, BlurRadius = radius, ShadowDepth = 0, Opacity = opacity };

        /// <summary>Безопасно парсит цвет из строки. Возвращает fallback при ошибке.</summary>
        private static Color TryParseColor(string hex, Color fallback)
        {
            try   { return (Color)ColorConverter.ConvertFromString(hex)!; }
            catch { return fallback; }
        }

        // ── Кнопка OK ───────────────────────────────────────────────────────

        private void OkBorder_Click(object sender, MouseButtonEventArgs e)
        {
            Result = true;
            DialogResult = true;
            Close();
        }

        private void OkBorder_Enter(object sender, MouseEventArgs e)
        {
            OkBtnBorder.Background = new LinearGradientBrush(
                new GradientStopCollection
                {
                    new GradientStop(_okBtnC1, 0.0),
                    new GradientStop(Color.FromArgb(0xDD,
                        (byte)Math.Min(255, _okBtnC1.R + 50),
                        (byte)Math.Min(255, _okBtnC1.G + 50),
                        (byte)Math.Min(255, _okBtnC1.B + 50)), 0.5),
                    new GradientStop(_okBtnC2, 1.0)
                },
                new Point(0, 0.5), new Point(1, 0.5));

            OkBtnBorder.Effect = MakeGlow(_okBtnC1);
        }

        private void OkBorder_Leave(object sender, MouseEventArgs e)
        {
            OkBtnBorder.Background = MakeGradient(_okBtnC1, _okBtnC2);
            OkBtnBorder.Effect     = null;
        }

        // ── Кнопка Отмена ───────────────────────────────────────────────────

        private void CancelBorder_Click(object sender, MouseButtonEventArgs e)
        {
            Result = false;
            DialogResult = false;
            Close();
        }

        private void CancelBorder_Enter(object sender, MouseEventArgs e)
        {
            CancelBtnBorder.Background   = MakeGradient(CancelHoverC1, CancelHoverC2);
            CancelBtnBorder.BorderBrush  = new SolidColorBrush(Color.FromArgb(0x80, 0x60, 0xA0, 0xFF));
            CancelBtnBorder.BorderThickness = new Thickness(1);
            CancelBtnBorder.Effect       = MakeGlow(CancelC1, radius: 18, opacity: 0.7);
        }

        private void CancelBorder_Leave(object sender, MouseEventArgs e)
        {
            CancelBtnBorder.Background      = MakeGradient(CancelC1, CancelC2);
            CancelBtnBorder.BorderBrush     = new SolidColorBrush(Color.FromArgb(0x40, 0x4A, 0x9E, 0xFF));
            CancelBtnBorder.BorderThickness = new Thickness(1);
            CancelBtnBorder.Effect          = null;
        }
    }
}
