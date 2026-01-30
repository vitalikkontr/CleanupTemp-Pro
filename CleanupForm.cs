using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using CleanupTemp_Pro;

namespace CleanupTempPro
{
    public class CleanupForm : Form
    {
        private Button btnStart, btnStop, btnExit, btnOpenTemp, btnExport, btnRefresh, btnTheme;
        private TextBox txtLog;
        private Label lblStats, lblStatus, lblTitle, lblVersion;
        private ProgressBar pbMain;
        private TabControl tabControl;
        private CheckBox chkAutoClose, chkPlaySound, chkShowDetails;
        private CancellationTokenSource cts;
        private Stopwatch stopwatch;
        private System.Windows.Forms.Timer updateTimer;
        private ToolTip toolTip; // ← ДОБАВЛЕНО

        private Dictionary<string, ProgressBar> progressBars;
        private Dictionary<string, CheckBox> cleanupOptions;
        private ListView listViewHistory;
        private int cleanupCount = 0;

        public CleanupForm()
        {
            this.Text = "CleanupTemp - Профессиональная версия";
            this.Size = new Size(1000, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = true;

            progressBars = new Dictionary<string, ProgressBar>();
            cleanupOptions = new Dictionary<string, CheckBox>();

            InitializeComponents();
            SetupCallbacks();
            SetupTimer();
            LoadHistory();
            InitializeToolTips(); // ← ДОБАВЛЕНО

            // Применяем тему
            ThemeManager.ApplyTheme(this);
        }

        private void InitializeToolTips()
        {
            // Создаём ToolTip
            toolTip = new ToolTip
            {
                AutoPopDelay = 8000,  // 8 секунд показывать
                InitialDelay = 400,   // 0.4 сек задержка
                ReshowDelay = 150,    // 0.15 сек между подсказками
                ShowAlways = true,
                IsBalloon = false
            };

            // ════════════════════════════════════════════════════════════════
            // КНОПКИ УПРАВЛЕНИЯ
            // ════════════════════════════════════════════════════════════════

            toolTip.SetToolTip(btnStart,
                "Начать процесс очистки выбранных папок и файлов");

            toolTip.SetToolTip(btnStop,
                "Остановить текущую операцию очистки");

            toolTip.SetToolTip(btnOpenTemp,
                "Открыть папку TEMP в проводнике");

            toolTip.SetToolTip(btnRefresh,
                "Обновить информацию о размере папок и статистику");

            toolTip.SetToolTip(btnExport,
                "Экспортировать лог очистки в текстовый файл");

            toolTip.SetToolTip(btnExit,
                "Выйти из программы");

            toolTip.SetToolTip(btnTheme,
                "Переключить между светлой и тёмной темой");

            // ════════════════════════════════════════════════════════════════
            // ЧЕКБОКСЫ НАСТРОЕК
            // ════════════════════════════════════════════════════════════════

            toolTip.SetToolTip(chkAutoClose,
                "Автоматически закрывать работающие приложения перед очисткой");

            toolTip.SetToolTip(chkPlaySound,
                "Воспроизвести звуковой сигнал при завершении очистки");

            toolTip.SetToolTip(chkShowDetails,
                "Показывать подробную информацию о процессе очистки");

            // ════════════════════════════════════════════════════════════════
            // ЧЕКБОКСЫ ОЧИСТКИ - ВРЕМЕННЫЕ ФАЙЛЫ WINDOWS
            // ════════════════════════════════════════════════════════════════

            toolTip.SetToolTip(cleanupOptions["WinTemp"],
                "✅ БЕЗОПАСНО\n" +
                "📁 C:\\Windows\\Temp\n" +
                "Временные файлы системы Windows. Можно безопасно удалять.");

            toolTip.SetToolTip(cleanupOptions["Prefetch"],
                "⚠️ ВНИМАНИЕ\n" +
                "📁 C:\\Windows\\Prefetch\n" +
                "Файлы для ускорения запуска программ.\n" +
                "Windows пересоздаст их автоматически.");

            toolTip.SetToolTip(cleanupOptions["RecycleBin"],
                "⚠️ ВНИМАНИЕ\n" +
                "📁 Корзина\n" +
                "Файлы будут удалены НАВСЕГДА без возможности восстановления!");

            toolTip.SetToolTip(cleanupOptions["RecentItems"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Недавние элементы\n" +
                "История недавно открытых файлов. Безопасно очищать.");

            toolTip.SetToolTip(cleanupOptions["TempSetup"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Временные установочные файлы\n" +
                "Оставшиеся файлы после установки программ.");

            // ════════════════════════════════════════════════════════════════
            // ЧЕКБОКСЫ ОЧИСТКИ - БРАУЗЕРЫ
            // ════════════════════════════════════════════════════════════════

            toolTip.SetToolTip(cleanupOptions["Opera"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Opera / Opera GX\n" +
                "Временные файлы браузера. Освободит место.");

            toolTip.SetToolTip(cleanupOptions["Chrome"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Google Chrome\n" +
                "Временные интернет-файлы, история загрузок.");

            toolTip.SetToolTip(cleanupOptions["Edge"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Microsoft Edge\n" +
                "Временные файлы браузера Edge.");

            toolTip.SetToolTip(cleanupOptions["Firefox"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Mozilla Firefox\n" +
                "Временные интернет-файлы Firefox.");

            toolTip.SetToolTip(cleanupOptions["Brave"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Brave Browser\n" +
                "Временные файлы Brave браузера.");

            toolTip.SetToolTip(cleanupOptions["Yandex"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Яндекс Браузер\n" +
                "Временные файлы Яндекс браузера.");

            toolTip.SetToolTip(cleanupOptions["Vivaldi"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Vivaldi\n" +
                "Временные файлы Vivaldi браузера.");

            toolTip.SetToolTip(cleanupOptions["Tor"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Tor Browser\n" +
                "Временные файлы Tor браузера.");

            // ════════════════════════════════════════════════════════════════
            // ЧЕКБОКСЫ ОЧИСТКИ - МЕССЕНДЖЕРЫ
            // ════════════════════════════════════════════════════════════════

            toolTip.SetToolTip(cleanupOptions["Telegram"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Telegram\n" +
                "Временные файлы, загруженные медиа. Сообщения сохранятся.");

            toolTip.SetToolTip(cleanupOptions["Discord"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Discord\n" +
                "Временные файлы Discord. Данные останутся на сервере.");

            toolTip.SetToolTip(cleanupOptions["Viber"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Viber\n" +
                "Временные файлы Viber.");

            toolTip.SetToolTip(cleanupOptions["Zoom"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Zoom\n" +
                "Временные файлы конференций Zoom.");

            toolTip.SetToolTip(cleanupOptions["Spotify"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Spotify\n" +
                "Кэшированная музыка. Будет загружена заново при прослушивании.");

            toolTip.SetToolTip(cleanupOptions["VSCode"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Visual Studio Code\n" +
                "Временные файлы редактора. Настройки сохранятся.");

            toolTip.SetToolTip(cleanupOptions["Teams"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Microsoft Teams\n" +
                "Временные файлы Teams.");

            toolTip.SetToolTip(cleanupOptions["Skype"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Skype\n" +
                "Временные файлы Skype.");

            toolTip.SetToolTip(cleanupOptions["Slack"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш Slack\n" +
                "Временные файлы Slack.");

            // ════════════════════════════════════════════════════════════════
            // ЧЕКБОКСЫ ОЧИСТКИ - СИСТЕМНЫЕ УТИЛИТЫ
            // ════════════════════════════════════════════════════════════════

            toolTip.SetToolTip(cleanupOptions["DNS"],
                "✅ БЕЗОПАСНО\n" +
                "🔧 Очистка DNS кэша\n" +
                "Сброс кэша DNS. Помогает решить проблемы с интернетом.");

            toolTip.SetToolTip(cleanupOptions["DISM"],
                "⚠️ МЕДЛЕННО\n" +
                "🔧 DISM очистка компонентов\n" +
                "Глубокая очистка системных компонентов. Занимает много времени.");

            toolTip.SetToolTip(cleanupOptions["ThumbnailCache"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш миниатюр изображений\n" +
                "Превью картинок. Windows создаст заново при необходимости.");

            toolTip.SetToolTip(cleanupOptions["IconCache"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Кэш иконок\n" +
                "Кэш значков файлов. Автоматически пересоздастся.");

            toolTip.SetToolTip(cleanupOptions["WindowsUpdate"],
                "⚠️ ОСТОРОЖНО\n" +
                "📁 Кэш Windows Update\n" +
                "Загруженные обновления. Может потребоваться повторная загрузка.");

            toolTip.SetToolTip(cleanupOptions["EventLogs"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Журналы событий Windows\n" +
                "Логи системных событий. Безопасно удалять старые.");

            toolTip.SetToolTip(cleanupOptions["DeliveryOptimization"],
                "⚠️ ВНИМАНИЕ\n" +
                "📁 Оптимизация доставки обновлений\n" +
                "Кэш обновлений. Освободит место, но обновления загрузятся снова.");

            toolTip.SetToolTip(cleanupOptions["SoftwareDistribution"],
                "⚠️ ОСТОРОЖНО\n" +
                "📁 C:\\Windows\\SoftwareDistribution\n" +
                "Папка распространения обновлений. Может потребовать перезагрузки.");

            toolTip.SetToolTip(cleanupOptions["MemoryDumps"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Дампы памяти при сбоях\n" +
                "Файлы отладки системных ошибок. Занимают много места.");

            toolTip.SetToolTip(cleanupOptions["ErrorReports"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Отчёты об ошибках Windows\n" +
                "Отчёты WER. Можно безопасно удалять.");

            toolTip.SetToolTip(cleanupOptions["TempInternet"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Временные файлы интернета\n" +
                "Кэш IE и системных компонентов.");

            toolTip.SetToolTip(cleanupOptions["FontCache"],
                "⚠️ ВНИМАНИЕ\n" +
                "📁 Кэш шрифтов\n" +
                "Кэш системных шрифтов. Пересоздастся после перезагрузки.");

            // ════════════════════════════════════════════════════════════════
            // ЧЕКБОКСЫ ОЧИСТКИ - ДОПОЛНИТЕЛЬНО
            // ════════════════════════════════════════════════════════════════

            toolTip.SetToolTip(cleanupOptions["LogFiles"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Системные .log файлы\n" +
                "Старые журналы программ и системы.");

            toolTip.SetToolTip(cleanupOptions["OldDrivers"],
                "❌ ОПАСНО\n" +
                "📁 Старые драйверы устройств\n" +
                "Может повлиять на возможность отката драйверов!");

            toolTip.SetToolTip(cleanupOptions["WinSxS"],
                "❌ КРАЙНЕ ОПАСНО\n" +
                "📁 WinSxS очистка\n" +
                "Хранилище компонентов Windows. Может нарушить работу системы!\n" +
                "Используйте только если точно знаете, что делаете!");

            toolTip.SetToolTip(cleanupOptions["RestorePoints"],
                "⚠️ ОСТОРОЖНО\n" +
                "📁 Точки восстановления системы\n" +
                "Удалит старые точки восстановления. Освободит много места.");

            toolTip.SetToolTip(cleanupOptions["TempUser"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Временные профили пользователей\n" +
                "Временные данные профилей.");

            toolTip.SetToolTip(cleanupOptions["DiagnosticData"],
                "✅ БЕЗОПАСНО\n" +
                "📁 Диагностические данные\n" +
                "Данные телеметрии Windows.");
        }

        private void InitializeComponents()
        {
            Panel panelTop = new Panel();
            panelTop.Dock = DockStyle.Top;
            panelTop.Height = 120;
            panelTop.BackColor = ThemeManager.GetBackgroundSecondary();
            this.Controls.Add(panelTop);

            lblTitle = new Label();
            lblTitle.Text = "CleanupTemp Профессиональная";
            lblTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblTitle.ForeColor = ThemeManager.GetTextAccent();
            lblTitle.Left = 20;
            lblTitle.Top = 10;
            lblTitle.AutoSize = true;
            panelTop.Controls.Add(lblTitle);

            lblVersion = new Label();
            lblVersion.Text = "v4.1 Расширенная";
            lblVersion.Font = new Font("Segoe UI", 8);
            lblVersion.ForeColor = ThemeManager.GetTextSecondary();
            lblVersion.Left = 20;
            lblVersion.Top = 45;
            lblVersion.AutoSize = true;
            panelTop.Controls.Add(lblVersion);

            // Кнопка переключения темы
            btnTheme = new Button();
            btnTheme.Text = "🌙 Тема";
            btnTheme.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnTheme.Left = 880;
            btnTheme.Top = 15;
            btnTheme.Width = 80;
            btnTheme.Height = 30;
            btnTheme.BackColor = ThemeManager.GetButtonSecondary();
            btnTheme.ForeColor = Color.White;
            btnTheme.FlatStyle = FlatStyle.Flat;
            btnTheme.Cursor = Cursors.Hand;
            btnTheme.FlatAppearance.BorderSize = 0;
            btnTheme.Click += BtnTheme_Click;
            panelTop.Controls.Add(btnTheme);

            lblStatus = new Label();
            lblStatus.Text = "Готов к очистке";
            lblStatus.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblStatus.ForeColor = ThemeManager.GetTextSuccess();
            lblStatus.Left = 20;
            lblStatus.Top = 65;
            lblStatus.Width = 500;
            panelTop.Controls.Add(lblStatus);

            lblStats = new Label();
            lblStats.Text = "0 МБ | 0 файлов | 0 папок | 0с";
            lblStats.Left = 580;
            lblStats.Top = 20;
            lblStats.Width = 280;
            lblStats.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblStats.ForeColor = ThemeManager.GetTextInfo();
            lblStats.TextAlign = ContentAlignment.MiddleRight;
            panelTop.Controls.Add(lblStats);

            Label lblProgress = new Label();
            lblProgress.Text = "Общий прогресс:";
            lblProgress.Font = new Font("Segoe UI", 9);
            lblProgress.ForeColor = ThemeManager.GetTextPrimary();
            lblProgress.Left = 20;
            lblProgress.Top = 90;
            lblProgress.AutoSize = true;
            panelTop.Controls.Add(lblProgress);

            pbMain = new ProgressBar();
            pbMain.Left = 140;
            pbMain.Top = 88;
            pbMain.Width = 820;
            pbMain.Height = 20;
            pbMain.Style = ProgressBarStyle.Continuous;
            panelTop.Controls.Add(pbMain);

            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = new Font("Segoe UI", 10);
            this.Controls.Add(tabControl);

            TabPage tabOptions = CreateTab("Настройки");
            CreateOptionsTab(tabOptions);

            TabPage tabProgress = CreateTab("Прогресс");
            CreateProgressTab(tabProgress);

            TabPage tabHistory = CreateTab("История");
            CreateHistoryTab(tabHistory);

            TabPage tabLogs = CreateTab("Логи");
            CreateLogsTab(tabLogs);

            tabControl.TabPages.Add(tabOptions);
            tabControl.TabPages.Add(tabProgress);
            tabControl.TabPages.Add(tabHistory);
            tabControl.TabPages.Add(tabLogs);

            Panel panelButtons = new Panel();
            panelButtons.Dock = DockStyle.Bottom;
            panelButtons.Height = 80;
            panelButtons.BackColor = ThemeManager.GetBackgroundSecondary();
            this.Controls.Add(panelButtons);

            btnStart = CreateButton("НАЧАТЬ ОЧИСТКУ", 20, 10, 180, 35, ThemeManager.GetButtonPrimary());
            btnStop = CreateButton("СТОП", 220, 10, 120, 35, ThemeManager.GetButtonDanger());
            btnOpenTemp = CreateButton("Открыть Temp", 360, 10, 130, 35, ThemeManager.GetButtonSuccess());
            btnRefresh = CreateButton("Обновить", 510, 10, 100, 35, ThemeManager.GetButtonSecondary());
            btnExport = CreateButton("Экспорт лога", 630, 10, 130, 35, ThemeManager.GetButtonWarning());
            btnExit = CreateButton("ВЫХОД", 780, 10, 120, 35, ThemeManager.GetButtonNeutral());

            btnStop.Enabled = false;

            chkAutoClose = CreateCheckBox("Автозакрытие приложений", 20, 50);
            chkPlaySound = CreateCheckBox("Звук завершения", 200, 50);
            chkShowDetails = CreateCheckBox("Показать детали", 360, 50);

            btnStart.Click += BtnStart_Click;
            btnStop.Click += BtnStop_Click;
            btnOpenTemp.Click += delegate
            {
                try
                {
                    Process.Start("explorer.exe", Path.GetTempPath());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка открытия папки temp: " + ex.Message, "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnRefresh.Click += BtnRefresh_Click;
            btnExport.Click += BtnExport_Click;
            btnExit.Click += delegate { this.Close(); };

            panelButtons.Controls.Add(btnStart);
            panelButtons.Controls.Add(btnStop);
            panelButtons.Controls.Add(btnOpenTemp);
            panelButtons.Controls.Add(btnRefresh);
            panelButtons.Controls.Add(btnExport);
            panelButtons.Controls.Add(btnExit);
            panelButtons.Controls.Add(chkAutoClose);
            panelButtons.Controls.Add(chkPlaySound);
            panelButtons.Controls.Add(chkShowDetails);

            SetupButtonHoverEffects();
        }

        private TabPage CreateTab(string title)
        {
            TabPage tab = new TabPage(title);
            tab.BackColor = ThemeManager.GetBackgroundPanel();
            tab.Padding = new Padding(10);
            return tab;
        }

        private void CreateOptionsTab(TabPage tab)
        {
            int yPos = 20;

            // Временные файлы Windows
            GroupBox grpTemp = CreateGroupBox("Временные файлы Windows", yPos);
            cleanupOptions["WinTemp"] = CreateOptionCheckBox("Windows Temp", 20, 25, false);
            cleanupOptions["Prefetch"] = CreateOptionCheckBox("Prefetch", 20, 50, false);
            cleanupOptions["RecycleBin"] = CreateOptionCheckBox("Корзина", 20, 75, false);
            cleanupOptions["RecentItems"] = CreateOptionCheckBox("Недавние элементы", 300, 25, false);
            cleanupOptions["TempSetup"] = CreateOptionCheckBox("Временные установки", 300, 50, false);
            grpTemp.Controls.Add(cleanupOptions["WinTemp"]);
            grpTemp.Controls.Add(cleanupOptions["Prefetch"]);
            grpTemp.Controls.Add(cleanupOptions["RecycleBin"]);
            grpTemp.Controls.Add(cleanupOptions["RecentItems"]);
            grpTemp.Controls.Add(cleanupOptions["TempSetup"]);
            tab.Controls.Add(grpTemp);
            yPos += 120;

            // Браузеры
            GroupBox grpBrowsers = CreateGroupBox("Браузеры", yPos);
            cleanupOptions["Opera"] = CreateOptionCheckBox("Opera / Opera GX", 20, 25, false);
            cleanupOptions["Chrome"] = CreateOptionCheckBox("Google Chrome", 20, 50, false);
            cleanupOptions["Edge"] = CreateOptionCheckBox("Microsoft Edge", 20, 75, false);
            cleanupOptions["Firefox"] = CreateOptionCheckBox("Mozilla Firefox", 300, 25, false);
            cleanupOptions["Brave"] = CreateOptionCheckBox("Brave Browser", 300, 50, false);
            cleanupOptions["Yandex"] = CreateOptionCheckBox("Яндекс Браузер", 300, 75, false);
            cleanupOptions["Vivaldi"] = CreateOptionCheckBox("Vivaldi", 580, 25, false);
            cleanupOptions["Tor"] = CreateOptionCheckBox("Tor Browser", 580, 50, false);
            grpBrowsers.Controls.Add(cleanupOptions["Opera"]);
            grpBrowsers.Controls.Add(cleanupOptions["Chrome"]);
            grpBrowsers.Controls.Add(cleanupOptions["Edge"]);
            grpBrowsers.Controls.Add(cleanupOptions["Firefox"]);
            grpBrowsers.Controls.Add(cleanupOptions["Brave"]);
            grpBrowsers.Controls.Add(cleanupOptions["Yandex"]);
            grpBrowsers.Controls.Add(cleanupOptions["Vivaldi"]);
            grpBrowsers.Controls.Add(cleanupOptions["Tor"]);
            tab.Controls.Add(grpBrowsers);
            yPos += 120;

            // Мессенджеры и приложения
            GroupBox grpMessengers = CreateGroupBox("Мессенджеры и приложения", yPos);
            cleanupOptions["Telegram"] = CreateOptionCheckBox("Telegram", 20, 25, false);
            cleanupOptions["Discord"] = CreateOptionCheckBox("Discord", 20, 50, false);
            cleanupOptions["Viber"] = CreateOptionCheckBox("Viber", 20, 75, false);
            cleanupOptions["Zoom"] = CreateOptionCheckBox("Zoom", 300, 25, false);
            cleanupOptions["Spotify"] = CreateOptionCheckBox("Spotify", 300, 50, false);
            cleanupOptions["VSCode"] = CreateOptionCheckBox("VS Code", 300, 75, false);
            cleanupOptions["Teams"] = CreateOptionCheckBox("Microsoft Teams", 580, 25, false);
            cleanupOptions["Skype"] = CreateOptionCheckBox("Skype", 580, 50, false);
            cleanupOptions["Slack"] = CreateOptionCheckBox("Slack", 580, 75, false);
            grpMessengers.Controls.Add(cleanupOptions["Telegram"]);
            grpMessengers.Controls.Add(cleanupOptions["Discord"]);
            grpMessengers.Controls.Add(cleanupOptions["Viber"]);
            grpMessengers.Controls.Add(cleanupOptions["Zoom"]);
            grpMessengers.Controls.Add(cleanupOptions["Spotify"]);
            grpMessengers.Controls.Add(cleanupOptions["VSCode"]);
            grpMessengers.Controls.Add(cleanupOptions["Teams"]);
            grpMessengers.Controls.Add(cleanupOptions["Skype"]);
            grpMessengers.Controls.Add(cleanupOptions["Slack"]);
            tab.Controls.Add(grpMessengers);
            yPos += 120;

            // Расширенные системные утилиты
            GroupBox grpSystem = CreateGroupBox("Системные утилиты и кэш", yPos);
            grpSystem.Height = 135;
            cleanupOptions["DNS"] = CreateOptionCheckBox("Очистка DNS кэша", 20, 25, false);
            cleanupOptions["DISM"] = CreateOptionCheckBox("DISM очистка (медленно)", 20, 50, false);
            cleanupOptions["ThumbnailCache"] = CreateOptionCheckBox("Кэш миниатюр", 20, 75, false);
            cleanupOptions["IconCache"] = CreateOptionCheckBox("Кэш иконок", 20, 100, false);
            cleanupOptions["WindowsUpdate"] = CreateOptionCheckBox("Кэш Windows Update", 300, 25, false);
            cleanupOptions["EventLogs"] = CreateOptionCheckBox("Логи событий Windows", 300, 50, false);
            cleanupOptions["DeliveryOptimization"] = CreateOptionCheckBox("Оптимизация доставки", 300, 75, false);
            cleanupOptions["SoftwareDistribution"] = CreateOptionCheckBox("SoftwareDistribution", 300, 100, false);
            cleanupOptions["MemoryDumps"] = CreateOptionCheckBox("Дампы памяти", 580, 25, false);
            cleanupOptions["ErrorReports"] = CreateOptionCheckBox("Отчёты об ошибках", 580, 50, false);
            cleanupOptions["TempInternet"] = CreateOptionCheckBox("Временные файлы интернета", 580, 75, false);
            cleanupOptions["FontCache"] = CreateOptionCheckBox("Кэш шрифтов", 580, 100, false);
            grpSystem.Controls.Add(cleanupOptions["DNS"]);
            grpSystem.Controls.Add(cleanupOptions["DISM"]);
            grpSystem.Controls.Add(cleanupOptions["ThumbnailCache"]);
            grpSystem.Controls.Add(cleanupOptions["IconCache"]);
            grpSystem.Controls.Add(cleanupOptions["WindowsUpdate"]);
            grpSystem.Controls.Add(cleanupOptions["EventLogs"]);
            grpSystem.Controls.Add(cleanupOptions["DeliveryOptimization"]);
            grpSystem.Controls.Add(cleanupOptions["SoftwareDistribution"]);
            grpSystem.Controls.Add(cleanupOptions["MemoryDumps"]);
            grpSystem.Controls.Add(cleanupOptions["ErrorReports"]);
            grpSystem.Controls.Add(cleanupOptions["TempInternet"]);
            grpSystem.Controls.Add(cleanupOptions["FontCache"]);
            tab.Controls.Add(grpSystem);
            yPos += 145;

            // Дополнительные опции
            GroupBox grpAdditional = CreateGroupBox("Дополнительно", yPos);
            cleanupOptions["LogFiles"] = CreateOptionCheckBox("Системные логи (.log)", 20, 25, false);
            cleanupOptions["OldDrivers"] = CreateOptionCheckBox("Старые драйверы", 20, 50, false);
            cleanupOptions["WinSxS"] = CreateOptionCheckBox("WinSxS очистка (осторожно!)", 20, 75, false);
            cleanupOptions["RestorePoints"] = CreateOptionCheckBox("Старые точки восстановления", 300, 25, false);
            cleanupOptions["TempUser"] = CreateOptionCheckBox("Временные профили пользователей", 300, 50, false);
            cleanupOptions["DiagnosticData"] = CreateOptionCheckBox("Диагностические данные", 300, 75, false);
            grpAdditional.Controls.Add(cleanupOptions["LogFiles"]);
            grpAdditional.Controls.Add(cleanupOptions["OldDrivers"]);
            grpAdditional.Controls.Add(cleanupOptions["WinSxS"]);
            grpAdditional.Controls.Add(cleanupOptions["RestorePoints"]);
            grpAdditional.Controls.Add(cleanupOptions["TempUser"]);
            grpAdditional.Controls.Add(cleanupOptions["DiagnosticData"]);
            tab.Controls.Add(grpAdditional);

            // Кнопки управления
            Button btnSelectAll = CreateButton("Выбрать всё", 760, 20, 150, 30, ThemeManager.GetButtonPrimary());
            Button btnDeselectAll = CreateButton("Снять всё", 760, 60, 150, 30, ThemeManager.GetButtonDanger());
            Button btnRecommended = CreateButton("Рекомендуемые", 760, 100, 150, 30, ThemeManager.GetButtonSuccess());
            Button btnSafety = CreateButton("Безопасные", 760, 140, 150, 30, ThemeManager.GetButtonSecondary());

            btnSelectAll.Click += delegate
            {
                foreach (var cb in cleanupOptions.Values)
                    cb.Checked = true;
            };
            btnDeselectAll.Click += delegate
            {
                foreach (var cb in cleanupOptions.Values)
                    cb.Checked = false;
            };
            btnRecommended.Click += delegate
            {
                foreach (var cb in cleanupOptions.Values)
                    cb.Checked = false;
                cleanupOptions["WinTemp"].Checked = true;
                cleanupOptions["Prefetch"].Checked = true;
                cleanupOptions["Chrome"].Checked = true;
                cleanupOptions["Edge"].Checked = true;
                cleanupOptions["DNS"].Checked = true;
                cleanupOptions["ThumbnailCache"].Checked = true;
                cleanupOptions["EventLogs"].Checked = true;
                cleanupOptions["ErrorReports"].Checked = true;
            };
            btnSafety.Click += delegate
            {
                foreach (var cb in cleanupOptions.Values)
                    cb.Checked = false;
                cleanupOptions["WinTemp"].Checked = true;
                cleanupOptions["RecycleBin"].Checked = true;
                cleanupOptions["RecentItems"].Checked = true;
                cleanupOptions["ThumbnailCache"].Checked = true;
                cleanupOptions["TempInternet"].Checked = true;
            };

            tab.Controls.Add(btnSelectAll);
            tab.Controls.Add(btnDeselectAll);
            tab.Controls.Add(btnRecommended);
            tab.Controls.Add(btnSafety);
        }

        private void CreateProgressTab(TabPage tab)
        {
            int yPos = 20;

            Label lblInfo = new Label();
            lblInfo.Text = "Отслеживание прогресса в реальном времени";
            lblInfo.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblInfo.ForeColor = ThemeManager.GetTextAccent();
            lblInfo.Left = 20;
            lblInfo.Top = yPos;
            lblInfo.AutoSize = true;
            tab.Controls.Add(lblInfo);
            yPos += 40;

            string[] categories = new string[] {
                "Временные файлы Windows", "Браузеры", "Мессенджеры", "Системные утилиты"
            };

            foreach (var category in categories)
            {
                Label lbl = new Label();
                lbl.Text = category;
                lbl.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                lbl.ForeColor = ThemeManager.GetTextInfo();
                lbl.Left = 20;
                lbl.Top = yPos;
                lbl.Width = 200;
                tab.Controls.Add(lbl);

                ProgressBar pb = new ProgressBar();
                pb.Left = 230;
                pb.Top = yPos;
                pb.Width = 650;
                pb.Height = 25;
                pb.Style = ProgressBarStyle.Continuous;
                progressBars[category] = pb;
                tab.Controls.Add(pb);

                Label lblPercent = new Label();
                lblPercent.Text = "0%";
                lblPercent.Left = 890;
                lblPercent.Top = yPos;
                lblPercent.Width = 50;
                lblPercent.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                lblPercent.ForeColor = Color.White;
                lblPercent.TextAlign = ContentAlignment.MiddleRight;
                tab.Controls.Add(lblPercent);
                pb.Tag = lblPercent;

                yPos += 45;
            }

            yPos += 20;
            Panel statsPanel = new Panel();
            statsPanel.Left = 20;
            statsPanel.Top = yPos;
            statsPanel.Width = 920;
            statsPanel.Height = 200;
            statsPanel.BackColor = ThemeManager.GetBackgroundLight();
            statsPanel.BorderStyle = BorderStyle.FixedSingle;
            tab.Controls.Add(statsPanel);

            Label lblStatsTitle = new Label();
            lblStatsTitle.Text = "Статистика сессии";
            lblStatsTitle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblStatsTitle.ForeColor = ThemeManager.GetTextAccent();
            lblStatsTitle.Left = 20;
            lblStatsTitle.Top = 15;
            lblStatsTitle.AutoSize = true;
            statsPanel.Controls.Add(lblStatsTitle);

            TextBox txtStats = new TextBox();
            txtStats.Multiline = true;
            txtStats.ReadOnly = true;
            txtStats.BackColor = ThemeManager.GetBackgroundLight();
            txtStats.ForeColor = ThemeManager.GetTextPrimary();
            txtStats.BorderStyle = BorderStyle.None;
            txtStats.Font = new Font("Consolas", 10);
            txtStats.Left = 20;
            txtStats.Top = 50;
            txtStats.Width = 880;
            txtStats.Height = 130;
            txtStats.Text = "Ожидание начала очистки...\n\nСтатистика будет отображаться здесь во время очистки:\n- Освобождённое место в реальном времени\n- Удалённые файлы и папки\n- Затраченное время";
            statsPanel.Controls.Add(txtStats);
        }

        private void CreateHistoryTab(TabPage tab)
        {
            Label lblTitle = new Label();
            lblTitle.Text = "История очистки";
            lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTitle.ForeColor = ThemeManager.GetTextAccent();
            lblTitle.Left = 20;
            lblTitle.Top = 10;
            lblTitle.AutoSize = true;
            tab.Controls.Add(lblTitle);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Отслеживайте все ваши сессии очистки";
            lblSubtitle.Font = new Font("Segoe UI", 9);
            lblSubtitle.ForeColor = ThemeManager.GetTextSecondary();
            lblSubtitle.Left = 20;
            lblSubtitle.Top = 38;
            lblSubtitle.AutoSize = true;
            tab.Controls.Add(lblSubtitle);

            listViewHistory = new ListView();
            listViewHistory.Left = 20;
            listViewHistory.Top = 70;
            listViewHistory.Width = 920;
            listViewHistory.Height = 460;
            listViewHistory.View = View.Details;
            listViewHistory.FullRowSelect = true;
            listViewHistory.GridLines = true;
            listViewHistory.BackColor = ThemeManager.GetBackgroundLight();
            listViewHistory.ForeColor = ThemeManager.GetTextPrimary();
            listViewHistory.Font = new Font("Segoe UI", 9);

            listViewHistory.Columns.Add("Дата и время", 180);
            listViewHistory.Columns.Add("Освобождено", 120);
            listViewHistory.Columns.Add("Файлы", 80);
            listViewHistory.Columns.Add("Папки", 80);
            listViewHistory.Columns.Add("Время", 100);
            listViewHistory.Columns.Add("Статус", 140);

            tab.Controls.Add(listViewHistory);

            Button btnClearHistory = CreateButton("Очистить историю", 20, 540, 150, 30, ThemeManager.GetButtonDanger());
            Button btnExportHistory = CreateButton("Экспорт истории", 180, 540, 150, 30, ThemeManager.GetButtonSecondary());

            btnClearHistory.Click += delegate { listViewHistory.Items.Clear(); };
            btnExportHistory.Click += BtnExportHistory_Click;

            tab.Controls.Add(btnClearHistory);
            tab.Controls.Add(btnExportHistory);
        }

        private void CreateLogsTab(TabPage tab)
        {
            Label lblTitle = new Label();
            lblTitle.Text = "Подробные логи";
            lblTitle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblTitle.ForeColor = ThemeManager.GetTextAccent();
            lblTitle.Left = 20;
            lblTitle.Top = 10;
            lblTitle.AutoSize = true;
            tab.Controls.Add(lblTitle);

            Panel logPanel = new Panel();
            logPanel.Left = 20;
            logPanel.Top = 45;
            logPanel.Width = 920;
            logPanel.Height = 525;
            logPanel.BorderStyle = BorderStyle.FixedSingle;
            tab.Controls.Add(logPanel);

            txtLog = new TextBox();
            txtLog.Multiline = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Dock = DockStyle.Fill;
            txtLog.BackColor = ThemeManager.GetBackgroundDark();
            txtLog.ForeColor = ThemeManager.GetTextSuccess();
            txtLog.Font = new Font("Consolas", 9);
            txtLog.ReadOnly = true;
            txtLog.BorderStyle = BorderStyle.None;
            logPanel.Controls.Add(txtLog);
        }

        private GroupBox CreateGroupBox(string title, int top)
        {
            GroupBox grp = new GroupBox();
            grp.Text = title;
            grp.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            grp.ForeColor = ThemeManager.GetTextInfo();
            grp.Left = 20;
            grp.Top = top;
            grp.Width = 920;
            grp.Height = 110;
            return grp;
        }

        private CheckBox CreateOptionCheckBox(string text, int left, int top, bool isChecked)
        {
            CheckBox cb = new CheckBox();
            cb.Text = text;
            cb.Left = left;
            cb.Top = top;
            cb.AutoSize = true;
            cb.Checked = isChecked;
            cb.Font = new Font("Segoe UI", 9);
            cb.ForeColor = ThemeManager.GetTextPrimary();
            return cb;
        }

        private CheckBox CreateCheckBox(string text, int left, int top)
        {
            CheckBox cb = new CheckBox();
            cb.Text = text;
            cb.Left = left;
            cb.Top = top;
            cb.AutoSize = true;
            cb.Checked = false;
            cb.Font = new Font("Segoe UI", 9);
            cb.ForeColor = ThemeManager.GetTextPrimary();
            return cb;
        }

        private Button CreateButton(string text, int left, int top, int width, int height, Color color)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Left = left;
            btn.Top = top;
            btn.Width = width;
            btn.Height = height;
            btn.BackColor = color;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SetupButtonHoverEffects()
        {
            foreach (Control ctrl in this.Controls)
            {
                SetupHoverRecursive(ctrl);
            }
        }

        private void SetupHoverRecursive(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl is Button)
                {
                    Button btn = (Button)ctrl;
                    Color originalColor = btn.BackColor;

                    btn.MouseEnter += delegate
                    {
                        if (btn.Enabled)
                        {
                            int amount = ThemeManager.CurrentTheme == ThemeType.Dark ? 30 : -20;
                            btn.BackColor = LightenColor(btn.BackColor, amount);
                        }
                    };
                    btn.MouseLeave += delegate
                    {
                        if (btn.Enabled)
                        {
                            // Восстанавливаем цвет в зависимости от типа кнопки
                            string btnText = btn.Text.ToLower();
                            if (btnText.Contains("начать") || btnText.Contains("старт") || btnText.Contains("выбрать всё"))
                            {
                                btn.BackColor = ThemeManager.GetButtonPrimary();
                            }
                            else if (btnText.Contains("стоп") || btnText.Contains("очистить") || btnText.Contains("снять"))
                            {
                                btn.BackColor = ThemeManager.GetButtonDanger();
                            }
                            else if (btnText.Contains("открыть") || btnText.Contains("рекомендуемые"))
                            {
                                btn.BackColor = ThemeManager.GetButtonSuccess();
                            }
                            else if (btnText.Contains("обновить") || btnText.Contains("экспорт") || btnText.Contains("тема"))
                            {
                                btn.BackColor = ThemeManager.GetButtonSecondary();
                            }
                            else if (btnText.Contains("безопасные"))
                            {
                                btn.BackColor = ThemeManager.GetButtonWarning();
                            }
                            else
                            {
                                btn.BackColor = ThemeManager.GetButtonNeutral();
                            }
                        }
                    };
                }
                SetupHoverRecursive(ctrl);
            }
        }

        private Color LightenColor(Color color, int amount)
        {
            int r = Math.Clamp(color.R + amount, 0, 255);
            int g = Math.Clamp(color.G + amount, 0, 255);
            int b = Math.Clamp(color.B + amount, 0, 255);

            return Color.FromArgb(color.A, r, g, b);
        }


        private void SetupCallbacks()
        {
            Cleanup.OnLog = Log;
            Cleanup.OnStatsUpdate = UpdateStats;
        }

        private void SetupTimer()
        {
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 100;
            updateTimer.Tick += delegate { UpdateStatsDisplay(); };
        }

        private void LoadHistory()
        {
            string[] item = new string[] {
                DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd HH:mm"),
                "230 МБ", "1234", "87", "12.3с", "Успешно"
            };
            listViewHistory.Items.Add(new ListViewItem(item));
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            lblStats.Text = string.Format("{0} | {1} файлов | {2} папок",
                Cleanup.GetFormattedFreedSpace(),
                Cleanup.GetTotalFiles(),
                Cleanup.GetTotalFolders());

            MessageBox.Show("Статистика обновлена!", "Информация",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnTheme_Click(object sender, EventArgs e)
        {
            // Переключаем тему
            if (ThemeManager.CurrentTheme == ThemeType.Dark)
            {
                ThemeManager.CurrentTheme = ThemeType.Light;
                btnTheme.Text = "🌙 Тема";
            }
            else
            {
                ThemeManager.CurrentTheme = ThemeType.Dark;
                btnTheme.Text = "☀ Тема";
            }

            // Применяем новую тему
            ThemeManager.ApplyTheme(this);

            // Обновляем цвет статуса
            if (lblStatus.Text.Contains("Готов"))
            {
                lblStatus.ForeColor = ThemeManager.GetTextSuccess();
            }
            else if (lblStatus.Text.Contains("Выполняется"))
            {
                lblStatus.ForeColor = ThemeManager.GetTextWarning();
            }
            else if (lblStatus.Text.Contains("завершена"))
            {
                lblStatus.ForeColor = ThemeManager.GetTextSuccess();
            }
            else if (lblStatus.Text.Contains("Отменено"))
            {
                lblStatus.ForeColor = Color.Orange;
            }
            else if (lblStatus.Text.Contains("ошибка"))
            {
                lblStatus.ForeColor = ThemeManager.GetTextError();
            }

            this.Refresh();
        }

        private void BtnExportHistory_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*";
            sfd.FileName = string.Format("История_Очистки_{0:yyyyMMdd}.csv", DateTime.Now);

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.AppendLine("Дата и время,Освобождено,Файлы,Папки,Время,Статус");

                    foreach (ListViewItem item in listViewHistory.Items)
                    {
                        string line = string.Format("{0},{1},{2},{3},{4},{5}",
                            item.SubItems[0].Text,
                            item.SubItems[1].Text,
                            item.SubItems[2].Text,
                            item.SubItems[3].Text,
                            item.SubItems[4].Text,
                            item.SubItems[5].Text);
                        sb.AppendLine(line);
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString());
                    MessageBox.Show(string.Format("История экспортирована в:\n{0}", sfd.FileName),
                        "Экспорт завершён", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Ошибка экспорта: {0}", ex.Message),
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            bool anySelected = false;
            foreach (var cb in cleanupOptions.Values)
            {
                if (cb.Checked)
                {
                    anySelected = true;
                    break;
                }
            }

            if (!anySelected)
            {
                MessageBox.Show("Пожалуйста, выберите хотя бы один пункт для очистки!",
                    "Ничего не выбрано", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Предупреждение для опасных опций
            if (cleanupOptions["WinSxS"].Checked || cleanupOptions["OldDrivers"].Checked)
            {
                var result = MessageBox.Show(
                    "Вы выбрали потенциально опасные опции очистки!\n\n" +
                    "WinSxS и старые драйверы могут повлиять на стабильность системы.\n" +
                    "Продолжить?",
                    "Внимание!",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                    return;
            }

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            btnOpenTemp.Enabled = false;
            btnRefresh.Enabled = false;
            txtLog.Clear();
            Cleanup.ResetStats();

            foreach (var pb in progressBars.Values)
                pb.Value = 0;
            pbMain.Value = 0;

            lblStatus.Text = "Выполняется очистка...";
            lblStatus.ForeColor = ThemeManager.GetTextWarning();

            stopwatch = Stopwatch.StartNew();
            updateTimer.Start();
            cleanupCount++;

            cts = new CancellationTokenSource();
            Task.Run(() => RunCleanup(cts.Token));
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            if (cts != null)
                cts.Cancel();
            Log("\nОчистка отменена пользователем!");
            lblStatus.Text = "Отменено";
            lblStatus.ForeColor = Color.Orange;
            FinishCleanup();
        }

        private void Log(string message)
        {
            if (txtLog.InvokeRequired)
                txtLog.Invoke(new Action<string>(Log), message);
            else
            {
                txtLog.AppendText(message + Environment.NewLine);
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
        }

        private void UpdateStats()
        {
            if (this.InvokeRequired)
                this.Invoke(new Action(UpdateStats));
            else
                UpdateStatsDisplay();
        }

        private void UpdateStatsDisplay()
        {
            double seconds = stopwatch != null && stopwatch.IsRunning ? stopwatch.Elapsed.TotalSeconds : 0;
            lblStats.Text = string.Format("{0} | {1} файлов | {2} папок | {3:F1}с",
                Cleanup.GetFormattedFreedSpace(),
                Cleanup.GetTotalFiles(),
                Cleanup.GetTotalFolders(),
                seconds);
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
            sfd.FileName = string.Format("Лог_Очистки_{0:yyyyMMdd_HHmmss}.txt", DateTime.Now);

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, txtLog.Text);
                    MessageBox.Show(string.Format("Лог экспортирован в:\n{0}", sfd.FileName),
                        "Экспорт завершён", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Ошибка экспорта: {0}", ex.Message),
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void RunCleanup(CancellationToken token)
        {
            try
            {
                Log("=== Сессия очистки начата ===");
                Log("Дата: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                Log("");

                var selectedOptions = cleanupOptions.Where(kvp => kvp.Value.Checked).Select(kvp => kvp.Key).ToList();
                int totalSteps = selectedOptions.Count;
                int currentStep = 0;

                foreach (var option in selectedOptions)
                {
                    if (token.IsCancellationRequested)
                        break;

                    currentStep++;
                    int progress = (int)((currentStep / (double)totalSteps) * 100);
                    UpdateProgressBarMain(progress);

                    Log($"\n[{currentStep}/{totalSteps}] Обработка: {option}");

                    string category = GetCategory(option);
                    for (int i = 0; i <= 100; i += 10)
                    {
                        if (token.IsCancellationRequested)
                            break;

                        UpdateProgressBar(category, i);
                        await Task.Delay(100, token);
                    }

                    await Cleanup.CleanupByOption(option, chkAutoClose.Checked, token);
                }

                if (!token.IsCancellationRequested)
                {
                    Log("\n=== Очистка успешно завершена ===");
                    UpdateProgressBarMain(100);

                    this.Invoke(new Action(() =>
                    {
                        lblStatus.Text = "Очистка завершена!";
                        lblStatus.ForeColor = ThemeManager.GetTextSuccess();

                        AddToHistory("Успешно");

                        if (chkPlaySound.Checked)
                            System.Media.SystemSounds.Asterisk.Play();
                    }));
                }
            }
            catch (OperationCanceledException)
            {
                Log("\nОперация очистки была отменена.");
            }
            catch (Exception ex)
            {
                Log($"\nОШИБКА: {ex.Message}");
                this.Invoke(new Action(() =>
                {
                    lblStatus.Text = "Произошла ошибка";
                    lblStatus.ForeColor = ThemeManager.GetTextError();
                    AddToHistory("Ошибка");
                }));
            }
            finally
            {
                FinishCleanup();
            }
        }

        private string GetCategory(string option)
        {
            if (option == "WinTemp" || option == "Prefetch" || option == "RecycleBin" ||
                option == "RecentItems" || option == "TempSetup")
                return "Временные файлы Windows";

            if (option == "Opera" || option == "Chrome" || option == "Edge" ||
                option == "Firefox" || option == "Brave" || option == "Yandex" ||
                option == "Vivaldi" || option == "Tor")
                return "Браузеры";

            if (option == "Telegram" || option == "Discord" || option == "Viber" ||
                option == "Zoom" || option == "Spotify" || option == "VSCode" ||
                option == "Teams" || option == "Skype" || option == "Slack")
                return "Мессенджеры";

            return "Системные утилиты";
        }

        private void AddToHistory(string status)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(AddToHistory), status);
            }
            else
            {
                string[] item = new string[] {
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Cleanup.GetFormattedFreedSpace(),
                    Cleanup.GetTotalFiles().ToString(),
                    Cleanup.GetTotalFolders().ToString(),
                    string.Format("{0:F1}с", stopwatch.Elapsed.TotalSeconds),
                    status
                };
                listViewHistory.Items.Insert(0, new ListViewItem(item));
            }
        }

        private void UpdateProgressBar(string category, int value)
        {
            if (progressBars.ContainsKey(category))
            {
                ProgressBar pb = progressBars[category];
                if (pb.InvokeRequired)
                {
                    pb.Invoke(new Action<string, int>(UpdateProgressBar), category, value);
                }
                else
                {
                    pb.Value = Math.Min(value, 100);
                    if (pb.Tag is Label)
                    {
                        Label lbl = (Label)pb.Tag;
                        lbl.Text = value.ToString() + "%";
                    }
                }
            }
        }

        private void UpdateProgressBarMain(int value)
        {
            if (pbMain.InvokeRequired)
                pbMain.Invoke(new Action<int>(UpdateProgressBarMain), value);
            else
                pbMain.Value = Math.Min(value, 100);
        }

        private void FinishCleanup()
        {
            if (stopwatch != null)
                stopwatch.Stop();
            updateTimer.Stop();

            if (this.InvokeRequired)
            {
                this.Invoke(new Action(FinishCleanup));
            }
            else
            {
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                btnOpenTemp.Enabled = true;
                btnRefresh.Enabled = true;

                if (cts != null)
                {
                    cts.Dispose();
                    cts = null;
                }
            }
        }

        private void InitializeComponent()
        {

        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                var result = MessageBox.Show(
                    "Очистка выполняется. Вы уверены, что хотите выйти?",
                    "Подтверждение выхода",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                cts.Cancel();
            }

            if (updateTimer != null)
                updateTimer.Stop();

            base.OnFormClosing(e);
        }
    }
}