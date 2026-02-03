using System;
using System.Collections.Generic;

namespace CleanupTemp_Pro
{
    public static class LanguageManager
    {
        public static string CurrentLanguage { get; set; } = "Русский";

        private static Dictionary<string, Dictionary<string, string>> translations = new Dictionary<string, Dictionary<string, string>>
        {
            {
                "Русский", new Dictionary<string, string>
                {
                    // Основные элементы интерфейса
                    { "Title", "CleanupTemp Профессиональная" },
                    { "Version", "v4.2 Расширенная" },
                    { "Status_Ready", "Готов к очистке" },
                    { "Status_Running", "Выполняется очистка..." },
                    { "Status_Completed", "Очистка завершена!" },
                    { "Status_Cancelled", "Отменено" },
                    { "Status_Error", "Произошла ошибка" },
                    { "Status_Success", "Успешно" },
                    
                    // Кнопки
                    { "Button_Start", "НАЧАТЬ ОЧИСТКУ" },
                    { "Button_Stop", "СТОП" },
                    { "Button_Exit", "ВЫХОД" },
                    { "Button_OpenTemp", "Открыть Temp" },
                    { "Button_Refresh", "Обновить" },
                    { "Button_Export", "Экспорт лога" },
                    { "Button_Theme", "🌙 Тема" },
                    { "Button_Settings", "⚙ Настройки" },
                    { "Button_SelectAll", "Выбрать всё" },
                    { "Button_DeselectAll", "Снять всё" },
                    { "Button_Recommended", "Рекомендуемые" },
                    { "Button_Safety", "Безопасные" },
                    { "Button_ClearHistory", "Очистить историю" },
                    { "Button_ExportHistory", "Экспорт истории" },
                    { "Button_About", "ℹ О программе" },
                    
                    // Вкладки
                    { "Tab_Settings", "Настройки" },
                    { "Tab_Progress", "Прогресс" },
                    { "Tab_History", "История" },
                    { "Tab_Logs", "Логи" },
                    
                    // Чекбоксы настроек
                    { "CheckBox_AutoClose", "Автозакрытие приложений" },
                    { "CheckBox_PlaySound", "Звук завершения" },
                    { "CheckBox_ShowDetails", "Показать детали" },
                    
                    // Группы опций
                    { "Group_TempFiles", "Временные файлы Windows" },
                    { "Group_Browsers", "Браузеры" },
                    { "Group_Messengers", "Мессенджеры и приложения" },
                    { "Group_SystemUtils", "Системные утилиты и кэш" },
                    { "Group_Additional", "Дополнительно" },
                    
                    // Опции очистки
                    { "Option_RecycleBin", "Корзина" },
                    { "Option_RecentItems", "Недавние элементы" },
                    { "Option_TempSetup", "Временные установки" },
                    { "Option_YandexBrowser", "Яндекс Браузер" },
                    { "Option_DNSCache", "Очистка DNS кэша" },
                    { "Option_DISM", "DISM очистка (медленно)" },
                    { "Option_ThumbnailCache", "Кэш миниатюр" },
                    { "Option_IconCache", "Кэш иконок" },
                    { "Option_WindowsUpdate", "Кэш Windows Update" },
                    { "Option_EventLogs", "Логи событий Windows" },
                    { "Option_DeliveryOptimization", "Оптимизация доставки" },
                    { "Option_MemoryDumps", "Дампы памяти" },
                    { "Option_ErrorReports", "Отчёты об ошибках" },
                    { "Option_TempInternet", "Временные файлы интернета" },
                    { "Option_FontCache", "Кэш шрифтов" },
                    { "Option_LogFiles", "Системные логи (.log)" },
                    { "Option_OldDrivers", "Старые драйверы" },
                    { "Option_WinSxS", "WinSxS очистка (осторожно!)" },
                    { "Option_RestorePoints", "Старые точки восстановления" },
                    { "Option_TempUser", "Временные профили пользователей" },
                    { "Option_DiagnosticData", "Диагностические данные" },
                    { "Option_RegistryCleanup", "Очистка реестра" },
                    { "Option_StartupPrograms", "Автозапуск программ" },
                    
                    // Прогресс
                    { "Progress_Label", "Общий прогресс:" },
                    { "Progress_TrackingRealtime", "Отслеживание прогресса в реальном времени" },
                    
                    // Категории прогресса
                    { "Category_TempFiles", "Временные файлы Windows" },
                    { "Category_Browsers", "Браузеры" },
                    { "Category_Messengers", "Мессенджеры" },
                    { "Category_SystemUtils", "Системные утилиты" },
                    
                    // Статистика
                    { "Stats_SessionTitle", "Статистика сессии" },
                    { "Stats_WaitingMessage", "Ожидание начала очистки...\n\nСтатистика будет отображаться здесь во время очистки:\n- Освобождённое место в реальном времени\n- Удалённые файлы и папки\n- Затраченное время" },
                    { "Stats_Files", "файлов" },
                    { "Stats_Folders", "папок" },
                    
                    // История
                    { "History_Title", "История очистки" },
                    { "History_Subtitle", "Отслеживайте все ваши сессии очистки" },
                    { "History_DateTime", "Дата и время" },
                    { "History_Freed", "Освобождено" },
                    { "History_Files", "Файлы" },
                    { "History_Folders", "Папки" },
                    { "History_Time", "Время" },
                    { "History_Status", "Статус" },
                    
                    // Логи
                    { "Logs_Title", "Подробные логи" },
                    
                    // Темы
                    { "Theme_Dark", "Тёмная" },
                    { "Theme_Light", "Светлая" },
                    
                    // Сообщения
                    { "Message_StatsUpdated", "Статистика обновлена!" },
                    { "Message_Information", "Информация" },
                    { "Message_HistoryExported", "История экспортирована в:\n{0}" },
                    { "Message_ExportComplete", "Экспорт завершён" },
                    { "Message_ExportError", "Ошибка экспорта: {0}" },
                    { "Message_Error", "Ошибка" },
                    { "Message_NothingSelected", "Пожалуйста, выберите хотя бы один пункт для очистки!" },
                    { "Message_Warning", "Ничего не выбрано" },
                    { "Message_DangerousOptions", "Вы выбрали потенциально опасные опции очистки!\n\nWinSxS и старые драйверы могут повлиять на стабильность системы.\nПродолжить?" },
                    { "Message_Attention", "Внимание!" },
                    { "Message_LogExported", "Лог экспортирован в:\n{0}" },
                    { "Message_SettingsSaved", "Настройки сохранены!" },
                    { "Message_ConfirmExit", "Очистка выполняется. Вы уверены, что хотите выйти?" },
                    { "Message_ExitTitle", "Подтверждение выхода" },
                    
                    // Логи очистки
                    { "Log_SessionStarted", "Сессия очистки начата" },
                    { "Log_Date", "Дата" },
                    { "Log_Processing", "Обработка" },
                    { "Log_CompletedSuccessfully", "Очистка успешно завершена" },
                    { "Log_CancelledByUser", "Очистка отменена пользователем!" },
                    { "Log_OperationCancelled", "Операция очистки была отменена." },
                    { "Log_Error", "ОШИБКА" },
                    
                    // === TOOLTIPS (Подсказки) ===
                    
                    // Tooltips - Кнопки управления
                    { "Tooltip_Settings", "Открыть настройки программы (язык, тема)" },
                    { "Tooltip_Start", "Начать процесс очистки выбранных папок и файлов" },
                    { "Tooltip_Stop", "Остановить текущую операцию очистки" },
                    { "Tooltip_OpenTemp", "Открыть папку TEMP в проводнике" },
                    { "Tooltip_Refresh", "Обновить информацию о размере папок и статистику" },
                    { "Tooltip_Export", "Экспортировать лог очистки в текстовый файл" },
                    { "Tooltip_Exit", "Выйти из программы" },
                    
                    // Tooltips - Чекбоксы настроек
                    { "Tooltip_AutoClose", "Автоматически закрывать работающие приложения перед очисткой" },
                    { "Tooltip_PlaySound", "Воспроизвести звуковой сигнал при завершении очистки" },
                    { "Tooltip_ShowDetails", "Показывать подробную информацию о процессе очистки" },
                    
                    // Tooltips - Временные файлы Windows
                    { "Tooltip_WinTemp", "✅ БЕЗОПАСНО\n📁 C:\\Windows\\Temp\nВременные файлы системы Windows. Можно безопасно удалять." },
                    { "Tooltip_Prefetch", "⚠️ ВНИМАНИЕ\n📁 C:\\Windows\\Prefetch\nФайлы для ускорения запуска программ.\nWindows пересоздаст их автоматически." },
                    { "Tooltip_RecycleBin", "⚠️ ВНИМАНИЕ\n📁 Корзина\nФайлы будут удалены НАВСЕГДА без возможности восстановления!" },
                    { "Tooltip_RecentItems", "✅ БЕЗОПАСНО\n📁 Недавние элементы\nИстория недавно открытых файлов. Безопасно очищать." },
                    { "Tooltip_TempSetup", "✅ БЕЗОПАСНО\n📁 Временные установочные файлы\nОставшиеся файлы после установки программ." },
                    
                    // Tooltips - Браузеры
                    { "Tooltip_Opera", "✅ БЕЗОПАСНО\n📁 Кэш Opera / Opera GX\nВременные файлы браузера. Освободит место." },
                    { "Tooltip_Chrome", "✅ БЕЗОПАСНО\n📁 Кэш Google Chrome\nВременные интернет-файлы, история загрузок." },
                    { "Tooltip_Edge", "✅ БЕЗОПАСНО\n📁 Кэш Microsoft Edge\nВременные файлы браузера Edge." },
                    { "Tooltip_Firefox", "✅ БЕЗОПАСНО\n📁 Кэш Mozilla Firefox\nВременные интернет-файлы Firefox." },
                    { "Tooltip_Brave", "✅ БЕЗОПАСНО\n📁 Кэш Brave Browser\nВременные файлы Brave браузера." },
                    { "Tooltip_Yandex", "✅ БЕЗОПАСНО\n📁 Кэш Яндекс Браузер\nВременные файлы Яндекс браузера." },
                    { "Tooltip_Vivaldi", "✅ БЕЗОПАСНО\n📁 Кэш Vivaldi\nВременные файлы Vivaldi браузера." },
                    { "Tooltip_Tor", "✅ БЕЗОПАСНО\n📁 Кэш Tor Browser\nВременные файлы Tor браузера." },
                    
                    // Tooltips - Мессенджеры
                    { "Tooltip_Telegram", "✅ БЕЗОПАСНО\n📁 Кэш Telegram\nВременные файлы, загруженные медиа. Сообщения сохранятся." },
                    { "Tooltip_Discord", "✅ БЕЗОПАСНО\n📁 Кэш Discord\nВременные файлы Discord. Данные останутся на сервере." },
                    { "Tooltip_Viber", "✅ БЕЗОПАСНО\n📁 Кэш Viber\nВременные файлы Viber." },
                    { "Tooltip_Zoom", "✅ БЕЗОПАСНО\n📁 Кэш Zoom\nВременные файлы конференций Zoom." },
                    { "Tooltip_Spotify", "✅ БЕЗОПАСНО\n📁 Кэш Spotify\nКэшированная музыка. Будет загружена заново при прослушивании." },
                    { "Tooltip_VSCode", "✅ БЕЗОПАСНО\n📁 Кэш Visual Studio Code\nВременные файлы редактора. Настройки сохранятся." },
                    { "Tooltip_Teams", "✅ БЕЗОПАСНО\n📁 Кэш Microsoft Teams\nВременные файлы Teams." },
                    { "Tooltip_Skype", "✅ БЕЗОПАСНО\n📁 Кэш Skype\nВременные файлы Skype." },
                    { "Tooltip_Slack", "✅ БЕЗОПАСНО\n📁 Кэш Slack\nВременные файлы Slack." },
                    
                    // Tooltips - Системные утилиты
                    { "Tooltip_DNS", "✅ БЕЗОПАСНО\n🔧 Очистка DNS кэша\nСброс кэша DNS. Помогает решить проблемы с интернетом." },
                    { "Tooltip_DISM", "⚠️ МЕДЛЕННО\n🔧 DISM очистка компонентов\nГлубокая очистка системных компонентов. Занимает много времени." },
                    { "Tooltip_ThumbnailCache", "✅ БЕЗОПАСНО\n📁 Кэш миниатюр изображений\nПревью картинок. Windows создаст заново при необходимости." },
                    { "Tooltip_IconCache", "✅ БЕЗОПАСНО\n📁 Кэш иконок\nКэш значков файлов. Автоматически пересоздастся." },
                    { "Tooltip_WindowsUpdate", "⚠️ ОСТОРОЖНО\n📁 Кэш Windows Update\nЗагруженные обновления. Может потребоваться повторная загрузка." },
                    { "Tooltip_EventLogs", "✅ БЕЗОПАСНО\n📁 Журналы событий Windows\nЛоги системных событий. Безопасно удалять старые." },
                    { "Tooltip_DeliveryOptimization", "⚠️ ВНИМАНИЕ\n📁 Оптимизация доставки обновлений\nКэш обновлений. Освободит место, но обновления загрузятся снова." },
                    { "Tooltip_SoftwareDistribution", "⚠️ ОСТОРОЖНО\n📁 C:\\Windows\\SoftwareDistribution\nПапка распространения обновлений. Может потребовать перезагрузки." },
                    { "Tooltip_MemoryDumps", "✅ БЕЗОПАСНО\n📁 Дампы памяти при сбоях\nФайлы отладки системных ошибок. Занимают много места." },
                    { "Tooltip_ErrorReports", "✅ БЕЗОПАСНО\n📁 Отчёты об ошибках Windows\nОтчёты WER. Можно безопасно удалять." },
                    { "Tooltip_TempInternet", "✅ БЕЗОПАСНО\n📁 Временные файлы интернета\nКэш IE и системных компонентов." },
                    { "Tooltip_FontCache", "⚠️ ВНИМАНИЕ\n📁 Кэш шрифтов\nКэш системных шрифтов. Пересоздастся после перезагрузки." },
                    
                    // Tooltips - Дополнительно
                    { "Tooltip_LogFiles", "✅ БЕЗОПАСНО\n📁 Системные .log файлы\nСтарые журналы программ и системы." },
                    { "Tooltip_OldDrivers", "❌ ОПАСНО\n📁 Старые драйверы устройств\nМожет повлиять на возможность отката драйверов!" },
                    { "Tooltip_WinSxS", "❌ КРАЙНЕ ОПАСНО\n📁 WinSxS очистка\nХранилище компонентов Windows. Может нарушить работу системы!\nИспользуйте только если точно знаете, что делаете!" },
                    { "Tooltip_RestorePoints", "⚠️ ОСТОРОЖНО\n📁 Точки восстановления системы\nУдалит старые точки восстановления. Освободит много места." },
                    { "Tooltip_TempUser", "✅ БЕЗОПАСНО\n📁 Временные профили пользователей\nВременные данные профилей." },
                    { "Tooltip_DiagnosticData", "✅ БЕЗОПАСНО\n📁 Диагностические данные\nДанные телеметрии Windows." },
                    { "Tooltip_RegistryCleanup", "⚠️ ОСТОРОЖНО\n📁 Очистка реестра\nУдаление устаревших ключей реестра. Может ускорить систему." },
                    { "Tooltip_StartupPrograms", "✅ БЕЗОПАСНО\n🔧 Автозапуск программ\nУправление программами, запускающимися при старте Windows." },
                    
                    // О программе
                    { "About_Title", "О программе CleanupTemp Pro" },
                    { "About_ProgramInfo", "CleanupTemp Professional v4.2\n\nПрограмма для очистки временных файлов Windows\n\nВозможности:\n• Очистка временных файлов системы\n• Очистка кэша браузеров\n• Очистка мессенджеров\n• Системные утилиты\n• Очистка реестра\n• Управление автозапуском\n• Поддержка тем (светлая/тёмная)\n• Мультиязычность (RU/EN/UA)\n\nРазработчик:\nВиталий Николаевич (vitalikkontr)\n\nGitHub:\nhttps://github.com/vitalikkontr/CleanupTemp-Pro\n\n© 2026 CleanupTemp Pro\nВсе права защищены." },
                }
            },
            {
                "English", new Dictionary<string, string>
                {
                    // Main interface elements
                    { "Title", "CleanupTemp Professional" },
                    { "Version", "v4.2 Extended" },
                    { "Status_Ready", "Ready to clean" },
                    { "Status_Running", "Cleaning in progress..." },
                    { "Status_Completed", "Cleanup completed!" },
                    { "Status_Cancelled", "Cancelled" },
                    { "Status_Error", "An error occurred" },
                    { "Status_Success", "Success" },
                    
                    // Buttons
                    { "Button_Start", "START CLEANUP" },
                    { "Button_Stop", "STOP" },
                    { "Button_Exit", "EXIT" },
                    { "Button_OpenTemp", "Open Temp" },
                    { "Button_Refresh", "Refresh" },
                    { "Button_Export", "Export Log" },
                    { "Button_Theme", "🌙 Theme" },
                    { "Button_Settings", "⚙ Settings" },
                    { "Button_SelectAll", "Select All" },
                    { "Button_DeselectAll", "Deselect All" },
                    { "Button_Recommended", "Recommended" },
                    { "Button_Safety", "Safe" },
                    { "Button_ClearHistory", "Clear History" },
                    { "Button_ExportHistory", "Export History" },
                    { "Button_About", "ℹ About" },
                    
                    // Tabs
                    { "Tab_Settings", "Settings" },
                    { "Tab_Progress", "Progress" },
                    { "Tab_History", "History" },
                    { "Tab_Logs", "Logs" },
                    
                    // Setting checkboxes
                    { "CheckBox_AutoClose", "Auto-close applications" },
                    { "CheckBox_PlaySound", "Play completion sound" },
                    { "CheckBox_ShowDetails", "Show details" },
                    
                    // Option groups
                    { "Group_TempFiles", "Windows Temporary Files" },
                    { "Group_Browsers", "Browsers" },
                    { "Group_Messengers", "Messengers and Applications" },
                    { "Group_SystemUtils", "System Utilities and Cache" },
                    { "Group_Additional", "Additional" },
                    
                    // Cleanup options
                    { "Option_RecycleBin", "Recycle Bin" },
                    { "Option_RecentItems", "Recent Items" },
                    { "Option_TempSetup", "Temporary Installers" },
                    { "Option_YandexBrowser", "Yandex Browser" },
                    { "Option_DNSCache", "DNS Cache Cleanup" },
                    { "Option_DISM", "DISM Cleanup (slow)" },
                    { "Option_ThumbnailCache", "Thumbnail Cache" },
                    { "Option_IconCache", "Icon Cache" },
                    { "Option_WindowsUpdate", "Windows Update Cache" },
                    { "Option_EventLogs", "Windows Event Logs" },
                    { "Option_DeliveryOptimization", "Delivery Optimization" },
                    { "Option_MemoryDumps", "Memory Dumps" },
                    { "Option_ErrorReports", "Error Reports" },
                    { "Option_TempInternet", "Temporary Internet Files" },
                    { "Option_FontCache", "Font Cache" },
                    { "Option_LogFiles", "System Log Files (.log)" },
                    { "Option_OldDrivers", "Old Drivers" },
                    { "Option_WinSxS", "WinSxS Cleanup (caution!)" },
                    { "Option_RestorePoints", "Old Restore Points" },
                    { "Option_TempUser", "Temporary User Profiles" },
                    { "Option_DiagnosticData", "Diagnostic Data" },
                    { "Option_RegistryCleanup", "Registry cleanup" },
                    { "Option_StartupPrograms", "Startup programs" },
                    
                    // Progress
                    { "Progress_Label", "Overall progress:" },
                    { "Progress_TrackingRealtime", "Real-time progress tracking" },
                    
                    // Progress categories
                    { "Category_TempFiles", "Windows Temporary Files" },
                    { "Category_Browsers", "Browsers" },
                    { "Category_Messengers", "Messengers" },
                    { "Category_SystemUtils", "System Utilities" },
                    
                    // Statistics
                    { "Stats_SessionTitle", "Session Statistics" },
                    { "Stats_WaitingMessage", "Waiting for cleanup to start...\n\nStatistics will be displayed here during cleanup:\n- Freed space in real-time\n- Deleted files and folders\n- Time elapsed" },
                    { "Stats_Files", "files" },
                    { "Stats_Folders", "folders" },
                    
                    // History
                    { "History_Title", "Cleanup History" },
                    { "History_Subtitle", "Track all your cleanup sessions" },
                    { "History_DateTime", "Date and Time" },
                    { "History_Freed", "Freed" },
                    { "History_Files", "Files" },
                    { "History_Folders", "Folders" },
                    { "History_Time", "Time" },
                    { "History_Status", "Status" },
                    
                    // Logs
                    { "Logs_Title", "Detailed Logs" },
                    
                    // Themes
                    { "Theme_Dark", "Dark" },
                    { "Theme_Light", "Light" },
                    
                    // Messages
                    { "Message_StatsUpdated", "Statistics updated!" },
                    { "Message_Information", "Information" },
                    { "Message_HistoryExported", "History exported to:\n{0}" },
                    { "Message_ExportComplete", "Export Complete" },
                    { "Message_ExportError", "Export error: {0}" },
                    { "Message_Error", "Error" },
                    { "Message_NothingSelected", "Please select at least one item to clean!" },
                    { "Message_Warning", "Nothing Selected" },
                    { "Message_DangerousOptions", "You have selected potentially dangerous cleanup options!\n\nWinSxS and old drivers may affect system stability.\nContinue?" },
                    { "Message_Attention", "Attention!" },
                    { "Message_LogExported", "Log exported to:\n{0}" },
                    { "Message_SettingsSaved", "Settings saved!" },
                    { "Message_ConfirmExit", "Cleanup is in progress. Are you sure you want to exit?" },
                    { "Message_ExitTitle", "Confirm Exit" },
                    
                    // Cleanup logs
                    { "Log_SessionStarted", "Cleanup session started" },
                    { "Log_Date", "Date" },
                    { "Log_Processing", "Processing" },
                    { "Log_CompletedSuccessfully", "Cleanup completed successfully" },
                    { "Log_CancelledByUser", "Cleanup cancelled by user!" },
                    { "Log_OperationCancelled", "Cleanup operation was cancelled." },
                    { "Log_Error", "ERROR" },
                    
                    // === TOOLTIPS ===
                    
                    // Tooltips - Control Buttons
                    { "Tooltip_Settings", "Open program settings (language, theme)" },
                    { "Tooltip_Start", "Start cleaning selected folders and files" },
                    { "Tooltip_Stop", "Stop current cleanup operation" },
                    { "Tooltip_OpenTemp", "Open TEMP folder in Explorer" },
                    { "Tooltip_Refresh", "Refresh folder size information and statistics" },
                    { "Tooltip_Export", "Export cleanup log to text file" },
                    { "Tooltip_Exit", "Exit program" },
                    
                    // Tooltips - Setting Checkboxes
                    { "Tooltip_AutoClose", "Automatically close running applications before cleanup" },
                    { "Tooltip_PlaySound", "Play sound signal when cleanup completes" },
                    { "Tooltip_ShowDetails", "Show detailed information about cleanup process" },
                    
                    // Tooltips - Windows Temp Files
                    { "Tooltip_WinTemp", "✅ SAFE\n📁 C:\\Windows\\Temp\nWindows system temporary files. Safe to delete." },
                    { "Tooltip_Prefetch", "⚠️ CAUTION\n📁 C:\\Windows\\Prefetch\nFiles to speed up program launch.\nWindows will recreate them automatically." },
                    { "Tooltip_RecycleBin", "⚠️ WARNING\n📁 Recycle Bin\nFiles will be deleted PERMANENTLY without recovery!" },
                    { "Tooltip_RecentItems", "✅ SAFE\n📁 Recent Items\nHistory of recently opened files. Safe to clean." },
                    { "Tooltip_TempSetup", "✅ SAFE\n📁 Temporary installers\nLeftover files after program installation." },
                    
                    // Tooltips - Browsers
                    { "Tooltip_Opera", "✅ SAFE\n📁 Opera / Opera GX Cache\nBrowser temporary files. Will free up space." },
                    { "Tooltip_Chrome", "✅ SAFE\n📁 Google Chrome Cache\nTemporary internet files, download history." },
                    { "Tooltip_Edge", "✅ SAFE\n📁 Microsoft Edge Cache\nEdge browser temporary files." },
                    { "Tooltip_Firefox", "✅ SAFE\n📁 Mozilla Firefox Cache\nFirefox temporary internet files." },
                    { "Tooltip_Brave", "✅ SAFE\n📁 Brave Browser Cache\nBrave browser temporary files." },
                    { "Tooltip_Yandex", "✅ SAFE\n📁 Yandex Browser Cache\nYandex browser temporary files." },
                    { "Tooltip_Vivaldi", "✅ SAFE\n📁 Vivaldi Cache\nVivaldi browser temporary files." },
                    { "Tooltip_Tor", "✅ SAFE\n📁 Tor Browser Cache\nTor browser temporary files." },
                    
                    // Tooltips - Messengers
                    { "Tooltip_Telegram", "✅ SAFE\n📁 Telegram Cache\nTemporary files, downloaded media. Messages will be saved." },
                    { "Tooltip_Discord", "✅ SAFE\n📁 Discord Cache\nDiscord temporary files. Data will remain on server." },
                    { "Tooltip_Viber", "✅ SAFE\n📁 Viber Cache\nViber temporary files." },
                    { "Tooltip_Zoom", "✅ SAFE\n📁 Zoom Cache\nZoom conference temporary files." },
                    { "Tooltip_Spotify", "✅ SAFE\n📁 Spotify Cache\nCached music. Will be downloaded again when playing." },
                    { "Tooltip_VSCode", "✅ SAFE\n📁 Visual Studio Code Cache\nEditor temporary files. Settings will be saved." },
                    { "Tooltip_Teams", "✅ SAFE\n📁 Microsoft Teams Cache\nTeams temporary files." },
                    { "Tooltip_Skype", "✅ SAFE\n📁 Skype Cache\nSkype temporary files." },
                    { "Tooltip_Slack", "✅ SAFE\n📁 Slack Cache\nSlack temporary files." },
                    
                    // Tooltips - System Utilities
                    { "Tooltip_DNS", "✅ SAFE\n🔧 DNS Cache Cleanup\nDNS cache reset. Helps solve internet problems." },
                    { "Tooltip_DISM", "⚠️ SLOW\n🔧 DISM component cleanup\nDeep system component cleanup. Takes a long time." },
                    { "Tooltip_ThumbnailCache", "✅ SAFE\n📁 Image thumbnail cache\nImage previews. Windows will recreate when needed." },
                    { "Tooltip_IconCache", "✅ SAFE\n📁 Icon cache\nFile icon cache. Will be automatically recreated." },
                    { "Tooltip_WindowsUpdate", "⚠️ CAUTION\n📁 Windows Update Cache\nDownloaded updates. May require re-downloading." },
                    { "Tooltip_EventLogs", "✅ SAFE\n📁 Windows Event Logs\nSystem event logs. Safe to delete old ones." },
                    { "Tooltip_DeliveryOptimization", "⚠️ WARNING\n📁 Update delivery optimization\nUpdate cache. Will free space but updates will download again." },
                    { "Tooltip_SoftwareDistribution", "⚠️ CAUTION\n📁 C:\\Windows\\SoftwareDistribution\nUpdate distribution folder. May require reboot." },
                    { "Tooltip_MemoryDumps", "✅ SAFE\n📁 Crash memory dumps\nSystem error debug files. Take up a lot of space." },
                    { "Tooltip_ErrorReports", "✅ SAFE\n📁 Windows Error Reports\nWER reports. Safe to delete." },
                    { "Tooltip_TempInternet", "✅ SAFE\n📁 Temporary Internet Files\nIE and system component cache." },
                    { "Tooltip_FontCache", "⚠️ WARNING\n📁 Font cache\nSystem font cache. Will be recreated after reboot." },
                    
                    // Tooltips - Additional
                    { "Tooltip_LogFiles", "✅ SAFE\n📁 System .log files\nOld program and system logs." },
                    { "Tooltip_OldDrivers", "❌ DANGEROUS\n📁 Old device drivers\nMay affect ability to rollback drivers!" },
                    { "Tooltip_WinSxS", "❌ EXTREMELY DANGEROUS\n📁 WinSxS cleanup\nWindows component store. May break system!\nUse only if you know what you're doing!" },
                    { "Tooltip_RestorePoints", "⚠️ CAUTION\n📁 System restore points\nWill delete old restore points. Will free a lot of space." },
                    { "Tooltip_TempUser", "✅ SAFE\n📁 Temporary user profiles\nTemporary profile data." },
                    { "Tooltip_DiagnosticData", "✅ SAFE\n📁 Diagnostic data\nWindows telemetry data." },
                    { "Tooltip_RegistryCleanup", "⚠️ CAUTION\n📁 Registry cleanup\nRemoving outdated registry keys. May speed up system." },
                    { "Tooltip_StartupPrograms", "✅ SAFE\n🔧 Startup programs\nManage programs that start with Windows." },
                    
                    // About
                    { "About_Title", "About CleanupTemp Pro" },
                    { "About_ProgramInfo", "CleanupTemp Professional v4.2\n\nWindows temporary file cleanup program\n\nFeatures:\n• System temporary file cleanup\n• Browser cache cleanup\n• Messenger cleanup\n• System utilities\n• Registry cleanup\n• Startup management\n• Theme support (light/dark)\n• Multilingual (RU/EN/UA)\n\nDeveloper:\nVitaliy Nikolaevich (vitalikkontr)\n\nGitHub:\nhttps://github.com/vitalikkontr/CleanupTemp-Pro\n\n© 2026 CleanupTemp Pro\nAll rights reserved." },
                }
            },
            {
                "Українська", new Dictionary<string, string>
                {
                    // Основні елементи інтерфейсу
                    { "Title", "CleanupTemp Професійна" },
                    { "Version", "v4.2 Розширена" },
                    { "Status_Ready", "Готовий до очищення" },
                    { "Status_Running", "Виконується очищення..." },
                    { "Status_Completed", "Очищення завершено!" },
                    { "Status_Cancelled", "Скасовано" },
                    { "Status_Error", "Сталася помилка" },
                    { "Status_Success", "Успішно" },
                    
                    // Кнопки
                    { "Button_Start", "ПОЧАТИ ОЧИЩЕННЯ" },
                    { "Button_Stop", "СТОП" },
                    { "Button_Exit", "ВИХІД" },
                    { "Button_OpenTemp", "Відкрити Temp" },
                    { "Button_Refresh", "Оновити" },
                    { "Button_Export", "Експорт логу" },
                    { "Button_Theme", "🌙 Тема" },
                    { "Button_Settings", "⚙ Налаштування" },
                    { "Button_SelectAll", "Вибрати все" },
                    { "Button_DeselectAll", "Зняти все" },
                    { "Button_Recommended", "Рекомендовані" },
                    { "Button_Safety", "Безпечні" },
                    { "Button_ClearHistory", "Очистити історію" },
                    { "Button_ExportHistory", "Експорт історії" },
                    { "Button_About", "ℹ Про програму" },
                    
                    // Вкладки
                    { "Tab_Settings", "Налаштування" },
                    { "Tab_Progress", "Прогрес" },
                    { "Tab_History", "Історія" },
                    { "Tab_Logs", "Логи" },
                    
                    // Чекбокси налаштувань
                    { "CheckBox_AutoClose", "Автозакриття додатків" },
                    { "CheckBox_PlaySound", "Звук завершення" },
                    { "CheckBox_ShowDetails", "Показати деталі" },
                    
                    // Групи опцій
                    { "Group_TempFiles", "Тимчасові файли Windows" },
                    { "Group_Browsers", "Браузери" },
                    { "Group_Messengers", "Месенджери та додатки" },
                    { "Group_SystemUtils", "Системні утиліти та кеш" },
                    { "Group_Additional", "Додатково" },
                    
                    // Опції очищення
                    { "Option_RecycleBin", "Кошик" },
                    { "Option_RecentItems", "Недавні елементи" },
                    { "Option_TempSetup", "Тимчасові інсталяції" },
                    { "Option_YandexBrowser", "Яндекс Браузер" },
                    { "Option_DNSCache", "Очищення DNS кешу" },
                    { "Option_DISM", "DISM очищення (повільно)" },
                    { "Option_ThumbnailCache", "Кеш мініатюр" },
                    { "Option_IconCache", "Кеш іконок" },
                    { "Option_WindowsUpdate", "Кеш Windows Update" },
                    { "Option_EventLogs", "Логи подій Windows" },
                    { "Option_DeliveryOptimization", "Оптимізація доставки" },
                    { "Option_MemoryDumps", "Дампи пам'яті" },
                    { "Option_ErrorReports", "Звіти про помилки" },
                    { "Option_TempInternet", "Тимчасові файли інтернету" },
                    { "Option_FontCache", "Кеш шрифтів" },
                    { "Option_LogFiles", "Системні логи (.log)" },
                    { "Option_OldDrivers", "Старі драйвери" },
                    { "Option_WinSxS", "WinSxS очищення (обережно!)" },
                    { "Option_RestorePoints", "Старі точки відновлення" },
                    { "Option_TempUser", "Тимчасові профілі користувачів" },
                    { "Option_DiagnosticData", "Діагностичні дані" },
                    { "Option_RegistryCleanup", "Очищення реєстру" },
                    { "Option_StartupPrograms", "Автозапуск програм" },
                    
                    // Прогрес
                    { "Progress_Label", "Загальний прогрес:" },
                    { "Progress_TrackingRealtime", "Відстеження прогресу в реальному часі" },
                    
                    // Категорії прогресу
                    { "Category_TempFiles", "Тимчасові файли Windows" },
                    { "Category_Browsers", "Браузери" },
                    { "Category_Messengers", "Месенджери" },
                    { "Category_SystemUtils", "Системні утиліти" },
                    
                    // Статистика
                    { "Stats_SessionTitle", "Статистика сесії" },
                    { "Stats_WaitingMessage", "Очікування початку очищення...\n\nСтатистика буде відображатися тут під час очищення:\n- Звільнене місце в реальному часі\n- Видалені файли та папки\n- Витрачений час" },
                    { "Stats_Files", "файлів" },
                    { "Stats_Folders", "папок" },
                    
                    // Історія
                    { "History_Title", "Історія очищення" },
                    { "History_Subtitle", "Відстежуйте всі ваші сесії очищення" },
                    { "History_DateTime", "Дата та час" },
                    { "History_Freed", "Звільнено" },
                    { "History_Files", "Файли" },
                    { "History_Folders", "Папки" },
                    { "History_Time", "Час" },
                    { "History_Status", "Статус" },
                    
                    // Логи
                    { "Logs_Title", "Детальні логи" },
                    
                    // Теми
                    { "Theme_Dark", "Темна" },
                    { "Theme_Light", "Світла" },
                    
                    // Повідомлення
                    { "Message_StatsUpdated", "Статистику оновлено!" },
                    { "Message_Information", "Інформація" },
                    { "Message_HistoryExported", "Історію експортовано в:\n{0}" },
                    { "Message_ExportComplete", "Експорт завершено" },
                    { "Message_ExportError", "Помилка експорту: {0}" },
                    { "Message_Error", "Помилка" },
                    { "Message_NothingSelected", "Будь ласка, виберіть хоча б один пункт для очищення!" },
                    { "Message_Warning", "Нічого не вибрано" },
                    { "Message_DangerousOptions", "Ви вибрали потенційно небезпечні опції очищення!\n\nWinSxS та старі драйвери можуть вплинути на стабільність системи.\nПродовжити?" },
                    { "Message_Attention", "Увага!" },
                    { "Message_LogExported", "Лог експортовано в:\n{0}" },
                    { "Message_SettingsSaved", "Налаштування збережено!" },
                    { "Message_ConfirmExit", "Очищення виконується. Ви впевнені, що хочете вийти?" },
                    { "Message_ExitTitle", "Підтвердження виходу" },
                    
                    // Логи очищення
                    { "Log_SessionStarted", "Сесію очищення розпочато" },
                    { "Log_Date", "Дата" },
                    { "Log_Processing", "Обробка" },
                    { "Log_CompletedSuccessfully", "Очищення успішно завершено" },
                    { "Log_CancelledByUser", "Очищення скасовано користувачем!" },
                    { "Log_OperationCancelled", "Операцію очищення було скасовано." },
                    { "Log_Error", "ПОМИЛКА" },
                    
                    // === TOOLTIPS (Підказки) ===
                    
                    // Tooltips - Кнопки керування
                    { "Tooltip_Settings", "Відкрити налаштування програми (мова, тема)" },
                    { "Tooltip_Start", "Почати процес очищення вибраних папок і файлів" },
                    { "Tooltip_Stop", "Зупинити поточну операцію очищення" },
                    { "Tooltip_OpenTemp", "Відкрити папку TEMP в провіднику" },
                    { "Tooltip_Refresh", "Оновити інформацію про розмір папок і статистику" },
                    { "Tooltip_Export", "Експортувати лог очищення в текстовий файл" },
                    { "Tooltip_Exit", "Вийти з програми" },
                    
                    // Tooltips - Чекбокси налаштувань
                    { "Tooltip_AutoClose", "Автоматично закривати працюючі додатки перед очищенням" },
                    { "Tooltip_PlaySound", "Відтворити звуковий сигнал при завершенні очищення" },
                    { "Tooltip_ShowDetails", "Показувати детальну інформацію про процес очищення" },
                    
                    // Tooltips - Тимчасові файли Windows
                    { "Tooltip_WinTemp", "✅ БЕЗПЕЧНО\n📁 C:\\Windows\\Temp\nТимчасові файли системи Windows. Можна безпечно видаляти." },
                    { "Tooltip_Prefetch", "⚠️ УВАГА\n📁 C:\\Windows\\Prefetch\nФайли для прискорення запуску програм.\nWindows створить їх автоматично." },
                    { "Tooltip_RecycleBin", "⚠️ УВАГА\n📁 Кошик\nФайли будуть видалені НАЗАВЖДИ без можливості відновлення!" },
                    { "Tooltip_RecentItems", "✅ БЕЗПЕЧНО\n📁 Недавні елементи\nІсторія нещодавно відкритих файлів. Безпечно очищати." },
                    { "Tooltip_TempSetup", "✅ БЕЗПЕЧНО\n📁 Тимчасові інсталяційні файли\nФайли, що залишилися після встановлення програм." },
                    
                    // Tooltips - Браузери
                    { "Tooltip_Opera", "✅ БЕЗПЕЧНО\n📁 Кеш Opera / Opera GX\nТимчасові файли браузера. Звільнить місце." },
                    { "Tooltip_Chrome", "✅ БЕЗПЕЧНО\n📁 Кеш Google Chrome\nТимчасові інтернет-файли, історія завантажень." },
                    { "Tooltip_Edge", "✅ БЕЗПЕЧНО\n📁 Кеш Microsoft Edge\nТимчасові файли браузера Edge." },
                    { "Tooltip_Firefox", "✅ БЕЗПЕЧНО\n📁 Кеш Mozilla Firefox\nТимчасові інтернет-файли Firefox." },
                    { "Tooltip_Brave", "✅ БЕЗПЕЧНО\n📁 Кеш Brave Browser\nТимчасові файли браузера Brave." },
                    { "Tooltip_Yandex", "✅ БЕЗПЕЧНО\n📁 Кеш Яндекс Браузер\nТимчасові файли Яндекс браузера." },
                    { "Tooltip_Vivaldi", "✅ БЕЗПЕЧНО\n📁 Кеш Vivaldi\nТимчасові файли браузера Vivaldi." },
                    { "Tooltip_Tor", "✅ БЕЗПЕЧНО\n📁 Кеш Tor Browser\nТимчасові файли браузера Tor." },
                    
                    // Tooltips - Месенджери
                    { "Tooltip_Telegram", "✅ БЕЗПЕЧНО\n📁 Кеш Telegram\nТимчасові файли, завантажені медіа. Повідомлення збережуться." },
                    { "Tooltip_Discord", "✅ БЕЗПЕЧНО\n📁 Кеш Discord\nТимчасові файли Discord. Дані залишаться на сервері." },
                    { "Tooltip_Viber", "✅ БЕЗПЕЧНО\n📁 Кеш Viber\nТимчасові файли Viber." },
                    { "Tooltip_Zoom", "✅ БЕЗПЕЧНО\n📁 Кеш Zoom\nТимчасові файли конференцій Zoom." },
                    { "Tooltip_Spotify", "✅ БЕЗПЕЧНО\n📁 Кеш Spotify\nКешована музика. Буде завантажена знову при прослуховуванні." },
                    { "Tooltip_VSCode", "✅ БЕЗПЕЧНО\n📁 Кеш Visual Studio Code\nТимчасові файли редактора. Налаштування збережуться." },
                    { "Tooltip_Teams", "✅ БЕЗПЕЧНО\n📁 Кеш Microsoft Teams\nТимчасові файли Teams." },
                    { "Tooltip_Skype", "✅ БЕЗПЕЧНО\n📁 Кеш Skype\nТимчасові файли Skype." },
                    { "Tooltip_Slack", "✅ БЕЗПЕЧНО\n📁 Кеш Slack\nТимчасові файли Slack." },
                    
                    // Tooltips - Системні утиліти
                    { "Tooltip_DNS", "✅ БЕЗПЕЧНО\n🔧 Очищення DNS кешу\nСкидання кешу DNS. Допомагає вирішити проблеми з інтернетом." },
                    { "Tooltip_DISM", "⚠️ ПОВІЛЬНО\n🔧 DISM очищення компонентів\nГлибоке очищення системних компонентів. Займає багато часу." },
                    { "Tooltip_ThumbnailCache", "✅ БЕЗПЕЧНО\n📁 Кеш мініатюр зображень\nПревью картинок. Windows створить знову при необхідності." },
                    { "Tooltip_IconCache", "✅ БЕЗПЕЧНО\n📁 Кеш іконок\nКеш значків файлів. Автоматично перествориться." },
                    { "Tooltip_WindowsUpdate", "⚠️ ОБЕРЕЖНО\n📁 Кеш Windows Update\nЗавантажені оновлення. Може знадобитися повторне завантаження." },
                    { "Tooltip_EventLogs", "✅ БЕЗПЕЧНО\n📁 Журнали подій Windows\nЛоги системних подій. Безпечно видаляти старі." },
                    { "Tooltip_DeliveryOptimization", "⚠️ УВАГА\n📁 Оптимізація доставки оновлень\nКеш оновлень. Звільнить місце, але оновлення завантажаться знову." },
                    { "Tooltip_SoftwareDistribution", "⚠️ ОБЕРЕЖНО\n📁 C:\\Windows\\SoftwareDistribution\nПапка розповсюдження оновлень. Може потребувати перезавантаження." },
                    { "Tooltip_MemoryDumps", "✅ БЕЗПЕЧНО\n📁 Дампи пам'яті при збоях\nФайли відлагодження системних помилок. Займають багато місця." },
                    { "Tooltip_ErrorReports", "✅ БЕЗПЕЧНО\n📁 Звіти про помилки Windows\nЗвіти WER. Можна безпечно видаляти." },
                    { "Tooltip_TempInternet", "✅ БЕЗПЕЧНО\n📁 Тимчасові файли інтернету\nКеш IE та системних компонентів." },
                    { "Tooltip_FontCache", "⚠️ УВАГА\n📁 Кеш шрифтів\nКеш системних шрифтів. Перествориться після перезавантаження." },
                    
                    // Tooltips - Додатково
                    { "Tooltip_LogFiles", "✅ БЕЗПЕЧНО\n📁 Системні .log файли\nСтарі журнали програм і системи." },
                    { "Tooltip_OldDrivers", "❌ НЕБЕЗПЕЧНО\n📁 Старі драйвери пристроїв\nМоже вплинути на можливість відкату драйверів!" },
                    { "Tooltip_WinSxS", "❌ ДУЖЕ НЕБЕЗПЕЧНО\n📁 WinSxS очищення\nСховище компонентів Windows. Може порушити роботу системи!\nВикористовуйте тільки якщо точно знаєте, що робите!" },
                    { "Tooltip_RestorePoints", "⚠️ ОБЕРЕЖНО\n📁 Точки відновлення системи\nВидалить старі точки відновлення. Звільнить багато місця." },
                    { "Tooltip_TempUser", "✅ БЕЗПЕЧНО\n📁 Тимчасові профілі користувачів\nТимчасові дані профілів." },
                    { "Tooltip_DiagnosticData", "✅ БЕЗПЕЧНО\n📁 Діагностичні дані\nДані телеметрії Windows." },
                    { "Tooltip_RegistryCleanup", "⚠️ ОБЕРЕЖНО\n📁 Очищення реєстру\nВидалення застарілих ключів реєстру. Може прискорити систему." },
                    { "Tooltip_StartupPrograms", "✅ БЕЗПЕЧНО\n🔧 Автозапуск програм\nКерування програмами, що запускаються при старті Windows." },
                    
                    // Про програму
                    { "About_Title", "Про програму CleanupTemp Pro" },
                    { "About_ProgramInfo", "CleanupTemp Professional v4.2\n\nПрограма для очищення тимчасових файлів Windows\n\nМожливості:\n• Очищення тимчасових файлів системи\n• Очищення кешу браузерів\n• Очищення месенджерів\n• Системні утиліти\n• Очищення реєстру\n• Керування автозапуском\n• Підтримка тем (світла/темна)\n• Мультимовність (RU/EN/UA)\n\nРозробник:\nВіталій Миколайович (vitalikkontr)\n\nGitHub:\nhttps://github.com/vitalikkontr/CleanupTemp-Pro\n\n© 2026 CleanupTemp Pro\nВсі права захищено." },
                }
            }
        };

        public static string Get(string key)
        {
            if (translations.ContainsKey(CurrentLanguage) &&
                translations[CurrentLanguage].ContainsKey(key))
            {
                return translations[CurrentLanguage][key];
            }
            return key; // Возвращаем ключ, если перевод не найден
        }

        // Сохранение языка в файл
        public static void SaveLanguage()
        {
            try
            {
                string settingsPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "CleanupTempPro",
                    "language.txt"
                );

                string directory = System.IO.Path.GetDirectoryName(settingsPath);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                System.IO.File.WriteAllText(settingsPath, CurrentLanguage);
            }
            catch
            {
                // Игнорируем ошибки сохранения
            }
        }

        // Загрузка языка из файла
        public static void LoadLanguage()
        {
            try
            {
                string settingsPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "CleanupTempPro",
                    "language.txt"
                );

                if (System.IO.File.Exists(settingsPath))
                {
                    string savedLanguage = System.IO.File.ReadAllText(settingsPath).Trim();
                    if (translations.ContainsKey(savedLanguage))
                    {
                        CurrentLanguage = savedLanguage;
                    }
                }
            }
            catch
            {
                // По умолчанию русский
                CurrentLanguage = "Русский";
            }
        }

        // Получить список доступных языков
        public static List<string> GetAvailableLanguages()
        {
            return new List<string>(translations.Keys);
        }
    }
}
