using System.Globalization;
using System.Threading;
using CleanupTempPro.Properties;

namespace CleanupTempPro
{
    public static class LocalizationHelper
    {
        public static void LoadSavedLanguage()
        {
            string savedLang = Settings.Default.Language;
            if (!string.IsNullOrEmpty(savedLang))
            {
                SetLanguage(savedLang);
            }
        }

        public static void SetLanguage(string languageCode)
        {
            try
            {
                CultureInfo culture = new CultureInfo(languageCode);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                // Сохранить выбор
                Settings.Default.Language = languageCode;
                Settings.Default.Save();
            }
            catch (CultureNotFoundException)
            {
                // Если язык не найден, использовать английский
                CultureInfo culture = new CultureInfo("en-US");
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                Settings.Default.Language = "en-US";
                Settings.Default.Save();
            }
        }

        public static string GetCurrentLanguage()
        {
            return Thread.CurrentThread.CurrentUICulture.Name;
        }

        public static string GetLanguageDisplayName(string languageCode)
        {
            return languageCode switch
            {
                "uk-UA" => "Українська",
                "ru-RU" => "Русский",
                "en-US" => "English",
                _ => "English"
            };
        }
    }
}