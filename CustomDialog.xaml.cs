using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

        // Запоминаем цвета кнопки OK для корректного восстановления в OkBorder_Leave
        private Color _okBtnC1;
        private Color _okBtnC2;

        public CustomDialog(string title, string message,
                            DialogKind kind = DialogKind.Info,
                            List<StatRow>? stats = null,
                            bool showCancel = false)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            TitleText.Text = title;
            MessageText.Text = message;

            // Позволяет перетаскивать окно мышкой
            this.MouseLeftButtonDown += (s, e) => {
                if (e.LeftButton == MouseButtonState.Pressed) DragMove();
            };

            // ── Цвета и иконка ──
            Color iconBg1, iconBg2, btnC1, btnC2;

            switch (kind)
            {
                case DialogKind.Success:
                    IconText.Text = "✅";
                    iconBg1 = Color.FromRgb(0x0A, 0x3A, 0x2A);
                    iconBg2 = Color.FromRgb(0x1A, 0x4A, 0x3A);
                    btnC1 = Color.FromRgb(0x00, 0xE6, 0x76);
                    btnC2 = Color.FromRgb(0x00, 0xB0, 0xFF);
                    OkBtnText.Text = "Отлично!";
                    break;
                case DialogKind.Warning:
                    IconText.Text = "⚠️";
                    iconBg1 = Color.FromRgb(0x3A, 0x2A, 0x00);
                    iconBg2 = Color.FromRgb(0x5A, 0x3A, 0x00);
                    btnC1 = Color.FromRgb(0xFF, 0x8C, 0x00);
                    btnC2 = Color.FromRgb(0xFF, 0xA5, 0x00);
                    OkBtnText.Text = "Понятно";
                    break;
                case DialogKind.Confirm:
                    IconText.Text = "❓";
                    iconBg1 = Color.FromRgb(0x1A, 0x2A, 0x5A);
                    iconBg2 = Color.FromRgb(0x2A, 0x1A, 0x5A);
                    btnC1 = Color.FromRgb(0xFF, 0x2E, 0x95);
                    btnC2 = Color.FromRgb(0x9D, 0x37, 0xFF);
                    OkBtnText.Text = "Очистить";
                    break;
                case DialogKind.Error:
                    IconText.Text = "❌";
                    iconBg1 = Color.FromRgb(0x3A, 0x0A, 0x15);
                    iconBg2 = Color.FromRgb(0x5A, 0x1A, 0x25);
                    btnC1 = Color.FromRgb(0xFF, 0x3D, 0x00);
                    btnC2 = Color.FromRgb(0xCC, 0x00, 0x44);
                    OkBtnText.Text = "Закрыть";
                    break;
                default:
                    IconText.Text = "ℹ️";
                    iconBg1 = Color.FromRgb(0x1A, 0x2A, 0x5A);
                    iconBg2 = Color.FromRgb(0x2A, 0x1A, 0x5A);
                    btnC1 = Color.FromRgb(0x29, 0x79, 0xFF);
                    btnC2 = Color.FromRgb(0xAA, 0x00, 0xFF);
                    OkBtnText.Text = "ОК";
                    break;
            }

            IconBorder.Background = new LinearGradientBrush(iconBg1, iconBg2, 45);
            _okBtnC1 = btnC1;
            _okBtnC2 = btnC2;
            OkBtnBorder.Background = new LinearGradientBrush(btnC1, btnC2, new Point(0, 0.5), new Point(1, 0.5));

            // ── Статистика ──
            if (stats != null && stats.Count > 0)
            {
                StatsPanel.Visibility = Visibility.Visible;
                foreach (var row in stats)
                {
                    var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                    grid.ColumnDefinitions.Add(new ColumnDefinition());
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var label = new TextBlock { Text = row.Label, FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xBB)), VerticalAlignment = VerticalAlignment.Center };
                    var brush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(row.Color)!
                    );

                    var val = new TextBlock
                    {
                        Text = row.Value,
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = brush,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(val, 1);
                    grid.Children.Add(label); grid.Children.Add(val);
                    StatsStack.Children.Add(grid);
                }
            }

            // ── Видимость кнопок ──
            if (showCancel)
            {
                CancelBtnBorder.Visibility = Visibility.Visible;
                Grid.SetColumn(OkBtnBorder, 2); Grid.SetColumnSpan(OkBtnBorder, 1);
            }
            else
            {
                CancelBtnBorder.Visibility = Visibility.Collapsed;
                Grid.SetColumn(OkBtnBorder, 0); Grid.SetColumnSpan(OkBtnBorder, 3);
            }
        }

        // --- Обработчики кнопок ---
        private void OkBorder_Click(object sender, MouseButtonEventArgs e) { Result = true; this.DialogResult = true; Close(); }
        private void OkBorder_Enter(object sender, MouseEventArgs e)
        {
            OkBtnBorder.Opacity = 1.0;
            // Осветляем на основе исходного цвета кнопки (зависит от kind)
            // Добавляем белый стоп посередине для эффекта "glow"
            OkBtnBorder.Background = new LinearGradientBrush(
                new GradientStopCollection {
                    new GradientStop(_okBtnC1, 0.0),
                    new GradientStop(Color.FromArgb(0xDD,
                        (byte)Math.Min(255, _okBtnC1.R + 50),
                        (byte)Math.Min(255, _okBtnC1.G + 50),
                        (byte)Math.Min(255, _okBtnC1.B + 50)), 0.5),
                    new GradientStop(_okBtnC2, 1.0)
                },
                new Point(0, 0.5), new Point(1, 0.5));
            OkBtnBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = _okBtnC1,
                BlurRadius = 35, ShadowDepth = 0, Opacity = 0.9
            };
        }
        private void OkBorder_Leave(object sender, MouseEventArgs e)
        {
            OkBtnBorder.Opacity = 1.0;
            // Восстанавливаем исходный цвет кнопки, запомненный в конструкторе по kind
            OkBtnBorder.Background = new LinearGradientBrush(
                _okBtnC1, _okBtnC2, new Point(0, 0.5), new Point(1, 0.5));
            OkBtnBorder.Effect = null;
        }

        private void CancelBorder_Click(object sender, MouseButtonEventArgs e) { Result = false; this.DialogResult = false; Close(); }

        // ЭФФЕКТ ДЛЯ КНОПКИ ОТМЕНА
        private void CancelBorder_Enter(object sender, MouseEventArgs e)
        {
            CancelBtnBorder.Opacity = 1.0;
            CancelBtnBorder.Background = new LinearGradientBrush(
                new GradientStopCollection {
                    new GradientStop(Color.FromRgb(0x30, 0x88, 0xFF), 0.0),
                    new GradientStop(Color.FromRgb(0x20, 0x70, 0xEE), 1.0)
                },
                new Point(0, 0.5), new Point(1, 0.5));
            CancelBtnBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(0x80, 0x60, 0xA0, 0xFF));
            CancelBtnBorder.BorderThickness = new System.Windows.Thickness(1);
            CancelBtnBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Color.FromRgb(0x30, 0x80, 0xFF),
                BlurRadius = 18, ShadowDepth = 0, Opacity = 0.7
            };
        }
        private void CancelBorder_Leave(object sender, MouseEventArgs e)
        {
            CancelBtnBorder.Opacity = 1.0;
            CancelBtnBorder.Background = new LinearGradientBrush(
                new GradientStopCollection {
                    new GradientStop(Color.FromRgb(0x29, 0x79, 0xFF), 0.0),
                    new GradientStop(Color.FromRgb(0x15, 0x65, 0xC0), 1.0)
                },
                new Point(0, 0.5), new Point(1, 0.5));
            CancelBtnBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00));
            CancelBtnBorder.BorderThickness = new System.Windows.Thickness(0);
            CancelBtnBorder.Effect = null;
        }
    }
}
