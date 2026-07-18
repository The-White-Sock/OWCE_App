using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;
using WatchConnectivity;

namespace OWCE.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication uiApplication, NSDictionary launchOptions)
        {
            // Crash/error telemetry (see #26) - initialize before anything else so as
            // much of startup as possible is covered. No-op when AppConstants.SentryDsn
            // is blank (the default, until a Sentry project DSN is configured).
            //
            // Not yet verified on real hardware/a Mac build (this environment can't build
            // or run iOS at all - see #20) - only that it follows the documented Sentry.Xamarin
            // setup and compiles conceptually the same way as the Android side.
            if (String.IsNullOrEmpty(AppConstants.SentryDsn) == false)
            {
                Sentry.SentryXamarin.Init(options =>
                {
                    options.Dsn = AppConstants.SentryDsn;
                    options.AddXamarinFormsIntegration();
                });
            }

            Rg.Plugins.Popup.Popup.Init();

            global::Xamarin.Forms.Forms.Init();
            LoadApplication(new App());

            // Apple Watch session manager
            WCSessionManager.SharedManager.StartSession();

            return base.FinishedLaunching(uiApplication, launchOptions);
        }
    }
}
