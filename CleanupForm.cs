using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace CleanupTempPro
{
    public class CleanupForm : Form
    {
        private Button btnStart, btnStop, btnExit, btnOpenTemp, btnExport, btnRefresh;
        private TextBox txtLog;
        private Label lblStats, lblStatus, lblTitle, lblVersion;
        private ProgressBar pbMain;
        private TabControl tabControl;
        private CheckBox chkAutoClose, chkPlaySound, chkShowDetails;
        private CancellationTokenSource cts;
        private Stopwatch stopwatch;
        private System.Windows.Forms.Timer updateTimer;
        
        private Dictionary<string, ProgressBar> progressBars;
        private Dictionary<string, CheckBox> cleanupOptions;
        private ListView listViewHistory;
        private int cleanupCount = 0;

        public CleanupForm()
        {
            this.Text = "CleanupTemp Pro - Профессиональная версия";
            this.Size = new Size(1000, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            progressBars = new Dictionary<string, ProgressBar>();
            cleanupOptions = new Dictionary<string, CheckBox>();

            InitializeComponents();
            SetupCallbacks();
            SetupTimer();
            LoadHistory();
        }

        private void InitializeComponents()
        {
            Panel panelTop = new Panel();
            panelTop.Dock = DockStyle.Top;
            panelTop.Height = 120;
            panelTop.BackColor = Color.FromArgb(37, 37, 38);
            this.Controls.Add(panelTop);

            lblTitle = new Label();
            lblTitle.Text = "CleanupTemp Pro Профессиональная";
            lblTitle.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(0, 150, 255);
            lblTitle.Left = 20;
            lblTitle.Top = 10;
            lblTitle.AutoSize = true;
            panelTop.Controls.Add(lblTitle);

            lblVersion = new Label();
            lblVersion.Text = "v3.0 Финальная";
            lblVersion.Font = new Font("Segoe UI", 8);
            lblVersion.ForeColor = Color.Gray;
            lblVersion.Left = 20;
            lblVersion.Top = 45;
            lblVersion.AutoSize = true;
            panelTop.Controls.Add(lblVersion);

            lblStatus = new Label();
            lblStatus.Text = "Готов к очистке";
            lblStatus.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblStatus.ForeColor = Color.FromArgb(0, 255, 127);
            lblStatus.Left = 20;
            lblStatus.Top = 65;
            lblStatus.Width = 500;
            panelTop.Controls.Add(lblStatus);

            lblStats = new Label();
            lblStats.Text = "0 МБ | 0 файлов | 0 папок | 0с";
            lblStats.Left = 580;
            lblStats.Top = 20;
            lblStats.Width = 380;
            lblStats.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblStats.ForeColor = Color.FromArgb(100, 200, 255);
            lblStats.TextAlign = ContentAlignment.MiddleRight;
            panelTop.Controls.Add(lblStats);

            Label lblProgress = new Label();
            lblProgress.Text = "Общий прогресс:";
            lblProgress.Font = new Font("Segoe UI", 9);
            lblProgress.ForeColor = Color.White;
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
            panelButtons.BackColor = Color.FromArgb(37, 37, 38);
            this.Controls.Add(panelButtons);

            btnStart = CreateButton("НАЧАТЬ ОЧИСТКУ", 20, 10, 180, 35, Color.FromArgb(0, 150, 255));
            btnStop = CreateButton("СТОП", 220, 10, 120, 35, Color.FromArgb(220, 50, 50));
            btnOpenTemp = CreateButton("Открыть Temp", 360, 10, 130, 35, Color.FromArgb(100, 150, 100));
            btnRefresh = CreateButton("Обновить", 510, 10, 100, 35, Color.FromArgb(100, 100, 200));
            btnExport = CreateButton("Экспорт лога", 630, 10, 130, 35, Color.FromArgb(150, 100, 150));
            btnExit = CreateButton("ВЫХОД", 780, 10, 120, 35, Color.FromArgb(100, 100, 100));

            btnStop.Enabled = false;

            chkAutoClose = CreateCheckBox("Автозакрытие приложений", 20, 50);
            chkPlaySound = CreateCheckBox("Звук завершения", 200, 50);
            chkShowDetails = CreateCheckBox("Показать детали", 360, 50);

            btnStart.Click += BtnStart_Click;
            btnStop.Click += BtnStop_Click;
            btnOpenTemp.Click += delegate { 
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
            tab.BackColor = Color.FromArgb(30, 30, 30);
            tab.Padding = new Padding(10);
            return tab;
        }

        private void CreateOptionsTab(TabPage tab)
        {
            int yPos = 20;
            
            GroupBox grpTemp = CreateGroupBox("Временные файлы Windows", yPos);
            cleanupOptions["WinTemp"] = CreateOptionCheckBox("Windows Temp", 20, 25, false);
            cleanupOptions["Prefetch"] = CreateOptionCheckBox("Prefetch", 20, 50, false);
            cleanupOptions["RecycleBin"] = CreateOptionCheckBox("Корзина", 20, 75, false);
            grpTemp.Controls.Add(cleanupOptions["WinTemp"]);
            grpTemp.Controls.Add(cleanupOptions["Prefetch"]);
            grpTemp.Controls.Add(cleanupOptions["RecycleBin"]);
            tab.Controls.Add(grpTemp);
            yPos += 120;

            GroupBox grpBrowsers = CreateGroupBox("Браузеры", yPos);
            cleanupOptions["Opera"] = CreateOptionCheckBox("Opera / Opera GX", 20, 25, false);
            cleanupOptions["Chrome"] = CreateOptionCheckBox("Google Chrome", 20, 50, false);
            cleanupOptions["Edge"] = CreateOptionCheckBox("Microsoft Edge", 20, 75, false);
            cleanupOptions["Firefox"] = CreateOptionCheckBox("Mozilla Firefox", 300, 25, false);
            cleanupOptions["Brave"] = CreateOptionCheckBox("Brave Browser", 300, 50, false);
            cleanupOptions["Yandex"] = CreateOptionCheckBox("Яндекс Браузер", 300, 75, false);
            grpBrowsers.Controls.Add(cleanupOptions["Opera"]);
            grpBrowsers.Controls.Add(cleanupOptions["Chrome"]);
            grpBrowsers.Controls.Add(cleanupOptions["Edge"]);
            grpBrowsers.Controls.Add(cleanupOptions["Firefox"]);
            grpBrowsers.Controls.Add(cleanupOptions["Brave"]);
            grpBrowsers.Controls.Add(cleanupOptions["Yandex"]);
            tab.Controls.Add(grpBrowsers);
            yPos += 120;

            GroupBox grpMessengers = CreateGroupBox("Мессенджеры и приложения", yPos);
            cleanupOptions["Telegram"] = CreateOptionCheckBox("Telegram", 20, 25, false);
            cleanupOptions["Discord"] = CreateOptionCheckBox("Discord", 20, 50, false);
            cleanupOptions["Viber"] = CreateOptionCheckBox("Viber", 20, 75, false);
            cleanupOptions["Zoom"] = CreateOptionCheckBox("Zoom", 300, 25, false);
            cleanupOptions["Spotify"] = CreateOptionCheckBox("Spotify", 300, 50, false);
            cleanupOptions["VSCode"] = CreateOptionCheckBox("VS Code", 300, 75, false);
            grpMessengers.Controls.Add(cleanupOptions["Telegram"]);
            grpMessengers.Controls.Add(cleanupOptions["Discord"]);
            grpMessengers.Controls.Add(cleanupOptions["Viber"]);
            grpMessengers.Controls.Add(cleanupOptions["Zoom"]);
            grpMessengers.Controls.Add(cleanupOptions["Spotify"]);
            grpMessengers.Controls.Add(cleanupOptions["VSCode"]);
            tab.Controls.Add(grpMessengers);
            yPos += 120;

            GroupBox grpSystem = CreateGroupBox("Системные утилиты", yPos);
            cleanupOptions["DNS"] = CreateOptionCheckBox("Очистка DNS кэша", 20, 25, false);
            cleanupOptions["DISM"] = CreateOptionCheckBox("DISM очистка (медленно)", 20, 50, false);
            grpSystem.Controls.Add(cleanupOptions["DNS"]);
            grpSystem.Controls.Add(cleanupOptions["DISM"]);
            tab.Controls.Add(grpSystem);

            Button btnSelectAll = CreateButton("Выбрать всё", 600, 20, 150, 30, Color.FromArgb(0, 150, 255));
            Button btnDeselectAll = CreateButton("Снять всё", 600, 60, 150, 30, Color.FromArgb(150, 50, 50));
            Button btnRecommended = CreateButton("Рекомендуемые", 600, 100, 150, 30, Color.FromArgb(100, 150, 100));
            
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
            };
            
            tab.Controls.Add(btnSelectAll);
            tab.Controls.Add(btnDeselectAll);
            tab.Controls.Add(btnRecommended);
        }

        private void CreateProgressTab(TabPage tab)
        {
            int yPos = 20;
            
            Label lblInfo = new Label();
            lblInfo.Text = "Отслеживание прогресса в реальном времени";
            lblInfo.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblInfo.ForeColor = Color.FromArgb(0, 150, 255);
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
                lbl.ForeColor = Color.FromArgb(100, 200, 255);
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
            statsPanel.BackColor = Color.FromArgb(40, 40, 40);
            statsPanel.BorderStyle = BorderStyle.FixedSingle;
            tab.Controls.Add(statsPanel);

            Label lblStatsTitle = new Label();
            lblStatsTitle.Text = "Статистика сессии";
            lblStatsTitle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblStatsTitle.ForeColor = Color.FromArgb(0, 150, 255);
            lblStatsTitle.Left = 20;
            lblStatsTitle.Top = 15;
            lblStatsTitle.AutoSize = true;
            statsPanel.Controls.Add(lblStatsTitle);

            TextBox txtStats = new TextBox();
            txtStats.Multiline = true;
            txtStats.ReadOnly = true;
            txtStats.BackColor = Color.FromArgb(40, 40, 40);
            txtStats.ForeColor = Color.White;
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
            lblTitle.ForeColor = Color.FromArgb(0, 150, 255);
            lblTitle.Left = 20;
            lblTitle.Top = 10;
            lblTitle.AutoSize = true;
            tab.Controls.Add(lblTitle);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Отслеживайте все ваши сессии очистки";
            lblSubtitle.Font = new Font("Segoe UI", 9);
            lblSubtitle.ForeColor = Color.Gray;
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
            listViewHistory.BackColor = Color.FromArgb(40, 40, 40);
            listViewHistory.ForeColor = Color.White;
            listViewHistory.Font = new Font("Segoe UI", 9);

            listViewHistory.Columns.Add("Дата и время", 180);
            listViewHistory.Columns.Add("Освобождено", 120);
            listViewHistory.Columns.Add("Файлы", 80);
            listViewHistory.Columns.Add("Папки", 80);
            listViewHistory.Columns.Add("Время", 100);
            listViewHistory.Columns.Add("Статус", 140);

            tab.Controls.Add(listViewHistory);

            Button btnClearHistory = CreateButton("Очистить историю", 20, 540, 150, 30, Color.FromArgb(150, 50, 50));
            Button btnExportHistory = CreateButton("Экспорт истории", 180, 540, 150, 30, Color.FromArgb(100, 100, 200));
            
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
            lblTitle.ForeColor = Color.FromArgb(0, 150, 255);
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
            txtLog.BackColor = Color.FromArgb(20, 20, 20);
            txtLog.ForeColor = Color.FromArgb(0, 255, 127);
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
            grp.ForeColor = Color.FromArgb(100, 200, 255);
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
            cb.ForeColor = Color.White;
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
            cb.ForeColor = Color.White;
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
                    if (btn.BackColor != Color.FromArgb(100, 100, 100))
                    {
                        Color originalColor = btn.BackColor;
                        btn.MouseEnter += delegate { btn.BackColor = LightenColor(originalColor, 30); };
                        btn.MouseLeave += delegate { btn.BackColor = originalColor; };
                    }
                }
                SetupHoverRecursive(ctrl);
            }
        }

        private Color LightenColor(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Min(255, color.R + amount),
                Math.Min(255, color.G + amount),
                Math.Min(255, color.B + amount)
            );
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
            lblStatus.ForeColor = Color.Yellow;

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
                    
                    this.Invoke(new Action(() => {
                        lblStatus.Text = "Очистка завершена!";
                        lblStatus.ForeColor = Color.FromArgb(0, 255, 127);
                        
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
                this.Invoke(new Action(() => {
                    lblStatus.Text = "Произошла ошибка";
                    lblStatus.ForeColor = Color.Red;
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
            if (option == "WinTemp" || option == "Prefetch" || option == "RecycleBin")
                return "Временные файлы Windows";
            if (option == "Opera" || option == "Chrome" || option == "Edge" || 
                option == "Firefox" || option == "Brave" || option == "Yandex")
                return "Браузеры";
            if (option == "Telegram" || option == "Discord" || option == "Viber" || 
                option == "Zoom" || option == "Spotify" || option == "VSCode")
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
