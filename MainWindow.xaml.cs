using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
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

// ── Алиасы: разрешаем конфликты WPF ↔ WinForms ──────────────────────────
// UseWindowsForms=true в .csproj делает видимыми System.Drawing и
// System.Windows.Forms. Для каждого конфликтующего типа явно фиксируем
// WPF-версию — остальной код файла остаётся без изменений.
using Color                = System.Windows.Media.Color;
using FontFamily           = System.Windows.Media.FontFamily;
using MouseEventArgs       = System.Windows.Input.MouseEventArgs;
using CheckBox             = System.Windows.Controls.CheckBox;
using Application          = System.Windows.Application;
using Point                = System.Windows.Point;
using Orientation          = System.Windows.Controls.Orientation;
using FontStyle            = System.Windows.FontStyle;
using ColorConverter       = System.Windows.Media.ColorConverter;
using Brushes              = System.Windows.Media.Brushes;
using Brush                = System.Windows.Media.Brush;
using HorizontalAlignment  = System.Windows.HorizontalAlignment;
using VerticalAlignment    = System.Windows.VerticalAlignment;

namespace CleanupTemp_Pro
{
    // ══════════════════════════════════════════════════════════════════
    //  МОДЕЛИ
    // ══════════════════════════════════════════════════════════════════

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

    /// <summary>Элемент белого списка — путь файла или папки, исключённый из очистки.</summary>
    public class ExclusionItem
    {
        public string Path { get; set; } = "";
        /// <summary>📁 для папки, 📄 для файла, 🔒 для ручного ввода.</summary>
        public string Icon { get; set; } = "🔒";
    }

    /// <summary>Файл для вкладки «Большие файлы».</summary>
    public class LargeFileItem
    {
        public string Icon     { get; set; } = "📦";
        public string Path     { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Category { get; set; } = "";
        public long   SizeBytes { get; set; }
        public string SizeText  => SizeHelper.Format(SizeBytes);
    }

    /// <summary>Сегмент круговой диаграммы очистки.</summary>
    public class ChartSegment
    {
        public string   Category  { get; set; } = "";
        public long     SizeBytes { get; set; }
        public double   Pct       { get; set; }   // 0..1
        public string   PctText   => $"{Pct * 100:F1}%";
        public string   SizeText  => SizeHelper.Format(SizeBytes);
        public string   HexColor  { get; set; } = "#888888";
        /// <summary>Кисть для биндинга в XAML DataTemplate.</summary>
        public SolidColorBrush ColorBrush =>
            new(Color.FromRgb(
                Convert.ToByte(HexColor.Substring(1, 2), 16),
                Convert.ToByte(HexColor.Substring(3, 2), 16),
                Convert.ToByte(HexColor.Substring(5, 2), 16)));
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

    // ══════════════════════════════════════════════════════════════════
    //  BulkObservableCollection — Reset вместо тысяч Add-уведомлений
    // ══════════════════════════════════════════════════════════════════

    public sealed class BulkObservableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Добавляет элементы без уведомления на каждый элемент.
        /// Бросает одно Reset-уведомление в конце.
        /// Для виртуализированного ListView Reset вызывает пересчёт — 
        /// поэтому используем только когда батч > 50 элементов.
        /// </summary>
        public void AddRange(IEnumerable<T> items)
        {
            var list = items as IList<T> ?? items.ToList();
            if (list.Count == 0) return;

            if (list.Count <= 50)
            {
                // Небольшой батч — добавляем по одному (виртуализация работает лучше)
                foreach (var item in list)
                    Add(item);
            }
            else
            {
                // Большой батч — без лишних уведомлений, одно Reset в конце
                foreach (var item in list)
                    Items.Add(item);
                OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                    System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  ЛОГГЕР — пишет в %AppData%\CleanupTempPro\app.log
    // ══════════════════════════════════════════════════════════════════

    internal static class AppLog
    {
        private static readonly string LogPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CleanupTempPro", "app.log");

        private static readonly object _lock = new();

        public static void Info(string msg)  => Write("INFO ", msg);
        public static void Warn(string msg)  => Write("WARN ", msg);
        public static void Error(string msg, Exception? ex = null)
            => Write("ERROR", ex == null ? msg
                : $"{msg} | {ex.GetType().Name}: {ex.Message}{Environment.NewLine}  StackTrace: {ex.StackTrace}");

        private static void Write(string level, string msg)
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(LogPath)!);
                string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {msg}";
                lock (_lock)
                {
                    // Ротация: если лог > 2МБ — обрезаем старую половину
                    var fi = new FileInfo(LogPath);
                    if (fi.Exists && fi.Length > 2 * 1024 * 1024)
                        RotateLog();
                    File.AppendAllText(LogPath, line + Environment.NewLine);
                }
                Debug.WriteLine(line);
            }
            catch { /* логгер не должен ронять приложение */ }
        }

        private static void RotateLog()
        {
            try
            {
                var lines = File.ReadAllLines(LogPath);
                File.WriteAllLines(LogPath, lines.Skip(lines.Length / 2));
            }
            catch { }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  P/INVOKE
    // ══════════════════════════════════════════════════════════════════

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct SHQUERYRBINFO { public int cbSize; public long i64Size; public long i64NumItems; }

    // ══════════════════════════════════════════════════════════════════
    //  MAIN WINDOW
    // ══════════════════════════════════════════════════════════════════

    public partial class MainWindow : Window
    {
        [DllImport("shell32.dll")]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string? root, uint flags);
        [DllImport("shell32.dll")]
        static extern int SHQueryRecycleBin(string? root, ref SHQUERYRBINFO info);

        // ── Состояние ─────────────────────────────────────────────────
        private CancellationTokenSource? _cts;
        private readonly BulkObservableCollection<FileItem> _fileItems    = new();
        private readonly ObservableCollection<HistoryItem>  _historyItems = new();
        // Полный список для очистки — не ограничен 5000 как _fileItems (только для отображения)
        private readonly List<FileItem> _allFoundItems = new();

        // ── Белый список исключений ───────────────────────────────────
        // HashSet для O(1) проверки при сканировании тысяч файлов
        private HashSet<string> _excludePaths = new(StringComparer.OrdinalIgnoreCase);
        private readonly ObservableCollection<ExclusionItem> _exclusionItems = new();
        private static readonly string ExclusionsPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CleanupTempPro", "exclusions.json");

        // ── Большие файлы ─────────────────────────────────────────────
        private readonly BulkObservableCollection<LargeFileItem> _largeFileItems = new();
        private CancellationTokenSource? _largeScanCts;

        // Флаги выполнения — читаются и пишутся только на UI потоке (async void гарантирует это)
        // Два отдельных флага чтобы сканирование и очистка не мешали друг другу
        private long _totalFoundBytes;
        private long _cleanedBytes;
        private bool _isScanning = false;  // только UI поток
        private bool _isCleaning = false;  // только UI поток
        private bool _canClean;
        private bool _canStop;
        private int  _statTemp, _statBrowser, _statRecycle;
        private DispatcherTimer? _pulseTimer;
        private bool _showingHistory;
        private volatile bool _wasInterruptedBySleep;

        // ── Защита папок ──────────────────────────────────────────────
        private static readonly HashSet<string> _protectedFolderNames =
            new(StringComparer.OrdinalIgnoreCase)
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

        private static readonly HashSet<string> _protectedTempSubfolders =
            new(StringComparer.OrdinalIgnoreCase)
        {
            ".net", "Cloudflare WARP", "WARP",
            "VBCSCompiler", "MSBuild", "VSLogs", "VisualStudio",
            "SquirrelTemp", "nvidia", "AMD",
            "7zS", "RarSFX", "wct",
        };

        private static readonly HashSet<string> _junkExtensions =
            new(StringComparer.OrdinalIgnoreCase)
        {
            ".tmp", ".bak", ".old", ".dmp", ".chk", ".gid",
            ".fts", ".ftg", ".wbk", ".xlk", ".~doc", ".~xls", ".~ppt", ".temp"
        };

        private static readonly HashSet<string> _safeExtensionsInTemp =
            new(StringComparer.OrdinalIgnoreCase)
        {
            ".dll", ".exe", ".sys", ".pdb", ".xml", ".json", ".config",
            ".ini", ".log", ".lock", ".pid", ".manifest", ".cat",
            ".svclog", ".etl", ".diaglog",
            ".msi", ".msp", ".cab",
            ".ps1", ".bat", ".cmd",
        };

        private static readonly HashSet<string> _junkFileNames =
            new(StringComparer.OrdinalIgnoreCase)
        {
            "thumbs.db", "ehthumbs.db", "ehthumbs_vista.db", ".ds_store"
        };

        private static readonly HashSet<string> _protectedFileNames =
            new(StringComparer.OrdinalIgnoreCase)
        {
            "desktop.ini", "thumbs.db", "autorun.inf",
            "WebCacheV01.dat", "WebCacheV24.dat",
            "WebCacheV01.jfm", "WebCacheV24.jfm",
            "V01tmp.log", "V24tmp.log",
            "journal.baj", "journal.log",
            "index",
            "lockfile", "LOCK", "LOG", "LOG.old",
            "places.sqlite", "cookies.sqlite", "webappsstore.sqlite",
        };

        private static readonly HashSet<string> _protectedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
        {
            ".dat", ".jfm", ".db-wal", ".db-shm", ".sqlite", ".sqlite-wal", ".sqlite-shm"
        };

        // Количество потоков — стартуем с безопасного дефолта (2),
        // WMI-запрос уточняет значение асинхронно после старта окна.
        // volatile гарантирует видимость обновления во всех потоках.
        // SSD/NVMe → 4 потока, HDD → 1 поток, неизвестно → 2 потока
        private static volatile int ScanDegree  = 2;
        private static volatile int CleanDegree = 2;

        /// <summary>
        /// Запускает определение типа диска (SSD/HDD) через WMI в фоновом потоке.
        /// Вызывается один раз из конструктора — не блокирует UI ни на миллисекунду.
        /// По завершении обновляет ScanDegree и CleanDegree (volatile, видны всем потокам).
        /// Если WMI занят/недоступен — поля остаются на безопасном дефолте (2 потока).
        /// </summary>
        private static void InitThreadCountAsync()
        {
            _ = Task.Run(() =>
            {
                try
                {
                    var scope = new System.Management.ManagementScope(
                        @"\\.\root\microsoft\windows\storage");
                    scope.Connect();

                    string sysRoot = Path.GetPathRoot(
                        Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? @"C:\";
                    string driveLetter = sysRoot.TrimEnd('\\'); // "C:"

                    using var partQuery = new System.Management.ManagementObjectSearcher(
                        scope,
                        new System.Management.ObjectQuery(
                            $"SELECT DiskNumber FROM MSFT_Partition WHERE DriveLetter='{driveLetter[0]}'"));

                    foreach (System.Management.ManagementObject part in partQuery.Get())
                    {
                        uint diskNumber = (uint)part["DiskNumber"];

                        using var diskQuery = new System.Management.ManagementObjectSearcher(
                            scope,
                            new System.Management.ObjectQuery(
                                $"SELECT MediaType FROM MSFT_PhysicalDisk WHERE DeviceId='{diskNumber}'"));

                        foreach (System.Management.ManagementObject disk in diskQuery.Get())
                        {
                            uint mediaType = (uint)disk["MediaType"];
                            // 3 = HDD, 4 = SSD, 5 = SCM (Storage Class Memory), 0 = Unknown
                            bool isSsd  = mediaType != 3;
                            int threads = isSsd ? Math.Min(4, Environment.ProcessorCount) : 1;
                            // Атомарно обновляем оба поля — следующий вызов Scan/Clean
                            // уже получит правильное значение
                            ScanDegree  = threads;
                            CleanDegree = threads;
                            AppLog.Info($"WMI async: Drive {driveLetter}: MediaType={mediaType} " +
                                        $"→ {(isSsd ? "SSD" : "HDD")}, threads={threads}");
                            return;
                        }
                    }
                    // WMI ответил, но диск не найден — оставляем дефолт 2
                    AppLog.Warn("WMI async: system disk not found in MSFT_Partition — keeping 2 threads");
                }
                catch (Exception ex)
                {
                    // WMI недоступен / нет прав / зависание → дефолт 2 уже стоит, ничего менять не нужно
                    AppLog.Warn($"WMI async failed: {ex.Message} — keeping 2 threads");
                }
            });
        }

        // ── Вспомогательные методы ────────────────────────────────────

        private static bool IsInProtectedFolder(string filePath)
        {
            var parts = filePath.Split(
                System.IO.Path.DirectorySeparatorChar,
                System.IO.Path.AltDirectorySeparatorChar);
            foreach (var part in parts)
                if (_protectedFolderNames.Contains(part) ||
                    _protectedTempSubfolders.Contains(part))
                    return true;
            return false;
        }

        /// <summary>
        /// Проверяет, попадает ли файл под белый список пользователя.
        /// Сравниваем как точное совпадение (файл), так и «начинается с» (папка).
        /// </summary>
        private bool IsExcluded(string filePath)
        {
            // Точное совпадение — файл явно добавлен в исключения
            if (_excludePaths.Contains(filePath)) return true;
            // Папка в исключениях — пропускаем всё, что в ней лежит
            foreach (var ex in _excludePaths)
            {
                if (filePath.StartsWith(ex + System.IO.Path.DirectorySeparatorChar,
                                        StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>Возвращает true если .tmp файл заблокирован активным процессом.</summary>
        private static bool IsTmpFileLocked(string path)
        {
            try
            {
                using var fs = new FileStream(
                    path, FileMode.Open, FileAccess.ReadWrite, FileShare.None,
                    bufferSize: 1, useAsync: false);
                return false;
            }
            catch (IOException)               { return true;  }
            catch (UnauthorizedAccessException) { return false; }
            catch (Exception ex)
            {
                AppLog.Warn($"IsTmpFileLocked unexpected: {path} | {ex.Message}");
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  НАСТРОЙКИ
        // ══════════════════════════════════════════════════════════════

        private static readonly string SettingsPath = System.IO.Path.Combine(
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
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SettingsPath)!);
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
            catch (Exception ex) { AppLog.Error("SaveSettings failed", ex); }
        }

        // Снимок галочек — CheckBox.IsChecked нельзя читать вне UI потока
        private record ScanOptions(
            bool TempFiles, bool WinTemp, bool BrowserCache, bool Prefetch,
            bool Thumbnails, bool DnsCache, bool MSOffice, bool EventLogs, bool ExternalDrives);

        private ScanOptions ReadScanOptions() => new(
            TempFiles     : ChkTempFiles?.IsChecked      == true,
            WinTemp       : ChkWinTemp?.IsChecked        == true,
            BrowserCache  : ChkBrowserCache?.IsChecked   == true,
            Prefetch      : ChkPrefetch?.IsChecked       == true,
            Thumbnails    : ChkThumbnails?.IsChecked     == true,
            DnsCache      : ChkDnsCache?.IsChecked       == true,
            MSOffice      : ChkMSOffice?.IsChecked       == true,
            EventLogs     : ChkEventLogs?.IsChecked      == true,
            ExternalDrives: ChkExternalDrives?.IsChecked == true);

        private bool _settingsLoaded = false;

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return;
                var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
                if (s == null) return;

                void Set(CheckBox? cb, bool v) { if (cb != null) cb.IsChecked = v; }
                Set(ChkTempFiles,     s.TempFiles);
                Set(ChkWinTemp,       s.WinTemp);
                Set(ChkRecycleBin,    s.RecycleBin);
                Set(ChkBrowserCache,  s.BrowserCache);
                Set(ChkThumbnails,    s.Thumbnails);
                Set(ChkDnsCache,      s.DnsCache);
                Set(ChkMSOffice,      s.MSOffice);
                Set(ChkPrefetch,      s.Prefetch);
                Set(ChkEventLogs,     s.EventLogs);
                Set(ChkExternalDrives, s.ExternalDrives);
            }
            catch (Exception ex) { AppLog.Error("LoadSettings failed", ex); }
            finally { _settingsLoaded = true; }
        }

        private void Chk_Changed(object sender, RoutedEventArgs e)
        {
            if (_settingsLoaded) SaveSettings();
        }

        // ══════════════════════════════════════════════════════════════
        //  USB / SLEEP / WAKE DETECTION
        // ══════════════════════════════════════════════════════════════

        private HwndSource? _hwndSource;

        private const int WM_DEVICECHANGE          = 0x0219;
        private const int DBT_DEVICEARRIVAL        = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVTYP_VOLUME        = 0x0002;
        private const int WM_POWERBROADCAST        = 0x0218;
        private const int PBT_APMSUSPEND           = 0x0004;
        private const int PBT_APMRESUMESUSPEND     = 0x0007;
        private const int PBT_APMRESUMEAUTOMATIC   = 0x0012;

        [StructLayout(LayoutKind.Sequential)]
        private struct DEV_BROADCAST_VOLUME
        {
            public int   dbcv_size, dbcv_devicetype, dbcv_reserved, dbcv_unitmask;
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
            if (msg == WM_POWERBROADCAST)
            {
                int ev = wParam.ToInt32();
                if (ev == PBT_APMSUSPEND) OnSystemSleep();
                else if (ev is PBT_APMRESUMESUSPEND or PBT_APMRESUMEAUTOMATIC) OnSystemWake();
            }
            else if (msg == WM_DEVICECHANGE)
            {
                int ev = wParam.ToInt32();
                if (ev is DBT_DEVICEARRIVAL or DBT_DEVICEREMOVECOMPLETE)
                {
                    if (lParam != IntPtr.Zero)
                    {
                        var vol = Marshal.PtrToStructure<DEV_BROADCAST_VOLUME>(lParam);
                        if (vol.dbcv_devicetype == DBT_DEVTYP_VOLUME)
                            Dispatcher.InvokeAsync(LoadDiskInfo);
                    }
                }
            }
            return IntPtr.Zero;
        }

        private void OnSystemSleep()
        {
            if (_isScanning || _isCleaning)
            {
                _wasInterruptedBySleep = true;
                _cts?.Cancel();
                AppLog.Info("System going to sleep — scan/clean cancelled");
            }
        }

        private void OnSystemWake()
        {
            if (_wasInterruptedBySleep)
            {
                _wasInterruptedBySleep = false;
                Dispatcher.InvokeAsync(() =>
                    SetStatus("Система пробудилась — запустите сканирование заново", StatusKind.Stopped));
                AppLog.Info("System woke up after interrupted operation");
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ══════════════════════════════════════════════════════════════

        public MainWindow()
        {
            InitializeComponent();
            FileListView.ItemsSource       = _fileItems;
            HistoryListView.ItemsSource    = _historyItems;
            LargeFilesListView.ItemsSource = _largeFileItems;
            ExclusionsListView.ItemsSource = _exclusionItems;
            LoadLogo();
            LoadDiskInfo();
            LoadSettings();
            LoadHistory();
            LoadExclusions();
            SetStatus("Готов к работе", StatusKind.Ready);
            StartPulse();
            SourceInitialized += (_, _) => InitUsbDetection();
            Closing += (_, _) =>
            {
                SaveSettings();
                SaveHistory();
                SaveExclusions();
                _hwndSource?.RemoveHook(WndProc);
                _hwndSource?.Dispose();
                // OnExplicitShutdown + NotifyIcon держат процесс живым после Close().
                // Явный Shutdown() завершает WPF dispatcher и убивает NotifyIcon loop.
                Application.Current.Shutdown();
            };
            // WMI-запрос к диску — строго в фоне, не блокирует UI при старте.
            // Обновит ScanDegree/CleanDegree до начала первого сканирования.
            InitThreadCountAsync();
            // Перехват сворачивания: Hide() вместо Minimize — окно уходит в трей
            StateChanged += MainWindow_StateChanged;
            AppLog.Info("CleanupTemp Pro started");
        }

        // ══════════════════════════════════════════════════════════════
        //  HISTORY — сохранение на диск
        // ══════════════════════════════════════════════════════════════

        private static readonly string HistoryPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CleanupTempPro", "history.json");

        private void SaveHistory()
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(HistoryPath)!);
                File.WriteAllText(HistoryPath,
                    JsonSerializer.Serialize(_historyItems.ToList(),
                        new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex) { AppLog.Error("SaveHistory failed", ex); }
        }

        private void LoadHistory()
        {
            try
            {
                if (!File.Exists(HistoryPath)) return;
                var items = JsonSerializer.Deserialize<List<HistoryItem>>(
                    File.ReadAllText(HistoryPath));
                if (items == null) return;
                foreach (var item in items)
                    _historyItems.Add(item);
            }
            catch (Exception ex) { AppLog.Error("LoadHistory failed", ex); }
        }

        private void AddHistory(int count, long bytes)
        {
            _historyItems.Insert(0, new HistoryItem
            {
                Date      = DateTime.Now.ToString("dd.MM.yyyy  HH:mm"),
                Freed     = SizeHelper.Format(bytes),
                FileCount = $"{count} файлов"
            });
            while (_historyItems.Count > 20)
                _historyItems.RemoveAt(_historyItems.Count - 1);
            SaveHistory();
        }

        // ══════════════════════════════════════════════════════════════
        //  UI — ВКЛАДКИ / ПУЛЬС / ДИСКИ / СТАТУС
        // ══════════════════════════════════════════════════════════════

        private readonly FontFamily _fontSemibold = new("Segoe UI Semibold");
        private readonly FontFamily _fontRegular  = new("Segoe UI");

        private enum ActiveTab { Files, History, Large, Exclusions }

        private void SwitchTab(ActiveTab tab)
        {
            _showingHistory = (tab == ActiveTab.History);

            FilesPanel.Visibility      = tab == ActiveTab.Files      ? Visibility.Visible : Visibility.Collapsed;
            HistoryPanel.Visibility    = tab == ActiveTab.History    ? Visibility.Visible : Visibility.Collapsed;
            LargeFilesPanel.Visibility = tab == ActiveTab.Large      ? Visibility.Visible : Visibility.Collapsed;
            ExclusionsPanel.Visibility = tab == ActiveTab.Exclusions ? Visibility.Visible : Visibility.Collapsed;

            void Reset(Border hdr, TextBlock txt)
            {
                hdr.Background = Brushes.Transparent;
                txt.Foreground = (Brush)FindResource("TextSecondaryBrush");
                txt.FontFamily = _fontRegular;
            }
            Reset(TabFilesHeader,      TabFilesText);
            Reset(TabHistoryHeader,    TabHistoryText);
            Reset(TabLargeHeader,      TabLargeText);
            Reset(TabExclusionsHeader, TabExclusionsText);

            void Activate(Border hdr, TextBlock txt)
            {
                hdr.Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x2A, 0x4A));
                txt.Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x9E, 0xFF));
                txt.FontFamily = _fontSemibold;
            }

            switch (tab)
            {
                case ActiveTab.Files:
                    Activate(TabFilesHeader, TabFilesText);
                    ListCountLabel.Text = _fileItems.Count > 0 ? $"{_fileItems.Count} объектов" : "";
                    break;
                case ActiveTab.History:
                    Activate(TabHistoryHeader, TabHistoryText);
                    ListCountLabel.Text = $"{_historyItems.Count} записей";
                    break;
                case ActiveTab.Large:
                    Activate(TabLargeHeader, TabLargeText);
                    ListCountLabel.Text = _largeFileItems.Count > 0
                        ? $"{_largeFileItems.Count} файлов" : "";
                    break;
                case ActiveTab.Exclusions:
                    Activate(TabExclusionsHeader, TabExclusionsText);
                    ListCountLabel.Text = _exclusionItems.Count > 0
                        ? $"{_exclusionItems.Count} исключений" : "нет исключений";
                    break;
            }
        }

        // Совместимость со старым вызовом SwitchTab(false) при старте сканирования
        private void SwitchTab(bool showHistory)
            => SwitchTab(showHistory ? ActiveTab.History : ActiveTab.Files);

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

        private void LoadDiskInfo()
        {
            try
            {
                DisksPanel.Items.Clear();
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady &&
                           d.DriveType is DriveType.Fixed or DriveType.Removable)
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
                        if      (pct >= 0.9)  { barC1 = Color.FromRgb(0xFF,0x3D,0x00); barC2 = Color.FromRgb(0xCC,0x00,0x44); }
                        else if (pct >= 0.75) { barC1 = Color.FromRgb(0xFF,0x8C,0x00); barC2 = Color.FromRgb(0xFF,0xA5,0x00); }
                        else                  { barC1 = Color.FromRgb(0x4A,0x9E,0xFF); barC2 = Color.FromRgb(0xA8,0x55,0xF7); }

                        var barContainer = new Border
                        {
                            Height = 6, CornerRadius = new CornerRadius(3),
                            Background = new SolidColorBrush(Color.FromRgb(0x1A,0x1A,0x3A))
                        };
                        var bar = new Border
                        {
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Height = 6, CornerRadius = new CornerRadius(3), Width = 0,
                            Background = new LinearGradientBrush(barC1, barC2,
                                new Point(0,0.5), new Point(1,0.5))
                        };
                        barContainer.Child = bar;

                        var card   = new StackPanel { Margin = new Thickness(0,0,0,8) };
                        var header = new Grid { Margin = new Thickness(0,0,0,2) };
                        header.ColumnDefinitions.Add(new ColumnDefinition());
                        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var namePanel = new StackPanel { Orientation = Orientation.Horizontal };
                        namePanel.Children.Add(new TextBlock
                        {
                            Text = driveIcon, FontSize = 11,
                            Margin = new Thickness(0,0,5,0),
                            VerticalAlignment = VerticalAlignment.Center
                        });
                        namePanel.Children.Add(new TextBlock
                        {
                            Text = $"{letter}  {label}", FontFamily = _fontSemibold,
                            FontSize = 11,
                            Foreground = new SolidColorBrush(Color.FromRgb(0xE8,0xE8,0xFF)),
                            VerticalAlignment = VerticalAlignment.Center
                        });
                        Grid.SetColumn(namePanel, 0);
                        header.Children.Add(namePanel);

                        var pctColor = pct >= 0.9  ? Color.FromRgb(0xFF,0x4A,0x6A)
                                     : pct >= 0.75 ? Color.FromRgb(0xFF,0x8C,0x00)
                                     :               Color.FromRgb(0x4A,0x9E,0xFF);
                        var pctBlock = new TextBlock
                        {
                            Text = $"{pct*100:F0}%", FontFamily = _fontSemibold,
                            FontSize = 11,
                            Foreground = new SolidColorBrush(pctColor),
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        Grid.SetColumn(pctBlock, 1);
                        header.Children.Add(pctBlock);
                        card.Children.Add(header);
                        card.Children.Add(new TextBlock
                        {
                            Text = $"{SizeHelper.Format(used)} / {SizeHelper.Format(drv.TotalSize)}",
                            FontFamily = _fontRegular, FontSize = 10,
                            Foreground = new SolidColorBrush(Color.FromRgb(0x88,0x88,0xBB)),
                            Margin = new Thickness(0,0,0,4)
                        });
                        card.Children.Add(barContainer);
                        DisksPanel.Items.Add(card);

                        var captBar  = bar;
                        var captCont = barContainer;
                        double captPct = pct;
                        Dispatcher.InvokeAsync(() =>
                        {
                            double w = captCont.ActualWidth > 0 ? captCont.ActualWidth : 230;
                            captBar.BeginAnimation(FrameworkElement.WidthProperty,
                                new DoubleAnimation(0, w * captPct, TimeSpan.FromSeconds(1.1))
                                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                        }, DispatcherPriority.Loaded);
                    }
                    catch (Exception ex) { AppLog.Warn($"LoadDiskInfo drive {drv.Name}: {ex.Message}"); }
                }
            }
            catch (Exception ex) { AppLog.Error("LoadDiskInfo failed", ex); }
        }

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
            if (ColorConverter.ConvertFromString(hex) is Color c)
                StatusDotColor.Color = c;

            // Синхронизируем трей — маппинг StatusKind → TrayStatus
            TrayStatus tray = kind switch
            {
                StatusKind.Scanning => TrayStatus.Scanning,
                StatusKind.Cleaning => TrayStatus.Cleaning,
                StatusKind.Done     => TrayStatus.Done,
                StatusKind.Stopped  => TrayStatus.Stopped,
                _                   => TrayStatus.Ready,
            };
            NotifyTray(tray, kind == StatusKind.Done ? text : null);
        }

        private void SetProgress(double pct, string label)
        {
            ProgressLabel.Text   = label;
            ProgressPercent.Text = $"{pct:F0}%";
            double w      = ProgressBarContainer.ActualWidth > 0 ? ProgressBarContainer.ActualWidth : 600;
            double target = Math.Clamp(w * pct / 100.0, 0, w);
            // Прямое присвоение вместо анимации — анимация при частых обновлениях грузит UI поток
            ProgressBarFill.Width = target;
        }

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

        // ══════════════════════════════════════════════════════════════
        //  КНОПКИ — HOVER ЭФФЕКТЫ
        // ══════════════════════════════════════════════════════════════

        private static System.Windows.Media.Effects.DropShadowEffect Glow(Color c, double r = 28, double o = 0.85)
            => new() { Color = c, BlurRadius = r, ShadowDepth = 0, Opacity = o };

        private static LinearGradientBrush HGrad(Color c1, Color c2)
            => new(c1, c2, new Point(0, 0.5), new Point(1, 0.5));

        // SCAN
        private void ScanBorder_Click(object sender, MouseButtonEventArgs e)
        {
            if (!ScanBtnBorder.IsEnabled) return;
            ScanBtn_Execute();
        }
        private void ScanBorder_Enter(object sender, MouseEventArgs e)
        {
            if (ScanBtnBorder.IsEnabled)
                ScanBtnBorder.Effect = Glow(Color.FromRgb(0x4A, 0x9E, 0xFF));
        }
        private void ScanBorder_Leave(object sender, MouseEventArgs e)
        {
            ScanBtnBorder.Opacity = _isScanning ? 0.4 : 1.0;
            ScanBtnBorder.Effect  = null;
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
                CleanBtnBorder.Effect = Glow(Color.FromRgb(0xFF, 0x50, 0x70), r: 24, o: 0.75);
        }
        private void CleanBorder_Leave(object sender, MouseEventArgs e)
        {
            CleanBtnBorder.Opacity = _canClean ? 1.0 : 0.5;
            CleanBtnBorder.Effect  = null;
        }

        // STOP
        private void StopBorder_Click(object sender, MouseButtonEventArgs e)
        {
            if (!StopBtnBorder.IsEnabled) return;
            _cts?.Cancel();
            SetStatus("Остановка...", StatusKind.Stopped);
            AppLog.Info("User requested stop");
        }
        private void StopBorder_Enter(object sender, MouseEventArgs e)
        {
            if (!StopBtnBorder.IsEnabled) return;
            StopBtnBorder.Background  = HGrad(Color.FromRgb(0x0A,0x30,0x4A), Color.FromRgb(0x0A,0x28,0x3A));
            StopBtnBorder.BorderBrush = HGrad(Color.FromRgb(0x4A,0x9E,0xFF), Color.FromRgb(0x06,0xD6,0xC7));
            StopBtnBorder.Effect      = Glow(Color.FromRgb(0x06,0xD6,0xC7), r: 35, o: 1.0);
        }
        private void StopBorder_Leave(object sender, MouseEventArgs e)
        {
            StopBtnBorder.Opacity     = _canStop ? 1.0 : 0.4;
            StopBtnBorder.Background  = HGrad(Color.FromRgb(0x0A,0x2A,0x3A), Color.FromRgb(0x0A,0x20,0x30));
            StopBtnBorder.BorderBrush = HGrad(Color.FromRgb(0x4A,0x9E,0xFF), Color.FromRgb(0x06,0xD6,0xC7));
            StopBtnBorder.Effect      = null;
        }

        // ══════════════════════════════════════════════════════════════
        //  WINDOWS UPDATE SERVICE
        // ══════════════════════════════════════════════════════════════

        private static bool IsWindowsUpdateActive()
        {
            try
            {
                using var svc = new ServiceController("wuauserv");
                if (svc.Status != ServiceControllerStatus.Running) return false;

                string dir = @"C:\Windows\SoftwareDistribution\Download";
                if (!Directory.Exists(dir)) return false;

                var cutoff = DateTime.UtcNow.AddMinutes(-10);
                bool active = Directory.EnumerateFiles(dir, "*.esd", SearchOption.AllDirectories)
                    .Concat(Directory.EnumerateFiles(dir, "*.cab", SearchOption.AllDirectories))
                    .Take(20)
                    .Any(f =>
                    {
                        try { return File.GetLastWriteTimeUtc(f) >= cutoff; }
                        catch { return false; }
                    });
                return active;
            }
            catch (Exception ex) { AppLog.Warn($"IsWindowsUpdateActive: {ex.Message}"); return false; }
        }

        private static bool StopWindowsUpdateService(out bool wasRunning)
        {
            wasRunning = false;
            try
            {
                try
                {
                    using var bits = new ServiceController("BITS");
                    if (bits.Status == ServiceControllerStatus.Running)
                    {
                        bits.Stop();
                        bits.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
                    }
                }
                catch (Exception ex) { AppLog.Warn($"BITS stop: {ex.Message}"); }

                using var svc = new ServiceController("wuauserv");
                wasRunning = svc.Status is ServiceControllerStatus.Running
                                       or ServiceControllerStatus.StartPending;

                if (svc.Status is not ServiceControllerStatus.Stopped
                               and not ServiceControllerStatus.StopPending)
                {
                    svc.Stop();
                    svc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(25));
                }

                // Короткий spin-wait — даём ОС отпустить хэндлы файлов
                var deadline = DateTime.UtcNow.AddMilliseconds(800);
                while (DateTime.UtcNow < deadline) Thread.Sleep(50);

                AppLog.Info("Windows Update service stopped");
                return true;
            }
            catch (Exception ex)
            {
                AppLog.Error("StopWindowsUpdateService failed", ex);
                return false;
            }
        }

        private static void StartWindowsUpdateService()
        {
            foreach (var name in new[] { "wuauserv", "BITS" })
            {
                try
                {
                    using var svc = new ServiceController(name);
                    if (svc.Status == ServiceControllerStatus.Stopped)
                        svc.Start();
                }
                catch (Exception ex) { AppLog.Warn($"StartService {name}: {ex.Message}"); }
            }
            AppLog.Info("Windows Update service restored");
        }

        // ══════════════════════════════════════════════════════════════
        //  ПУТИ СКАНИРОВАНИЯ
        // ══════════════════════════════════════════════════════════════

        private static List<(string Path, string Cat, string Icon)> GetScanPaths(ScanOptions opt)
        {
            var L = new List<(string, string, string)>();
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            if (opt.TempFiles)
                L.Add((System.IO.Path.GetTempPath(), "Temp (пользователь)", "🗂️"));

            if (opt.WinTemp)
            {
                L.Add((@"C:\Windows\Temp", "Windows Temp", "⚙️"));
                L.Add((@"C:\Windows\SoftwareDistribution\Download", "Windows Update кэш", "⚙️"));
            }

            if (opt.BrowserCache)
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
                        L.Add((System.IO.Path.Combine(d, "startupCache"), "Firefox Startup", "🦊"));
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

            if (opt.Prefetch)
                L.Add((@"C:\Windows\Prefetch", "Prefetch", "⚡"));

            if (opt.Thumbnails)
                L.Add((System.IO.Path.Combine(local, @"Microsoft\Windows\Explorer"), "Thumbnails кэш", "🖼️"));

            if (opt.DnsCache)
                L.Add((System.IO.Path.Combine(local, @"Microsoft\Windows\INetCache"), "IE/Edge Legacy Cache", "🔗"));

            if (opt.MSOffice)
            {
                L.Add((System.IO.Path.Combine(local, @"Microsoft\Office\16.0\OfficeFileCache"),   "Office кэш",      "📎"));
                L.Add((System.IO.Path.Combine(local, @"Microsoft\Office\16.0\OfficeFileCache\0"), "Office FileCache", "📎"));
            }

            if (opt.ExternalDrives)
                L.AddRange(GetExternalDrivePaths());

            return L;
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
            else yield return root;
        }

        private static void AddBrowserCachePaths(List<(string, string, string)> L,
                                                  string profilePath, string browserName)
        {
            foreach (var sub in new[]
            {
                "Cache", "Cache2", "Code Cache", "GPUCache",
                "DawnCache", "ShaderCache", "blob_storage"
            })
            {
                string full = System.IO.Path.Combine(profilePath, sub);
                if (Directory.Exists(full))
                    L.Add((full, $"{browserName} кэш", "🌐"));
            }
            string netCache = System.IO.Path.Combine(profilePath, "Network", "Cache");
            if (Directory.Exists(netCache))
                L.Add((netCache, $"{browserName} Network Cache", "🌐"));
        }

        private static List<(string, string, string)> GetExternalDrivePaths()
        {
            var result = new List<(string, string, string)>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!drive.IsReady) continue;
                    if (drive.DriveType is not DriveType.Fixed and not DriveType.Removable) continue;

                    string root   = drive.Name;
                    string letter = root.TrimEnd('\\');
                    string icon   = drive.DriveType == DriveType.Removable ? "💾" : "🖥️";
                    string label  = string.IsNullOrWhiteSpace(drive.VolumeLabel)
                                  ? letter : $"{drive.VolumeLabel} ({letter})";

                    // Directory.Exists на HDD/USB может зависнуть на секунды
                    void TryAdd(string path, string cat, string? ic = null)
                    {
                        try
                        {
                            var t = Task.Run(() => Directory.Exists(path));
                            if (t.Wait(TimeSpan.FromSeconds(2)) && t.Result)
                                result.Add((path, cat, ic ?? icon));
                        }
                        catch { }
                    }

                    TryAdd(System.IO.Path.Combine(root, "$RECYCLE.BIN"), $"Корзина {label}", "🗑️");
                    foreach (var n in new[] { "Temp","temp","tmp","Tmp","TEMP","_Temp","$Temp","TempFiles" })
                        TryAdd(System.IO.Path.Combine(root, n), $"Temp {label}");
                    TryAdd(System.IO.Path.Combine(root, @"Windows\Temp"), $"Windows Temp {label}");
                    TryAdd(System.IO.Path.Combine(root, @"Windows\SoftwareDistribution\Download"), $"WU кэш {label}");
                    TryAdd(System.IO.Path.Combine(root, @"Windows\Prefetch"), $"Prefetch {label}");

                    string usersRoot = System.IO.Path.Combine(root, "Users");
                    if (!Directory.Exists(usersRoot)) continue;

                    string[] skipUsers = { "Public", "Default", "All Users", "Default User" };
                    string[] userDirs;
                    try
                    {
                        var t = Task.Run(() => Directory.GetDirectories(usersRoot));
                        userDirs = t.Wait(TimeSpan.FromSeconds(3)) ? t.Result : Array.Empty<string>();
                    }
                    catch { userDirs = Array.Empty<string>(); }
                    foreach (var userDir in userDirs)
                    {
                        string uName = System.IO.Path.GetFileName(userDir);
                        if (Array.Exists(skipUsers, s =>
                            string.Equals(s, uName, StringComparison.OrdinalIgnoreCase))) continue;

                        string localData = System.IO.Path.Combine(userDir, @"AppData\Local");
                        string roamData  = System.IO.Path.Combine(userDir, @"AppData\Roaming");

                        TryAdd(System.IO.Path.Combine(localData, "Temp"),                                  $"Temp пользователя {label}");
                        TryAdd(System.IO.Path.Combine(localData, @"Microsoft\Windows\INetCache"),         $"IE/Edge Cache ({letter})", "🔗");
                        TryAdd(System.IO.Path.Combine(localData, @"Microsoft\Windows\Explorer"),          $"Thumbnails {label}", "🖼️");
                        TryAdd(System.IO.Path.Combine(localData, @"Microsoft\Office\16.0\OfficeFileCache"), $"Office кэш ({letter})", "📎");
                        TryAdd(System.IO.Path.Combine(localData, @"slack\Cache"),                         $"Slack кэш ({letter})", "💬");
                        TryAdd(System.IO.Path.Combine(roamData,  @"Microsoft\Teams\Service Worker\CacheStorage"), $"Teams кэш ({letter})", "💬");

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
                    }

                    TryAdd(root, $"Мусор в корне {label}");
                }
                catch (Exception ex) { AppLog.Warn($"GetExternalDrivePaths {drive.Name}: {ex.Message}"); }
            }
            return result;
        }

        // ══════════════════════════════════════════════════════════════
        //  SCAN LOGIC
        // ══════════════════════════════════════════════════════════════

        private async void ScanBtn_Execute()
        {
            // Все флаги на UI потоке — никакого Interlocked не нужно
            if (_isScanning || _isCleaning) return;
            _isScanning = true;

            _fileItems.Clear();
            _allFoundItems.Clear();
            _totalFoundBytes = 0;
            _statTemp = _statBrowser = _statRecycle = 0;
            StatTempFiles.Text = StatBrowserFiles.Text = StatRecycleBin.Text = "0";
            StatCleaned.Text   = "0";
            TotalSizeText.Text = "0 МБ";
            FileCountText.Text = "Поиск...";
            ListCountLabel.Text = "";
            SetStatus("Сканирование...", StatusKind.Scanning);
            SetProgress(0, "Подготовка...");
            if (_showingHistory) SwitchTab(false);

            var oldCts = _cts;
            _cts = new CancellationTokenSource();
            oldCts?.Cancel();
            oldCts?.Dispose();
            SetUiRunning(true);

            // Читаем все CheckBox на UI потоке — в Task.Run они недоступны
            var scanOpt     = ReadScanOptions();
            bool doRecycle  = ChkRecycleBin?.IsChecked  == true;
            var token       = _cts.Token;

            // GetScanPaths содержит Directory.Exists на внешних дисках — выносим в фон
            SetProgress(0, "Составляю список папок...");
            var paths = await Task.Run(() => GetScanPaths(scanOpt));

            AppLog.Info($"Scan started: {paths.Count} dirs, recycle={doRecycle}, eventlog={scanOpt.EventLogs}");
            var sw = Stopwatch.StartNew();

            bool wuActive = false;
            if (ChkWinTemp?.IsChecked == true && !token.IsCancellationRequested)
                await Task.Run(() => { wuActive = IsWindowsUpdateActive(); });
            if (wuActive)
                SetStatus("⚠ Обнаружена активная загрузка обновлений Windows", StatusKind.Error);

            try
            {
                await Task.Run(() =>
                {
                    int total = paths.Count, done = 0;

                    // ── 4 параллельных потока сканирования ──────────────
                    var scanOpts = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = ScanDegree,
                        CancellationToken      = CancellationToken.None  // не прерываем Parallel сам по себе
                    };

                    int lastScanProgress = 0;
                    Parallel.ForEach(paths, scanOpts, item =>
                    {
                        if (token.IsCancellationRequested) return;
                        if (Directory.Exists(item.Path))
                            ScanDir(item.Path, item.Cat, item.Icon, token);

                        int idx = Interlocked.Increment(ref done);
                        int p   = total > 0 ? (int)(idx * 100.0 / total) : 0;
                        // Прогресс только вперёд — параллельные потоки могут финишировать не по порядку
                        int prev = Interlocked.Exchange(ref lastScanProgress, p);
                        if (p > prev)
                            Dispatcher.InvokeAsync(() => SetProgress(p, $"Сканирование... {p}%"),
                                DispatcherPriority.Background);
                    });

                    if (doRecycle && !token.IsCancellationRequested)
                    {
                        Dispatcher.Invoke(() => SetProgress(95, "Проверяю корзину..."));
                        ScanRecycleBin();
                    }

                    if (scanOpt.EventLogs && !token.IsCancellationRequested)
                    {
                        Dispatcher.Invoke(() => SetProgress(97, "Проверяю логи событий..."));
                        var channels      = GetEventLogChannels();
                        long totalLogBytes = channels.Sum(c => c.SizeBytes);
                        int  logCount      = channels.Count(c => c.SizeBytes > 0);
                        if (totalLogBytes > 0)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                _totalFoundBytes += totalLogBytes;
                                _statTemp        += logCount;
                                _fileItems.Add(new FileItem
                                {
                                    Icon      = "📋",
                                    Path      = $"Логи событий Windows ({logCount} каналов с записями)",
                                    Category  = "Логи событий",
                                    SizeBytes = totalLogBytes
                                });
                                TotalSizeText.Text  = SizeHelper.Format(_totalFoundBytes);
                                FileCountText.Text  = $"{_allFoundItems.Count} объектов";
                                ListCountLabel.Text = $"{_allFoundItems.Count} объектов";
                                StatTempFiles.Text  = _statTemp.ToString();
                            });
                        }
                    }
                }, CancellationToken.None);
            }
            catch (OperationCanceledException) { AppLog.Info("Scan cancelled"); }
            catch (Exception ex) { AppLog.Error("Scan unexpected error", ex); }
            finally
            {
                // finally у async void всегда на UI потоке после await — Dispatcher не нужен
                _isScanning = false;
                SetUiRunning(false, _allFoundItems.Count > 0);
            }

            bool wasCancelled = _cts?.IsCancellationRequested == true;
            if (wasCancelled)
            {
                SetProgress(0, "Сканирование остановлено");
                SetStatus("Остановлено", StatusKind.Stopped);
                AppLog.Info($"Scan stopped by user after {sw.Elapsed.TotalSeconds:F1}s");
                return;
            }

            // Ждём пока все фоновые InvokeAsync завершатся
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);

            AppLog.Info($"Scan done in {sw.Elapsed.TotalSeconds:F1}s: " +
                        $"{_allFoundItems.Count} items, {SizeHelper.Format(_totalFoundBytes)}");

            if (_allFoundItems.Count > 0)
            {
                bool browsersOpen = ChkBrowserCache?.IsChecked == true &&
                    new[] { "chrome","msedge","firefox","brave","opera","browser","vivaldi" }
                        .Any(n => Process.GetProcessesByName(n).Length > 0);
                string hint = browsersOpen ? " ⚠ закройте браузеры перед очисткой" : "";
                SetProgress(100, $"Найдено {_allFoundItems.Count} объектов • {SizeHelper.Format(_totalFoundBytes)}");
                SetStatus($"Найдено {SizeHelper.Format(_totalFoundBytes)} мусора{hint}", StatusKind.Found);
            }
            else
            {
                SetProgress(100, "Система чиста! ✓");
                SetStatus("Система чиста! ✓", StatusKind.Done);
            }
        }

        /// <summary>
        /// Сканирует папку в текущем потоке (вызывается из Parallel.ForEach).
        /// Файлы накапливаются в батчи и сбрасываются в UI редко (600мс / 2000 файлов).
        /// </summary>
        private void ScanDir(string dir, string cat, string icon, CancellationToken token)
        {
            CancellationTokenSource? timeoutCts = null;
            CancellationTokenSource? linkedCts  = null;
            try
            {
                bool isRootJunk     = cat.StartsWith("Мусор в корне", StringComparison.OrdinalIgnoreCase);
                bool isRecycleBinDir = dir.IndexOf("$RECYCLE.BIN", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isSoftwareDist  = dir.IndexOf("SoftwareDistribution", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isBrowser = cat.Contains("Chrome") || cat.Contains("Edge")   ||
                                 cat.Contains("Firefox")|| cat.Contains("Brave")  ||
                                 cat.Contains("Opera")  || cat.Contains("Яндекс") ||
                                 cat.Contains("Vivaldi");
                bool isRecycle = cat.Contains("орзин");

                // Таймаут только для SoftwareDistribution — она может висеть
                CancellationToken effectiveToken;
                if (isSoftwareDist)
                {
                    timeoutCts    = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    linkedCts     = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);
                    effectiveToken = linkedCts.Token;
                }
                else effectiveToken = token;

                var opts = new EnumerationOptions
                {
                    IgnoreInaccessible    = true,
                    RecurseSubdirectories = !isRootJunk,
                    AttributesToSkip      = isRecycleBinDir ? FileAttributes.None : FileAttributes.System
                };

                // Минимальный возраст файла — чтобы не трогать только что созданные
                DateTime minAge = (cat.Contains("Temp") || cat.Contains("WU кэш") ||
                                   cat.Contains("Windows Update") || cat.Contains("Мусор в корне"))
                    ? DateTime.UtcNow.AddMinutes(-5)
                    : (cat.Contains("Thumbnails") || cat.Contains("Prefetch") ||
                       cat.Contains("INetCache")  || isBrowser)
                        ? DateTime.UtcNow.AddMinutes(-2)
                        : DateTime.MaxValue;

                var sw          = Stopwatch.StartNew();
                long batchBytes = 0;
                int  batchT = 0, batchBr = 0, batchRc = 0;
                var  batch  = new List<FileItem>(64);

                void Flush()
                {
                    if (batch.Count == 0) return;
                    var items = batch.ToList();
                    long bytes = batchBytes;
                    int t = batchT, br = batchBr, rc = batchRc;
                    batch.Clear(); batchBytes = 0; batchT = batchBr = batchRc = 0;

                    Dispatcher.InvokeAsync(() =>
                    {
                        // _allFoundItems — полный список для очистки (без ограничения)
                        _allFoundItems.AddRange(items);
                        // _fileItems — только для отображения, ограничен 5000
                        const int MaxDisplayItems = 5000;
                        int canAdd = MaxDisplayItems - _fileItems.Count;
                        if (canAdd > 0)
                            _fileItems.AddRange(items.Take(canAdd));
                        _totalFoundBytes += bytes;
                        _statTemp        += t;
                        _statBrowser     += br;
                        _statRecycle     += rc;
                        TotalSizeText.Text    = SizeHelper.Format(_totalFoundBytes);
                        FileCountText.Text    = $"{_allFoundItems.Count} файлов";
                        ListCountLabel.Text   = $"{_allFoundItems.Count} объектов";
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
                            bool tilde  = name.StartsWith("~", StringComparison.Ordinal);
                            if (!tilde && !_junkExtensions.Contains(ext) && !_junkFileNames.Contains(name))
                                continue;
                        }

                        if (IsInProtectedFolder(file)) continue;
                        // Белый список — проверяем O(1) по HashSet
                        if (IsExcluded(file)) continue;
                        var fi = new FileInfo(file);
                        if (fi.LastWriteTimeUtc > minAge) continue;

                        string fileName = fi.Name;
                        string fileExt  = fi.Extension;

                        if (_protectedFileNames.Contains(fileName)) continue;
                        if (isBrowser && _protectedExtensions.Contains(fileExt)) continue;

                        long sz = fi.Length;
                        if (sz == 0 && (cat.Contains("Temp") || cat.Contains("temp"))) continue;
                        if (cat.Contains("Temp") && _safeExtensionsInTemp.Contains(fileExt)) continue;
                        // IsTmpFileLocked убран — открытие FileStream на каждом .tmp очень медленно.
                        // Заблокированные файлы поймаются через IOException при очистке.

                        batchBytes += sz;
                        if      (isBrowser) batchBr++;
                        else if (isRecycle) batchRc++;
                        else                batchT++;
                        batch.Add(new FileItem { Icon = icon, Path = file, Category = cat, SizeBytes = sz });

                        if (sw.ElapsedMilliseconds >= 600 || batch.Count >= 2000)
                        {
                            Flush();
                            sw.Restart();
                        }
                    }
                    catch (UnauthorizedAccessException) { /* пропускаем системные файлы без прав */ }
                    catch (Exception ex) { AppLog.Warn($"ScanDir file error: {file} | {ex.Message}"); }
                }
                Flush();
            }
            catch (OperationCanceledException) { /* таймаут SoftwareDistribution или пользователь */ }
            catch (Exception ex) { AppLog.Error($"ScanDir failed: {dir}", ex); }
            finally
            {
                linkedCts?.Dispose();
                timeoutCts?.Dispose();
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  КОРЗИНА
        // ══════════════════════════════════════════════════════════════

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
                        _fileItems.Add(new FileItem
                        {
                            Icon      = "🗑️",
                            Path      = $"Корзина ({cnt} объектов)",
                            Category  = "Корзина",
                            SizeBytes = sz
                        });
                        TotalSizeText.Text  = SizeHelper.Format(_totalFoundBytes);
                        FileCountText.Text  = $"{_allFoundItems.Count} файлов";
                        ListCountLabel.Text = $"{_allFoundItems.Count} объектов";
                        StatRecycleBin.Text = _statRecycle.ToString();
                    });
                }
            }
            catch (Exception ex) { AppLog.Error("ScanRecycleBin failed", ex); }
        }

        // ══════════════════════════════════════════════════════════════
        //  ЛОГИ СОБЫТИЙ
        // ══════════════════════════════════════════════════════════════

        private static List<(string Channel, long SizeBytes)> GetEventLogChannels()
        {
            var result = new List<(string, long)>();
            const long emptyThreshold = 69_632;
            try
            {
                var di = new DirectoryInfo(@"C:\Windows\System32\winevt\Logs");
                if (!di.Exists) return result;
                foreach (var fi in di.EnumerateFiles("*.evtx"))
                {
                    try
                    {
                        long sz = fi.Length;
                        if (sz <= emptyThreshold) continue;
                        string channel = fi.Name
                            .Replace(".evtx", "", StringComparison.OrdinalIgnoreCase)
                            .Replace("%4", "/");
                        result.Add((channel, sz - emptyThreshold));
                    }
                    catch { /* файл заблокирован службой EventLog — пропускаем */ }
                }
            }
            catch (Exception ex) { AppLog.Error("GetEventLogChannels failed", ex); }
            return result;
        }

        private static long ClearAllEventLogChannels(
            List<(string Channel, long SizeBytes)> channels, CancellationToken token)
        {
            long totalCleared = 0;
            var opts = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken      = token
            };
            try
            {
                Parallel.ForEach(channels, opts, item =>
                {
                    try
                    {
                        using var session = new EventLogSession();
                        session.ClearLog(item.Channel);
                        Interlocked.Add(ref totalCleared, item.SizeBytes);
                    }
                    catch (Exception ex) { AppLog.Warn($"ClearLog {item.Channel}: {ex.Message}"); }
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { AppLog.Error("ClearAllEventLogChannels failed", ex); }
            return totalCleared;
        }

        // ══════════════════════════════════════════════════════════════
        //  CLEAN LOGIC
        // ══════════════════════════════════════════════════════════════

        private async void CleanBtn_Execute()
        {
            if (_allFoundItems.Count == 0) return;
            if (_isScanning || _isCleaning) return;
            _isCleaning = true;
            SetUiRunning(true);

            var dlg = new CustomDialog(
                "Подтверждение очистки",
                $"Будет удалено {_allFoundItems.Count} объектов.\nЭто действие нельзя отменить.",
                DialogKind.Confirm,
                stats: new List<StatRow>
                {
                    new() { Label = "Найдено файлов:", Value = _allFoundItems.Count.ToString(),     Color = "#AAAACC" },
                    new() { Label = "Займёт места:",   Value = SizeHelper.Format(_totalFoundBytes), Color = "#FF4A6A" },
                },
                showCancel: true);
            dlg.ShowDialog();
            if (!dlg.Result)
            {
                _isCleaning = false;
                SetUiRunning(false, _allFoundItems.Count > 0);
                return;
            }

            // Предупреждение если WU активен
            bool hasWuFiles = _fileItems.Any(x =>
                x.Category == "Windows Update кэш" ||
                x.Category.StartsWith("WU кэш"));
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
                        DialogKind.Warning, showCancel: true);
                    wuDlg.ShowDialog();
                    if (!wuDlg.Result)
                    {
                        _isCleaning = false;
                        SetUiRunning(false, _allFoundItems.Count > 0);
                        return;
                    }
                }
            }

            // Предупреждение об открытых браузерах
            if (ChkBrowserCache?.IsChecked == true)
            {
                var browserProcesses = new Dictionary<string, string>
                {
                    { "chrome",  "Google Chrome"  }, { "msedge",  "Microsoft Edge" },
                    { "firefox", "Firefox"         }, { "brave",   "Brave"          },
                    { "opera",   "Opera"           }, { "operagx", "Opera GX"       },
                    { "browser", "Яндекс Браузер"  }, { "vivaldi", "Vivaldi"        },
                };
                var running = browserProcesses
                    .Where(b => Process.GetProcessesByName(b.Key).Length > 0)
                    .Select(b => b.Value).ToList();

                if (running.Count > 0)
                {
                    var warnDlg = new CustomDialog(
                        "Браузеры открыты!",
                        $"Обнаружены запущенные браузеры:\n{string.Join(", ", running)}\n\n" +
                        "Кэш будет удалён, но браузер немедленно воссоздаст его.\n" +
                        "Рекомендуется закрыть браузеры и повторить очистку.",
                        DialogKind.Warning, showCancel: true);
                    warnDlg.ShowDialog();
                    if (!warnDlg.Result)
                    {
                        _isCleaning = false;
                        SetUiRunning(false, _allFoundItems.Count > 0);
                        return;
                    }
                }
            }

            var oldCleanCts = _cts;
            _cts = new CancellationTokenSource();
            oldCleanCts?.Cancel();
            oldCleanCts?.Dispose();
            _cleanedBytes = 0;
            StatCleaned.Text = "0";
            SetStatus("Очистка...", StatusKind.Cleaning);
            SetProgress(0, "Начинаю очистку...");

            // Берём полный список — _fileItems ограничен 5000 для отображения,
            // _allFoundItems содержит все найденные файлы
            var snapshot        = _allFoundItems.ToList();
            // Снапшот для графика: сохраняем ДО того как _allFoundItems будет очищен в finally
            var cleanedSnapshot = snapshot.ToList();
            bool doRecycle = snapshot.Any(x => x.Category == "Корзина")
                          || _fileItems.Any(x => x.Category == "Корзина");
            var regular    = snapshot.Where(x => x.Category != "Корзина").ToList();
            // cleanDirs берём из уже известных путей файлов — не перечисляем профили браузеров снова
            var cleanDirs  = regular
                .Where(x => x.Category != "Логи событий")
                .Select(x => System.IO.Path.GetDirectoryName(x.Path))
                .Where(d => d != null)
                .Distinct()
                .Cast<string>()
                .ToList();

            int done = 0, deleted = 0, skipped = 0;
            var token = _cts.Token;
            bool needWuStop = regular.Any(x =>
                x.Path.IndexOf("SoftwareDistribution", StringComparison.OrdinalIgnoreCase) >= 0);

            AppLog.Info($"Clean started: {regular.Count} files, recycle={doRecycle}, needWuStop={needWuStop}");
            var sw = Stopwatch.StartNew();

            try
            {
                await Task.Run(() =>
                {
                    bool wuWasRunning = false;
                    if (needWuStop)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            SetProgress(1, "Останавливаю службу Windows Update...");
                            SetStatus("Останавливаю службу обновлений...", StatusKind.Cleaning);
                        });
                        bool stopped = StopWindowsUpdateService(out wuWasRunning);
                        Dispatcher.Invoke(() => SetProgress(3,
                            stopped ? "Служба остановлена. Начинаю очистку..."
                                    : "Не удалось остановить службу — пробую удалить..."));
                    }

                    // ── ВНЕШНИЙ try-finally: службы WU восстанавливаются ВСЕГДА ──
                    // Охватывает все фазы: логи, файлы, корзину, пустые папки.
                    // Даже если Корзина или CleanEmptyDirs бросят необработанное
                    // исключение — finally гарантированно запустит wuauserv/BITS.
                    try
                    {
                        // ── Логи событий отдельно до параллельного удаления ──
                        var eventLogItem = regular.FirstOrDefault(x => x.Category == "Логи событий");
                        if (eventLogItem != null && !token.IsCancellationRequested)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                SetProgress(5, "Очищаю логи событий...");
                                SetStatus("Очищаю логи событий...", StatusKind.Cleaning);
                            });
                            var channels      = GetEventLogChannels();
                            long clearedBytes = ClearAllEventLogChannels(channels, token);
                            if (clearedBytes > 0)
                            {
                                Interlocked.Add(ref _cleanedBytes, clearedBytes);
                                Interlocked.Increment(ref deleted);
                            }
                            else Interlocked.Increment(ref skipped);
                            Interlocked.Increment(ref done);
                            Dispatcher.Invoke(() =>
                            {
                                var entry = _fileItems.FirstOrDefault(x => x.Category == "Логи событий");
                                if (entry != null) _fileItems.Remove(entry);
                                StatCleaned.Text = SizeHelper.Format(Interlocked.Read(ref _cleanedBytes));
                            }, DispatcherPriority.Background);
                        }

                        // ── 4 потока параллельного удаления файлов ───────────
                        var regularFiles = regular
                            .Where(x => x.Category != "Логи событий")
                            .ToList();

                        // Сбрасываем done для точного отображения прогресса файлов
                        done = 0;

                        var parallelOpts = new ParallelOptions
                        {
                            MaxDegreeOfParallelism = CleanDegree,
                            CancellationToken      = token
                        };
                        long lastUiUpdateMs = 0;
                        // Собираем пути удалённых файлов в потокобезопасную коллекцию
                        // чтобы НЕ делать File.Exists() в UI потоке на тысячах файлов
                        var deletedPaths = new System.Collections.Concurrent.ConcurrentBag<string>();

                        try
                        {
                            Parallel.ForEach(regularFiles, parallelOpts, item =>
                            {
                                try
                                {
                                    if (File.Exists(item.Path))
                                    {
                                        try
                                        {
                                            File.Delete(item.Path);
                                        }
                                        catch (UnauthorizedAccessException)
                                        {
                                            // Только если не удалось — пробуем снять атрибуты
                                            File.SetAttributes(item.Path, FileAttributes.Normal);
                                            File.Delete(item.Path);
                                        }
                                        deletedPaths.Add(item.Path);
                                        Interlocked.Add(ref _cleanedBytes, item.SizeBytes);
                                        Interlocked.Increment(ref deleted);
                                    }
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    AppLog.Warn($"Access denied: {item.Path}");
                                    Interlocked.Increment(ref skipped);
                                }
                                catch (IOException ex)
                                {
                                    AppLog.Warn($"IO error deleting: {item.Path} | {ex.Message}");
                                    Interlocked.Increment(ref skipped);
                                }
                                catch (Exception ex)
                                {
                                    AppLog.Error($"Unexpected delete error: {item.Path}", ex);
                                    Interlocked.Increment(ref skipped);
                                }

                                int d2     = Interlocked.Increment(ref done);
                                long nowMs = sw.ElapsedMilliseconds;
                                long prevMs = Interlocked.Exchange(ref lastUiUpdateMs, nowMs);

                                // Обновляем UI не чаще чем раз в 400мс (было 200) 
                                if (nowMs - prevMs >= 400 || d2 == regularFiles.Count)
                                {
                                    long c2 = Interlocked.Read(ref _cleanedBytes);
                                    // Снимаем снапшот и очищаем bag — не копим миллионы строк
                                    var deletedSnap = new HashSet<string>();
                                    while (deletedPaths.TryTake(out var p))
                                        deletedSnap.Add(p);

                                    Dispatcher.InvokeAsync(() =>
                                    {
                                        var toRemove = _fileItems
                                            .Where(x => x.Category != "Корзина" &&
                                                        x.Category != "Логи событий" &&
                                                        deletedSnap.Contains(x.Path))
                                            .ToList();
                                        SetProgress(
                                            regularFiles.Count > 0 ? d2 * 100.0 / regularFiles.Count : 100,
                                            $"Удалено {d2} / {regularFiles.Count} • {SizeHelper.Format(c2)}");
                                        StatCleaned.Text = SizeHelper.Format(c2);
                                        foreach (var r in toRemove) _fileItems.Remove(r);
                                    }, DispatcherPriority.Background);
                                }
                            });
                        }
                        catch (OperationCanceledException) { AppLog.Info("Clean cancelled mid-way"); }

                        // ── Корзина ──────────────────────────────────────────
                        if (doRecycle && !token.IsCancellationRequested)
                        {
                            Dispatcher.Invoke(() => SetProgress(96, "Очищаю корзину..."));
                            try
                            {
                                var rbi = new SHQUERYRBINFO { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
                                SHQueryRecycleBin(null, ref rbi);
                                int hr = SHEmptyRecycleBin(IntPtr.Zero, null, 0x00000001 | 0x00000002 | 0x00000004);
                                if (hr != 0 && hr != unchecked((int)0x80070057)) // 0x80070057 = корзина уже пуста
                                    AppLog.Warn($"SHEmptyRecycleBin HRESULT: 0x{hr:X8}");
                                Interlocked.Add(ref _cleanedBytes, rbi.i64Size);
                                long c3 = Interlocked.Read(ref _cleanedBytes);
                                Dispatcher.Invoke(() =>
                                {
                                    var rb = _fileItems.FirstOrDefault(x => x.Category == "Корзина");
                                    if (rb != null) _fileItems.Remove(rb);
                                    StatCleaned.Text    = SizeHelper.Format(c3);
                                    StatRecycleBin.Text = "0";
                                });
                            }
                            catch (Exception ex) { AppLog.Error("SHEmptyRecycleBin failed", ex); }
                        }

                        Dispatcher.Invoke(() => SetProgress(99, "Удаляю пустые папки..."));
                        CleanEmptyDirs(cleanDirs);
                    }
                    finally
                    {
                        // Службы запускаем ВСЕГДА — даже если очистка прервана,
                        // отменена токеном, упала с исключением или убита процессом.
                        // Windows не должна оставаться без обновлений из-за нашей программы.
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

                }, CancellationToken.None);
            }
            catch (OperationCanceledException) { AppLog.Info("Clean OperationCanceledException"); }
            catch (Exception ex)               { AppLog.Error("CleanBtn_Execute outer error", ex); }
            finally
            {
                // finally у async void после await — на UI потоке, Dispatcher не нужен
                _isCleaning = false;

                bool wasCancelled = _cts?.IsCancellationRequested == true;
                long freed        = Interlocked.Read(ref _cleanedBytes);

                AppLog.Info($"Clean finished in {sw.Elapsed.TotalSeconds:F1}s: " +
                            $"deleted={deleted}, skipped={skipped}, freed={SizeHelper.Format(freed)}");

                _totalFoundBytes = 0;
                _cleanedBytes    = 0;
                _statTemp = _statBrowser = _statRecycle = 0;
                _fileItems.Clear();
                _allFoundItems.Clear();
                StatTempFiles.Text = StatBrowserFiles.Text = StatRecycleBin.Text = "0";
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
                        new() { Label = "Удалено файлов:", Value = deleted.ToString(),       Color = "#4A9EFF" },
                        new() { Label = "Освобождено:",    Value = SizeHelper.Format(freed), Color = "#06D6C7" },
                    };
                    if (skipped > 0)
                        stats.Add(new() { Label = "Пропущено (заняты):", Value = skipped.ToString(), Color = "#FF8C00" });
                    new CustomDialog("Очистка завершена!", "🌟  Ваш компьютер стал чище!",
                        DialogKind.Success, stats).ShowDialog();

                    // Показываем пирог после закрытия диалога — пользователь уже
                    // знает что всё хорошо, теперь видит разбивку по категориям
                    ShowCleanupChart(freed, cleanedSnapshot);
                }
            }
        }

        /// <summary>
        /// Удаляет пустые папки снизу вверх рекурсивно.
        /// Не загружает всё дерево в память — обходит на лету.
        /// </summary>
        private void CleanEmptyDirs(IEnumerable<string> roots)
        {
            int removedCount = 0;
            foreach (var root in roots)
            {
                if (!Directory.Exists(root)) continue;
                try
                {
                    // Обходим только подпапки — корневую папку (Temp, Cache и т.д.) не трогаем
                    foreach (var sub in Directory.EnumerateDirectories(root))
                    {
                        try { RemoveEmptySubdirs(sub, ref removedCount); }
                        catch (Exception ex) { AppLog.Warn($"CleanEmptyDirs sub {sub}: {ex.Message}"); }
                    }
                }
                catch (Exception ex) { AppLog.Warn($"CleanEmptyDirs root {root}: {ex.Message}"); }
            }
            if (removedCount > 0)
                AppLog.Info($"CleanEmptyDirs: removed {removedCount} empty folders");
        }

        private static void RemoveEmptySubdirs(string dir, ref int count)
        {
            try
            {
                foreach (var sub in Directory.EnumerateDirectories(dir))
                    RemoveEmptySubdirs(sub, ref count);

                // После рекурсивного прохода проверяем — возможно папка стала пустой
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Directory.Delete(dir);
                    count++;
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (Exception ex) { AppLog.Warn($"RemoveEmptySubdirs {dir}: {ex.Message}"); }
        }

        // ══════════════════════════════════════════════════════════════
        //  БЕЛЫЙ СПИСОК (ИСКЛЮЧЕНИЯ)
        // ══════════════════════════════════════════════════════════════

        private void LoadExclusions()
        {
            try
            {
                if (!File.Exists(ExclusionsPath)) return;
                var list = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(ExclusionsPath));
                if (list == null) return;
                _excludePaths = new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
                foreach (var path in list)
                    _exclusionItems.Add(new ExclusionItem
                    {
                        Path = path,
                        Icon = Directory.Exists(path) ? "📁" : "📄"
                    });
                AppLog.Info($"Exclusions loaded: {list.Count} entries");
            }
            catch (Exception ex) { AppLog.Error("LoadExclusions failed", ex); }
        }

        private void SaveExclusions()
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ExclusionsPath)!);
                File.WriteAllText(ExclusionsPath,
                    JsonSerializer.Serialize(_excludePaths.ToList(),
                        new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex) { AppLog.Error("SaveExclusions failed", ex); }
        }

        private void AddToExclusions(string path, string icon)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (_excludePaths.Add(path)) // HashSet.Add возвращает false если уже есть
            {
                _exclusionItems.Add(new ExclusionItem { Path = path, Icon = icon });
                SaveExclusions();
                AppLog.Info($"Exclusion added: {path}");
                SetStatus($"В исключения добавлено: {System.IO.Path.GetFileName(path)}", StatusKind.Done);
            }
        }

        // Контекстное меню FileListView: «Исключить файл»
        private void AddExclusion_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItem is not FileItem item) return;
            AddToExclusions(item.Path, "📄");
        }

        // Контекстное меню FileListView: «Исключить всю папку»
        private void AddFolderExclusion_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItem is not FileItem item) return;
            string? folder = System.IO.Path.GetDirectoryName(item.Path);
            if (folder != null)
                AddToExclusions(folder, "📁");
        }

        // Кнопка «+ Добавить путь» на вкладке исключений — открываем FolderBrowserDialog
        private void AddExclusionManual_Click(object sender, MouseButtonEventArgs e)
        {
            // Используем OpenFileDialog в режиме выбора папки (ValidateNames=false, CheckFileExists=false)
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title            = "Выберите файл или папку для исключения",
                CheckFileExists  = false,
                CheckPathExists  = true,
                FileName         = "Выберите папку или файл",
                Filter           = "Все файлы|*.*",
                ValidateNames    = false,
            };
            if (dlg.ShowDialog() == true)
            {
                string selected = dlg.FileName;
                // Если пользователь написал имя несуществующего файла — берём папку
                if (!File.Exists(selected) && Directory.Exists(System.IO.Path.GetDirectoryName(selected)))
                    selected = System.IO.Path.GetDirectoryName(selected)!;
                string icon = Directory.Exists(selected) ? "📁" : "📄";
                AddToExclusions(selected, icon);
            }
        }

        // Кнопка «✕» рядом с каждым элементом исключения
        private void RemoveExclusion_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe) return;
            string? path = fe.Tag as string;
            if (path == null) return;
            _excludePaths.Remove(path);
            var item = _exclusionItems.FirstOrDefault(x => x.Path == path);
            if (item != null) _exclusionItems.Remove(item);
            SaveExclusions();
            AppLog.Info($"Exclusion removed: {path}");
        }

        // Кнопка «🗑 Очистить всё»
        private void ClearExclusions_Click(object sender, MouseButtonEventArgs e)
        {
            var dlg = new CustomDialog("Очистить белый список?",
                "Все исключения будут удалены. Сканирование снова будет проверять эти файлы.",
                DialogKind.Confirm, showCancel: true);
            dlg.ShowDialog();
            if (!dlg.Result) return;
            _excludePaths.Clear();
            _exclusionItems.Clear();
            SaveExclusions();
            AppLog.Info("All exclusions cleared");
        }

        // Hover эффекты для кнопок на вкладке исключений
        private void ExclusionBtn_Enter(object sender, MouseEventArgs e)
        {
            if (sender is Border b)
                b.Effect = Glow(Color.FromRgb(0x4A, 0x9E, 0xFF), r: 14, o: 0.6);
        }
        private void ExclusionBtn_Leave(object sender, MouseEventArgs e)
        {
            if (sender is Border b) b.Effect = null;
        }

        // ══════════════════════════════════════════════════════════════
        //  БОЛЬШИЕ ФАЙЛЫ
        // ══════════════════════════════════════════════════════════════

        private static readonly string[] _largeFileDriveLetters =
            DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType is DriveType.Fixed or DriveType.Removable)
                .Select(d => d.RootDirectory.FullName)
                .ToArray();

        private static string GetLargeFileIcon(string ext) => ext.ToLowerInvariant() switch
        {
            ".iso" or ".img" or ".vhd" or ".vhdx" => "💿",
            ".zip" or ".rar" or ".7z" or ".tar"
                or ".gz" or ".bz2" or ".xz"       => "🗜️",
            ".mp4" or ".mkv" or ".avi" or ".mov"
                or ".wmv" or ".flv" or ".m2ts"    => "🎬",
            ".mp3" or ".flac" or ".wav" or ".aac" => "🎵",
            ".exe" or ".msi"                       => "⚙️",
            ".pdf"                                 => "📕",
            ".psd" or ".ai" or ".raw" or ".nef"
                or ".cr2" or ".arw"               => "🎨",
            ".vmdk" or ".ova" or ".ovf"            => "🖥️",
            ".bak" or ".backup"                    => "💾",
            _                                      => "📦",
        };

        private async void LargeScan_Click(object sender, MouseButtonEventArgs e)
        {
            // Отменяем предыдущее сканирование если оно идёт
            _largeScanCts?.Cancel();
            _largeScanCts?.Dispose();
            _largeScanCts = new CancellationTokenSource();
            var token = _largeScanCts.Token;

            _largeFileItems.Clear();
            LargeFilesStatus.Text = "Поиск...";
            LargeScanBtnBorder.IsEnabled = false;
            LargeScanBtnBorder.Opacity   = 0.5;

            // Читаем порог на UI потоке
            long threshold = 524_288_000L; // дефолт 500 МБ
            if (LargeSizeThreshold.SelectedItem is ComboBoxItem ci &&
                long.TryParse(ci.Tag?.ToString(), out long t))
                threshold = t;

            var sw = Stopwatch.StartNew();
            long totalFound = 0;
            int  countFound = 0;

            try
            {
                await Task.Run(() =>
                {
                    // Сканируем все готовые фиксированные и съёмные диски
                    var drives = DriveInfo.GetDrives()
                        .Where(d => d.IsReady &&
                               d.DriveType is DriveType.Fixed or DriveType.Removable)
                        .ToList();

                    var batch = new List<LargeFileItem>(32);

                    void Flush()
                    {
                        if (batch.Count == 0) return;
                        var snap = batch.ToList();
                        batch.Clear();
                        Dispatcher.InvokeAsync(() =>
                        {
                            _largeFileItems.AddRange(snap);
                            LargeFilesStatus.Text =
                                $"Найдено: {countFound} файлов • {SizeHelper.Format(totalFound)}";
                        }, DispatcherPriority.Background);
                    }

                    foreach (var drive in drives)
                    {
                        if (token.IsCancellationRequested) break;
                        var opts = new EnumerationOptions
                        {
                            IgnoreInaccessible    = true,
                            RecurseSubdirectories = true,
                            AttributesToSkip      = FileAttributes.System | FileAttributes.ReparsePoint,
                        };
                        try
                        {
                            foreach (var file in Directory.EnumerateFiles(drive.RootDirectory.FullName, "*", opts))
                            {
                                if (token.IsCancellationRequested) break;
                                try
                                {
                                    var fi = new FileInfo(file);
                                    if (fi.Length < threshold) continue;

                                    string ext      = fi.Extension;
                                    string category = drive.VolumeLabel is { Length: > 0 } lbl
                                        ? $"{lbl} ({drive.Name.TrimEnd('\\')})"
                                        : drive.Name.TrimEnd('\\');

                                    batch.Add(new LargeFileItem
                                    {
                                        Icon      = GetLargeFileIcon(ext),
                                        Path      = file,
                                        FileName  = fi.Name,
                                        Category  = category,
                                        SizeBytes = fi.Length,
                                    });
                                    Interlocked.Add(ref totalFound, fi.Length);
                                    Interlocked.Increment(ref countFound);

                                    if (batch.Count >= 50)
                                        Flush();
                                }
                                catch (UnauthorizedAccessException) { }
                                catch (Exception ex) { AppLog.Warn($"LargeFile scan: {file} | {ex.Message}"); }
                            }
                        }
                        catch (Exception ex) { AppLog.Warn($"LargeFile drive {drive.Name}: {ex.Message}"); }
                        Flush();
                    }
                }, CancellationToken.None);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { AppLog.Error("LargeScan failed", ex); }
            finally
            {
                LargeScanBtnBorder.IsEnabled = true;
                LargeScanBtnBorder.Opacity   = 1.0;
                // Сортируем по убыванию размера — самые жирные файлы сверху
                await Dispatcher.InvokeAsync(() =>
                {
                    var sorted = _largeFileItems.OrderByDescending(x => x.SizeBytes).ToList();
                    _largeFileItems.Clear();
                    _largeFileItems.AddRange(sorted);
                    LargeFilesStatus.Text = countFound == 0
                        ? "Файлов крупнее порога не найдено"
                        : $"Найдено: {countFound} файлов • {SizeHelper.Format(totalFound)} • " +
                          $"за {sw.Elapsed.TotalSeconds:F1}с";
                });
                AppLog.Info($"LargeScan done: {countFound} files, {SizeHelper.Format(totalFound)}, {sw.Elapsed.TotalSeconds:F1}s");
            }
        }

        // Контекстное меню: «Открыть папку»
        private void LargeFile_OpenFolder(object sender, RoutedEventArgs e)
        {
            if (LargeFilesListView.SelectedItem is not LargeFileItem item) return;
            string? folder = System.IO.Path.GetDirectoryName(item.Path);
            if (folder != null && Directory.Exists(folder))
                Process.Start("explorer.exe", $"/select,\"{item.Path}\"");
        }

        // Контекстное меню: «Удалить файл» (с подтверждением)
        private void LargeFile_Delete(object sender, RoutedEventArgs e)
        {
            if (LargeFilesListView.SelectedItem is not LargeFileItem item) return;
            var dlg = new CustomDialog(
                "Удалить файл?",
                $"{item.FileName}\n{SizeHelper.Format(item.SizeBytes)}",
                DialogKind.Confirm,
                stats: new List<StatRow>
                {
                    new() { Label = "Путь:", Value = item.Path, Color = "#AAAACC" },
                    new() { Label = "Размер:", Value = item.SizeText, Color = "#FF8C00" },
                },
                showCancel: true);
            dlg.ShowDialog();
            if (!dlg.Result) return;
            try
            {
                File.Delete(item.Path);
                _largeFileItems.Remove(item);
                AppLog.Info($"LargeFile deleted: {item.Path}");
                SetStatus($"Удалено: {item.FileName}", StatusKind.Done);
            }
            catch (Exception ex)
            {
                AppLog.Error($"LargeFile delete failed: {item.Path}", ex);
                new CustomDialog("Ошибка удаления", ex.Message, DialogKind.Error).ShowDialog();
            }
        }

        // Hover эффекты для кнопки «Найти» на вкладке больших файлов
        private void LargeScanBtn_Enter(object sender, MouseEventArgs e)
        {
            if (LargeScanBtnBorder.IsEnabled)
                LargeScanBtnBorder.Effect = Glow(Color.FromRgb(0xA8, 0x55, 0xF7), r: 18, o: 0.8);
        }
        private void LargeScanBtn_Leave(object sender, MouseEventArgs e)
            => LargeScanBtnBorder.Effect = null;

        // ══════════════════════════════════════════════════════════════
        //  ТРЕЙ — ИНТЕГРАЦИЯ
        // ══════════════════════════════════════════════════════════════

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState != WindowState.Minimized) return;
            // Скрываем окно в трей вместо стандартного сворачивания на панель задач
            Hide();
            App.Instance.ShowBalloon(
                "CleanupTemp Pro свёрнут в трей",
                "Дважды щёлкните иконку, чтобы открыть снова.",
                durationMs: 2500);
        }

        /// <summary>
        /// Вызывается из App при нажатии «Сканировать сейчас» в трей-меню.
        /// Безопасен — проверяет флаги перед запуском.
        /// </summary>
        public void TriggerScanFromTray()
        {
            if (_isScanning || _isCleaning) return;
            ScanBtn_Execute();
        }

        /// <summary>
        /// Единая точка обновления трея из MainWindow.
        /// Все вызовы идут через этот метод — не разбросаны по коду.
        /// </summary>
        private void NotifyTray(TrayStatus status, string? detail = null)
            => App.Instance.UpdateTrayStatus(status, detail);

        // ══════════════════════════════════════════════════════════════
        //  PIE CHART — отчёт по очистке
        // ══════════════════════════════════════════════════════════════

        // Цветовая палитра категорий — консистентна между запусками
        private static readonly Dictionary<string, string> _categoryColors = new(StringComparer.OrdinalIgnoreCase)
        {
            // Temp / System
            { "User Temp",               "#4A9EFF" },
            { "Windows Temp",            "#3A8EEF" },
            { "Prefetch",                "#2A7EDF" },
            { "Логи событий",            "#5599FF" },
            // Windows Update
            { "Windows Update кэш",      "#A855F7" },
            // Browser
            { "Chrome кэш",              "#F59E0B" },
            { "Edge кэш",                "#E8A020" },
            { "Firefox кэш",             "#FF6B35" },
            { "Brave кэш",               "#FB923C" },
            { "Opera кэш",               "#EF4444" },
            { "Yandex кэш",              "#DC2626" },
            { "Vivaldi кэш",             "#C084FC" },
            // Thumbnails / Office
            { "Thumbnails кэш",          "#06D6C7" },
            { "IE/Edge Legacy Cache",    "#10B981" },
            { "Office кэш",              "#34D399" },
            { "Office FileCache",        "#6EE7B7" },
            // Recycle
            { "Корзина",                 "#FF4A6A" },
        };

        // Цвета для неизвестных категорий (ротация)
        private static readonly string[] _fallbackColors =
        {
            "#818CF8", "#F472B6", "#38BDF8", "#A3E635",
            "#FDE68A", "#FCA5A5", "#6EE7B7", "#C4B5FD"
        };

        private string GetCategoryColor(string category, int fallbackIndex)
        {
            // Точное совпадение
            if (_categoryColors.TryGetValue(category, out var c)) return c;
            // Частичное совпадение (браузерные подкатегории, внешние диски и т.д.)
            foreach (var kv in _categoryColors)
                if (category.Contains(kv.Key, StringComparison.OrdinalIgnoreCase)) return kv.Value;
            return _fallbackColors[fallbackIndex % _fallbackColors.Length];
        }

        /// <summary>
        /// Вызывается из CleanBtn_Execute после успешной очистки.
        /// Группирует _allFoundItems по категориям, рисует диаграмму и показывает оверлей.
        /// </summary>
        private void ShowCleanupChart(long totalFreed, List<FileItem> cleanedItems)
        {
            if (totalFreed == 0 || cleanedItems.Count == 0) return;

            // ── Группировка: сжимаем похожие категории браузеров ──
            var groups = cleanedItems
                .GroupBy(f => NormalizeCategory(f.Category))
                .Select((g, i) => new ChartSegment
                {
                    Category  = g.Key,
                    SizeBytes = g.Sum(x => x.SizeBytes),
                    HexColor  = GetCategoryColor(g.Key, i),
                })
                .OrderByDescending(s => s.SizeBytes)
                .ToList();

            // Считаем проценты; группы < 1.5% объединяем в «Прочее»
            const double minPct = 0.015;
            var main  = groups.Where(s => (double)s.SizeBytes / totalFreed >= minPct).ToList();
            var other = groups.Where(s => (double)s.SizeBytes / totalFreed <  minPct).ToList();
            if (other.Count > 0)
                main.Add(new ChartSegment
                {
                    Category  = "Прочее",
                    SizeBytes = other.Sum(s => s.SizeBytes),
                    HexColor  = "#555577",
                });

            double total = main.Sum(s => s.SizeBytes);
            foreach (var seg in main) seg.Pct = total > 0 ? (double)seg.SizeBytes / total : 0;

            // ── Обновляем UI ──
            ChartTotalValue.Text = SizeHelper.Format(totalFreed);
            ChartLegend.ItemsSource = main;
            DrawPieChart(main);
            ChartOverlay.Visibility = Visibility.Visible;
        }

        /// <summary>Нормализует имя категории: браузеры → «Chrome кэш» и т.д.</summary>
        private static string NormalizeCategory(string cat)
        {
            if (cat.StartsWith("Chrome",  StringComparison.OrdinalIgnoreCase)) return "Chrome кэш";
            if (cat.StartsWith("Edge",    StringComparison.OrdinalIgnoreCase)) return "Edge кэш";
            if (cat.StartsWith("Firefox", StringComparison.OrdinalIgnoreCase)) return "Firefox кэш";
            if (cat.StartsWith("Brave",   StringComparison.OrdinalIgnoreCase)) return "Brave кэш";
            if (cat.StartsWith("Opera",   StringComparison.OrdinalIgnoreCase)) return "Opera кэш";
            if (cat.StartsWith("Yandex",  StringComparison.OrdinalIgnoreCase)) return "Yandex кэш";
            if (cat.StartsWith("Vivaldi", StringComparison.OrdinalIgnoreCase)) return "Vivaldi кэш";
            if (cat.StartsWith("WU кэш",  StringComparison.OrdinalIgnoreCase)) return "Windows Update кэш";
            if (cat.StartsWith("Корзина", StringComparison.OrdinalIgnoreCase)) return "Корзина";
            if (cat.StartsWith("Temp",    StringComparison.OrdinalIgnoreCase)) return "User Temp";
            if (cat.StartsWith("Windows Temp", StringComparison.OrdinalIgnoreCase)) return "Windows Temp";
            return cat;
        }

        /// <summary>
        /// Рисует пончиковую диаграмму на ChartCanvas через PathGeometry.
        /// Каждый сегмент — дуга между двумя углами с анимацией разворачивания.
        /// </summary>
        private void DrawPieChart(List<ChartSegment> segments)
        {
            ChartCanvas.Children.Clear();

            const double cx = 90, cy = 90;   // центр
            const double ro = 85, ri = 52;   // внешний и внутренний радиус (пончик)

            double startAngle = -90.0;        // начинаем сверху

            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];
                if (seg.Pct <= 0) continue;

                double sweep = seg.Pct * 360.0;
                // Маленький зазор между сегментами — визуальное разделение
                double gap = segments.Count > 1 ? 1.5 : 0;
                double endAngle = startAngle + sweep - gap;

                var path = CreateDonutSegment(cx, cy, ro, ri, startAngle + gap / 2, endAngle);
                path.Fill = seg.ColorBrush;
                path.Stroke = new SolidColorBrush(Color.FromArgb(0x30, 0x00, 0x00, 0x00));
                path.StrokeThickness = 0.5;

                // Tooltip при наведении
                path.ToolTip = $"{seg.Category}\n{seg.SizeText}  ({seg.PctText})";

                // Анимация: разворачивание от 0 до полного угла через ScaleTransform
                // Используем RotateTransform для последовательного «вырастания»
                path.RenderTransformOrigin = new Point(0.5, 0.5);
                path.Opacity = 0;
                ChartCanvas.Children.Add(path);

                // Staggered fade-in: каждый сегмент с задержкой
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400))
                {
                    BeginTime    = TimeSpan.FromMilliseconds(i * 80),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                };
                path.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                startAngle += sweep;
            }
        }

        /// <summary>
        /// Создаёт Path для одного сегмента пончика.
        /// Геометрия: два дуговых отрезка + две прямые линии замыкания.
        /// </summary>
        private static System.Windows.Shapes.Path CreateDonutSegment(
            double cx, double cy,
            double ro, double ri,
            double startDeg, double endDeg)
        {
            // Конвертируем градусы → радианы
            double startRad = startDeg * Math.PI / 180.0;
            double endRad   = endDeg   * Math.PI / 180.0;

            bool isLargeArc = (endDeg - startDeg) > 180.0;

            // Четыре точки сегмента
            var p1 = new Point(cx + ro * Math.Cos(startRad), cy + ro * Math.Sin(startRad));
            var p2 = new Point(cx + ro * Math.Cos(endRad),   cy + ro * Math.Sin(endRad));
            var p3 = new Point(cx + ri * Math.Cos(endRad),   cy + ri * Math.Sin(endRad));
            var p4 = new Point(cx + ri * Math.Cos(startRad), cy + ri * Math.Sin(startRad));

            var fig = new PathFigure { StartPoint = p1, IsClosed = true };
            // Внешняя дуга (по часовой)
            fig.Segments.Add(new ArcSegment(p2, new System.Windows.Size(ro, ro),
                0, isLargeArc, SweepDirection.Clockwise, true));
            // Линия к внутреннему радиусу
            fig.Segments.Add(new LineSegment(p3, true));
            // Внутренняя дуга (против часовой)
            fig.Segments.Add(new ArcSegment(p4, new System.Windows.Size(ri, ri),
                0, isLargeArc, SweepDirection.Counterclockwise, true));

            var geo = new PathGeometry();
            geo.Figures.Add(fig);
            return new System.Windows.Shapes.Path { Data = geo };
        }

        // Кнопка «← К результатам» — скрываем оверлей
        private void ChartBack_Click(object sender, MouseButtonEventArgs e)
        {
            ChartOverlay.Visibility = Visibility.Collapsed;
            SwitchTab(ActiveTab.Files);
        }

        private void ChartBackBtn_Enter(object sender, MouseEventArgs e)
        {
            if (sender is Border b)
                b.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x5A));
        }
        private void ChartBackBtn_Leave(object sender, MouseEventArgs e)
        {
            if (sender is Border b)
                b.Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x2A, 0x4A));
        }

        // ══════════════════════════════════════════════════════════════
        //  TOOLBAR
        // ══════════════════════════════════════════════════════════════

        // Получаем все CheckBox из контейнера — не нужно поддерживать список вручную
        private IEnumerable<CheckBox> GetAllCheckBoxes()
        {
            return new[]
            {
                ChkTempFiles, ChkWinTemp, ChkRecycleBin, ChkBrowserCache,
                ChkPrefetch, ChkThumbnails, ChkEventLogs, ChkDnsCache,
                ChkMSOffice, ChkExternalDrives
            }.Where(cb => cb != null)!;
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in GetAllCheckBoxes()) cb.IsChecked = true;
            SaveSettings();
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in GetAllCheckBoxes()) cb.IsChecked = false;
            SaveSettings();
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
            => new AboutWindow { Owner = this }.ShowDialog();

        // ══════════════════════════════════════════════════════════════
        //  WINDOW CHROME
        // ══════════════════════════════════════════════════════════════

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) MaximizeRestore();
            else if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }
        private void MinBtn_Click(object sender, RoutedEventArgs e)
        {
            // Скрываем в трей напрямую через кнопку — не через StateChanged
            // чтобы не показывать промежуточный Minimized на панели задач
            Hide();
            App.Instance.ShowBalloon(
                "CleanupTemp Pro свёрнут в трей",
                "Дважды щёлкните иконку, чтобы открыть снова.",
                durationMs: 2500);
        }
        private void MaxBtn_Click(object sender, RoutedEventArgs e) => MaximizeRestore();
        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void MaximizeRestore() =>
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal : WindowState.Maximized;

        private void TabHeader_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is not Border header) return;
            bool isTransparent = header.Background == Brushes.Transparent ||
                                 (header.Background as SolidColorBrush)?.Color.A == 0;
            if (isTransparent)
                header.Background = new SolidColorBrush(Color.FromArgb(50, 74, 158, 255));
            header.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Color.FromRgb(0x4A, 0x9E, 0xFF),
                BlurRadius = 8, ShadowDepth = 0, Opacity = 0.3
            };
        }

        private void TabHeader_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is not Border header) return;
            bool isFilesActive   = FilesPanel.Visibility   == Visibility.Visible;
            bool isHistoryActive = HistoryPanel.Visibility == Visibility.Visible;
            if (header == TabFilesHeader   && !isFilesActive)   header.Background = Brushes.Transparent;
            if (header == TabHistoryHeader && !isHistoryActive) header.Background = Brushes.Transparent;
            header.Opacity = 0.9;
            header.Effect  = null;
        }

        // ══════════════════════════════════════════════════════════════
        //  LOGO LOADER
        // ══════════════════════════════════════════════════════════════

        private void LoadLogo()
        {
            var bmp = TryLoadBitmap(new Uri("pack://application:,,,/app_icon.png", UriKind.Absolute));
            if (bmp == null)
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                foreach (var name in new[] { "app_icon.png", "CleanupTempPro_Logo.png", "logo.png" })
                {
                    string p = System.IO.Path.Combine(dir, name);
                    if (File.Exists(p))
                    {
                        bmp = TryLoadBitmap(new Uri(p, UriKind.Absolute));
                        if (bmp != null) break;
                    }
                }
            }
            if (bmp != null) TitleLogoImage.Source = bmp;
            else AppLog.Warn("LoadLogo: no logo file found");
        }

        // ══════════════════════════════════════════════════════════════
        //  SHIMMER — переливание заголовка и иконки при наведении
        // ══════════════════════════════════════════════════════════════

        // Анимации храним как поля — чтобы остановить их при MouseLeave
        private DoubleAnimation? _shimmerAnim;
        private DoubleAnimation? _shimmerAnim2;
        private DoubleAnimation? _iconGlowAnim;

        private void TitleLogo_MouseEnter(object sender, MouseEventArgs e)
        {
            // ── Shimmer на тексте «Temp» ──
            var brush1 = new LinearGradientBrush();
            brush1.StartPoint = new Point(0, 0.5);
            brush1.EndPoint   = new Point(1, 0.5);
            brush1.GradientStops.Add(new GradientStop(Color.FromRgb(0x4A, 0x9E, 0xFF), 0.0));
            brush1.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xFF, 0xFF), 0.5));
            brush1.GradientStops.Add(new GradientStop(Color.FromRgb(0xA8, 0x55, 0xF7), 1.0));
            TitleRunTemp.Foreground = brush1;

            _shimmerAnim = new DoubleAnimation(-0.5, 1.5, TimeSpan.FromMilliseconds(900))
            {
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            brush1.GradientStops[1].BeginAnimation(GradientStop.OffsetProperty, _shimmerAnim);

            // ── Shimmer на тексте «Pro» ──
            var brush2 = new LinearGradientBrush();
            brush2.StartPoint = new Point(0, 0.5);
            brush2.EndPoint   = new Point(1, 0.5);
            brush2.GradientStops.Add(new GradientStop(Color.FromRgb(0x06, 0xD6, 0xC7), 0.0));
            brush2.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0xFF, 0xFF), 0.5));
            brush2.GradientStops.Add(new GradientStop(Color.FromRgb(0x4A, 0x9E, 0xFF), 1.0));
            TitleRunPro.Foreground = brush2;

            _shimmerAnim2 = new DoubleAnimation(-0.5, 1.5, TimeSpan.FromMilliseconds(900))
            {
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime      = TimeSpan.FromMilliseconds(150),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            brush2.GradientStops[1].BeginAnimation(GradientStop.OffsetProperty, _shimmerAnim2);

            // ── Glow на иконке ──
            _iconGlowAnim = new DoubleAnimation(8, 22, TimeSpan.FromMilliseconds(700))
            {
                AutoReverse    = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            var glow = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color       = Color.FromRgb(0x4A, 0x9E, 0xFF),
                ShadowDepth = 0,
                Opacity     = 0.85,
                BlurRadius  = 8,
            };
            TitleLogoImage.Effect = glow;
            glow.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty, _iconGlowAnim);
        }

        private void TitleLogo_MouseLeave(object sender, MouseEventArgs e)
        {
            _shimmerAnim?.SetValue(Timeline.BeginTimeProperty, null);
            _shimmerAnim2?.SetValue(Timeline.BeginTimeProperty, null);

            var origTemp = new LinearGradientBrush();
            origTemp.StartPoint = new Point(0, 0.5);
            origTemp.EndPoint   = new Point(1, 0.5);
            origTemp.GradientStops.Add(new GradientStop(Color.FromRgb(0x4A, 0x9E, 0xFF), 0.0));
            origTemp.GradientStops.Add(new GradientStop(Color.FromRgb(0xA8, 0x55, 0xF7), 1.0));
            origTemp.Freeze();
            TitleRunTemp.Foreground = origTemp;

            TitleRunPro.Foreground = new SolidColorBrush(Color.FromRgb(0x06, 0xD6, 0xC7));
            TitleLogoImage.Effect  = null;
        }

        private void TabFiles_Click(object sender, MouseButtonEventArgs e)
            => SwitchTab(ActiveTab.Files);

        private void TabHistory_Click(object sender, MouseButtonEventArgs e)
            => SwitchTab(ActiveTab.History);

        private void TabLarge_Click(object sender, MouseButtonEventArgs e)
            => SwitchTab(ActiveTab.Large);

        private void TabExclusions_Click(object sender, MouseButtonEventArgs e)
            => SwitchTab(ActiveTab.Exclusions);

        private static BitmapImage? TryLoadBitmap(Uri uri)
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
            catch (Exception ex) { AppLog.Warn($"TryLoadBitmap {uri}: {ex.Message}"); return null; }
        }
    }
}
