using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Threading;

// NotifyIcon — единственный класс из WinForms в проекте.
// Нативный WPF-аналог отсутствует в .NET 8.
using WinForms = System.Windows.Forms;
using Application    = System.Windows.Application;
// Font(string, float, FontStyle) использует System.Drawing.FontStyle — фиксируем явно
using FontStyle      = System.Drawing.FontStyle;

namespace CleanupTemp_Pro
{
    public partial class App : Application
    {
        // ── Трей ──────────────────────────────────────────────────────
        private WinForms.NotifyIcon?       _trayIcon;
        private WinForms.ToolStripMenuItem? _trayStatusItem;
        private WinForms.ToolStripMenuItem? _trayScanItem;

        public static App Instance => (App)Current;

        // ── Жизненный цикл ────────────────────────────────────────────

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            BuildTrayIcon();
            // StartupUri убран — создаём окно вручную
            var win = new MainWindow();
            MainWindow = win;
            win.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            base.OnExit(e);
        }

        // ── Построение иконки ─────────────────────────────────────────

        private void BuildTrayIcon()
        {
            Icon? icon = TryLoadIcon();

            _trayStatusItem = new WinForms.ToolStripMenuItem("Готов к работе")
            {
                Enabled   = false,
                ForeColor = System.Drawing.Color.FromArgb(80, 200, 160),
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            };

            _trayScanItem = new WinForms.ToolStripMenuItem("🔍  Сканировать сейчас")
            {
                Font = new Font("Segoe UI", 9f),
            };
            _trayScanItem.Click += (_, _) =>
            {
                ShowMainWindow();
                if (MainWindow is MainWindow mw)
                    mw.TriggerScanFromTray();
            };

            var showItem = new WinForms.ToolStripMenuItem("🖥️  Открыть CleanupTemp Pro")
            {
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            };
            showItem.Click += (_, _) => ShowMainWindow();

            var exitItem = new WinForms.ToolStripMenuItem("✕  Выход")
            {
                Font      = new Font("Segoe UI", 9f),
                ForeColor = System.Drawing.Color.FromArgb(220, 80, 80),
            };
            exitItem.Click += (_, _) =>
            {
                MainWindow?.Close();
                _trayIcon?.Dispose();
                Shutdown();
            };

            var menu = new WinForms.ContextMenuStrip
            {
                BackColor  = System.Drawing.Color.FromArgb(18, 18, 36),
                ForeColor  = System.Drawing.Color.FromArgb(220, 220, 255),
                Font       = new Font("Segoe UI", 9f),
                RenderMode = WinForms.ToolStripRenderMode.System,
            };
            menu.Items.AddRange(new WinForms.ToolStripItem[]
            {
                _trayStatusItem,
                new WinForms.ToolStripSeparator(),
                showItem,
                _trayScanItem,
                new WinForms.ToolStripSeparator(),
                exitItem,
            });

            _trayIcon = new WinForms.NotifyIcon
            {
                Icon             = icon ?? SystemIcons.Application,
                Text             = "CleanupTemp Pro",
                ContextMenuStrip = menu,
                Visible          = true,
            };

            _trayIcon.DoubleClick  += (_, _) => ShowMainWindow();
            _trayIcon.MouseClick   += (_, e) =>
            {
                if (e.Button == WinForms.MouseButtons.Left)
                    ShowMainWindow();
            };
        }

        private static Icon? TryLoadIcon()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/app.ico", UriKind.Absolute);
                var info = GetResourceStream(uri);
                if (info != null)
                {
                    using var stream = info.Stream;
                    // Icon копирует данные из потока в свой буфер — поток можно закрывать сразу
                    return new Icon(stream);
                }
            }
            catch { }

            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(path))
            {
                try { return new Icon(path); }
                catch { }
            }
            return null;
        }

        // ── Показ окна ────────────────────────────────────────────────

        public void ShowMainWindow()
        {
            Dispatcher.Invoke(() =>
            {
                if (MainWindow == null) return;
                MainWindow.Show();
                if (MainWindow.WindowState == WindowState.Minimized)
                    MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
                MainWindow.Topmost = true;
                MainWindow.Topmost = false;
                MainWindow.Focus();
            });
        }

        // ── Публичное API для MainWindow ──────────────────────────────

        /// <summary>
        /// Обновляет иконку и тултип трея по текущему состоянию.
        /// Потокобезопасен: вызывается из любого потока.
        /// </summary>
        public void UpdateTrayStatus(TrayStatus status, string? detail = null)
        {
            if (_trayIcon == null || _trayStatusItem == null) return;
            Dispatcher.BeginInvoke(() =>
            {
                switch (status)
                {
                    case TrayStatus.Ready:
                        _trayStatusItem.Text      = "Готов к работе";
                        _trayStatusItem.ForeColor = System.Drawing.Color.FromArgb(80, 200, 160);
                        _trayScanItem!.Enabled    = true;
                        _trayIcon.Text            = "CleanupTemp Pro";
                        break;

                    case TrayStatus.Scanning:
                        _trayStatusItem.Text      = "Сканирование...";
                        _trayStatusItem.ForeColor = System.Drawing.Color.FromArgb(74, 158, 255);
                        _trayScanItem!.Enabled    = false;
                        _trayIcon.Text            = "CleanupTemp Pro — сканирование";
                        break;

                    case TrayStatus.Cleaning:
                        _trayStatusItem.Text      = "Очистка...";
                        _trayStatusItem.ForeColor = System.Drawing.Color.FromArgb(255, 140, 0);
                        _trayScanItem!.Enabled    = false;
                        _trayIcon.Text            = "CleanupTemp Pro — очистка";
                        break;

                    case TrayStatus.Done:
                        string freed = detail ?? "";
                        _trayStatusItem.Text      = freed.Length > 0 ? $"Очищено: {freed}" : "Готов";
                        _trayStatusItem.ForeColor = System.Drawing.Color.FromArgb(6, 214, 199);
                        _trayScanItem!.Enabled    = true;
                        _trayIcon.Text            = freed.Length > 0
                            ? $"CleanupTemp Pro — освобождено {freed}"
                            : "CleanupTemp Pro";
                        // Balloon только если окно скрыто — не спамим поверх открытого UI
                        if (MainWindow?.IsVisible == false)
                            ShowBalloon("Очистка завершена!", $"Освобождено {freed}");
                        break;

                    case TrayStatus.Stopped:
                        _trayStatusItem.Text      = "Остановлено";
                        _trayStatusItem.ForeColor = System.Drawing.Color.FromArgb(136, 136, 187);
                        _trayScanItem!.Enabled    = true;
                        _trayIcon.Text            = "CleanupTemp Pro";
                        break;
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>Balloon-уведомление из системного трея.</summary>
        public void ShowBalloon(string title, string text, int durationMs = 4000)
        {
            if (_trayIcon == null) return;
            Dispatcher.BeginInvoke(() =>
                _trayIcon.ShowBalloonTip(durationMs, title, text, WinForms.ToolTipIcon.Info));
        }
    }

    /// <summary>Статусы для синхронизации между MainWindow и треем.</summary>
    public enum TrayStatus { Ready, Scanning, Cleaning, Done, Stopped }
}
