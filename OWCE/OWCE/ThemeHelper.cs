using Xamarin.Essentials;
using Xamarin.Forms;

namespace OWCE
{
    // Colors set directly in C# (rather than XAML, where {AppThemeBinding ...} already
    // reacts to theme changes automatically) don't have an equivalent built-in helper -
    // this picks the right one for whatever the app's effective theme is right now (see
    // #35). Used at the point a control is constructed; most callers of this are
    // recreated whenever their containing page is (eg a fresh BoardPage per connection),
    // so this naturally stays correct without needing to track live theme changes too.
    public static class ThemeHelper
    {
        public const string ThemePreferenceKey = "app_theme";

        public static Color Pick(Color light, Color dark)
        {
            return Application.Current.RequestedTheme == OSAppTheme.Dark ? dark : light;
        }

        // "system" (the default) maps to Unspecified, which tells Xamarin.Forms to
        // follow the OS-level setting rather than pin to one theme.
        public static OSAppTheme LoadPersistedTheme()
        {
            return Preferences.Get(ThemePreferenceKey, "system") switch
            {
                "light" => OSAppTheme.Light,
                "dark" => OSAppTheme.Dark,
                _ => OSAppTheme.Unspecified,
            };
        }

        public static void PersistTheme(OSAppTheme theme)
        {
            Preferences.Set(ThemePreferenceKey, theme switch
            {
                OSAppTheme.Light => "light",
                OSAppTheme.Dark => "dark",
                _ => "system",
            });
        }
    }
}
