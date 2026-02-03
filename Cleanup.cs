using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CleanupTempPro
{
    public static class Cleanup
    {
        private static long totalFreedSpace = 0;
        private static int totalFiles = 0;
        private static int totalFolders = 0;

        public static Action<string> OnLog;
        public static Action OnStatsUpdate;

        [DllImport("shell32.dll")]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        public static void ResetStats()
        {
            totalFreedSpace = 0;
            totalFiles = 0;
            totalFolders = 0;
        }

        public static string GetFormattedFreedSpace()
        {
            double mb = totalFreedSpace / (1024.0 * 1024.0);
            if (mb < 1024)
                return string.Format("{0:F1} МБ", mb);
            else
                return string.Format("{0:F2} ГБ", mb / 1024.0);
        }

        public static int GetTotalFiles()
        {
            return totalFiles;
        }

        public static int GetTotalFolders()
        {
            return totalFolders;
        }

        public static async Task CleanupByOption(string option, bool autoClose, CancellationToken token)
        {
            try
            {
                switch (option)
                {
                    // Временные файлы Windows
                    case "WinTemp":
                        await CleanWindowsTemp(token);
                        break;
                    case "Prefetch":
                        await CleanPrefetch(token);
                        break;
                    case "RecycleBin":
                        await CleanRecycleBin(token);
                        break;
                    case "RecentItems":
                        await CleanRecentItems(token);
                        break;
                    case "TempSetup":
                        await CleanTempSetup(token);
                        break;

                    // Браузеры
                    case "Opera":
                        await CleanBrowserCache("Opera", autoClose, token);
                        break;
                    case "Chrome":
                        await CleanBrowserCache("Chrome", autoClose, token);
                        break;
                    case "Edge":
                        await CleanBrowserCache("Edge", autoClose, token);
                        break;
                    case "Firefox":
                        await CleanBrowserCache("Firefox", autoClose, token);
                        break;
                    case "Brave":
                        await CleanBrowserCache("Brave", autoClose, token);
                        break;
                    case "Yandex":
                        await CleanBrowserCache("Яндекс Браузер", autoClose, token);
                        break;
                    case "Vivaldi":
                        await CleanBrowserCache("Vivaldi", autoClose, token);
                        break;
                    case "Tor":
                        await CleanBrowserCache("Tor", autoClose, token);
                        break;

                    // Мессенджеры и приложения
                    case "Telegram":
                        await CleanAppCache("Telegram", autoClose, token);
                        break;
                    case "Discord":
                        await CleanAppCache("Discord", autoClose, token);
                        break;
                    case "Viber":
                        await CleanViberCache(autoClose, token);
                        break;
                    case "Zoom":
                        await CleanAppCache("Zoom", autoClose, token);
                        break;
                    case "Spotify":
                        await CleanAppCache("Spotify", autoClose, token);
                        break;
                    case "VSCode":
                        await CleanAppCache("VS Code", autoClose, token);
                        break;
                    case "Teams":
                        await CleanAppCache("Teams", autoClose, token);
                        break;
                    case "Skype":
                        await CleanAppCache("Skype", autoClose, token);
                        break;
                    case "Slack":
                        await CleanAppCache("Slack", autoClose, token);
                        break;

                    // Системные утилиты
                    case "DNS":
                        await FlushDNS(token);
                        break;
                    case "DISM":
                        await RunDISM(token);
                        break;
                    case "ThumbnailCache":
                        await CleanThumbnailCache(token);
                        break;
                    case "IconCache":
                        await CleanIconCache(token);
                        break;
                    case "WindowsUpdate":
                        await CleanWindowsUpdateCache(token);
                        break;
                    case "EventLogs":
                        await CleanEventLogs(token);
                        break;
                    case "DeliveryOptimization":
                        await CleanDeliveryOptimization(token);
                        break;
                    case "SoftwareDistribution":
                        await CleanSoftwareDistribution(token);
                        break;
                    case "MemoryDumps":
                        await CleanMemoryDumps(token);
                        break;
                    case "ErrorReports":
                        await CleanErrorReports(token);
                        break;
                    case "TempInternet":
                        await CleanTempInternetFiles(token);
                        break;
                    case "FontCache":
                        await CleanFontCache(token);
                        break;

                    // Дополнительные опции
                    case "LogFiles":
                        await CleanLogFiles(token);
                        break;
                    case "OldDrivers":
                        await CleanOldDrivers(token);
                        break;
                    case "WinSxS":
                        await CleanWinSxS(token);
                        break;
                    case "RestorePoints":
                        await CleanOldRestorePoints(token);
                        break;
                    case "TempUser":
                        await CleanTempUserProfiles(token);
                        break;
                    case "DiagnosticData":
                        await CleanDiagnosticData(token);
                        break;
                    case "RegistryCleanup":
                        await CleanRegistry(token);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка в {option}: {ex.Message}");
            }
        }

        // ========== ВРЕМЕННЫЕ ФАЙЛЫ WINDOWS ==========

        private static async Task CleanWindowsTemp(CancellationToken token)
        {
            Log("Очистка папки Windows Temp...");
            string tempPath = Path.GetTempPath();
            await CleanDirectory(tempPath, token);

            // Дополнительная очистка системной папки Temp
            string systemTemp = @"C:\Windows\Temp";
            if (Directory.Exists(systemTemp))
            {
                await CleanDirectory(systemTemp, token);
            }

            Log($"  ✓ Windows Temp очищен");
        }

        private static async Task CleanPrefetch(CancellationToken token)
        {
            Log("Очистка Prefetch...");
            string prefetchPath = @"C:\Windows\Prefetch";
            if (Directory.Exists(prefetchPath))
            {
                await CleanDirectory(prefetchPath, token);
                Log($"  ✓ Prefetch очищен");
            }
            else
            {
                Log("  ⚠ Папка Prefetch не найдена");
            }
        }

        private static async Task CleanRecycleBin(CancellationToken token)
        {
            Log("Очистка Корзины...");
            await Task.Run(() =>
            {
                try
                {
                    SHEmptyRecycleBin(IntPtr.Zero, null, 0);
                    Log("  ✓ Корзина очищена");
                }
                catch (Exception ex)
                {
                    Log($"  ⚠ Ошибка: {ex.Message}");
                }
            }, token);
        }

        private static async Task CleanRecentItems(CancellationToken token)
        {
            Log("Очистка недавних элементов...");
            string recentPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Recent));

            if (Directory.Exists(recentPath))
            {
                await CleanDirectory(recentPath, token);
                Log("  ✓ Недавние элементы очищены");
            }
            else
            {
                Log("  ⚠ Папка недавних элементов не найдена");
            }
        }

        private static async Task CleanTempSetup(CancellationToken token)
        {
            Log("Очистка временных файлов установки...");
            string[] setupPaths = new string[]
            {
                @"C:\Windows\Installer\$PatchCache$",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp")
            };

            foreach (string path in setupPaths)
            {
                if (Directory.Exists(path) && !token.IsCancellationRequested)
                {
                    await CleanDirectory(path, token);
                }
            }
            Log("  ✓ Временные файлы установки очищены");
        }

        // ========== БРАУЗЕРЫ ==========

        private static async Task CleanBrowserCache(string browser, bool autoClose, CancellationToken token)
        {
            Log($"Очистка кэша {browser}...");

            string cachePath = GetBrowserCachePath(browser);

            if (autoClose)
            {
                await CloseProcessByName(GetBrowserProcessName(browser), token);
                await Task.Delay(1000, token); // Даём время на закрытие
            }

            if (Directory.Exists(cachePath))
            {
                await CleanDirectory(cachePath, token);
                Log($"  ✓ Кэш {browser} очищен");
            }
            else
            {
                Log($"  ⚠ Кэш {browser} не найден по пути: {cachePath}");
            }
        }

        // ========== МЕССЕНДЖЕРЫ И ПРИЛОЖЕНИЯ ==========

        private static async Task CleanAppCache(string app, bool autoClose, CancellationToken token)
        {
            Log($"Очистка кэша {app}...");

            string cachePath = GetAppCachePath(app);

            if (autoClose)
            {
                await CloseProcessByName(GetAppProcessName(app), token);
                await Task.Delay(1000, token);
            }

            if (Directory.Exists(cachePath))
            {
                await CleanDirectory(cachePath, token);
                Log($"  ✓ Кэш {app} очищен");
            }
            else
            {
                Log($"  ⚠ Кэш {app} не найден");
            }
        }

        private static async Task CleanViberCache(bool autoClose, CancellationToken token)
        {
            Log("Очистка кэша Viber...");
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string viberBase = Path.Combine(appData, @"ViberPC");

            if (autoClose)
            {
                await CloseProcessByName("Viber", token);
                await Task.Delay(2000, token);
            }

            if (!Directory.Exists(viberBase))
            {
                Log("  ⚠ Viber не найден");
                return;
            }

            try
            {
                var userFolders = Directory.GetDirectories(viberBase)
                    .Where(d => Path.GetFileName(d).All(char.IsDigit))
                    .ToList();

                if (userFolders.Count == 0)
                {
                    Log("  ⚠ Папка пользователя Viber не найдена");
                    return;
                }

                string userFolder = userFolders[0];
                Log($"  → Найдена папка пользователя: {Path.GetFileName(userFolder)}");

                int cleanedFolders = 0;
                long startSize = totalFreedSpace;

                // ТОЛЬКО САМЫЕ БЕЗОПАСНЫЕ папки для полной очистки
                string[] safeFolders = new string[] {
            "Temporary"  // Только временные файлы - 100% безопасно
        };

                foreach (string folder in safeFolders)
                {
                    if (token.IsCancellationRequested) break;
                    string folderPath = Path.Combine(userFolder, folder);
                    if (Directory.Exists(folderPath))
                    {
                        Log($"  → Очистка {folder}...");
                        await CleanDirectoryContents(folderPath, token);
                        cleanedFolders++;
                    }
                }

                // Очистка СТАРЫХ файлов (30+ дней) - более консервативно
                string[] oldFileFolders = new string[] {
            "Thumbnails",      // Старые превью можно удалить
            "QmlUriCache",     // Только старый скомпилированный кэш
            "QmlWebCache"      // Только старый веб-кэш
        };

                foreach (string folder in oldFileFolders)
                {
                    if (token.IsCancellationRequested) break;
                    string folderPath = Path.Combine(userFolder, folder);
                    if (Directory.Exists(folderPath))
                    {
                        Log($"  → Очистка старых файлов (30+ дней) в {folder}...");
                        await CleanOldFiles(folderPath, TimeSpan.FromDays(30), token); // Увеличил до 30 дней
                        cleanedFolders++;
                    }
                }

                long freedSpace = totalFreedSpace - startSize;
                double mb = freedSpace / (1024.0 * 1024.0);

                if (cleanedFolders > 0)
                    Log($"  ✓ Кэш Viber очищен ({cleanedFolders} папок, {mb:F1} МБ)");
                else
                    Log("  ⚠ Папки кэша не найдены или уже пусты");

                Log("  ℹ Данные аккаунта и сообщения сохранены");
            }
            catch (Exception ex)
            {
                Log($"  ⚠ Ошибка очистки Viber: {ex.Message}");
            }
        }
        private static async Task CleanDirectoryContents(string path, CancellationToken token)
        {
            if (!Directory.Exists(path))
                return;

            await Task.Run(() =>
            {
                try
                {
                    foreach (string file in Directory.GetFiles(path))
                    {
                        if (token.IsCancellationRequested) break;

                        try
                        {
                            var fi = new FileInfo(file);
                            totalFreedSpace += fi.Length;
                            File.Delete(file);
                        }
                        catch { }
                    }

                    foreach (string dir in Directory.GetDirectories(path))
                    {
                        if (token.IsCancellationRequested) break;

                        try
                        {
                            long size = GetDirectorySize(dir);
                            totalFreedSpace += size;
                            Directory.Delete(dir, true);
                        }
                        catch { }
                    }
                }
                catch { }
            }, token);
        }


        private static async Task CleanOldFiles(string path, TimeSpan maxAge, CancellationToken token)
        {
            if (!Directory.Exists(path))
                return;

            await Task.Run(() =>
            {
                try
                {
                    DateTime cutoff = DateTime.Now - maxAge;

                    foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    {
                        if (token.IsCancellationRequested) break;

                        try
                        {
                            var fi = new FileInfo(file);
                            if (fi.LastAccessTime < cutoff)
                            {
                                totalFreedSpace += fi.Length;
                                File.Delete(file);
                            }
                        }
                        catch { }
                    }

                    // Удаляем пустые папки
                    foreach (string dir in Directory.GetDirectories(path))
                    {
                        if (token.IsCancellationRequested) break;

                        try
                        {
                            if (!Directory.EnumerateFileSystemEntries(dir).Any())
                                Directory.Delete(dir, false);
                        }
                        catch { }
                    }
                }
                catch { }
            }, token);
        }

        // Вспомогательный метод для подсчета размера папки
        private static long GetDirectorySize(string path)
        {
            try
            {
                return new DirectoryInfo(path)
                    .GetFiles("*", SearchOption.AllDirectories)
                    .Sum(fi => fi.Length);
            }
            catch
            {
                return 0;
            }
        }

        // ========== СИСТЕМНЫЕ УТИЛИТЫ ==========

        private static async Task FlushDNS(CancellationToken token)
        {
            Log("Очистка DNS кэша...");
            await Task.Run(() =>
            {
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = "ipconfig";
                    process.StartInfo.Arguments = "/flushdns";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit();
                    Log("  ✓ DNS кэш очищен");
                }
                catch (Exception ex)
                {
                    Log($"  ⚠ Ошибка: {ex.Message}");
                }
            }, token);
        }

        private static async Task RunDISM(CancellationToken token)
        {
            Log("Запуск DISM очистки (это может занять несколько минут)...");
            await Task.Run(() =>
            {
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = "DISM.exe";
                    process.StartInfo.Arguments = "/online /Cleanup-Image /StartComponentCleanup /ResetBase";
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.Verb = "runas";
                    process.Start();
                    process.WaitForExit();
                    Log("  ✓ DISM очистка завершена");
                }
                catch (Exception ex)
                {
                    Log($"  ⚠ Ошибка (требуются права администратора): {ex.Message}");
                }
            }, token);
        }

        private static async Task CleanThumbnailCache(CancellationToken token)
        {
            Log("Очистка кэша миниатюр...");
            string thumbCache = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Microsoft\Windows\Explorer");

            if (Directory.Exists(thumbCache))
            {
                await CleanDirectory(thumbCache, token);
                Log("  ✓ Кэш миниатюр очищен");
            }
            else
            {
                Log("  ⚠ Кэш миниатюр не найден");
            }
        }

        private static async Task CleanIconCache(CancellationToken token)
        {
            Log("Очистка кэша иконок...");
            string iconCache = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"IconCache.db");

            await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(iconCache))
                    {
                        File.Delete(iconCache);
                        Log("  ✓ Кэш иконок очищен");
                    }
                    else
                    {
                        Log("  ⚠ Файл кэша иконок не найден");
                    }
                }
                catch (Exception ex)
                {
                    Log($"  ⚠ Ошибка: {ex.Message}");
                }
            }, token);
        }

        private static async Task CleanWindowsUpdateCache(CancellationToken token)
        {
            Log("Очистка кэша Windows Update...");
            string updateCache = @"C:\Windows\SoftwareDistribution\Download";

            if (Directory.Exists(updateCache))
            {
                await CleanDirectory(updateCache, token);
                Log("  ✓ Кэш Windows Update очищен");
            }
            else
            {
                Log("  ⚠ Кэш Windows Update не найден");
            }
        }

        private static async Task CleanEventLogs(CancellationToken token)
        {
            Log("Очистка логов событий Windows...");
            await Task.Run(() =>
            {
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = "wevtutil";
                    process.StartInfo.Arguments = "cl System";
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.Verb = "runas";
                    process.Start();
                    process.WaitForExit();

                    process.StartInfo.Arguments = "cl Application";
                    process.Start();
                    process.WaitForExit();

                    Log("  ✓ Логи событий очищены");
                }
                catch (Exception ex)
                {
                    Log($"  ⚠ Ошибка (требуются права администратора): {ex.Message}");
                }
            }, token);
        }

        private static async Task CleanDeliveryOptimization(CancellationToken token)
        {
            Log("Очистка Оптимизации доставки...");
            string deliveryPath = @"C:\Windows\ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache";

            if (Directory.Exists(deliveryPath))
            {
                await CleanDirectory(deliveryPath, token);
                Log("  ✓ Оптимизация доставки очищена");
            }
            else
            {
                Log("  ⚠ Папка Оптимизации доставки не найдена");
            }
        }

        private static async Task CleanSoftwareDistribution(CancellationToken token)
        {
            Log("Очистка SoftwareDistribution...");
            string softDist = @"C:\Windows\SoftwareDistribution";

            if (Directory.Exists(softDist))
            {
                await CleanDirectory(softDist, token);
                Log("  ✓ SoftwareDistribution очищена");
            }
            else
            {
                Log("  ⚠ Папка SoftwareDistribution не найдена");
            }
        }

        private static async Task CleanMemoryDumps(CancellationToken token)
        {
            Log("Очистка дампов памяти...");
            string[] dumpPaths = new string[]
            {
                @"C:\Windows\Minidump",
                @"C:\Windows\memory.dmp",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"CrashDumps")
            };

            foreach (string path in dumpPaths)
            {
                if (token.IsCancellationRequested)
                    break;

                try
                {
                    if (Directory.Exists(path))
                    {
                        await CleanDirectory(path, token);
                    }
                    else if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch { }
            }
            Log("  ✓ Дампы памяти очищены");
        }

        private static async Task CleanErrorReports(CancellationToken token)
        {
            Log("Очистка отчётов об ошибках...");
            string[] errorPaths = new string[]
            {
                @"C:\ProgramData\Microsoft\Windows\WER\ReportQueue",
                @"C:\ProgramData\Microsoft\Windows\WER\ReportArchive",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\WER")
            };

            foreach (string path in errorPaths)
            {
                if (token.IsCancellationRequested)
                    break;

                if (Directory.Exists(path))
                {
                    await CleanDirectory(path, token);
                }
            }
            Log("  ✓ Отчёты об ошибках очищены");
        }

        private static async Task CleanTempInternetFiles(CancellationToken token)
        {
            Log("Очистка временных файлов интернета...");
            string inetCache = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Microsoft\Windows\INetCache");

            if (Directory.Exists(inetCache))
            {
                await CleanDirectory(inetCache, token);
                Log("  ✓ Временные файлы интернета очищены");
            }
            else
            {
                Log("  ⚠ Папка временных файлов интернета не найдена");
            }
        }

        private static async Task CleanFontCache(CancellationToken token)
        {
            Log("Очистка кэша шрифтов...");
            string fontCache = @"C:\Windows\ServiceProfiles\LocalService\AppData\Local\FontCache";

            if (Directory.Exists(fontCache))
            {
                await CleanDirectory(fontCache, token);
                Log("  ✓ Кэш шрифтов очищен");
            }
            else
            {
                Log("  ⚠ Кэш шрифтов не найден");
            }
        }

        // ========== ДОПОЛНИТЕЛЬНЫЕ ОПЦИИ ==========

        private static async Task CleanLogFiles(CancellationToken token)
        {
            Log("Очистка системных .log файлов...");
            string[] logPaths = new string[]
            {
                @"C:\Windows\Logs",
                @"C:\Windows\System32\LogFiles"
            };

            foreach (string path in logPaths)
            {
                if (token.IsCancellationRequested)
                    break;

                if (Directory.Exists(path))
                {
                    await CleanDirectory(path, token);
                }
            }
            Log("  ✓ Системные логи очищены");
        }

        private static async Task CleanOldDrivers(CancellationToken token)
        {
            Log("Очистка старых драйверов...");
            await Task.Run(() =>
            {
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = "rundll32.exe";
                    process.StartInfo.Arguments = "pnpclean.dll,RunDLL_PnpClean /DRIVERS /MAXCLEAN";
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.Verb = "runas";
                    process.Start();
                    process.WaitForExit();
                    Log("  ✓ Старые драйверы очищены");
                }
                catch (Exception ex)
                {
                    Log($"  ⚠ Ошибка (требуются права администратора): {ex.Message}");
                }
            }, token);
        }

        private static async Task CleanWinSxS(CancellationToken token)
        {
            Log("Очистка WinSxS (ОСТОРОЖНО! Это может занять много времени)...");
            await Task.Run(() =>
            {
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = "DISM.exe";
                    process.StartInfo.Arguments = "/online /Cleanup-Image /StartComponentCleanup /ResetBase";
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.Verb = "runas";
                    process.Start();
                    process.WaitForExit();
                    Log("  ✓ WinSxS очищена");
                }
                catch (Exception ex)
                {
                    Log($"  ⚠ Ошибка (требуются права администратора): {ex.Message}");
                }
            }, token);
        }

        private static async Task CleanOldRestorePoints(CancellationToken token)
        {
            Log("Удаление старых точек восстановления...");
            await Task.Run(() =>
            {
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = "vssadmin";
                    process.StartInfo.Arguments = "delete shadows /for=c: /oldest";
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.Verb = "runas";
                    process.Start();
                    process.WaitForExit();
                    Log("  ✓ Старые точки восстановления удалены");
                }
                catch (Exception ex)
                {
                    Log($"  ⚠ Ошибка (требуются права администратора): {ex.Message}");
                }
            }, token);
        }

        private static async Task CleanTempUserProfiles(CancellationToken token)
        {
            Log("Очистка временных профилей пользователей...");
            string tempProfiles = @"C:\Users\Temp";

            if (Directory.Exists(tempProfiles))
            {
                await CleanDirectory(tempProfiles, token);
                Log("  ✓ Временные профили очищены");
            }
            else
            {
                Log("  ⚠ Временные профили не найдены");
            }
        }

        private static async Task CleanDiagnosticData(CancellationToken token)
        {
            Log("Очистка диагностических данных...");
            string diagData = @"C:\ProgramData\Microsoft\Diagnosis";

            if (Directory.Exists(diagData))
            {
                await CleanDirectory(diagData, token);
                Log("  ✓ Диагностические данные очищены");
            }
            else
            {
                Log("  ⚠ Диагностические данные не найдены");
            }
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

        private static async Task CleanDirectory(string path, CancellationToken token)
        {
            await Task.Run(() =>
            {
                try
                {
                    DirectoryInfo di = new DirectoryInfo(path);

                    foreach (FileInfo file in di.GetFiles())
                    {
                        if (token.IsCancellationRequested)
                            break;

                        try
                        {
                            long size = file.Length;
                            file.Delete();
                            totalFiles++;
                            totalFreedSpace += size;
                            OnStatsUpdate?.Invoke();
                        }
                        catch { }
                    }

                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        if (token.IsCancellationRequested)
                            break;

                        try
                        {
                            dir.Delete(true);
                            totalFolders++;
                            OnStatsUpdate?.Invoke();
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    Log($"  ⚠ Ошибка доступа к директории: {ex.Message}");
                }
            }, token);
        }

        private static async Task CloseProcessByName(string processName, CancellationToken token)
        {
            await Task.Run(() =>
            {
                try
                {
                    foreach (Process proc in Process.GetProcessesByName(processName))
                    {
                        try
                        {
                            proc.Kill();
                            proc.WaitForExit(2000);
                        }
                        catch { }
                    }
                }
                catch { }
            }, token);
        }

        private static string GetBrowserCachePath(string browser)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            switch (browser)
            {
                case "Opera":
                    string operaBase = Path.Combine(appData, @"Opera Software");
                    string operaStableCache = Path.Combine(operaBase, @"Opera Stable\Default\Cache");
                    if (Directory.Exists(operaStableCache))
                        return operaStableCache;

                    string operaOneCache = Path.Combine(operaBase, @"Opera One\Default\Cache");
                    if (Directory.Exists(operaOneCache))
                        return operaOneCache;

                    string operaGXCache = Path.Combine(operaBase, @"Opera GX Stable\Default\Cache");
                    if (Directory.Exists(operaGXCache))
                        return operaGXCache;

                    return operaStableCache;

                case "Chrome":
                    return Path.Combine(appData, @"Google\Chrome\User Data\Default\Cache");
                case "Edge":
                    return Path.Combine(appData, @"Microsoft\Edge\User Data\Default\Cache");
                case "Firefox":
                    return Path.Combine(appData, @"Mozilla\Firefox\Profiles");
                case "Brave":
                    return Path.Combine(appData, @"BraveSoftware\Brave-Browser\User Data\Default\Cache");
                case "Яндекс Браузер":
                    return Path.Combine(appData, @"Yandex\YandexBrowser\User Data\Default\Cache");
                case "Vivaldi":
                    return Path.Combine(appData, @"Vivaldi\User Data\Default\Cache");
                case "Tor":
                    return Path.Combine(appData, @"Tor Browser\Browser\TorBrowser\Data\Browser\profile.default\cache2");
                default:
                    return "";
            }
        }

        private static string GetAppCachePath(string app)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            switch (app)
            {
                case "Telegram":
                    return Path.Combine(appData, @"Telegram Desktop\tdata\user_data");
                case "Discord":
                    return Path.Combine(appData, @"discord\Cache");
                case "Zoom":
                    return Path.Combine(appData, @"Zoom\logs");
                case "Spotify":
                    return Path.Combine(localAppData, @"Spotify\Data");
                case "VS Code":
                    return Path.Combine(appData, @"Code\Cache");
                case "Teams":
                    return Path.Combine(appData, @"Microsoft\Teams\Cache");
                case "Skype":
                    return Path.Combine(appData, @"Skype\DataRv\Cache");
                case "Slack":
                    return Path.Combine(appData, @"Slack\Cache");
                default:
                    return "";
            }
        }

        private static string GetBrowserProcessName(string browser)
        {
            switch (browser)
            {
                case "Opera": return "opera";
                case "Chrome": return "chrome";
                case "Edge": return "msedge";
                case "Firefox": return "firefox";
                case "Brave": return "brave";
                case "Яндекс Браузер": return "browser";
                case "Vivaldi": return "vivaldi";
                case "Tor": return "firefox";
                default: return "";
            }
        }

        private static string GetAppProcessName(string app)
        {
            switch (app)
            {
                case "Telegram": return "Telegram";
                case "Discord": return "Discord";
                case "Viber": return "Viber";
                case "Zoom": return "Zoom";
                case "Spotify": return "Spotify";
                case "VS Code": return "Code";
                case "Teams": return "Teams";
                case "Skype": return "Skype";
                case "Slack": return "Slack";
                default: return "";
            }
        }

        private static async Task CleanRegistry(CancellationToken token)
        {
            Log("Очистка реестра...");
            await Task.Run(() =>
            {
                try
                {
                    using (Process process = new Process())
                    {
                        process.StartInfo.FileName = "cmd.exe";
                        process.StartInfo.Arguments = "/c reg delete \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\RecentDocs\" /f";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.Verb = "runas";
                        
                        process.Start();
                        process.WaitForExit(5000);
                    }
                    
                    Log("  ✓ Реестр очищен");
                }
                catch (Exception ex)
                {
                    Log($"  ⚠ Ошибка очистки реестра: {ex.Message}");
                }
            }, token);
        }


        private static void Log(string message)
        {
            OnLog?.Invoke(message);
        }
    }
}