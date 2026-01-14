using System;
using System.IO;
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
                    case "WinTemp":
                        await CleanWindowsTemp(token);
                        break;
                    case "Prefetch":
                        await CleanPrefetch(token);
                        break;
                    case "RecycleBin":
                        await CleanRecycleBin(token);
                        break;
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
                    case "Telegram":
                        await CleanAppCache("Telegram", autoClose, token);
                        break;
                    case "Discord":
                        await CleanAppCache("Discord", autoClose, token);
                        break;
                    case "Viber":
                        await CleanAppCache("Viber", autoClose, token);
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
                    case "DNS":
                        await FlushDNS(token);
                        break;
                    case "DISM":
                        await RunDISM(token);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка в {option}: {ex.Message}");
            }
        }

        private static async Task CleanWindowsTemp(CancellationToken token)
        {
            Log("Очистка папки Windows Temp...");
            string tempPath = Path.GetTempPath();
            await CleanDirectory(tempPath, token);
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

        private static async Task CleanBrowserCache(string browser, bool autoClose, CancellationToken token)
        {
            Log($"Очистка кэша {browser}...");
            
            string cachePath = GetBrowserCachePath(browser);
            
            if (autoClose)
            {
                await CloseProcessByName(GetBrowserProcessName(browser), token);
            }

            if (Directory.Exists(cachePath))
            {
                await CleanDirectory(cachePath, token);
                Log($"  ✓ Кэш {browser} очищен");
            }
            else
            {
                Log($"  ⚠ Кэш {browser} не найден");
            }
        }

        private static async Task CleanAppCache(string app, bool autoClose, CancellationToken token)
        {
            Log($"Очистка кэша {app}...");
            
            string cachePath = GetAppCachePath(app);
            
            if (autoClose)
            {
                await CloseProcessByName(GetAppProcessName(app), token);
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
                    process.StartInfo.Arguments = "/online /Cleanup-Image /StartComponentCleanup";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.Verb = "runas";
                    process.Start();
                    process.WaitForExit();
                    Log("  ✓ DISM очистка завершена");
                }
                catch (Exception ex)
                {
                    Log($"  ⚠ Ошибка: {ex.Message}");
                }
            }, token);
        }

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
                    return Path.Combine(appData, @"Opera Software\Opera Stable\Cache");
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
                case "Viber":
                    return Path.Combine(appData, @"ViberPC");
                case "Zoom":
                    return Path.Combine(appData, @"Zoom\logs");
                case "Spotify":
                    return Path.Combine(localAppData, @"Spotify\Data");
                case "VS Code":
                    return Path.Combine(appData, @"Code\Cache");
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
                default: return "";
            }
        }

        private static void Log(string message)
        {
            OnLog?.Invoke(message);
        }
    }
}