using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Linq;

namespace CleanupTemp_Pro
{
    public enum ButtonRole
    {
        Primary,
        Danger,
        Success,
        Secondary,
        Warning,
        Neutral
    }
}
public enum ThemeType
    {
        Light,
        Dark,
    }
    namespace CleanupTemp_Pro
    {
      

    public static class ThemeManager
    {
        private static ThemeType currentTheme = ThemeType.Dark;
        private static string settingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CleanupTempPro",
            "theme.txt"
        );

        // Загрузка темы из настроек при запуске
        static ThemeManager()
        {
            LoadTheme();
        }

        // Загрузить тему из настроек
        public static void LoadTheme()
        {
            try
            {
                string directory = Path.GetDirectoryName(settingsFile);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (File.Exists(settingsFile))
                {
                    string theme = File.ReadAllText(settingsFile).Trim();
                    if (theme == "Light")
                    {
                        currentTheme = ThemeType.Light;
                    }
                    else if (theme == "Dark")
                    {
                        currentTheme = ThemeType.Dark;
                    }
                    else
                    {
                        currentTheme = ThemeType.Dark; // По умолчанию тёмная
                    }
                }
                else
                {
                    currentTheme = ThemeType.Dark; // По умолчанию тёмная
                }
            }
            catch
            {
                currentTheme = ThemeType.Dark; // По умолчанию тёмная
            }
        }

        // Сохранить тему в настройки
        public static void SaveTheme()
        {
            try
            {
                string directory = Path.GetDirectoryName(settingsFile);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(settingsFile, currentTheme.ToString());
            }
            catch
            {
                // Игнорируем ошибки сохранения
            }
        }

        // Тёмная тема
        public static class DarkTheme
        {
            public static Color Background = Color.FromArgb(45, 45, 48);
            public static Color BackgroundSecondary = Color.FromArgb(37, 37, 38);
            public static Color BackgroundPanel = Color.FromArgb(30, 30, 30);
            public static Color BackgroundDark = Color.FromArgb(20, 20, 20);
            public static Color BackgroundLight = Color.FromArgb(40, 40, 40);

            public static Color TextPrimary = Color.White;
            public static Color TextSecondary = Color.Gray;
            public static Color TextAccent = Color.FromArgb(0, 150, 255);
            public static Color TextSuccess = Color.FromArgb(0, 255, 127);
            public static Color TextWarning = Color.Yellow;
            public static Color TextError = Color.Red;
            public static Color TextInfo = Color.FromArgb(100, 200, 255);

            public static Color ButtonPrimary = Color.FromArgb(0, 150, 255);
            public static Color ButtonDanger = Color.FromArgb(220, 50, 50);
            public static Color ButtonSuccess = Color.FromArgb(100, 150, 100);
            public static Color ButtonSecondary = Color.FromArgb(100, 100, 200);
            public static Color ButtonWarning = Color.FromArgb(150, 100, 150);
            public static Color ButtonNeutral = Color.FromArgb(100, 100, 100);
        }

        // Светлая тема
        public static class LightTheme
        {
            public static Color Background = Color.FromArgb(240, 240, 240);
            public static Color BackgroundSecondary = Color.FromArgb(250, 250, 250);
            public static Color BackgroundPanel = Color.White;
            public static Color BackgroundDark = Color.FromArgb(245, 245, 245);
            public static Color BackgroundLight = Color.FromArgb(255, 255, 255);

            public static Color TextPrimary = Color.FromArgb(30, 30, 30);
            public static Color TextSecondary = Color.FromArgb(100, 100, 100);
            public static Color TextAccent = Color.FromArgb(0, 100, 200);
            public static Color TextSuccess = Color.FromArgb(0, 150, 50);
            public static Color TextWarning = Color.FromArgb(200, 150, 0);
            public static Color TextError = Color.FromArgb(200, 0, 0);
            public static Color TextInfo = Color.FromArgb(0, 120, 200);

            public static Color ButtonPrimary = Color.FromArgb(0, 120, 215);
            public static Color ButtonDanger = Color.FromArgb(200, 40, 40);
            public static Color ButtonSuccess = Color.FromArgb(80, 130, 80);
            public static Color ButtonSecondary = Color.FromArgb(80, 80, 180);
            public static Color ButtonWarning = Color.FromArgb(130, 80, 130);
            public static Color ButtonNeutral = Color.FromArgb(120, 120, 120);
        }

        public static ThemeType CurrentTheme
        {
            get { return currentTheme; }
            set
            {
                currentTheme = value;
                SaveTheme(); // Автоматически сохраняем при изменении темы
            }
        }

        public static Color GetBackground()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.Background : LightTheme.Background;
        }

        public static Color GetBackgroundSecondary()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.BackgroundSecondary : LightTheme.BackgroundSecondary;
        }

        public static Color GetBackgroundPanel()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.BackgroundPanel : LightTheme.BackgroundPanel;
        }

        public static Color GetBackgroundDark()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.BackgroundDark : LightTheme.BackgroundDark;
        }

        public static Color GetBackgroundLight()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.BackgroundLight : LightTheme.BackgroundLight;
        }

        public static Color GetTextPrimary()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.TextPrimary : LightTheme.TextPrimary;
        }

        public static Color GetTextSecondary()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.TextSecondary : LightTheme.TextSecondary;
        }

        public static Color GetTextAccent()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.TextAccent : LightTheme.TextAccent;
        }

        public static Color GetTextSuccess()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.TextSuccess : LightTheme.TextSuccess;
        }

        public static Color GetTextWarning()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.TextWarning : LightTheme.TextWarning;
        }

        public static Color GetTextError()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.TextError : LightTheme.TextError;
        }

        public static Color GetTextInfo()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.TextInfo : LightTheme.TextInfo;
        }

        public static Color GetButtonPrimary()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.ButtonPrimary : LightTheme.ButtonPrimary;
        }

        public static Color GetButtonDanger()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.ButtonDanger : LightTheme.ButtonDanger;
        }

        public static Color GetButtonSuccess()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.ButtonSuccess : LightTheme.ButtonSuccess;
        }

        public static Color GetButtonSecondary()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.ButtonSecondary : LightTheme.ButtonSecondary;
        }

        public static Color GetButtonWarning()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.ButtonWarning : LightTheme.ButtonWarning;
        }

        public static Color GetButtonNeutral()
        {
            return currentTheme == ThemeType.Dark ? DarkTheme.ButtonNeutral : LightTheme.ButtonNeutral;
        }

        public static void ApplyTheme(Form form)
        {
            form.BackColor = GetBackground();
            form.ForeColor = GetTextPrimary();
            ApplyThemeToControls(form.Controls);
        }

        // НОВЫЙ МЕТОД: Применение темы по тегам вместо текста кнопок
        public static void ApplyThemeByTags(Form form)
        {
            form.BackColor = GetBackground();
            form.ForeColor = GetTextPrimary();
            ApplyThemeToControlsByTags(form.Controls);
        }

        private static void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control ctrl in controls)
            {
                if (ctrl is Panel)
                {
                    Panel panel = (Panel)ctrl;
                    if (panel.Dock == DockStyle.Top || panel.Dock == DockStyle.Bottom)
                    {
                        panel.BackColor = GetBackgroundSecondary();
                    }
                    else
                    {
                        panel.BackColor = GetBackgroundPanel();
                    }
                    panel.ForeColor = GetTextPrimary();
                }
                else if (ctrl is TabControl)
                {
                    TabControl tab = (TabControl)ctrl;
                    tab.BackColor = GetBackgroundPanel();
                    tab.ForeColor = GetTextPrimary();
                }
                else if (ctrl is TabPage)
                {
                    TabPage page = (TabPage)ctrl;
                    page.BackColor = GetBackgroundPanel();
                    page.ForeColor = GetTextPrimary();
                }
                else if (ctrl is Label)
                {
                    Label lbl = (Label)ctrl;
                    if (lbl.Font.Bold && lbl.Font.Size >= 12)
                    {
                        // Заголовки остаются акцентными
                        if (lbl.ForeColor == DarkTheme.TextAccent || lbl.ForeColor == LightTheme.TextAccent)
                        {
                            lbl.ForeColor = GetTextAccent();
                        }
                        else if (lbl.ForeColor == DarkTheme.TextSuccess || lbl.ForeColor == LightTheme.TextSuccess)
                        {
                            lbl.ForeColor = GetTextSuccess();
                        }
                        else if (lbl.ForeColor == DarkTheme.TextInfo || lbl.ForeColor == LightTheme.TextInfo)
                        {
                            lbl.ForeColor = GetTextInfo();
                        }
                        else
                        {
                            lbl.ForeColor = GetTextPrimary();
                        }
                    }
                    else if (lbl.Font.Size <= 9 && !lbl.Font.Bold)
                    {
                        lbl.ForeColor = GetTextSecondary();
                    }
                    else
                    {
                        lbl.ForeColor = GetTextPrimary();
                    }
                }
                else if (ctrl is CheckBox)
                {
                    CheckBox cb = (CheckBox)ctrl;
                    cb.ForeColor = GetTextPrimary();
                }
                else if (ctrl is GroupBox)
                {
                    GroupBox gb = (GroupBox)ctrl;
                    gb.ForeColor = GetTextInfo();
                }
                else if (ctrl is TextBox)
                {
                    TextBox tb = (TextBox)ctrl;
                    if (tb.ReadOnly)
                    {
                        if (tb.Multiline && tb.Font.Name == "Consolas")
                        {
                            // Это лог-текстбокс
                            tb.BackColor = GetBackgroundDark();
                            tb.ForeColor = GetTextSuccess();
                        }
                        else
                        {
                            tb.BackColor = GetBackgroundLight();
                            tb.ForeColor = GetTextPrimary();
                        }
                    }
                    else
                    {
                        tb.BackColor = GetBackgroundLight();
                        tb.ForeColor = GetTextPrimary();
                    }
                }
                else if (ctrl is ListView)
                {
                    ListView lv = (ListView)ctrl;
                    lv.BackColor = GetBackgroundLight();
                    lv.ForeColor = GetTextPrimary();
                }
                else if (ctrl is ProgressBar)
                {
                    // ProgressBar не меняет цвета напрямую
                }
                else if (ctrl is Button)
                {
                    // Кнопки обрабатываются отдельно, чтобы сохранить их специфические цвета
                    Button btn = (Button)ctrl;
                    btn.ForeColor = Color.White;

                    // Определяем тип кнопки по тексту и применяем соответствующий цвет
                    // Поддержка русского, английского и украинского языков
                    string btnText = btn.Text.ToLower();

                    // PRIMARY (синий) - кнопки начала действия, выбора всего
                    string text = btnText.ToLowerInvariant();

                    string[] keywords =
                    {
                      "начать", "старт",
                      "cleanup", "start",
                      "выбрать всё", "выбрать все",
                      "select all",
                      "почати", "вибрати все",
                      "очищення"
                    };

                    if (btn.Tag is ButtonRole role)
                    {
                        switch (role)
                        {
                            case ButtonRole.Primary:
                                btn.BackColor = GetButtonPrimary();
                                break;
                            case ButtonRole.Danger:
                                btn.BackColor = GetButtonDanger();
                                break;
                            case ButtonRole.Success:
                                btn.BackColor = GetButtonSuccess();
                                break;
                            case ButtonRole.Secondary:
                                btn.BackColor = GetButtonSecondary();
                                break;
                            case ButtonRole.Warning:
                                btn.BackColor = GetButtonWarning();
                                break;
                            default:
                                btn.BackColor = GetButtonNeutral();
                                break;
                        }
                    }
                }
                    // Рекурсивно применяем к дочерним элементам
                    if (ctrl.Controls.Count > 0)
                {
                    ApplyThemeToControls(ctrl.Controls);
                }
            }
        }

        // НОВЫЙ МЕТОД: Применение темы к контролам используя теги
        private static void ApplyThemeToControlsByTags(Control.ControlCollection controls)
        {
            foreach (Control ctrl in controls)
            {
                if (ctrl is Panel)
                {
                    Panel panel = (Panel)ctrl;
                    if (panel.Dock == DockStyle.Top || panel.Dock == DockStyle.Bottom)
                    {
                        panel.BackColor = GetBackgroundSecondary();
                    }
                    else
                    {
                        panel.BackColor = GetBackgroundPanel();
                    }
                    panel.ForeColor = GetTextPrimary();
                }
                else if (ctrl is TabControl)
                {
                    TabControl tab = (TabControl)ctrl;
                    tab.BackColor = GetBackgroundPanel();
                    tab.ForeColor = GetTextPrimary();
                }
                else if (ctrl is TabPage)
                {
                    TabPage page = (TabPage)ctrl;
                    page.BackColor = GetBackgroundPanel();
                    page.ForeColor = GetTextPrimary();
                }
                else if (ctrl is Label)
                {
                    Label lbl = (Label)ctrl;
                    if (lbl.Font.Bold && lbl.Font.Size >= 12)
                    {
                        // Заголовки остаются акцентными
                        if (lbl.ForeColor == DarkTheme.TextAccent || lbl.ForeColor == LightTheme.TextAccent)
                        {
                            lbl.ForeColor = GetTextAccent();
                        }
                        else if (lbl.ForeColor == DarkTheme.TextSuccess || lbl.ForeColor == LightTheme.TextSuccess)
                        {
                            lbl.ForeColor = GetTextSuccess();
                        }
                        else if (lbl.ForeColor == DarkTheme.TextInfo || lbl.ForeColor == LightTheme.TextInfo)
                        {
                            lbl.ForeColor = GetTextInfo();
                        }
                        else
                        {
                            lbl.ForeColor = GetTextPrimary();
                        }
                    }
                    else if (lbl.Font.Size <= 9 && !lbl.Font.Bold)
                    {
                        lbl.ForeColor = GetTextSecondary();
                    }
                    else
                    {
                        lbl.ForeColor = GetTextPrimary();
                    }
                }
                else if (ctrl is CheckBox)
                {
                    CheckBox cb = (CheckBox)ctrl;
                    cb.ForeColor = GetTextPrimary();
                }
                else if (ctrl is GroupBox)
                {
                    GroupBox gb = (GroupBox)ctrl;
                    gb.ForeColor = GetTextInfo();
                }
                else if (ctrl is TextBox)
                {
                    TextBox tb = (TextBox)ctrl;
                    if (tb.ReadOnly)
                    {
                        if (tb.Multiline && tb.Font.Name == "Consolas")
                        {
                            // Это лог-текстбокс
                            tb.BackColor = GetBackgroundDark();
                            tb.ForeColor = GetTextSuccess();
                        }
                        else
                        {
                            tb.BackColor = GetBackgroundLight();
                            tb.ForeColor = GetTextPrimary();
                        }
                    }
                    else
                    {
                        tb.BackColor = GetBackgroundLight();
                        tb.ForeColor = GetTextPrimary();
                    }
                }
                else if (ctrl is ListView)
                {
                    ListView lv = (ListView)ctrl;
                    lv.BackColor = GetBackgroundLight();
                    lv.ForeColor = GetTextPrimary();
                }
                else if (ctrl is ProgressBar)
                {
                    // ProgressBar не меняет цвета напрямую
                }
                else if (ctrl is Button)
                {
                    Button btn = (Button)ctrl;
                    btn.ForeColor = Color.White;

                    // ИСПОЛЬЗУЕМ ТЕГИ ВМЕСТО ТЕКСТА для определения цвета кнопки
                    // Это решает проблему с изменением цвета при смене языка
                    if (btn.Tag != null && btn.Tag is string)
                    {
                        string tag = btn.Tag.ToString().ToLower();
                        
                        switch (tag)
                        {
                            case "primary":
                                btn.BackColor = GetButtonPrimary();
                                break;
                            case "danger":
                                btn.BackColor = GetButtonDanger();
                                break;
                            case "success":
                                btn.BackColor = GetButtonSuccess();
                                break;
                            case "secondary":
                                btn.BackColor = GetButtonSecondary();
                                break;
                            case "warning":
                                btn.BackColor = GetButtonWarning();
                                break;
                            case "neutral":
                                btn.BackColor = GetButtonNeutral();
                                break;
                            default:
                                btn.BackColor = GetButtonNeutral();
                                break;
                        }
                    }
                    else
                    {
                        // Если тег не установлен, используем нейтральный цвет
                        btn.BackColor = GetButtonNeutral();
                    }
                }

                // Рекурсивно применяем к дочерним элементам
                if (ctrl.Controls.Count > 0)
                {
                    ApplyThemeToControlsByTags(ctrl.Controls);
                }
            }
        }
    }
}
