using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CleanupTemp_Pro;

namespace CleanupTempPro
{
    public class CleanupForm : Form
    {
        private Button btnStart, btnStop, btnExit, btnOpenTemp, btnExport, btnRefresh, btnSettings;
        private TextBox txtLog;
        private Label lblStats, lblStatus, lblTitle, lblVersion;
        private ProgressBar pbMain;
        private TabControl tabControl;
        private CheckBox chkAutoClose, chkPlaySound, chkShowDetails;
        private CancellationTokenSource cts;
        private Stopwatch stopwatch;
        private System.Windows.Forms.Timer updateTimer;
        private ToolTip toolTip;

        private Dictionary<string, ProgressBar> progressBars;
        private Dictionary<string, CheckBox> cleanupOptions;
        private ListView listViewHistory;

        public CleanupForm()
        {
            // Загружаем сохраненный язык перед инициализацией
            LanguageManager.LoadLanguage();
            
            this.Text = LanguageManager.Get("Title");
            this.Size = new Size(1200, 900); // Увеличено с 850 до 900 для большей высоты
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
            InitializeToolTips();

            // Применяем тему
            ThemeManager.ApplyTheme(this);
        }

        private void InitializeToolTips()
        {
            // Создаём ToolTip
            toolTip = new ToolTip
            {
                AutoPopDelay = 8000,
                InitialDelay = 400,
                ReshowDelay = 150,
                ShowAlways = true,
                IsBalloon = false
            };

            // ════════════════════════════════════════════════════════════════
            // КНОПКИ УПРАВЛЕНИЯ
            // ════════════════════════════════════════════════════════════════

            toolTip.SetToolTip(btnSettings,
                LanguageManager.Get("Tooltip_Settings"));

            toolTip.SetToolTip(btnStart,
                LanguageManager.Get("Tooltip_Start"));

            toolTip.SetToolTip(btnStop,
                LanguageManager.Get("Tooltip_Stop"));

            toolTip.SetToolTip(btnOpenTemp,
                LanguageManager.Get("Tooltip_OpenTemp"));

            toolTip.SetToolTip(btnRefresh,
                LanguageManager.Get("Tooltip_Refresh"));

            toolTip.SetToolTip(btnExport,
                LanguageManager.Get("Tooltip_Export"));

            toolTip.SetToolTip(btnExit,
                LanguageManager.Get("Tooltip_Exit"));

            // ════════════════════════════════════════════════════════════════
            // ЧЕКБОКСЫ НАСТРОЕК
            // ════════════════════════════════════════════════════════════════

            chkAutoClose.Text = LanguageManager.Get("CheckBox_AutoClose");
            chkPlaySound.Text = LanguageManager.Get("CheckBox_PlaySound");
            chkShowDetails.Text = LanguageManager.Get("CheckBox_ShowDetails");

            toolTip.SetToolTip(chkAutoClose,
                LanguageManager.Get("Tooltip_AutoClose"));
            toolTip.SetToolTip(chkPlaySound,
                LanguageManager.Get("Tooltip_PlaySound"));
            toolTip.SetToolTip(chkShowDetails,
                LanguageManager.Get("Tooltip_ShowDetails"));


            // ════════════════════════════════════════════════════════════════
            // ЧЕКБОКСЫ ОЧИСТКИ - ВРЕМЕННЫЕ ФАЙЛЫ WINDOWS
            // ════════════════════════════════════════════════════════════════

            toolTip.SetToolTip(cleanupOptions["WinTemp"],
                LanguageManager.Get("Tooltip_WinTemp"));

            toolTip.SetToolTip(cleanupOptions["Prefetch"],
                LanguageManager.Get("Tooltip_Prefetch"));

            toolTip.SetToolTip(cleanupOptions["RecycleBin"],
                LanguageManager.Get("Tooltip_RecycleBin"));

            toolTip.SetToolTip(cleanupOptions["RecentItems"],
                LanguageManager.Get("Tooltip_RecentItems"));

            toolTip.SetToolTip(cleanupOptions["TempSetup"],
                LanguageManager.Get("Tooltip_TempSetup"));

            // ════════════════════════════════════════════════════════════════
            // ЧЕКБОКСЫ ОЧИСТКИ - БРАУЗЕРЫ
            // ════════════════════════════════════════════════════════════════

            toolTip.SetToolTip(cleanupOptions["Opera"],
                LanguageManager.Get("Tooltip_Opera"));

            toolTip.SetToolTip(cleanupOptions["Chrome"],
                LanguageManager.Get("Tooltip_Chrome"));

            toolTip.SetToolTip(cleanupOptions["Edge"],
                LanguageManager.Get("Tooltip_Edge"));

            toolTip.SetToolTip(cleanupOptions["Firefox"],
                LanguageManager.Get("Tooltip_Firefox"));

            toolTip.SetToolTip(cleanupOptions["Brave"],
                LanguageManager.Get("Tooltip_Brave"));

            toolTip.SetToolTip(cleanupOptions["Yandex"],
                LanguageManager.Get("Tooltip_Yandex"));

            toolTip.SetToolTip(cleanupOptions["Vivaldi"],
                LanguageManager.Get("Tooltip_Vivaldi"));

            toolTip.SetToolTip(cleanupOptions["Tor"],
                LanguageManager.Get("Tooltip_Tor"));

            // ════════════════════════════════════════════════════════════════
            // ЧЕКБОКСЫ ОЧИСТКИ - МЕССЕНДЖЕРЫ
            // ════════════════════════════════════════════════════════════════

            toolTip.SetToolTip(cleanupOptions["Telegram"],
                LanguageManager.Get("Tooltip_Telegram"));

            toolTip.SetToolTip(cleanupOptions["Discord"],
                LanguageManager.Get("Tooltip_Discord"));

            toolTip.SetToolTip(cleanupOptions["Viber"],
                LanguageManager.Get("Tooltip_Viber"));

            toolTip.SetToolTip(cleanupOptions["Zoom"],
                LanguageManager.Get("Tooltip_Zoom"));

            toolTip.SetToolTip(cleanupOptions["Spotify"],
                LanguageManager.Get("Tooltip_Spotify"));

            toolTip.SetToolTip(cleanupOptions["VSCode"],
                LanguageManager.Get("Tooltip_VSCode"));

            toolTip.SetToolTip(cleanupOptions["Teams"],
                LanguageManager.Get("Tooltip_Teams"));

            toolTip.SetToolTip(cleanupOptions["Skype"],
                LanguageManager.Get("Tooltip_Skype"));

            toolTip.SetToolTip(cleanupOptions["Slack"],
                LanguageManager.Get("Tooltip_Slack"));

            // ════════════════════════════════════════════════════════════════
            // ЧЕКБОКСЫ ОЧИСТКИ - СИСТЕМНЫЕ УТИЛИТЫ
            // ════════════════════════════════════════════════════════════════

            toolTip.SetToolTip(cleanupOptions["DNS"],
                LanguageManager.Get("Tooltip_DNS"));

            toolTip.SetToolTip(cleanupOptions["DISM"],
                LanguageManager.Get("Tooltip_DISM"));

            toolTip.SetToolTip(cleanupOptions["ThumbnailCache"],
                LanguageManager.Get("Tooltip_ThumbnailCache"));

            toolTip.SetToolTip(cleanupOptions["IconCache"],
                LanguageManager.Get("Tooltip_IconCache"));

            toolTip.SetToolTip(cleanupOptions["WindowsUpdate"],
                LanguageManager.Get("Tooltip_WindowsUpdate"));

            toolTip.SetToolTip(cleanupOptions["EventLogs"],
                LanguageManager.Get("Tooltip_EventLogs"));

            toolTip.SetToolTip(cleanupOptions["DeliveryOptimization"],
                LanguageManager.Get("Tooltip_DeliveryOptimization"));

            toolTip.SetToolTip(cleanupOptions["SoftwareDistribution"],
                LanguageManager.Get("Tooltip_SoftwareDistribution"));

            toolTip.SetToolTip(cleanupOptions["MemoryDumps"],
                LanguageManager.Get("Tooltip_MemoryDumps"));

            toolTip.SetToolTip(cleanupOptions["ErrorReports"],
                LanguageManager.Get("Tooltip_ErrorReports"));

            toolTip.SetToolTip(cleanupOptions["TempInternet"],
                LanguageManager.Get("Tooltip_TempInternet"));

            toolTip.SetToolTip(cleanupOptions["FontCache"],
                LanguageManager.Get("Tooltip_FontCache"));

            // ════════════════════════════════════════════════════════════════
            // ЧЕКБОКСЫ ОЧИСТКИ - ДОПОЛНИТЕЛЬНО
            // ════════════════════════════════════════════════════════════════

            toolTip.SetToolTip(cleanupOptions["LogFiles"],
                LanguageManager.Get("Tooltip_LogFiles"));

            toolTip.SetToolTip(cleanupOptions["OldDrivers"],
                LanguageManager.Get("Tooltip_OldDrivers"));

            toolTip.SetToolTip(cleanupOptions["WinSxS"],
                LanguageManager.Get("Tooltip_WinSxS"));

            toolTip.SetToolTip(cleanupOptions["RegistryCleanup"],
                LanguageManager.Get("Tooltip_RegistryCleanup"));

            toolTip.SetToolTip(cleanupOptions["RestorePoints"],
                LanguageManager.Get("Tooltip_RestorePoints"));

            toolTip.SetToolTip(cleanupOptions["TempUser"],
                LanguageManager.Get("Tooltip_TempUser"));

            toolTip.SetToolTip(cleanupOptions["DiagnosticData"],
                LanguageManager.Get("Tooltip_DiagnosticData"));

        }

        private void InitializeComponents()
        {
            Panel panelTop = new Panel();
            panelTop.Dock = DockStyle.Top;
            panelTop.Height = 120;
            panelTop.BackColor = ThemeManager.GetBackgroundSecondary();
            this.Controls.Add(panelTop);

            lblTitle = new Label();
            lblTitle.Text = LanguageManager.Get("Title");
            lblTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblTitle.ForeColor = ThemeManager.GetTextAccent();
            lblTitle.Left = 20;
            lblTitle.Top = 10;
            lblTitle.AutoSize = true;
            panelTop.Controls.Add(lblTitle);

            lblVersion = new Label();
            lblVersion.Text = LanguageManager.Get("Version");
            lblVersion.Font = new Font("Segoe UI", 8);
            lblVersion.ForeColor = ThemeManager.GetTextSecondary();
            lblVersion.Left = 20;
            lblVersion.Top = 45;
            lblVersion.AutoSize = true;
            panelTop.Controls.Add(lblVersion);

            // Кнопка About (О программе)
            Button btnAbout = new Button();
            btnAbout.Text = LanguageManager.Get("Button_About");
            btnAbout.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnAbout.Left = 840;
            btnAbout.Top = 15;
            btnAbout.Width = 150;
            btnAbout.Height = 30;
            btnAbout.BackColor = ThemeManager.GetButtonSecondary();
            btnAbout.ForeColor = Color.White;
            btnAbout.FlatStyle = FlatStyle.Flat;
            btnAbout.Cursor = Cursors.Hand;
            btnAbout.FlatAppearance.BorderSize = 0;
            btnAbout.Tag = "secondary"; // Добавляем тег для определения типа кнопки
            btnAbout.TextAlign = ContentAlignment.MiddleCenter; // Центрируем текст
            btnAbout.UseVisualStyleBackColor = false;
            btnAbout.Click += BtnAbout_Click;
            panelTop.Controls.Add(btnAbout);

            // Кнопка Settings (в правом верхнем углу)
            btnSettings = new Button();
            btnSettings.Text = LanguageManager.Get("Button_Settings");
            btnSettings.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnSettings.Left = 1000;
            btnSettings.Top = 15;
            btnSettings.Width = 150;
            btnSettings.Height = 30;
            btnSettings.BackColor = ThemeManager.GetButtonSecondary();
            btnSettings.ForeColor = Color.White;
            btnSettings.FlatStyle = FlatStyle.Flat;
            btnSettings.Cursor = Cursors.Hand;
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.Tag = "secondary"; // Добавляем тег для определения типа кнопки
            btnSettings.TextAlign = ContentAlignment.MiddleCenter; // Центрируем текст
            btnSettings.UseVisualStyleBackColor = false;
            btnSettings.Click += BtnSettings_Click;
            panelTop.Controls.Add(btnSettings);

            lblStatus = new Label();
            lblStatus.Text = LanguageManager.Get("Status_Ready");
            lblStatus.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblStatus.ForeColor = ThemeManager.GetTextSuccess();
            lblStatus.Left = 20;
            lblStatus.Top = 65;
            lblStatus.Width = 500;
            panelTop.Controls.Add(lblStatus);

            lblStats = new Label();
            lblStats.Text = "0 МБ | 0 файлов | 0 папок | 0с";
            lblStats.Left = 430;
            lblStats.Top = 20;
            lblStats.Width = 280;
            lblStats.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblStats.ForeColor = ThemeManager.GetTextInfo();
            lblStats.TextAlign = ContentAlignment.MiddleRight;
            panelTop.Controls.Add(lblStats);

            Label lblProgress = new Label();
            lblProgress.Text = LanguageManager.Get("Progress_Label");
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

            TabPage tabOptions = CreateTab(LanguageManager.Get("Tab_Settings"));
            CreateOptionsTab(tabOptions);

            TabPage tabProgress = CreateTab(LanguageManager.Get("Tab_Progress"));
            CreateProgressTab(tabProgress);

            TabPage tabHistory = CreateTab(LanguageManager.Get("Tab_History"));
            CreateHistoryTab(tabHistory);

            TabPage tabLogs = CreateTab(LanguageManager.Get("Tab_Logs"));
            CreateLogsTab(tabLogs);

            tabControl.TabPages.Add(tabOptions);
            tabControl.TabPages.Add(tabProgress);
            tabControl.TabPages.Add(tabHistory);
            tabControl.TabPages.Add(tabLogs);

            Panel panelButtons = new Panel();
            panelButtons.Dock = DockStyle.Bottom;
            panelButtons.Height = 80; // Увеличена высота панели кнопок
            panelButtons.BackColor = ThemeManager.GetBackgroundSecondary();
            this.Controls.Add(panelButtons);

            // Первый ряд кнопок - центрируем их
            int buttonHeight = 35;
            int buttonWidth = 130; // Увеличиваем ширину для украинского текста
            int buttonY = 10;
            int totalButtonsWidth = buttonWidth * 6 + 10 * 5; // 6 кнопок + 5 промежутков по 10px
            int startX = (1200 - totalButtonsWidth) / 2; // Центрируем относительно ширины формы

            btnStart = CreateButton(LanguageManager.Get("Button_Start"), startX, buttonY, buttonWidth, buttonHeight, ThemeManager.GetButtonPrimary());
            btnStart.Tag = "primary";
            btnStart.Click += BtnStart_Click;
            panelButtons.Controls.Add(btnStart);

            btnStop = CreateButton(LanguageManager.Get("Button_Stop"), startX + buttonWidth + 10, buttonY, buttonWidth, buttonHeight, ThemeManager.GetButtonDanger());
            btnStop.Tag = "danger";
            btnStop.Enabled = true;
            btnStop.Click += BtnStop_Click;
            panelButtons.Controls.Add(btnStop);

            btnOpenTemp = CreateButton(LanguageManager.Get("Button_OpenTemp"), startX + (buttonWidth + 10) * 2, buttonY, buttonWidth, buttonHeight, ThemeManager.GetButtonSuccess());
            btnOpenTemp.Tag = "success";
            btnOpenTemp.Click += BtnOpenTemp_Click;
            panelButtons.Controls.Add(btnOpenTemp);

            btnRefresh = CreateButton(LanguageManager.Get("Button_Refresh"), startX + (buttonWidth + 10) * 3, buttonY, buttonWidth, buttonHeight, ThemeManager.GetButtonSecondary());
            btnRefresh.Tag = "secondary";
            btnRefresh.Click += BtnRefresh_Click;
            panelButtons.Controls.Add(btnRefresh);

            btnExport = CreateButton(LanguageManager.Get("Button_Export"), startX + (buttonWidth + 10) * 4, buttonY, buttonWidth, buttonHeight, ThemeManager.GetButtonSecondary());
            btnExport.Tag = "secondary";
            btnExport.Click += BtnExport_Click;
            panelButtons.Controls.Add(btnExport);

            btnExit = CreateButton(LanguageManager.Get("Button_Exit"), startX + (buttonWidth + 10) * 5, buttonY, buttonWidth, buttonHeight, ThemeManager.GetButtonNeutral());
            btnExit.Tag = "neutral";
            btnExit.Click += BtnExit_Click;
            panelButtons.Controls.Add(btnExit);

            // Второй ряд - чекбоксы настроек (центрируем)
            int checkboxY = 50;
            int totalCheckboxWidth = 180 * 3 + 20 * 2; // 3 чекбокса по 180px + 2 промежутка по 20px
            int checkboxStartX = (1200 - totalCheckboxWidth) / 2;

            chkAutoClose = new CheckBox();
            chkAutoClose.Text = LanguageManager.Get("Checkbox_AutoClose");
            chkAutoClose.Left = checkboxStartX;
            chkAutoClose.Top = checkboxY;
            chkAutoClose.Width = 180;
            chkAutoClose.ForeColor = ThemeManager.GetTextPrimary();
            panelButtons.Controls.Add(chkAutoClose);

            chkPlaySound = new CheckBox();
            chkPlaySound.Text = LanguageManager.Get("Checkbox_PlaySound");
            chkPlaySound.Left = checkboxStartX + 200;
            chkPlaySound.Top = checkboxY;
            chkPlaySound.Width = 180;
            chkPlaySound.Checked = false;
            chkPlaySound.ForeColor = ThemeManager.GetTextPrimary();
            panelButtons.Controls.Add(chkPlaySound);

            chkShowDetails = new CheckBox();
            chkShowDetails.Text = LanguageManager.Get("Checkbox_ShowDetails");
            chkShowDetails.Left = checkboxStartX + 400;
            chkShowDetails.Top = checkboxY;
            chkShowDetails.Width = 200;
            chkShowDetails.Checked = false;
            chkShowDetails.ForeColor = ThemeManager.GetTextPrimary();
            panelButtons.Controls.Add(chkShowDetails);
        }

        private TabPage CreateTab(string title)
        {
            TabPage tab = new TabPage(title);
            tab.BackColor = ThemeManager.GetBackgroundPanel();
            tab.ForeColor = ThemeManager.GetTextPrimary();
            tab.AutoScroll = false; // Включаем прокрутку для вкладок
            return tab;
        }

        private GroupBox CreateGroupBox(string title, int yPos)
        {
            GroupBox grp = new GroupBox();
            grp.Text = title;
            grp.Left = 20;
            grp.Top = yPos;
            grp.Width = 1090;
            grp.Height = 110;
            grp.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            grp.ForeColor = ThemeManager.GetTextInfo();
            return grp;
        }

        private CheckBox CreateOptionCheckBox(string text, int x, int y, bool isChecked)
        {
            CheckBox cb = new CheckBox();
            cb.Text = text;
            cb.Left = x;
            cb.Top = y;
            cb.Width = 250;
            cb.Checked = isChecked;
            cb.Font = new Font("Segoe UI", 9);
            cb.ForeColor = ThemeManager.GetTextPrimary();
            return cb;
        }

        private Button CreateButton(string text, int x, int y, int width, int height, Color backColor)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Left = x;
            btn.Top = y;
            btn.Width = width;
            btn.Height = height;
            btn.BackColor = backColor;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.TextAlign = ContentAlignment.MiddleCenter; // Центрируем текст
            btn.UseVisualStyleBackColor = false; // Отключаем стандартные стили
            return btn;
        }

        private void CreateOptionsTab(TabPage tab)
        {
            int yPos = 20;

            // Временные файлы Windows
            GroupBox grpTemp = CreateGroupBox(LanguageManager.Get("Group_TempFiles"), yPos);
            cleanupOptions["WinTemp"] = CreateOptionCheckBox(LanguageManager.Get("Option_WindowsTemp"), 20, 25, true);
            cleanupOptions["Prefetch"] = CreateOptionCheckBox(LanguageManager.Get("Option_Prefetch"), 20, 50, true);
            cleanupOptions["RecycleBin"] = CreateOptionCheckBox(LanguageManager.Get("Option_RecycleBin"), 20, 75, false);
            cleanupOptions["RecentItems"] = CreateOptionCheckBox(LanguageManager.Get("Option_RecentItems"), 300, 25, false);
            cleanupOptions["TempSetup"] = CreateOptionCheckBox(LanguageManager.Get("Option_TempSetup"), 300, 50, false);
            grpTemp.Controls.Add(cleanupOptions["WinTemp"]);
            grpTemp.Controls.Add(cleanupOptions["Prefetch"]);
            grpTemp.Controls.Add(cleanupOptions["RecycleBin"]);
            grpTemp.Controls.Add(cleanupOptions["RecentItems"]);
            grpTemp.Controls.Add(cleanupOptions["TempSetup"]);
            tab.Controls.Add(grpTemp);
            yPos += 120;

            // Браузеры
            GroupBox grpBrowsers = CreateGroupBox(LanguageManager.Get("Group_Browsers"), yPos);
            cleanupOptions["Opera"] = CreateOptionCheckBox("Opera", 20, 25, false);
            cleanupOptions["Chrome"] = CreateOptionCheckBox("Google Chrome", 20, 50, false);
            cleanupOptions["Edge"] = CreateOptionCheckBox("Microsoft Edge", 20, 75, false);
            cleanupOptions["Firefox"] = CreateOptionCheckBox("Mozilla Firefox", 300, 25, false);
            cleanupOptions["Brave"] = CreateOptionCheckBox("Brave Browser", 300, 50, false);
            cleanupOptions["Yandex"] = CreateOptionCheckBox(LanguageManager.Get("Option_YandexBrowser"), 300, 75, false);
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
            GroupBox grpMessengers = CreateGroupBox(LanguageManager.Get("Group_Messengers"), yPos);
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
            GroupBox grpSystem = CreateGroupBox(LanguageManager.Get("Group_SystemUtils"), yPos);
            grpSystem.Height = 135;
            cleanupOptions["DNS"] = CreateOptionCheckBox(LanguageManager.Get("Option_DNSCache"), 20, 25, false);
            cleanupOptions["DISM"] = CreateOptionCheckBox(LanguageManager.Get("Option_DISM"), 20, 50, false);
            cleanupOptions["ThumbnailCache"] = CreateOptionCheckBox(LanguageManager.Get("Option_ThumbnailCache"), 20, 75, false);
            cleanupOptions["IconCache"] = CreateOptionCheckBox(LanguageManager.Get("Option_IconCache"), 20, 100, false);
            cleanupOptions["WindowsUpdate"] = CreateOptionCheckBox(LanguageManager.Get("Option_WindowsUpdate"), 300, 25, false);
            cleanupOptions["EventLogs"] = CreateOptionCheckBox(LanguageManager.Get("Option_EventLogs"), 300, 50, false);
            cleanupOptions["DeliveryOptimization"] = CreateOptionCheckBox(LanguageManager.Get("Option_DeliveryOptimization"), 300, 75, false);
            cleanupOptions["SoftwareDistribution"] = CreateOptionCheckBox("SoftwareDistribution", 300, 100, false);
            cleanupOptions["MemoryDumps"] = CreateOptionCheckBox(LanguageManager.Get("Option_MemoryDumps"), 580, 25, false);
            cleanupOptions["ErrorReports"] = CreateOptionCheckBox(LanguageManager.Get("Option_ErrorReports"), 580, 50, false);
            cleanupOptions["TempInternet"] = CreateOptionCheckBox(LanguageManager.Get("Option_TempInternet"), 580, 75, false);
            cleanupOptions["FontCache"] = CreateOptionCheckBox(LanguageManager.Get("Option_FontCache"), 580, 100, false);
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

            // Дополнительные опции (без StartupPrograms)
            GroupBox grpAdditional = CreateGroupBox(LanguageManager.Get("Group_Additional"), yPos);
            grpAdditional.Height = 135;
            cleanupOptions["LogFiles"] = CreateOptionCheckBox(LanguageManager.Get("Option_LogFiles"), 20, 25, false);
            cleanupOptions["OldDrivers"] = CreateOptionCheckBox(LanguageManager.Get("Option_OldDrivers"), 20, 50, false);
            cleanupOptions["WinSxS"] = CreateOptionCheckBox(LanguageManager.Get("Option_WinSxS"), 20, 75, false);
            cleanupOptions["RegistryCleanup"] = CreateOptionCheckBox(LanguageManager.Get("Option_RegistryCleanup"), 20, 100, false);
            cleanupOptions["RestorePoints"] = CreateOptionCheckBox(LanguageManager.Get("Option_RestorePoints"), 300, 25, false);
            cleanupOptions["TempUser"] = CreateOptionCheckBox(LanguageManager.Get("Option_TempUser"), 300, 50, false);
            cleanupOptions["DiagnosticData"] = CreateOptionCheckBox(LanguageManager.Get("Option_DiagnosticData"), 300, 75, false);
            grpAdditional.Controls.Add(cleanupOptions["LogFiles"]);
            grpAdditional.Controls.Add(cleanupOptions["OldDrivers"]);
            grpAdditional.Controls.Add(cleanupOptions["WinSxS"]);
            grpAdditional.Controls.Add(cleanupOptions["RegistryCleanup"]);
            grpAdditional.Controls.Add(cleanupOptions["RestorePoints"]);
            grpAdditional.Controls.Add(cleanupOptions["TempUser"]);
            grpAdditional.Controls.Add(cleanupOptions["DiagnosticData"]);
            tab.Controls.Add(grpAdditional);
            yPos += 145;

            // Кнопки управления - 3 кнопки в ряд по центру внизу
            int buttonWidth = 170;
            int buttonHeight = 40;
            int spacing = 20;
            int totalWidth = (buttonWidth * 3) + (spacing * 2); // 3 кнопки + 2 промежутка
            int startX = (1120 - totalWidth) / 2; // Центрируем относительно новой ширины 1120px
            
            Button btnSelectAll = CreateButton(LanguageManager.Get("Button_SelectAll"), startX, yPos + 30, buttonWidth, buttonHeight, ThemeManager.GetButtonPrimary());
            btnSelectAll.Tag = "primary"; // Добавляем тег
            Button btnDeselectAll = CreateButton(LanguageManager.Get("Button_DeselectAll"), startX + buttonWidth + spacing, yPos + 30, buttonWidth, buttonHeight, ThemeManager.GetButtonDanger());
            btnDeselectAll.Tag = "danger"; // Добавляем тег
            Button btnStartupPrograms = CreateButton(LanguageManager.Get("Option_StartupPrograms"), startX + (buttonWidth + spacing) * 2, yPos + 30, buttonWidth, buttonHeight, ThemeManager.GetButtonSuccess());
            btnStartupPrograms.Tag = "success"; // Добавляем тег

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
            btnStartupPrograms.Click += async delegate
            {
                // Открываем Task Manager для управления автозагрузкой
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "taskmgr.exe",
                        Arguments = "/0 /startup",
                        UseShellExecute = true
                    });
                    MessageBox.Show(
                        "Диспетчер задач открыт на вкладке 'Автозагрузка'.\n\nВы можете управлять программами, которые запускаются вместе с Windows.",
                        "Управление автозагрузкой",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Не удалось открыть диспетчер задач: {ex.Message}\n\nОткройте его вручную: Ctrl+Shift+Esc → вкладка 'Автозагрузка'",
                        "Ошибка",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            };

            tab.Controls.Add(btnSelectAll);
            tab.Controls.Add(btnDeselectAll);
            tab.Controls.Add(btnStartupPrograms);
        }

        private void CreateProgressTab(TabPage tab)
        {
            int yPos = 20;

            Label lblInfo = new Label();
            lblInfo.Text = LanguageManager.Get("Progress_TrackingRealtime");
            lblInfo.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblInfo.ForeColor = ThemeManager.GetTextInfo();
            lblInfo.Left = 20;
            lblInfo.Top = yPos;
            lblInfo.AutoSize = true;
            tab.Controls.Add(lblInfo);
            yPos += 35;

            string[] categories = new string[] {
                LanguageManager.Get("Category_TempFiles"),
                LanguageManager.Get("Category_Browsers"),
                LanguageManager.Get("Category_Messengers"),
                LanguageManager.Get("Category_SystemUtils")
            };

            foreach (string category in categories)
            {
                Panel categoryPanel = new Panel();
                categoryPanel.Left = 20;
                categoryPanel.Top = yPos;
                categoryPanel.Width = 1120;
                categoryPanel.Height = 50;
                categoryPanel.BackColor = ThemeManager.GetBackgroundLight();
                tab.Controls.Add(categoryPanel);

                Label lblCategory = new Label();
                lblCategory.Text = category;
                lblCategory.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                lblCategory.ForeColor = ThemeManager.GetTextAccent();
                lblCategory.Left = 10;
                lblCategory.Top = 5;
                lblCategory.Width = 250;
                categoryPanel.Controls.Add(lblCategory);

                ProgressBar pb = new ProgressBar();
                pb.Left = 10;
                pb.Top = 25;
                pb.Width = 1000;
                pb.Height = 18;
                pb.Style = ProgressBarStyle.Continuous;
                categoryPanel.Controls.Add(pb);

                Label lblPercent = new Label();
                lblPercent.Text = "0%";
                lblPercent.Left = 1020;
                lblPercent.Top = 25;
                lblPercent.Width = 80;
                lblPercent.ForeColor = ThemeManager.GetTextSecondary();
                lblPercent.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                pb.Tag = lblPercent;
                categoryPanel.Controls.Add(lblPercent);

                progressBars[category] = pb;

                yPos += 60;
            }
        }

        private void CreateHistoryTab(TabPage tab)
        {
            Label lblTitle = new Label();
            lblTitle.Text = LanguageManager.Get("History_RecentCleanups");
            lblTitle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblTitle.ForeColor = ThemeManager.GetTextInfo();
            lblTitle.Left = 20;
            lblTitle.Top = 20;
            lblTitle.AutoSize = true;
            tab.Controls.Add(lblTitle);

            listViewHistory = new ListView();
            listViewHistory.Left = 20;
            listViewHistory.Top = 50;
            listViewHistory.Width = 1120;
            listViewHistory.Height = 520; // Увеличено для лучшей видимости
            listViewHistory.View = View.Details;
            listViewHistory.FullRowSelect = true;
            listViewHistory.GridLines = true;
            listViewHistory.BackColor = ThemeManager.GetBackgroundLight();
            listViewHistory.ForeColor = ThemeManager.GetTextPrimary();

            listViewHistory.Columns.Add(LanguageManager.Get("History_Date"), 150);
            listViewHistory.Columns.Add(LanguageManager.Get("History_Freed"), 120);
            listViewHistory.Columns.Add(LanguageManager.Get("History_Files"), 100);
            listViewHistory.Columns.Add(LanguageManager.Get("History_Folders"), 100);
            listViewHistory.Columns.Add(LanguageManager.Get("History_Time"), 100);
            listViewHistory.Columns.Add(LanguageManager.Get("History_Status"), 150);

            tab.Controls.Add(listViewHistory);

            Button btnClearHistory = CreateButton(LanguageManager.Get("Button_ClearHistory"), 20, 580, 150, 35, ThemeManager.GetButtonDanger());
            btnClearHistory.Tag = "danger"; // Добавляем тег
            btnClearHistory.Click += delegate
            {
                listViewHistory.Items.Clear();
                SaveHistory();
            };
            tab.Controls.Add(btnClearHistory);
        }

        private void CreateLogsTab(TabPage tab)
        {
            Label lblTitle = new Label();
            lblTitle.Text = LanguageManager.Get("Logs_SystemLogs");
            lblTitle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblTitle.ForeColor = ThemeManager.GetTextInfo();
            lblTitle.Left = 20;
            lblTitle.Top = 20;
            lblTitle.AutoSize = true;
            tab.Controls.Add(lblTitle);

            txtLog = new TextBox();
            txtLog.Multiline = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Left = 20;
            txtLog.Top = 50;
            txtLog.Width = 1120;
            txtLog.Height = 520; // Увеличено для лучшей видимости
            txtLog.Font = new Font("Consolas", 9);
            txtLog.ReadOnly = true;
            txtLog.BackColor = ThemeManager.GetBackgroundDark();
            txtLog.ForeColor = ThemeManager.GetTextSuccess();
            tab.Controls.Add(txtLog);

            Button btnClearLog = CreateButton(LanguageManager.Get("Button_ClearLog"), 20, 580, 150, 35, ThemeManager.GetButtonDanger());
            btnClearLog.Tag = "danger"; // Добавляем тег
            btnClearLog.Click += delegate
            {
                txtLog.Clear();
                Log(LanguageManager.Get("Log_Ready"));
            };
            tab.Controls.Add(btnClearLog);
        }

        private void SetupCallbacks()
        {
            Cleanup.OnLog += Log;
            Cleanup.OnStatsUpdate += OnStatsUpdated;
        }

        private void OnStatsUpdated()
        {
            // Получаем обновленную статистику из Cleanup
            string freedSpace = Cleanup.GetFormattedFreedSpace();
            int files = Cleanup.GetTotalFiles();
            int folders = Cleanup.GetTotalFolders();
            string elapsed = stopwatch != null && stopwatch.IsRunning 
                ? string.Format("{0:00}:{1:00}:{2:00}", stopwatch.Elapsed.Hours, stopwatch.Elapsed.Minutes, stopwatch.Elapsed.Seconds)
                : "0с";
            
            UpdateStats(freedSpace, files, folders, elapsed);
        }

        private void SetupTimer()
        {
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 500;
            updateTimer.Tick += (s, e) =>
            {
                if (stopwatch != null && stopwatch.IsRunning)
                {
                    string elapsed = string.Format("{0:00}:{1:00}:{2:00}",
                        stopwatch.Elapsed.Hours,
                        stopwatch.Elapsed.Minutes,
                        stopwatch.Elapsed.Seconds);
                    UpdateStats(Cleanup.GetFormattedFreedSpace(), Cleanup.GetTotalFiles(), Cleanup.GetTotalFolders(), elapsed);
                }
            };
        }

        private void Log(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(Log), message);
            }
            else
            {
                txtLog.AppendText(message + Environment.NewLine);

                if (chkShowDetails.Checked)
                {
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                }
            }
        }

        private void UpdateStats(string freedSpace, int totalFiles, int totalFolders, string elapsedTime)
        {
            if (lblStats.InvokeRequired)
            {
                lblStats.Invoke(new Action<string, int, int, string>(UpdateStats), freedSpace, totalFiles, totalFolders, elapsedTime);
            }
            else
            {
                lblStats.Text = $"{freedSpace} | {totalFiles} {LanguageManager.Get("Stats_Files")} | {totalFolders} {LanguageManager.Get("Stats_Folders")} | {elapsedTime}";
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            StartCleanup();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            if (cts != null)
            {
                cts.Cancel();
                lblStatus.Text = LanguageManager.Get("Status_Stopping");
                lblStatus.ForeColor = ThemeManager.GetTextWarning();
            }
        }

        private void BtnOpenTemp_Click(object sender, EventArgs e)
        {
            try
            {
                string tempPath = Path.GetTempPath();
                Process.Start("explorer.exe", tempPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LanguageManager.Get("Message_Error")}: {ex.Message}", LanguageManager.Get("Message_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            Cleanup.ResetStats();
            UpdateStats("0 МБ", 0, 0, "0с");
            lblStatus.Text = LanguageManager.Get("Status_Ready");
            lblStatus.ForeColor = ThemeManager.GetTextSuccess();

            foreach (var pb in progressBars.Values)
            {
                pb.Value = 0;
                if (pb.Tag is Label)
                {
                    Label lbl = (Label)pb.Tag;
                    lbl.Text = "0%";
                }
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"CleanupLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(desktop, fileName);

                File.WriteAllText(filePath, txtLog.Text);
                MessageBox.Show(
                    string.Format(LanguageManager.Get("Message_ExportSuccess"), filePath),
                    LanguageManager.Get("Message_Success"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{LanguageManager.Get("Message_Error")}: {ex.Message}", LanguageManager.Get("Message_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void LoadHistory()
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "CleanupTempPro");

                if (!Directory.Exists(appDataPath))
                    Directory.CreateDirectory(appDataPath);

                string historyFile = Path.Combine(appDataPath, "history.txt");

                if (File.Exists(historyFile))
                {
                    string[] lines = File.ReadAllLines(historyFile);
                    foreach (string line in lines.Reverse())
                    {
                        string[] parts = line.Split('|');
                        if (parts.Length == 6)
                        {
                            ListViewItem item = new ListViewItem(parts);
                            listViewHistory.Items.Add(item);
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки загрузки истории
            }
        }

        private void SaveHistory()
        {
            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "CleanupTempPro");

                if (!Directory.Exists(appDataPath))
                    Directory.CreateDirectory(appDataPath);

                string historyFile = Path.Combine(appDataPath, "history.txt");

                List<string> lines = new List<string>();
                foreach (ListViewItem item in listViewHistory.Items)
                {
                    string line = string.Join("|", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => s.Text));
                    lines.Add(line);
                }

                File.WriteAllLines(historyFile, lines.ToArray());
            }
            catch
            {
                // Игнорируем ошибки сохранения истории
            }
        }

        private void StartCleanup()
        {
            var selectedOptions = cleanupOptions
                .Where(kv => kv.Value.Checked)
                .Select(kv => kv.Key)
                .ToList();

            if (selectedOptions.Count == 0)
            {
                MessageBox.Show(
                    LanguageManager.Get("Message_NoOptionsSelected"),
                    LanguageManager.Get("Message_Warning"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            btnOpenTemp.Enabled = false;
            btnRefresh.Enabled = false;

            lblStatus.Text = LanguageManager.Get("Status_InProgress");
            lblStatus.ForeColor = ThemeManager.GetTextWarning();

            Cleanup.ResetStats();
            txtLog.Clear();
            Log("=== " + LanguageManager.Get("Log_StartedAt") + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===\n");

            cts = new CancellationTokenSource();
            stopwatch = Stopwatch.StartNew();
            updateTimer.Start();

            Task.Run(() => PerformCleanup(selectedOptions, cts.Token));
        }

        private async Task PerformCleanup(List<string> selectedOptions, CancellationToken token)
        {
            try
            {
                foreach (var option in selectedOptions)
                {
                    if (token.IsCancellationRequested)
                        break;

                    Log($"\n→ {LanguageManager.Get("Log_Processing")}: {option}");

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
                    Log("\n=== " + LanguageManager.Get("Log_CompletedSuccessfully") + " ===");
                    UpdateProgressBarMain(100);

                    this.Invoke(new Action(() =>
                    {
                        lblStatus.Text = LanguageManager.Get("Status_Completed");
                        lblStatus.ForeColor = ThemeManager.GetTextSuccess();

                        AddToHistory(LanguageManager.Get("Status_Success"));

                        if (chkPlaySound.Checked)
                            System.Media.SystemSounds.Asterisk.Play();
                    }));
                }
            }
            catch (OperationCanceledException)
            {
                Log("\n" + LanguageManager.Get("Log_OperationCancelled"));
            }
            catch (Exception ex)
            {
                Log($"\n{LanguageManager.Get("Log_Error")}: {ex.Message}");
                this.Invoke(new Action(() =>
                {
                    lblStatus.Text = LanguageManager.Get("Status_Error");
                    lblStatus.ForeColor = ThemeManager.GetTextError();
                    AddToHistory(LanguageManager.Get("Status_Error"));
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
                return LanguageManager.Get("Category_TempFiles");

            if (option == "Opera" || option == "Chrome" || option == "Edge" ||
                option == "Firefox" || option == "Brave" || option == "Yandex" ||
                option == "Vivaldi" || option == "Tor")
                return LanguageManager.Get("Category_Browsers");

            if (option == "Telegram" || option == "Discord" || option == "Viber" ||
                option == "Zoom" || option == "Spotify" || option == "VSCode" ||
                option == "Teams" || option == "Skype" || option == "Slack")
                return LanguageManager.Get("Category_Messengers");

            return LanguageManager.Get("Category_SystemUtils");
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
                SaveHistory();
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

        private void BtnAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                LanguageManager.Get("About_ProgramInfo"),
                LanguageManager.Get("About_Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            using (SettingsForm settingsForm = new SettingsForm())
            {
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    // Обновляем интерфейс после изменения настроек
                    UpdateUIAfterLanguageChange();
                    
                    MessageBox.Show(
                        LanguageManager.Get("Message_SettingsSaved"),
                        LanguageManager.Get("Message_Information"),
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);
                }
            }
        }

        private void UpdateUIAfterLanguageChange()
        {
            // Обновляем все текстовые элементы после смены языка
            this.Text = LanguageManager.Get("Title");
            lblTitle.Text = LanguageManager.Get("Title");
            lblVersion.Text = LanguageManager.Get("Version");
            btnSettings.Text = LanguageManager.Get("Settings");
            
            // Обновляем кнопки управления
            btnStart.Text = LanguageManager.Get("Button_Start");
            btnStop.Text = LanguageManager.Get("Button_Stop");
            btnOpenTemp.Text = LanguageManager.Get("Button_OpenTemp");
            btnRefresh.Text = LanguageManager.Get("Button_Refresh");
            btnExport.Text = LanguageManager.Get("Button_Export");
            btnExit.Text = LanguageManager.Get("Button_Exit");
            
            // Обновляем чекбоксы
            chkAutoClose.Text = LanguageManager.Get("Checkbox_AutoClose");
            chkPlaySound.Text = LanguageManager.Get("Checkbox_PlaySound");
            chkShowDetails.Text = LanguageManager.Get("Checkbox_ShowDetails");
            
            // Обновляем статус если форма готова к очистке
            if (btnStart.Enabled)
            {
                lblStatus.Text = LanguageManager.Get("Status_Ready");
            }

            // Применяем тему заново используя теги вместо текста кнопок
            ThemeManager.ApplyThemeByTags(this);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                var result = MessageBox.Show(
                    LanguageManager.Get("Message_ConfirmExit"),
                    LanguageManager.Get("Message_ExitTitle"),
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
