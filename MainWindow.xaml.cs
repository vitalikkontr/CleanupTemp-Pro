using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Threading;

namespace CleanupTemp_Pro
{
    public class FileItem
    {
        public string Icon      { get; set; } = "📄";
        public string Path      { get; set; } = "";
        public string Category  { get; set; } = "";
        public long   SizeBytes { get; set; }
        public string SizeText  => SizeHelper.Format(SizeBytes);
    }

    public class HistoryItem
    {
        public string Date      { get; set; } = "";
        public string Freed     { get; set; } = "";
        public string FileCount { get; set; } = "";
        public string Icon      { get; set; } = "✅";
    }

    public static class SizeHelper
    {
        public static string Format(long b)
        {
            if (b < 1024)                 return $"{b} Б";
            if (b < 1024 * 1024)         return $"{b / 1024.0:F1} КБ";
            if (b < 1024L * 1024 * 1024) return $"{b / (1024.0 * 1024):F1} МБ";
            return $"{b / (1024.0 * 1024 * 1024):F2} ГБ";
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SHQUERYRBINFO { public int cbSize; public long i64Size; public long i64NumItems; }

    public partial class MainWindow : Window
    {
        [DllImport("shell32.dll")]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string? root, uint flags);
        [DllImport("shell32.dll")]
        static extern int SHQueryRecycleBin(string? root, ref SHQUERYRBINFO info);

        private CancellationTokenSource? _cts;
        private readonly ObservableCollection<FileItem>    _fileItems    = new();
        private readonly ObservableCollection<HistoryItem> _historyItems = new();

        private long _totalFoundBytes;
        private long _cleanedBytes;
        private bool _isRunning;
        private bool _canClean;
        private bool _canStop;
        private int  _statTemp, _statBrowser, _statRecycle;
        private DispatcherTimer? _pulseTimer;
        private bool _showingHistory;
        // Флаг: операция была прервана уходом системы в сон
        // (нужен т.к. к моменту пробуждения Task.Run уже мог сбросить _isRunning)
        private volatile bool _wasInterruptedBySleep;

        // ── ЗАЩИЩЁННЫЕ ПАПКИ ─────────────────────────────────────────
        private static readonly HashSet<string> _protectedFolderNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ViberPC", "Viber", "Telegram Desktop", "Telegram",
            "WhatsApp", "Signal", "Skype", "Discord",
            "Slack", "Teams", "Element",
            "Thunderbird", "Outlook",
            "Dropbox", "OneDrive", "Google Drive", "Yandex.Disk",
            "Steam", "Epic Games", "GOG Galaxy", "Battle.net",
            "Documents", "Документы", "Мои документы",
            "Downloads", "Загрузки",
            "Pictures", "Изображения", "Мои рисунки",
            "Videos", "Видео", "Мои видеозаписи",
            "Music", "Музыка", "Моя музыка",
            "Desktop", "Рабочий стол",
            "UnsavedFiles",
        };

        private static readonly HashSet<string> _junkExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".tmp", ".bak", ".old", ".dmp", ".chk", ".gid",
            ".fts", ".ftg", ".wbk", ".xlk", ".~doc", ".~xls", ".~ppt", ".temp"
        };

        private static readonly HashSet<string> _junkFileNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "thumbs.db", "desktop.ini", "ehthumbs.db", "ehthumbs_vista.db", ".ds_store"
        };

        private static bool IsInProtectedFolder(string filePath)
        {
            var parts = filePath.Split(System.IO.Path.DirectorySeparatorChar,
                                       System.IO.Path.AltDirectorySeparatorChar);
            foreach (var part in parts)
                if (_protectedFolderNames.Contains(part))
                    return true;
            return false;
        }

        // ── НАСТРОЙКИ ────────────────────────────────────────────────────
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CleanupTempPro", "settings.json");

        private class AppSettings
        {
            public bool TempFiles      { get; set; } = true;
            public bool WinTemp        { get; set; } = true;
            public bool RecycleBin     { get; set; } = true;
            public bool BrowserCache   { get; set; } = true;
            public bool Thumbnails     { get; set; } = true;
            public bool DnsCache       { get; set; } = true;
            public bool MSOffice       { get; set; } = false;
            public bool Prefetch       { get; set; } = false;
            public bool EventLogs      { get; set; } = false;
            public bool ExternalDrives { get; set; } = false;
        }

        private void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                var s = new AppSettings
                {
                    TempFiles      = ChkTempFiles?.IsChecked      == true,
                    WinTemp        = ChkWinTemp?.IsChecked        == true,
                    RecycleBin     = ChkRecycleBin?.IsChecked     == true,
                    BrowserCache   = ChkBrowserCache?.IsChecked   == true,
                    Thumbnails     = ChkThumbnails?.IsChecked     == true,
                    DnsCache       = ChkDnsCache?.IsChecked       == true,
                    MSOffice       = ChkMSOffice?.IsChecked       == true,
                    Prefetch       = ChkPrefetch?.IsChecked       == true,
                    EventLogs      = ChkEventLogs?.IsChecked      == true,
                    ExternalDrives = ChkExternalDrives?.IsChecked == true,
                };
                File.WriteAllText(SettingsPath,
                    JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private bool _settingsLoaded = false;

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return;
                var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
                if (s == null) return;
                if (ChkTempFiles    != null) ChkTempFiles.IsChecked    = s.TempFiles;
                if (ChkWinTemp      != null) ChkWinTemp.IsChecked      = s.WinTemp;
                if (ChkRecycleBin   != null) ChkRecycleBin.IsChecked   = s.RecycleBin;
                if (ChkBrowserCache != null) ChkBrowserCache.IsChecked = s.BrowserCache;
                if (ChkThumbnails   != null) ChkThumbnails.IsChecked   = s.Thumbnails;
                if (ChkDnsCache     != null) ChkDnsCache.IsChecked     = s.DnsCache;
                if (ChkMSOffice     != null) ChkMSOffice.IsChecked     = s.MSOffice;
                if (ChkPrefetch     != null) ChkPrefetch.IsChecked     = s.Prefetch;
                if (ChkEventLogs    != null) ChkEventLogs.IsChecked    = s.EventLogs;
                if (ChkExternalDrives != null) ChkExternalDrives.IsChecked = s.ExternalDrives;
            }
            catch { }
            finally { _settingsLoaded = true; }
        }

        private void Chk_Changed(object sender, RoutedEventArgs e)
        {
            if (_settingsLoaded) SaveSettings();
        }

        // ── USB HOTPLUG ───────────────────────────────────────────────────
        private HwndSource? _hwndSource;

        public MainWindow()
        {
            InitializeComponent();
            FileListView.ItemsSource    = _fileItems;
            HistoryListView.ItemsSource = _historyItems;
            LoadLogo();
            LoadDiskInfo();
            LoadSettings();
            SetStatus("Готов к работе", StatusKind.Ready);
            StartPulse();
            SourceInitialized += (_, _) => InitUsbDetection();
            Closing += (_, _) =>
            {
                SaveSettings();
                _hwndSource?.RemoveHook(WndProc);
                _hwndSource?.Dispose();
            };
        }

        private const int WM_DEVICECHANGE          = 0x0219;
        private const int DBT_DEVICEARRIVAL        = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVTYP_VOLUME        = 0x0002;
        private const int DBTF_MEDIA               = 0x0001;
        private const int DBTF_NET                 = 0x0002;

        // ── SLEEP / WAKE ──────────────────────────────────────────────────
        private const int WM_POWERBROADCAST        = 0x0218;
        private const int PBT_APMSUSPEND           = 0x0004; // система уходит в сон
        private const int PBT_APMRESUMESUSPEND     = 0x0007; // пробуждение после сна
        private const int PBT_APMRESUMEAUTOMATIC   = 0x0012; // пробуждение (автоматическое/по таймеру)

        [StructLayout(LayoutKind.Sequential)]
        private struct DEV_BROADCAST_VOLUME
        {
            public int   dbcv_size;
            public int   dbcv_devicetype;
            public int   dbcv_reserved;
            public int   dbcv_unitmask;
            public short dbcv_flags;
        }

        private void InitUsbDetection()
        {
            var helper = new WindowInteropHelper(this);
            _hwndSource = HwndSource.FromHwnd(helper.Handle);
            _hwndSource?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // ── Сон / Пробуждение ─────────────────────────────────────────
            if (msg == WM_POWERBROADCAST)
            {
                int ev = wParam.ToInt32();

                if (ev == PBT_APMSUSPEND)
                {
                    // Система уходит в сон — отменяем текущую операцию
                    // чтобы после пробуждения не было подвисшего состояния UI
                    OnSystemSleep();
                }
                else if (ev == PBT_APMRESUMESUSPEND || ev == PBT_APMRESUMEAUTOMATIC)
                {
                    // Система проснулась — восстанавливаем UI
                    OnSystemWake();
                }

                return IntPtr.Zero;
            }

            // ── USB Hotplug ───────────────────────────────────────────────
            if (msg != WM_DEVICECHANGE) return IntPtr.Zero;

            int devEv = wParam.ToInt32();
            if (devEv != DBT_DEVICEARRIVAL && devEv != DBT_DEVICEREMOVECOMPLETE)
                return IntPtr.Zero;

            if (lParam == IntPtr.Zero) return IntPtr.Zero;

            var vol = Marshal.PtrToStructure<DEV_BROADCAST_VOLUME>(lParam);
            if (vol.dbcv_devicetype != DBT_DEVTYP_VOLUME) return IntPtr.Zero;
            if ((vol.dbcv_flags & DBTF_MEDIA) != 0)       return IntPtr.Zero;
            if ((vol.dbcv_flags & DBTF_NET)   != 0)       return IntPtr.Zero;

            bool arrival = (devEv == DBT_DEVICEARRIVAL);
            for (int i = 0; i < 26; i++)
                if ((vol.dbcv_unitmask & (1 << i)) != 0)
                    OnUsbDriveChanged((char)('A' + i), arrival);

            return IntPtr.Zero;
        }

        /// <summary>
        /// Вызывается когда система уходит в сон.
        /// Отменяем текущий токен — это корректно прервёт Task.Run.
        /// </summary>
        private void OnSystemSleep()
        {
            if (_isRunning)
            {
                _wasInterruptedBySleep = true;
                _cts?.Cancel();
            }
        }

        /// <summary>
        /// Вызывается после пробуждения из сна.
        /// Гарантируем что UI в корректном состоянии — кнопки разблокированы,
        /// статус сброшен, никакой "висящей" операции нет.
        /// </summary>
        private void OnSystemWake()
        {
            // Небольшая задержка — дать Windows полностью проснуться
            // прежде чем трогать UI и диски
            var wakeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
            wakeTimer.Tick += (_, _) =>
            {
                wakeTimer.Stop();

                // Проверяем флаг прерывания — _isRunning мог уже быть сброшен
                // в блоке finally Task.Run к этому моменту
                if (_wasInterruptedBySleep)
                {
                    _wasInterruptedBySleep = false;
                    // На случай если Task.Run ещё не успел завершить finally
                    _isRunning = false;
                    long freed = _cleanedBytes;
                    _cleanedBytes    = 0;
                    _totalFoundBytes = 0;
                    _statTemp = _statBrowser = _statRecycle = 0;
                    StatTempFiles.Text    = "0";
                    StatBrowserFiles.Text = "0";
                    StatRecycleBin.Text   = "0";
                    TotalSizeText.Text    = "0 МБ";
                    FileCountText.Text    = "0 файлов";
                    SetUiRunning(false, false);
                    SetProgress(0, "Операция прервана — система ушла в сон");
                    SetStatus("⏸ Прервано (сон ПК) — нажмите «Сканировать» снова", StatusKind.Stopped);
                    ListCountLabel.Text = freed > 0 ? $"Успело освободиться: {SizeHelper.Format(freed)}" : "";
                }
                else
                {
                    // Операция не шла — просто обновляем статус
                    SetStatus("Готов к работе", StatusKind.Ready);
                }

                // В любом случае обновляем инфо о дисках — после сна они могут измениться
                LoadDiskInfo();
            };
            wakeTimer.Start();
        }

        private void OnUsbDriveChanged(char letter, bool arrived)
        {
            if (arrived)
            {
                var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
                t.Tick += (_, _) =>
                {
                    t.Stop();
                    LoadDiskInfo();
                    SetStatus(ChkExternalDrives?.IsChecked == true
                        ? $"💾  Флешка {letter}: подключена — нажмите «Сканировать»"
                        : $"💾  Флешка {letter}: подключена", StatusKind.Ready);
                };
                t.Start();
            }
            else
            {
                LoadDiskInfo();
                SetStatus($"📤  Диск {letter}: отключён", StatusKind.Stopped);
            }
        }

        // ── LOGO ─────────────────────────────────────────────────────────
        private void LoadLogo()
        {
            BitmapImage? bmp = TryLoadBitmap(
                new Uri("pack://application:,,,/app_icon.png", UriKind.Absolute));

            if (bmp == null)
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                foreach (var name in new[] { "app_icon.png", "CleanupTempPro_Logo.png",
                                             "Cleanup.png", "logo.png" })
                {
                    string p = System.IO.Path.Combine(exeDir, name);
                    if (File.Exists(p))
                    {
                        bmp = TryLoadBitmap(new Uri(p, UriKind.Absolute));
                        if (bmp != null) break;
                    }
                }
            }

            if (bmp != null)
                TitleLogoImage.Source = bmp;
        }

        private static BitmapImage? TryLoadBitmap(Uri uri)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource   = uri;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }

        // ── ВКЛАДКИ ───────────────────────────────────────────────────────
        private void TabFiles_Click(object sender, MouseButtonEventArgs e)
        {
            FilesPanel.Visibility = Visibility.Visible;
            HistoryPanel.Visibility = Visibility.Collapsed;

            TabFilesHeader.Background = new SolidColorBrush(Color.FromRgb(26, 42, 74));
            TabFilesText.Foreground = new SolidColorBrush(Color.FromRgb(74, 158, 255));
            TabFilesText.FontWeight = FontWeights.Bold;

            TabHistoryHeader.Background = Brushes.Transparent;
            TabHistoryText.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 187));
            TabHistoryText.FontWeight = FontWeights.Normal;
        }

        private void TabHistory_Click(object sender, MouseButtonEventArgs e)
        {
            FilesPanel.Visibility = Visibility.Collapsed;
            HistoryPanel.Visibility = Visibility.Visible;

            TabHistoryHeader.Background = new SolidColorBrush(Color.FromRgb(26, 42, 74));
            TabHistoryText.Foreground = new SolidColorBrush(Color.FromRgb(74, 158, 255));
            TabHistoryText.FontWeight = FontWeights.Bold;

            TabFilesHeader.Background = Brushes.Transparent;
            TabFilesText.Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 187));
            TabFilesText.FontWeight = FontWeights.Normal;
        }

        private void SwitchTab(bool showHistory)
        {
            _showingHistory = showHistory;
            FilesPanel.Visibility   = showHistory ? Visibility.Collapsed : Visibility.Visible;
            HistoryPanel.Visibility = showHistory ? Visibility.Visible   : Visibility.Collapsed;

            if (showHistory)
            {
                TabHistoryHeader.Background = new SolidColorBrush(Color.FromRgb(0x1A,0x2A,0x4A));
                TabHistoryText.Foreground   = new SolidColorBrush(Color.FromRgb(0x4A,0x9E,0xFF));
                TabHistoryText.FontFamily   = new FontFamily("Segoe UI Semibold");
                TabFilesHeader.Background   = Brushes.Transparent;
                TabFilesText.Foreground     = (Brush)FindResource("TextSecondaryBrush");
                TabFilesText.FontFamily     = new FontFamily("Segoe UI");
                ListCountLabel.Text         = $"{_historyItems.Count} записей";
            }
            else
            {
                TabFilesHeader.Background   = new SolidColorBrush(Color.FromRgb(0x1A,0x2A,0x4A));
                TabFilesText.Foreground     = new SolidColorBrush(Color.FromRgb(0x4A,0x9E,0xFF));
                TabFilesText.FontFamily     = new FontFamily("Segoe UI Semibold");
                TabHistoryHeader.Background = Brushes.Transparent;
                TabHistoryText.Foreground   = (Brush)FindResource("TextSecondaryBrush");
                TabHistoryText.FontFamily   = new FontFamily("Segoe UI");
                ListCountLabel.Text         = _fileItems.Count > 0 ? $"{_fileItems.Count} объектов" : "";
            }
        }

        // ── PULSE ─────────────────────────────────────────────────────────
        private void StartPulse()
        {
            _pulseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _pulseTimer.Tick += (_, _) =>
            {
                var a = new DoubleAnimation(1, 0.25, TimeSpan.FromSeconds(1)) { AutoReverse = true };
                StatusDot.BeginAnimation(UIElement.OpacityProperty, a);
            };
            _pulseTimer.Start();
        }

        // ── DISK INFO ────────────────────────────────────────────────────
        private void LoadDiskInfo()
        {
            try
            {
                DisksPanel.Items.Clear();

                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady &&
                           (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable))
                    .ToList();

                foreach (var drv in drives)
                {
                    try
                    {
                        long   used = drv.TotalSize - drv.AvailableFreeSpace;
                        double pct  = drv.TotalSize > 0 ? (double)used / drv.TotalSize : 0;
                        string letter = drv.Name.TrimEnd('\\');
                        bool   isRemovable = drv.DriveType == DriveType.Removable;

                        string label = string.IsNullOrWhiteSpace(drv.VolumeLabel)
                            ? (isRemovable ? "Съёмный диск" : "Локальный диск")
                            : drv.VolumeLabel;

                        string driveIcon = isRemovable ? "💾"
                            : letter.StartsWith("C", StringComparison.OrdinalIgnoreCase) ? "🖥️"
                            : "💿";

                        Color barC1, barC2;
                        if (pct >= 0.9)      { barC1 = Color.FromRgb(0xFF,0x3D,0x00); barC2 = Color.FromRgb(0xCC,0x00,0x44); }
                        else if (pct >= 0.75){ barC1 = Color.FromRgb(0xFF,0x8C,0x00); barC2 = Color.FromRgb(0xFF,0xA5,0x00); }
                        else                 { barC1 = Color.FromRgb(0x4A,0x9E,0xFF); barC2 = Color.FromRgb(0xA8,0x55,0xF7); }

                        var barContainer = new Border
                        {
                            Height       = 6,
                            CornerRadius = new CornerRadius(3),
                            Background   = new SolidColorBrush(Color.FromRgb(0x1A,0x1A,0x3A))
                        };
                        var bar = new Border
                        {
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Height       = 6,
                            CornerRadius = new CornerRadius(3),
                            Width        = 0,
                            Background   = new LinearGradientBrush(barC1, barC2,
                                               new Point(0, 0.5), new Point(1, 0.5))
                        };
                        barContainer.Child = bar;

                        var card = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

                        var header = new Grid { Margin = new Thickness(0, 0, 0, 2) };
                        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var namePanel = new StackPanel { Orientation = Orientation.Horizontal };
                        namePanel.Children.Add(new TextBlock
                        {
                            Text              = driveIcon,
                            FontSize          = 11,
                            Margin            = new Thickness(0, 0, 5, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        });
                        namePanel.Children.Add(new TextBlock
                        {
                            Text              = $"{letter}  {label}",
                            FontFamily        = new FontFamily("Segoe UI Semibold"),
                            FontSize          = 11,
                            Foreground        = new SolidColorBrush(Color.FromRgb(0xE8,0xE8,0xFF)),
                            VerticalAlignment = VerticalAlignment.Center
                        });
                        Grid.SetColumn(namePanel, 0);
                        header.Children.Add(namePanel);

                        var pctColor = pct >= 0.9  ? Color.FromRgb(0xFF,0x4A,0x6A)
                                     : pct >= 0.75 ? Color.FromRgb(0xFF,0x8C,0x00)
                                     :               Color.FromRgb(0x4A,0x9E,0xFF);
                        var pctBlock = new TextBlock
                        {
                            Text              = $"{pct * 100:F0}%",
                            FontFamily        = new FontFamily("Segoe UI Semibold"),
                            FontSize          = 11,
                            Foreground        = new SolidColorBrush(pctColor),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(pctBlock, 1);
                        header.Children.Add(pctBlock);
                        card.Children.Add(header);

                        card.Children.Add(new TextBlock
                        {
                            Text       = $"{SizeHelper.Format(used)} / {SizeHelper.Format(drv.TotalSize)}",
                            FontFamily = new FontFamily("Segoe UI"),
                            FontSize   = 10,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x88,0x88,0xBB)),
                            Margin     = new Thickness(0, 0, 0, 4)
                        });

                        card.Children.Add(barContainer);
                        DisksPanel.Items.Add(card);

                        var capturedBar  = bar;
                        var capturedCont = barContainer;
                        double captPct   = pct;
                        Dispatcher.InvokeAsync(() =>
                        {
                            double w = capturedCont.ActualWidth > 0 ? capturedCont.ActualWidth : 230;
                            capturedBar.BeginAnimation(FrameworkElement.WidthProperty,
                                new DoubleAnimation(0, w * captPct, TimeSpan.FromSeconds(1.1))
                                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                        }, DispatcherPriority.Loaded);
                    }
                    catch { }
                }
            }
            catch { }
        }

        // ── STATUS ────────────────────────────────────────────────────────
        enum StatusKind { Ready, Scanning, Cleaning, Found, Done, Stopped, Error }

        private void SetStatus(string text, StatusKind kind)
        {
            StatusText.Text = text;
            string hex = kind switch
            {
                StatusKind.Scanning => "#4A9EFF",
                StatusKind.Cleaning => "#FF8C00",
                StatusKind.Found    => "#FF4A6A",
                StatusKind.Done     => "#06D6C7",
                StatusKind.Stopped  => "#8888BB",
                StatusKind.Error    => "#FF4A6A",
                _                   => "#06D6C7"
            };
            StatusDotColor.Color = (Color)ColorConverter.ConvertFromString(hex);
        }

        private void SetProgress(double pct, string label)
        {
            ProgressLabel.Text   = label;
            ProgressPercent.Text = $"{pct:F0}%";
            double w = ProgressBarContainer.ActualWidth > 0 ? ProgressBarContainer.ActualWidth : 600;
            double target = Math.Max(0, Math.Min(w, w * pct / 100.0));
            ProgressBarFill.BeginAnimation(FrameworkElement.WidthProperty,
                new DoubleAnimation(target, TimeSpan.FromMilliseconds(200))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                });
        }

        // ── УПРАВЛЕНИЕ СОСТОЯНИЕМ КНОПОК ──────────────────────────────────
        private void SetUiRunning(bool running, bool hasFiles = false)
        {
            _canStop  = running;
            _canClean = !running && hasFiles;

            ScanBtnBorder.Opacity   = running ? 0.4 : 1.0;
            ScanBtnBorder.IsEnabled = !running;

            CleanBtnBorder.Opacity   = _canClean ? 1.0 : 0.5;
            CleanBtnBorder.IsEnabled = _canClean;

            StopBtnBorder.Opacity   = running ? 1.0 : 0.4;
            StopBtnBorder.IsEnabled = running;
        }

        // ── ОБРАБОТЧИКИ BORDER-КНОПОК ────────────────────────────────────

        // SCAN
        private void ScanBorder_Click(object sender, MouseButtonEventArgs e)
        {
            if (!ScanBtnBorder.IsEnabled) return;
            ScanBtn_Execute();
        }
        private void ScanBorder_Enter(object sender, MouseEventArgs e)
        {
            if (ScanBtnBorder.IsEnabled)
            {
                ScanBtnBorder.Opacity = 1.0;
                ScanBtnBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(0x4A, 0x9E, 0xFF),
                    BlurRadius = 28, ShadowDepth = 0, Opacity = 0.85
                };
            }
        }
        private void ScanBorder_Leave(object sender, MouseEventArgs e)
        {
            ScanBtnBorder.Opacity = _isRunning ? 0.4 : 1.0;
            ScanBtnBorder.Effect = null;
        }

        // CLEAN
        private void CleanBorder_Click(object sender, MouseButtonEventArgs e)
        {
            if (!CleanBtnBorder.IsEnabled) return;
            CleanBtn_Execute();
        }
        private void CleanBorder_Enter(object sender, MouseEventArgs e)
        {
            if (CleanBtnBorder.IsEnabled)
            {
                CleanBtnBorder.Opacity = 1.0;
                CleanBtnBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(0xFF, 0x50, 0x70),
                    BlurRadius = 24, ShadowDepth = 0, Opacity = 0.75
                };
            }
        }
        private void CleanBorder_Leave(object sender, MouseEventArgs e)
        {
            CleanBtnBorder.Opacity = _canClean ? 1.0 : 0.5;
            CleanBtnBorder.Effect = null;
        }

        // STOP
        private void StopBorder_Click(object sender, MouseButtonEventArgs e)
        {
            if (!StopBtnBorder.IsEnabled) return;
            _cts?.Cancel();
            SetStatus("Остановка...", StatusKind.Stopped);
        }
        private void StopBorder_Enter(object sender, MouseEventArgs e)
        {
            if (StopBtnBorder.IsEnabled)
            {
                StopBtnBorder.Opacity = 1.0;
                StopBtnBorder.Background = new LinearGradientBrush(
                    new GradientStopCollection {
                        new GradientStop(Color.FromRgb(0x0A, 0x30, 0x4A), 0.0),
                        new GradientStop(Color.FromRgb(0x0A, 0x28, 0x3A), 1.0)
                    },
                    new Point(0, 0.5), new Point(1, 0.5));
                StopBtnBorder.BorderBrush = new LinearGradientBrush(
                    new GradientStopCollection {
                        new GradientStop(Color.FromRgb(0x4A, 0x9E, 0xFF), 0.0),
                        new GradientStop(Color.FromRgb(0x06, 0xD6, 0xC7), 1.0)
                    },
                    new Point(0, 0.5), new Point(1, 0.5));
                StopBtnBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(0x06, 0xD6, 0xC7),
                    BlurRadius = 35, ShadowDepth = 0, Opacity = 1.0
                };
            }
        }
        private void StopBorder_Leave(object sender, MouseEventArgs e)
        {
            StopBtnBorder.Opacity = _canStop ? 1.0 : 0.4;
            StopBtnBorder.Background = new LinearGradientBrush(
                new GradientStopCollection {
                    new GradientStop(Color.FromRgb(0x0A, 0x2A, 0x3A), 0.0),
                    new GradientStop(Color.FromRgb(0x0A, 0x20, 0x30), 1.0)
                },
                new Point(0, 0.5), new Point(1, 0.5));
            StopBtnBorder.BorderBrush = new LinearGradientBrush(
                new GradientStopCollection {
                    new GradientStop(Color.FromRgb(0x4A, 0x9E, 0xFF), 0.0),
                    new GradientStop(Color.FromRgb(0x06, 0xD6, 0xC7), 1.0)
                },
                new Point(0, 0.5), new Point(1, 0.5));
            StopBtnBorder.Effect = null;
        }

        // ═══════════════════════════════════════
        //  WINDOWS UPDATE SERVICE — стоп/старт
        // ═══════════════════════════════════════

        /// <summary>
        /// Проверяет, идёт ли прямо сейчас загрузка обновлений Windows.
        /// Признак: служба wuauserv запущена И в папке есть файлы .esd/.cab без .psf (неполные).
        /// </summary>
        private static bool IsWindowsUpdateActive()
        {
            try
            {
                using var svc = new ServiceController("wuauserv");
                if (svc.Status != ServiceControllerStatus.Running) return false;

                string dir = @"C:\Windows\SoftwareDistribution\Download";
                if (!Directory.Exists(dir)) return false;

                // Ищем частично скачанные файлы — признак активной загрузки
                var partialFiles = Directory.EnumerateFiles(dir, "*.esd", SearchOption.AllDirectories)
                    .Concat(Directory.EnumerateFiles(dir, "*.cab", SearchOption.AllDirectories))
                    .Take(5)
                    .ToList();

                return partialFiles.Count > 0;
            }
            catch { return false; }
        }

        /// <summary>
        /// Останавливает службу Windows Update (wuauserv) и BITS.
        /// Возвращает true если службы были остановлены успешно.
        /// wasRunning = true если wuauserv была запущена до остановки.
        /// </summary>
        private static bool StopWindowsUpdateService(out bool wasRunning)
        {
            wasRunning = false;
            try
            {
                // Сначала останавливаем BITS (Background Intelligent Transfer) — он держит файлы
                try
                {
                    using var bits = new ServiceController("BITS");
                    if (bits.Status == ServiceControllerStatus.Running)
                    {
                        bits.Stop();
                        bits.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
                    }
                }
                catch { /* BITS мог быть уже остановлен */ }

                // Затем wuauserv
                using var svc = new ServiceController("wuauserv");
                wasRunning = svc.Status == ServiceControllerStatus.Running
                          || svc.Status == ServiceControllerStatus.StartPending;

                if (svc.Status != ServiceControllerStatus.Stopped &&
                    svc.Status != ServiceControllerStatus.StopPending)
                {
                    svc.Stop();
                    svc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(25));
                }

                // Даём файловой системе пару секунд отпустить хэндлы
                Thread.Sleep(1500);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Запускает службу Windows Update обратно (если она была запущена).
        /// </summary>
        private static void StartWindowsUpdateService()
        {
            try
            {
                using var svc = new ServiceController("wuauserv");
                if (svc.Status == ServiceControllerStatus.Stopped)
                    svc.Start();
            }
            catch { }

            try
            {
                using var bits = new ServiceController("BITS");
                if (bits.Status == ServiceControllerStatus.Stopped)
                    bits.Start();
            }
            catch { }
        }

        // ═══════════════════════════════════════
        //  SCAN
        // ═══════════════════════════════════════
        private async void ScanBtn_Execute()
        {
            if (_isRunning) return;
            _fileItems.Clear();
            _totalFoundBytes = 0;
            _statTemp = _statBrowser = _statRecycle = 0;
            StatTempFiles.Text = StatBrowserFiles.Text = StatRecycleBin.Text = "0";
            StatCleaned.Text = "0";
            TotalSizeText.Text = "0 МБ"; FileCountText.Text = "Поиск...";
            ListCountLabel.Text = "";

            if (_showingHistory) SwitchTab(false);

            _isRunning = true;
            _cts?.Cancel();   // сначала отменяем (если старый токен ещё жив)
            _cts?.Dispose();  // потом освобождаем
            _cts = new CancellationTokenSource();
            SetUiRunning(true);
            SetStatus("Сканирование...", StatusKind.Scanning);
            SetProgress(0, "Подготовка...");

            var paths      = GetScanPaths();
            bool doRecycle = ChkRecycleBin?.IsChecked == true;
            var token      = _cts.Token;

            // ── НОВОЕ: предупреждение если WU активна ──
            bool wuActive = false;
            await Task.Run(() => { wuActive = IsWindowsUpdateActive(); });
            if (wuActive && ChkWinTemp?.IsChecked == true)
            {
                SetStatus("⚠ Обнаружена активная загрузка обновлений Windows", StatusKind.Error);
            }

            try
            {
                await Task.Run(() =>
                {
                    int total = paths.Count, done = 0;
                    foreach (var (dir, cat, icon) in paths)
                    {
                        if (token.IsCancellationRequested) break;
                        int p = total > 0 ? (int)(done * 100.0 / total) : 0;
                        Dispatcher.Invoke(() => SetProgress(p, $"Сканирую: {cat}..."));
                        if (Directory.Exists(dir)) ScanDir(dir, cat, icon, token);
                        done++;
                    }
                    if (doRecycle && !token.IsCancellationRequested)
                    {
                        Dispatcher.Invoke(() => SetProgress(95, "Проверяю корзину..."));
                        ScanRecycleBin();
                    }
                }, CancellationToken.None);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isRunning = false;
                bool wasCancelled = _cts?.IsCancellationRequested == true;
                SetUiRunning(false, _fileItems.Count > 0);
                if (wasCancelled)
                {
                    SetProgress(0, "Сканирование остановлено");
                    SetStatus("Остановлено", StatusKind.Stopped);
                }
                else if (_fileItems.Count > 0)
                {
                    bool browsersOpen = ChkBrowserCache?.IsChecked == true &&
                        new[] { "chrome", "msedge", "firefox", "brave", "opera", "browser", "vivaldi" }
                            .Any(n => Process.GetProcessesByName(n).Length > 0);

                    string hint = browsersOpen ? " ⚠ закройте браузеры перед очисткой" : "";
                    SetProgress(100, $"Найдено {_fileItems.Count} объектов • {SizeHelper.Format(_totalFoundBytes)}");
                    SetStatus($"Найдено {SizeHelper.Format(_totalFoundBytes)} мусора{hint}", StatusKind.Found);
                }
                else
                {
                    SetProgress(100, "Система чиста — мусор не найден");
                    SetStatus("Система чиста! ✓", StatusKind.Done);
                }
            }
        }

        private void ScanDir(string dir, string cat, string icon, CancellationToken token)
        {
            try
            {
                bool isRootJunk = cat.StartsWith("Мусор в корне", StringComparison.OrdinalIgnoreCase);
                bool isRecycleBinDir = dir.IndexOf("$RECYCLE.BIN", StringComparison.OrdinalIgnoreCase) >= 0;

                // ── НОВОЕ: для SoftwareDistribution ставим таймаут 30 сек ──
                bool isSoftwareDist = dir.IndexOf("SoftwareDistribution", StringComparison.OrdinalIgnoreCase) >= 0;
                using var timeoutCts = isSoftwareDist
                    ? new CancellationTokenSource(TimeSpan.FromSeconds(30))
                    : new CancellationTokenSource();
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);
                var effectiveToken = linkedCts.Token;

                var opts = new EnumerationOptions
                {
                    IgnoreInaccessible    = true,
                    RecurseSubdirectories = !isRootJunk,
                    AttributesToSkip      = isRecycleBinDir
                        ? FileAttributes.None
                        : FileAttributes.System
                };

                bool isBrowser = cat.Contains("Chrome") || cat.Contains("Edge") ||
                                 cat.Contains("Firefox") || cat.Contains("Brave") ||
                                 cat.Contains("Opera")   || cat.Contains("Яндекс") ||
                                 cat.Contains("Vivaldi");
                bool isRecycle = cat.Contains("орзин");

                var sw          = Stopwatch.StartNew();
                long batchBytes = 0;
                int  batchT = 0, batchBr = 0, batchRc = 0;
                var  batchItems = new List<FileItem>(64);

                void Flush()
                {
                    if (batchItems.Count == 0) return;
                    var items = batchItems.ToList();
                    long bytes = batchBytes;
                    int t = batchT, br = batchBr, rc = batchRc;
                    batchItems.Clear(); batchBytes = 0; batchT = batchBr = batchRc = 0;

                    Dispatcher.Invoke(() =>
                    {
                        foreach (var it in items) _fileItems.Add(it);
                        _totalFoundBytes += bytes;
                        _statTemp        += t;
                        _statBrowser     += br;
                        _statRecycle     += rc;
                        TotalSizeText.Text    = SizeHelper.Format(_totalFoundBytes);
                        FileCountText.Text    = $"{_fileItems.Count} файлов";
                        ListCountLabel.Text   = $"{_fileItems.Count} объектов";
                        StatTempFiles.Text    = _statTemp.ToString();
                        StatBrowserFiles.Text = _statBrowser.ToString();
                        StatRecycleBin.Text   = _statRecycle.ToString();
                    }, DispatcherPriority.Background);
                }

                foreach (var file in Directory.EnumerateFiles(dir, "*", opts))
                {
                    if (effectiveToken.IsCancellationRequested) break;
                    try
                    {
                        if (isRootJunk)
                        {
                            string ext  = System.IO.Path.GetExtension(file);
                            string name = System.IO.Path.GetFileName(file);
                            bool   isTildeFile = name.StartsWith("~", StringComparison.Ordinal);
                            if (!isTildeFile &&
                                !_junkExtensions.Contains(ext) &&
                                !_junkFileNames.Contains(name))
                                continue;
                        }

                        var fi = new FileInfo(file);
                        if (!fi.Exists) continue;
                        if (IsInProtectedFolder(file)) continue;
                        long sz = fi.Length;
                        batchBytes += sz;
                        if      (isBrowser) batchBr++;
                        else if (isRecycle) batchRc++;
                        else                batchT++;
                        batchItems.Add(new FileItem { Icon = icon, Path = file, Category = cat, SizeBytes = sz });

                        if (sw.ElapsedMilliseconds >= 150 || batchItems.Count >= 200)
                        {
                            Flush(); sw.Restart();
                        }
                    }
                    catch { }
                }
                Flush();
            }
            catch { }
        }

        private void ScanRecycleBin()
        {
            try
            {
                var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
                if (SHQueryRecycleBin(null, ref info) == 0 && info.i64NumItems > 0)
                {
                    long sz = info.i64Size, cnt = info.i64NumItems;
                    Dispatcher.Invoke(() =>
                    {
                        _totalFoundBytes += sz;
                        _statRecycle     += (int)cnt;
                        _fileItems.Add(new FileItem { Icon = "🗑️",
                            Path = $"Корзина ({cnt} объектов)", Category = "Корзина", SizeBytes = sz });
                        TotalSizeText.Text  = SizeHelper.Format(_totalFoundBytes);
                        FileCountText.Text  = $"{_fileItems.Count} записей";
                        ListCountLabel.Text = $"{_fileItems.Count} объектов";
                        StatRecycleBin.Text = _statRecycle.ToString();
                    });
                }
            }
            catch { }
        }

        private List<(string, string, string)> GetScanPaths()
        {
            var L = new List<(string, string, string)>();
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (ChkTempFiles?.IsChecked == true)
                L.Add((System.IO.Path.GetTempPath(), "Temp (пользователь)", "🗂️"));

            if (ChkWinTemp?.IsChecked == true)
            {
                L.Add((@"C:\Windows\Temp", "Windows Temp", "⚙️"));
                L.Add((@"C:\Windows\SoftwareDistribution\Download", "Windows Update кэш", "⚙️"));
            }

            if (ChkBrowserCache?.IsChecked == true)
            {
                foreach (var p in GetChromeProfiles(local))
                    AddBrowserCachePaths(L, p, "Chrome");

                foreach (var p in GetChromiumProfiles(local, @"Microsoft\Edge\User Data"))
                    AddBrowserCachePaths(L, p, "Edge");

                string ff = System.IO.Path.Combine(local, @"Mozilla\Firefox\Profiles");
                if (Directory.Exists(ff))
                    foreach (var d in Directory.GetDirectories(ff))
                    {
                        L.Add((System.IO.Path.Combine(d, "cache2"),       "Firefox кэш",    "🦊"));
                        L.Add((System.IO.Path.Combine(d, "startupCache"), "Firefox Startup",  "🦊"));
                    }

                foreach (var p in GetChromiumProfiles(local, @"BraveSoftware\Brave-Browser\User Data"))
                    AddBrowserCachePaths(L, p, "Brave");

                foreach (var p in GetChromiumProfiles(local, @"Opera Software\Opera Stable"))
                    AddBrowserCachePaths(L, p, "Opera");

                foreach (var p in GetChromiumProfiles(local, @"Opera Software\Opera GX Stable"))
                    AddBrowserCachePaths(L, p, "Opera GX");

                foreach (var p in GetChromiumProfiles(local, @"Opera Software\Opera One"))
                    AddBrowserCachePaths(L, p, "Opera One");

                foreach (var p in GetChromiumProfiles(local, @"Yandex\YandexBrowser\User Data"))
                    AddBrowserCachePaths(L, p, "Яндекс");

                foreach (var p in GetChromiumProfiles(local, @"Vivaldi\User Data"))
                    AddBrowserCachePaths(L, p, "Vivaldi");
            }

            if (ChkPrefetch?.IsChecked == true)
                L.Add((@"C:\Windows\Prefetch", "Prefetch", "⚡"));

            if (ChkThumbnails?.IsChecked == true)
                L.Add((System.IO.Path.Combine(local, @"Microsoft\Windows\Explorer"), "Thumbnails кэш", "🖼️"));

            if (ChkEventLogs?.IsChecked == true)
                L.Add((@"C:\Windows\System32\winevt\Logs", "Логи событий", "📋"));

            if (ChkDnsCache?.IsChecked == true)
            {
                L.Add((System.IO.Path.Combine(local, @"Microsoft\Windows\INetCache"), "IE/Edge Legacy Cache", "🔗"));
                string webCachePath = System.IO.Path.Combine(local, @"Microsoft\Windows\WebCache");
                if (Directory.Exists(webCachePath))
                    L.Add((webCachePath, "Edge Legacy WebCache", "🔗"));
            }

            if (ChkMSOffice?.IsChecked == true)
            {
                L.Add((System.IO.Path.Combine(local, @"Microsoft\Office\16.0\OfficeFileCache"), "Office кэш", "📎"));
                L.Add((System.IO.Path.Combine(local, @"Microsoft\Office\16.0\OfficeFileCache\0"), "Office FileCache", "📎"));
            }

            if (ChkExternalDrives?.IsChecked == true)
                L.AddRange(GetExternalDrivePaths());

            return L;
        }

        private static List<(string, string, string)> GetExternalDrivePaths()
        {
            var result = new List<(string, string, string)>();

            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!drive.IsReady) continue;
                    if (drive.DriveType != DriveType.Fixed &&
                        drive.DriveType != DriveType.Removable)
                        continue;

                    string root        = drive.Name;
                    string letter      = root.TrimEnd('\\');
                    string icon        = drive.DriveType == DriveType.Removable ? "💾" : "🖥️";
                    string label       = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                                        ? letter : $"{drive.VolumeLabel} ({letter})";

                    void TryAdd(string path, string cat, string? ic = null)
                    {
                        if (Directory.Exists(path))
                            result.Add((path, cat, ic ?? icon));
                    }

                    TryAdd(System.IO.Path.Combine(root, "$RECYCLE.BIN"), $"Корзина {label}", "🗑️");

                    foreach (var n in new[] { "Temp", "temp", "tmp", "Tmp", "TEMP", "_Temp", "$Temp", "TempFiles" })
                        TryAdd(System.IO.Path.Combine(root, n), $"Temp {label}");

                    TryAdd(System.IO.Path.Combine(root, @"Windows\Temp"), $"Windows Temp {label}");
                    TryAdd(System.IO.Path.Combine(root, @"Windows\SoftwareDistribution\Download"), $"WU кэш {label}");
                    TryAdd(System.IO.Path.Combine(root, @"Windows\Prefetch"), $"Prefetch {label}");

                    string usersRoot = System.IO.Path.Combine(root, "Users");
                    if (Directory.Exists(usersRoot))
                    {
                        string[] skipNames = { "Public", "Default", "All Users", "Default User" };

                        foreach (var userDir in Directory.GetDirectories(usersRoot))
                        {
                            string uName = System.IO.Path.GetFileName(userDir);
                            if (Array.Exists(skipNames, s =>
                                    string.Equals(s, uName, StringComparison.OrdinalIgnoreCase)))
                                continue;

                            string localData = System.IO.Path.Combine(userDir, @"AppData\Local");
                            string roamData  = System.IO.Path.Combine(userDir, @"AppData\Roaming");

                            TryAdd(System.IO.Path.Combine(localData, "Temp"), $"Temp пользователя {label}");
                            TryAdd(System.IO.Path.Combine(localData, @"Microsoft\Windows\INetCache"), $"IE/Edge Cache ({letter})", "🔗");
                            TryAdd(System.IO.Path.Combine(localData, @"Microsoft\Windows\Explorer"), $"Thumbnails {label}", "🖼️");
                            TryAdd(System.IO.Path.Combine(localData, @"Microsoft\Office\16.0\OfficeFileCache"), $"Office кэш ({letter})", "📎");

                            foreach (var cp in GetChromeProfiles(localData))
                                AddBrowserCachePaths(result, cp, $"Chrome ({letter})");

                            foreach (var ep in GetChromiumProfiles(localData, @"Microsoft\Edge\User Data"))
                                AddBrowserCachePaths(result, ep, $"Edge ({letter})");

                            string ffProfiles = System.IO.Path.Combine(localData, @"Mozilla\Firefox\Profiles");
                            if (Directory.Exists(ffProfiles))
                                foreach (var d in Directory.GetDirectories(ffProfiles))
                                {
                                    TryAdd(System.IO.Path.Combine(d, "cache2"),       $"Firefox кэш ({letter})",    "🦊");
                                    TryAdd(System.IO.Path.Combine(d, "startupCache"), $"Firefox Startup ({letter})", "🦊");
                                }

                            foreach (var bp in GetChromiumProfiles(localData, @"BraveSoftware\Brave-Browser\User Data"))
                                AddBrowserCachePaths(result, bp, $"Brave ({letter})");

                            foreach (var yp in GetChromiumProfiles(localData, @"Yandex\YandexBrowser\User Data"))
                                AddBrowserCachePaths(result, yp, $"Яндекс ({letter})");

                            foreach (var op in GetChromiumProfiles(localData, @"Opera Software\Opera Stable"))
                                AddBrowserCachePaths(result, op, $"Opera ({letter})");

                            TryAdd(System.IO.Path.Combine(roamData,  @"Microsoft\Teams\Service Worker\CacheStorage"), $"Teams кэш ({letter})", "💬");
                            TryAdd(System.IO.Path.Combine(localData, @"slack\Cache"), $"Slack кэш ({letter})", "💬");
                        }
                    }

                    TryAdd(root, $"Мусор в корне {label}");
                }
                catch { }
            }

            return result;
        }

        private static IEnumerable<string> GetChromeProfiles(string local)
        {
            string chrome = System.IO.Path.Combine(local, @"Google\Chrome\User Data");
            if (!Directory.Exists(chrome)) yield break;
            yield return System.IO.Path.Combine(chrome, "Default");
            foreach (var d in Directory.GetDirectories(chrome, "Profile*")) yield return d;
        }

        private static IEnumerable<string> GetChromiumProfiles(string local, string relPath)
        {
            string root = System.IO.Path.Combine(local, relPath);
            if (!Directory.Exists(root)) yield break;
            string def = System.IO.Path.Combine(root, "Default");
            if (Directory.Exists(def))
            {
                yield return def;
                foreach (var d in Directory.GetDirectories(root, "Profile*")) yield return d;
            }
            else
            {
                yield return root;
            }
        }

        private static void AddBrowserCachePaths(List<(string, string, string)> L,
                                                  string profilePath, string browserName)
        {
            var subfolders = new[]
            {
                "Cache", "Cache2", "Code Cache", "GPUCache",
                "DawnCache", "ShaderCache", "blob_storage",
            };
            foreach (var sub in subfolders)
            {
                string full = System.IO.Path.Combine(profilePath, sub);
                if (Directory.Exists(full))
                    L.Add((full, $"{browserName} кэш", "🌐"));
            }
            string netCache = System.IO.Path.Combine(profilePath, "Network", "Cache");
            if (Directory.Exists(netCache))
                L.Add((netCache, $"{browserName} Network Cache", "🌐"));
        }

        // ═══════════════════════════════════════
        //  CLEAN
        // ═══════════════════════════════════════
        private async void CleanBtn_Execute()
        {
            if (_isRunning || _fileItems.Count == 0) return;

            var dlg = new CustomDialog(
                "Подтверждение очистки",
                $"Будет удалено {_fileItems.Count} объектов.\nЭто действие нельзя отменить.",
                DialogKind.Confirm,
                stats: new List<StatRow>
                {
                    new() { Label = "Найдено файлов:", Value = _fileItems.Count.ToString(),         Color = "#AAAACC" },
                    new() { Label = "Займёт места:",   Value = SizeHelper.Format(_totalFoundBytes), Color = "#FF4A6A" },
                },
                showCancel: true);
            dlg.ShowDialog();
            if (!dlg.Result) return;

            // ── НОВОЕ: предупреждение если WU скачивает обновления ──
            bool hasWuFiles = _fileItems.Any(x => x.Category == "Windows Update кэш"
                                               || x.Category.StartsWith("WU кэш"));
            if (hasWuFiles)
            {
                bool wuActive = false;
                await Task.Run(() => { wuActive = IsWindowsUpdateActive(); });

                if (wuActive)
                {
                    var wuDlg = new CustomDialog(
                        "Обновления Windows загружаются!",
                        "Прямо сейчас Windows скачивает обновления.\n\n" +
                        "Программа остановит службу обновлений, очистит кэш и запустит её снова.\n" +
                        "Обновления будут скачаны заново при следующем запуске Windows Update.\n\n" +
                        "Продолжить?",
                        DialogKind.Warning,
                        showCancel: true);
                    wuDlg.ShowDialog();
                    if (!wuDlg.Result) return;
                }
            }

            // Предупреждение об открытых браузерах
            if (ChkBrowserCache?.IsChecked == true)
            {
                var browserProcesses = new Dictionary<string, string>
                {
                    { "chrome",          "Google Chrome"   },
                    { "msedge",          "Microsoft Edge"  },
                    { "firefox",         "Firefox"         },
                    { "brave",           "Brave"           },
                    { "opera",           "Opera"           },
                    { "operagx",         "Opera GX"        },
                    { "browser",         "Яндекс Браузер"  },
                    { "vivaldi",         "Vivaldi"         },
                };
                var runningBrowsers = browserProcesses
                    .Where(b => Process.GetProcessesByName(b.Key).Length > 0)
                    .Select(b => b.Value)
                    .ToList();

                if (runningBrowsers.Count > 0)
                {
                    var warnDlg = new CustomDialog(
                        "Браузеры открыты!",
                        $"Обнаружены запущенные браузеры:\n{string.Join(", ", runningBrowsers)}\n\nКэш будет удалён, но браузер немедленно воссоздаст его. Именно поэтому после очистки снова находится мусор.\n\nРекомендуется закрыть браузеры и повторить очистку.",
                        DialogKind.Warning,
                        showCancel: true);
                    warnDlg.ShowDialog();
                    if (!warnDlg.Result) return;
                }
            }

            _isRunning = true;
            _cts?.Cancel();   // сначала отменяем
            _cts?.Dispose();  // потом освобождаем
            _cts = new CancellationTokenSource();
            _cleanedBytes = 0;
            StatCleaned.Text = "0";
            SetUiRunning(true);
            SetStatus("Очистка...", StatusKind.Cleaning);
            SetProgress(0, "Начинаю очистку...");

            var snapshot   = _fileItems.ToList();
            bool doRecycle = snapshot.Any(x => x.Category == "Корзина");
            var regular    = snapshot.Where(x => x.Category != "Корзина").ToList();
            var cleanDirs  = GetScanPaths().Select(p => p.Item1).Distinct().ToList();

            int done = 0, deleted = 0, skipped = 0;
            var token = _cts.Token;

            // ── Определяем, есть ли среди файлов что-то из SoftwareDistribution ──
            bool needWuStop = regular.Any(x =>
                x.Path.IndexOf("SoftwareDistribution", StringComparison.OrdinalIgnoreCase) >= 0);

            try
            {
                await Task.Run(() =>
                {
                    var sw = Stopwatch.StartNew();

                    // ── НОВОЕ: останавливаем Windows Update если нужно ──
                    bool wuWasRunning = false;
                    if (needWuStop)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            SetProgress(1, "Останавливаю службу Windows Update...");
                            SetStatus("Останавливаю службу обновлений...", StatusKind.Cleaning);
                        });

                        bool stopped = StopWindowsUpdateService(out wuWasRunning);

                        Dispatcher.Invoke(() =>
                        {
                            if (stopped)
                                SetProgress(3, "Служба остановлена. Начинаю очистку...");
                            else
                                SetProgress(3, "Не удалось остановить службу — пробую удалить...");
                        });
                    }

                    try
                    {
                        foreach (var item in regular)
                        {
                            if (token.IsCancellationRequested) break;
                            try
                            {
                                if (File.Exists(item.Path))
                                {
                                    var attr = File.GetAttributes(item.Path);
                                    if ((attr & (FileAttributes.ReadOnly | FileAttributes.System)) != 0)
                                        File.SetAttributes(item.Path, FileAttributes.Normal);
                                    File.Delete(item.Path);
                                    Interlocked.Add(ref _cleanedBytes, item.SizeBytes);
                                    Interlocked.Increment(ref deleted);
                                }
                            }
                            catch { Interlocked.Increment(ref skipped); }

                            done++;
                            if (sw.ElapsedMilliseconds >= 200 || done == regular.Count)
                            {
                                sw.Restart();
                                int d2 = done; long c2 = _cleanedBytes;
                                var deletedPaths = new HashSet<string>(
                                    regular.Where(x => !File.Exists(x.Path)).Select(x => x.Path));
                                Dispatcher.Invoke(() =>
                                {
                                    var toRemove = _fileItems
                                        .Where(x => x.Category != "Корзина" && deletedPaths.Contains(x.Path))
                                        .ToList();
                                    SetProgress(regular.Count > 0 ? d2 * 100.0 / regular.Count : 100,
                                        $"Удалено {d2} / {regular.Count} • {SizeHelper.Format(c2)}");
                                    StatCleaned.Text = SizeHelper.Format(c2);
                                    foreach (var r in toRemove) _fileItems.Remove(r);
                                }, DispatcherPriority.Background);
                            }
                        }
                    }
                    finally
                    {
                        // ── НОВОЕ: всегда запускаем службу обратно, даже если была ошибка ──
                        if (needWuStop && wuWasRunning)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                SetProgress(97, "Запускаю службу Windows Update...");
                                SetStatus("Восстанавливаю службу обновлений...", StatusKind.Cleaning);
                            });
                            StartWindowsUpdateService();
                        }
                    }

                    if (doRecycle && !token.IsCancellationRequested)
                    {
                        Dispatcher.Invoke(() => SetProgress(96, "Очищаю корзину..."));
                        try
                        {
                            var rbi = new SHQUERYRBINFO { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
                            SHQueryRecycleBin(null, ref rbi);
                            SHEmptyRecycleBin(IntPtr.Zero, null, 0x00000001 | 0x00000002 | 0x00000004);
                            Interlocked.Add(ref _cleanedBytes, rbi.i64Size);
                            long c3 = _cleanedBytes;
                            Dispatcher.Invoke(() =>
                            {
                                var rb = _fileItems.FirstOrDefault(x => x.Category == "Корзина");
                                if (rb != null) _fileItems.Remove(rb);
                                StatCleaned.Text    = SizeHelper.Format(c3);
                                StatRecycleBin.Text = "0";
                            });
                        }
                        catch { }
                    }

                    Dispatcher.Invoke(() => SetProgress(99, "Удаляю пустые папки..."));
                    CleanEmptyDirs(cleanDirs);
                }, CancellationToken.None);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isRunning = false;
                bool wasCancelled = _cts?.IsCancellationRequested == true;
                _totalFoundBytes = 0;
                long freed = _cleanedBytes;
                _cleanedBytes = 0;
                _statTemp    = 0;
                _statBrowser = 0;
                _statRecycle = 0;
                StatTempFiles.Text    = "0";
                StatBrowserFiles.Text = "0";
                StatRecycleBin.Text   = "0";
                SetUiRunning(false, false);

                if (wasCancelled)
                {
                    SetProgress(0, $"Остановлено • Освобождено {SizeHelper.Format(freed)}");
                    SetStatus("Остановлено", StatusKind.Stopped);
                    ListCountLabel.Text = skipped > 0 ? $"Пропущено: {skipped}" : "";
                }
                else
                {
                    SetProgress(100, $"Готово! Освобождено {SizeHelper.Format(freed)}");
                    SetStatus($"Освобождено {SizeHelper.Format(freed)} ✓", StatusKind.Done);
                    ListCountLabel.Text = skipped > 0 ? $"Пропущено: {skipped}" : "";
                }

                TotalSizeText.Text = "0 МБ";
                FileCountText.Text = "0 файлов";
                AddHistory(deleted + (doRecycle ? 1 : 0), freed);
                LoadDiskInfo();

                if (freed > 0 && !wasCancelled)
                {
                    var stats = new List<StatRow>
                    {
                        new() { Label = "Удалено файлов:",  Value = deleted.ToString(),         Color = "#4A9EFF" },
                        new() { Label = "Освобождено:",     Value = SizeHelper.Format(freed),   Color = "#06D6C7" },
                    };
                    if (skipped > 0)
                        stats.Add(new() { Label = "Пропущено (заняты):", Value = skipped.ToString(), Color = "#FF8C00" });

                    new CustomDialog("Очистка завершена!",
                        "🌟  Ваш компьютер стал чище!",
                        DialogKind.Success, stats).ShowDialog();
                }
            }
        }

        private void CleanEmptyDirs(IEnumerable<string> roots)
        {
            foreach (var root in roots)
            {
                try
                {
                    foreach (var d in Directory.GetDirectories(root, "*", SearchOption.AllDirectories)
                                               .OrderByDescending(x => x.Length))
                        try { if (!Directory.EnumerateFileSystemEntries(d).Any()) Directory.Delete(d); }
                        catch { }
                }
                catch { }
            }
        }

        private void AddHistory(int count, long bytes)
        {
            _historyItems.Insert(0, new HistoryItem
            {
                Date      = DateTime.Now.ToString("dd.MM.yyyy  HH:mm"),
                Freed     = SizeHelper.Format(bytes),
                FileCount = $"{count} файлов"
            });
            while (_historyItems.Count > 20) _historyItems.RemoveAt(_historyItems.Count - 1);
        }

        // ── TOOLBAR ──────────────────────────────────────────────────────
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in new[] { ChkTempFiles, ChkWinTemp, ChkRecycleBin,
                ChkBrowserCache, ChkPrefetch, ChkThumbnails, ChkEventLogs,
                ChkDnsCache, ChkMSOffice, ChkExternalDrives })
                if (cb != null) cb.IsChecked = true;
            SaveSettings();
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in new[] { ChkTempFiles, ChkWinTemp, ChkRecycleBin,
                ChkBrowserCache, ChkPrefetch, ChkThumbnails, ChkEventLogs,
                ChkDnsCache, ChkMSOffice, ChkExternalDrives })
                if (cb != null) cb.IsChecked = false;
            SaveSettings();
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
            => new AboutWindow { Owner = this }.ShowDialog();

        // ── WINDOW CHROME ─────────────────────────────────────────────────
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) MaximizeRestore();
            else if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }
        private void MinBtn_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void MaxBtn_Click(object sender, RoutedEventArgs e) => MaximizeRestore();
        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void MaximizeRestore() =>
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

        // ── ЭФФЕКТЫ ДЛЯ ВКЛАДОК ────────────────────────────────────────────
        private void TabHeader_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border header)
            {
                if (header.Background == Brushes.Transparent ||
                    (header.Background as SolidColorBrush)?.Color.A == 0)
                {
                    header.Background = new SolidColorBrush(Color.FromArgb(50, 74, 158, 255));
                }
                header.Opacity = 1.0;
                header.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(0x4A, 0x9E, 0xFF),
                    BlurRadius = 8, ShadowDepth = 0, Opacity = 0.3
                };
            }
        }

        private void TabHeader_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border header)
            {
                bool isFilesActive   = FilesPanel.Visibility   == Visibility.Visible;
                bool isHistoryActive = HistoryPanel.Visibility == Visibility.Visible;

                if (header == TabFilesHeader   && !isFilesActive)
                    header.Background = Brushes.Transparent;

                if (header == TabHistoryHeader && !isHistoryActive)
                    header.Background = Brushes.Transparent;

                header.Opacity = 0.9;
                header.Effect = null;
            }
        }
    }
}
