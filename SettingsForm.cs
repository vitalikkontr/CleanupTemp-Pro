using System;
using System.Drawing;
using System.Windows.Forms;

namespace CleanupTemp_Pro
{
    public class SettingsForm : Form
    {
        private ComboBox cmbLanguage;
        private ComboBox cmbTheme;
        private Button btnSave;
        private Button btnCancel;
        private Label lblLanguage;
        private Label lblTheme;

        public SettingsForm()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "Настройки";
            this.Size = new Size(450, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Применяем тему
            this.BackColor = ThemeManager.GetBackgroundPanel();

            // Заголовок
            Label lblTitle = new Label();
            lblTitle.Text = "Настройки программы";
            lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTitle.ForeColor = ThemeManager.GetTextAccent();
            lblTitle.Left = 20;
            lblTitle.Top = 20;
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            // Язык
            lblLanguage = new Label();
            lblLanguage.Text = "Язык интерфейса:";
            lblLanguage.Font = new Font("Segoe UI", 10);
            lblLanguage.ForeColor = ThemeManager.GetTextPrimary();
            lblLanguage.Left = 20;
            lblLanguage.Top = 70;
            lblLanguage.AutoSize = true;
            this.Controls.Add(lblLanguage);

            cmbLanguage = new ComboBox();
            cmbLanguage.Font = new Font("Segoe UI", 10);
            cmbLanguage.Left = 180;
            cmbLanguage.Top = 67;
            cmbLanguage.Width = 220;
            cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLanguage.Items.AddRange(new object[] {
                "Русский",
                "English",
                "Українська"
            });
            cmbLanguage.SelectedIndex = 0; // По умолчанию русский
            this.Controls.Add(cmbLanguage);

            // Тема
            lblTheme = new Label();
            lblTheme.Text = "Тема оформления:";
            lblTheme.Font = new Font("Segoe UI", 10);
            lblTheme.ForeColor = ThemeManager.GetTextPrimary();
            lblTheme.Left = 20;
            lblTheme.Top = 120;
            lblTheme.AutoSize = true;
            this.Controls.Add(lblTheme);

            cmbTheme = new ComboBox();
            cmbTheme.Font = new Font("Segoe UI", 10);
            cmbTheme.Left = 180;
            cmbTheme.Top = 117;
            cmbTheme.Width = 220;
            cmbTheme.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTheme.Items.AddRange(new object[] {
                "Тёмная тема",
                "Светлая тема"
            });
            cmbTheme.SelectedIndex = ThemeManager.CurrentTheme == ThemeType.Dark ? 0 : 1;
            this.Controls.Add(cmbTheme);

            // Кнопка Сохранить
            btnSave = new Button();
            btnSave.Text = "Сохранить";
            btnSave.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnSave.Left = 180;
            btnSave.Top = 200;
            btnSave.Width = 120;
            btnSave.Height = 35;
            btnSave.BackColor = ThemeManager.GetButtonPrimary();
            btnSave.ForeColor = Color.White;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Cursor = Cursors.Hand;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            // Кнопка Отмена
            btnCancel = new Button();
            btnCancel.Text = "Отмена";
            btnCancel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnCancel.Left = 310;
            btnCancel.Top = 200;
            btnCancel.Width = 100;
            btnCancel.Height = 35;
            btnCancel.BackColor = ThemeManager.GetButtonNeutral();
            btnCancel.ForeColor = Color.White;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += BtnCancel_Click;
            this.Controls.Add(btnCancel);
        }

        // Загрузка текущих настроек при открытии формы
        private void LoadCurrentSettings()
        {
            // Загружаем текущий язык
            string currentLang = LanguageManager.CurrentLanguage;
            for (int i = 0; i < cmbLanguage.Items.Count; i++)
            {
                if (cmbLanguage.Items[i].ToString() == currentLang)
                {
                    cmbLanguage.SelectedIndex = i;
                    break;
                }
            }

            // Загружаем текущую тему
            cmbTheme.SelectedIndex = ThemeManager.CurrentTheme == ThemeType.Dark ? 0 : 1;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            bool needRestart = false;

            // Сохранение языка
            string newLanguage = cmbLanguage.SelectedItem.ToString();
            if (newLanguage != LanguageManager.CurrentLanguage)
            {
                LanguageManager.CurrentLanguage = newLanguage;
                LanguageManager.SaveLanguage();
                needRestart = true;
            }

            // Сохранение темы
            ThemeType newTheme = (cmbTheme.SelectedIndex == 0) ? ThemeType.Dark : ThemeType.Light;
            if (newTheme != ThemeManager.CurrentTheme)
            {
                ThemeManager.CurrentTheme = newTheme;
                ThemeManager.SaveTheme();
            }

            // Уведомление пользователя
            if (needRestart)
            {
                MessageBox.Show(
                    "Настройки сохранены!\n\nДля применения языка необходимо перезапустить программу.",
                    "Информация",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    "Настройки сохранены!",
                    "Информация",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}