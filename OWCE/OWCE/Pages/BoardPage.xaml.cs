using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using OWCE.DependencyInterfaces;
using OWCE.Pages.Popup;
using OWCE.Views;
using Rg.Plugins.Popup.Services;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace OWCE.Pages
{
    public partial class BoardPage : BaseContentPage
    {
        ConnectingAlert _reconnectingAlert;
        bool _isTornDown;


        public OWBoard Board { get; private set; }
        /*
        public string SpeedHeader
        {
            get
            {
                var unit = App.Current.MetricDisplay ? "km/h" : "mph";
                return unit; // return $"Speed ({unit})";
            }
        }*/

        Grid _sideMenuItem;

        IAsyncCommand _startRecordRideCommand;
        public IAsyncCommand StartRecordRideCommand => _startRecordRideCommand ??= new AsyncCommand(StartRecordingAsync, allowsMultipleExecutions: false);

        IAsyncCommand _stopRecordRideCommand;
        public IAsyncCommand StopRecordRideCommand => _stopRecordRideCommand ??= new AsyncCommand(StopRecordingAsync, allowsMultipleExecutions: false);



        public BoardPage(OWBoard board) : base()
        {
            Board = board;

            InitializeComponent();
            BindingContext = board;

            AppVersionLabel.Text = $"{AppInfo.VersionString} (build {AppInfo.BuildString})";

            // TODO: Fix ImperialSwitch.IsToggled = !App.Current.MetricDisplay;


            Board.Init();
            // I really don't like this.
            _ = Board.SubscribeToBLE();

            App.Current.OWBLE.BoardDisconnected += OWBLE_BoardDisconnected;
            App.Current.OWBLE.BoardReconnecting += OWBLE_BoardReconnecting;
            App.Current.OWBLE.BoardReconnected += OWBLE_BoardReconnected;
            App.Current.OWBLE.BoardReconnectFailed += OWBLE_BoardReconnectFailed;

            // Shift title to the right.
            Label titleLabel = GetTitleLabel();
            titleLabel.HorizontalOptions = LayoutOptions.End;
            titleLabel.Padding = new Thickness(0, 0, 16, 0);


            var sideMenuItem = new CustomToolbarItem()
            {
                Position = CustomToolbarItemPosition.Left,
                IconImageSource = "burger_menu",
                Command = new AsyncCommand(async () =>
                {
                    await PopupNavigation.Instance.PushAsync(Popup.SideMenuPopup.Instance);
                }, allowsMultipleExecutions: false),
            };
            CustomToolbarItems.Add(sideMenuItem);

        }

        private void OWBLE_BoardDisconnected()
        {
            System.Diagnostics.Debug.WriteLine("OWBLE_BoardDisconnected");
        }

        private void OWBLE_BoardReconnecting()
        {
            System.Diagnostics.Debug.WriteLine("OWBLE_BoardReconnecting");

            // This fires on every retry attempt (every couple of seconds while the
            // board is unreachable), not just once. Previously this created and
            // pushed a brand new popup instance every single time, stacking dozens
            // of them on top of each other the longer a reconnect took - each with
            // its own semi-transparent overlay, which is why the screen behind them
            // would eventually read as solid black. Only show one at a time.
            if (_reconnectingAlert != null)
            {
                return;
            }

            _reconnectingAlert = new ConnectingAlert(Board.Name, new Command(async () =>
            {
                // Previously a no-op TODO - Cancel dismissed this popup's UI but
                // never actually stopped the underlying auto-reconnect loop, which
                // just kept retrying and re-showing a new popup a couple seconds
                // later regardless. Give up on reconnecting for real instead.
                await GiveUpAndDisconnect();
            }), "Reconnecting...");

            PopupNavigation.Instance.PushAsync(_reconnectingAlert, true);
        }


        private void OWBLE_BoardReconnected()
        {
            System.Diagnostics.Debug.WriteLine("OWBLE_BoardReconnected");

            if (PopupNavigation.Instance.PopupStack.Contains(_reconnectingAlert))
            {
                PopupNavigation.Instance.RemovePageAsync(_reconnectingAlert);
                _reconnectingAlert = null;
            }
        }

        // Fires once the OWBLE layer itself gives up retrying (see
        // ReconnectGiveUpAfter) - previously the "Reconnecting..." popup would
        // otherwise sit there forever if the user never pressed Cancel themselves,
        // since the underlying retry loop had no automatic cutoff at all.
        private async void OWBLE_BoardReconnectFailed()
        {
            System.Diagnostics.Debug.WriteLine("OWBLE_BoardReconnectFailed");

            await GiveUpAndDisconnect();
        }

        private async Task GiveUpAndDisconnect()
        {
            if (PopupNavigation.Instance.PopupStack.Any())
            {
                await PopupNavigation.Instance.PopAllAsync();
            }
            _reconnectingAlert = null;
            await DisconnectAndPop();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            Popup.SideMenuPopup.Instance.Title = "OWCE";

            if (_sideMenuItem == null)
            {
                var dataTemplate = (DataTemplate)Resources["SideMenu"];
                _sideMenuItem = dataTemplate.CreateContent() as Grid;
                _sideMenuItem.BindingContext = this;
            }
            Popup.SideMenuPopup.Instance.PageSpecificSideMenu = _sideMenuItem;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            /*
            if (GaugeAbsolueLayout.WidthRequest.AlmostEqualTo(width) == false)
            {
                GaugeAbsolueLayout.WidthRequest = width;
                GaugeAbsolueLayout.HeightRequest = width;
                GaugeAbsolueLayout.MinimumWidthRequest = width;
                GaugeAbsolueLayout.MinimumHeightRequest = width;
            }
            */
        }

        protected override bool OnBackButtonPressed()
        {
            Disconnect_Tapped(null, EventArgs.Empty);
            //DisconnectAndPop();
            return true;
        }

        async void Disconnect_Tapped(System.Object sender, System.EventArgs e)
        {
            string result = await DisplayActionSheet("Are you sure you want to disconnect?", "Cancel", "Disconnect");

            if (result == "Disconnect")
            {
                if (PopupNavigation.Instance.PopupStack.Any())
                {
                    await PopupNavigation.Instance.PopAllAsync();
                }
                await DisconnectAndPop();
            }
        }

        bool _isDisconnecting;

        public async Task DisconnectAndPop()
        {
            if (_isDisconnecting)
            {
                // Already in progress - reachable concurrently from the user's
                // Disconnect action, the reconnect popup's own Cancel command, and
                // OWBLE's independent reconnect-give-up watchdog (BoardReconnectFailed)
                // landing at nearly the same time. Without this guard, a second call
                // here would call Navigation.PopModalAsync() twice on the same modal.
                return;
            }
            _isDisconnecting = true;

            await App.Current.OWBLE.Disconnect();

            Board.SaveCachedData();
            Board.StopLogging();
            TearDown();

            await Navigation.PopModalAsync();

            IWatch watchService = DependencyService.Get<IWatch>();

            watchService.StopListeningForWatchMessages();
        }

        // Removes this page's subscriptions on the (app-lifetime) OWBLE singleton, and
        // the board's own subscriptions/timers. Without this, every connect/disconnect
        // cycle would leak a full BoardPage + OWBoard instance and leave their handlers
        // (and OWBoard's RSSI polling timer) running forever.
        void TearDown()
        {
            if (_isTornDown)
            {
                return;
            }

            _isTornDown = true;

            App.Current.OWBLE.BoardDisconnected -= OWBLE_BoardDisconnected;
            App.Current.OWBLE.BoardReconnecting -= OWBLE_BoardReconnecting;
            App.Current.OWBLE.BoardReconnected -= OWBLE_BoardReconnected;
            App.Current.OWBLE.BoardReconnectFailed -= OWBLE_BoardReconnectFailed;

            Board.Teardown();
        }


        static void ImperialSwitch_IsToggledChanged(object _, bool isToggled)
        {
            App.Current.MetricDisplay = !isToggled;
            Preferences.Set("metric_display", !isToggled);

            MessagingCenter.Send<App>(App.Current, App.UnitDisplayUpdatedKey);
        }

        async Task StartRecordingAsync()
        {
            await Popup.SideMenuPopup.Instance.CloseCommand_Clicked();
            Board.StartLogging();
        }

        async Task StopRecordingAsync()
        {
            await Popup.SideMenuPopup.Instance.CloseCommand_Clicked();
            Board.StopLogging();
        }
    }
}
