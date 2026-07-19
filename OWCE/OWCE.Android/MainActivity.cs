using System;

using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Plugin.CurrentActivity;

namespace OWCE.Droid
{
    [Activity(Label = "OWCE", Icon = "@mipmap/ic_launcher", Theme = "@style/MainTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        //public static int BLE_REQUEST_CODE = 230948;
        public static int REQUEST_ENABLE_BT = 3;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            // Crash/error telemetry (see #26) - initialize before anything else so as
            // much of startup as possible is covered. No-op when AppConstants.SentryDsn
            // is blank (the default, until a Sentry project DSN is configured).
            if (String.IsNullOrEmpty(AppConstants.SentryDsn) == false)
            {
                Sentry.SentryXamarin.Init(options =>
                {
                    options.Dsn = AppConstants.SentryDsn;
                });

                // Belt-and-braces alongside SentryXamarin.Init()'s own automatic wiring -
                // there are real reports of unhandled Android exceptions not reliably
                // reaching Sentry through that alone (getsentry/sentry-dotnet#122).
                AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
                {
                    Sentry.SentrySdk.CaptureException(args.Exception);
                };
            }

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;


            base.OnCreate(savedInstanceState);

            Rg.Plugins.Popup.Popup.Init(this);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());

            // MainTheme (styles.xml) is a fixed AppCompat.Light theme with no day/night
            // handling, so the OS-drawn system navigation bar (the bar with the
            // back/home/recents buttons, distinct from anything Xamarin.Forms draws)
            // always rendered light regardless of the in-app theme (#35) - this keeps
            // it in sync, both at startup and whenever the theme changes afterwards
            // (Settings' Light/Dark/Follow System picker, or an OS-level change while
            // following system).
            ApplySystemNavigationBarTheme();
            global::Xamarin.Forms.Application.Current.RequestedThemeChanged += (sender, e) => ApplySystemNavigationBarTheme();
        }

        void ApplySystemNavigationBarTheme()
        {
            bool isDark = global::Xamarin.Forms.Application.Current.RequestedTheme == global::Xamarin.Forms.OSAppTheme.Dark;

            Window.SetNavigationBarColor(isDark ? Color.ParseColor("#1C2D41") : Color.ParseColor("#E6E6E6"));

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                int uiOptions = (int)Window.DecorView.SystemUiVisibility;
                uiOptions = isDark
                    ? uiOptions & ~(int)SystemUiFlags.LightNavigationBar
                    : uiOptions | (int)SystemUiFlags.LightNavigationBar;
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public override void OnBackPressed()
        {
            if (Rg.Plugins.Popup.Popup.SendBackPressed(base.OnBackPressed))
            {
                // Do something if there are some pages in the `PopupStack`
            }
            else
            {
                // Do something if there are not any pages in the `PopupStack`
            }
        }
    }
}

