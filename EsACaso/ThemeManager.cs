using System.Drawing;
using System.Windows.Forms;

namespace EsACaso
{
    public static class ThemeManager
    {
        public static void ApplyThemeToForm(Form form)
        {
            if (SettingsManager.DarkTheme)
                ApplyDark(form);
            else
                ApplyLight(form);
        }

        public static void ToggleTheme()
        {
            SettingsManager.DarkTheme = !SettingsManager.DarkTheme;
            foreach (Form f in Application.OpenForms)
                ApplyThemeToForm(f);
        }

        private static void ApplyDark(Form form)
        {
            form.BackColor = Color.FromArgb(15, 15, 15);
            form.ForeColor = Color.FromArgb(204, 204, 204);
        }

        private static void ApplyLight(Form form)
        {
            form.BackColor = Color.WhiteSmoke;
            form.ForeColor = Color.Black;
        }
    }
}
