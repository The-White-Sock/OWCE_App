using System;
using System.Collections.Generic;
using OWCE.Views;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace OWCE.Pages
{
    public partial class AppSettingsPage : BaseContentPage
    {
        const string FollowSystemOption = "Follow System";
        const string LightOption = "Light";
        const string DarkOption = "Dark";

        public bool MetricDisplay { get; set; }
        public bool AutoRideRecording { get; set; }

        public IReadOnlyList<string> ThemeOptions { get; } = new[] { FollowSystemOption, LightOption, DarkOption };

        public string SelectedThemeOption { get; set; }

        public AppSettingsPage()
        {
            InitializeComponent();

            MetricDisplay = App.Current.MetricDisplay;
            AutoRideRecording = Preferences.Get("auto_ride_recording", false);

            SelectedThemeOption = ThemeHelper.LoadPersistedTheme() switch
            {
                OSAppTheme.Light => LightOption,
                OSAppTheme.Dark => DarkOption,
                _ => FollowSystemOption,
            };

            CustomToolbarItems.Add(new CustomToolbarItem()
            {
                Position = CustomToolbarItemPosition.Left,
                Text = "Cancel",
                Command = new AsyncCommand(async () =>
                {
                    await Navigation.PopModalAsync();
                }, allowsMultipleExecutions: false),
            });


            CustomToolbarItems.Add(new CustomToolbarItem()
            {
                Position = CustomToolbarItemPosition.Right,
                Text = "Save",
                Command = new AsyncCommand(async () =>
                {
                    App.Current.MetricDisplay = MetricDisplay;

                    Preferences.Set("metric_display", MetricDisplay);
                    Preferences.Set("auto_ride_recording", AutoRideRecording);

                    var selectedTheme = SelectedThemeOption switch
                    {
                        LightOption => OSAppTheme.Light,
                        DarkOption => OSAppTheme.Dark,
                        _ => OSAppTheme.Unspecified,
                    };
                    ThemeHelper.PersistTheme(selectedTheme);
                    Application.Current.UserAppTheme = selectedTheme;

                    MessagingCenter.Send<App>(App.Current, App.UnitDisplayUpdatedKey);

                    await Navigation.PopModalAsync();
                }, allowsMultipleExecutions: false),
            });

            BindingContext = this;

        }
    }
}

