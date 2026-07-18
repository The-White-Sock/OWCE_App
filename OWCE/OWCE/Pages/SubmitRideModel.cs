using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using OWCE.Network;
using Xamarin.CommunityToolkit.ObjectModel;

namespace OWCE.Pages
{
    public class SubmitRideModel : Xamarin.CommunityToolkit.ObjectModel.ObservableObject
    {
        string _rideName = String.Empty;
        public string RideName
        {
            get => _rideName;
            set => SetProperty(ref _rideName, value);
        }

        bool _isAftermarketBattery;
        public bool IsAftermarketBattery
        {
            get => _isAftermarketBattery;
            set => SetProperty(ref _isAftermarketBattery, value);
        }

        string _batteryType = String.Empty;
        public string BatteryType
        {
            get => _batteryType;
            set => SetProperty(ref _batteryType, value);
        }

        bool _removeIdentifiers = true;
        public bool RemoveIdentifiers
        {
            get => _removeIdentifiers;
            set => SetProperty(ref _removeIdentifiers, value);
        }

        bool _allowPublicly;
        public bool AllowPublicly
        {
            get => _allowPublicly;
            set => SetProperty(ref _allowPublicly, value);
        }

        string _additionalNotes = String.Empty;
        public string AdditionalNotes
        {
            get => _additionalNotes;
            set => SetProperty(ref _additionalNotes, value);
        }

        AsyncRelayCommand _viewDataSubmittedCommand;
        public AsyncRelayCommand ViewDataSubmittedCommand => _viewDataSubmittedCommand ??= new AsyncRelayCommand(ViewDataSubmittedAsync);

        WeakReference<SubmitRidePage> _page;

        public SubmitRideModel(SubmitRidePage page)
        {
            _page = new WeakReference<SubmitRidePage>(page);
        }

        async Task ViewDataSubmittedAsync()
        {
            if (_page.TryGetTarget(out SubmitRidePage page))
            {
                await page.ViewDataSubmittedAsync();
            }
        }

        internal SubmitRideRequest GetSubmitRideRequest()
        {
            return new SubmitRideRequest()
            {
                RideName = RideName,
                AftermarketBattery = IsAftermarketBattery,
                BatteryType = BatteryType,
                RemoveIdentifiers = RemoveIdentifiers,
                AllowPublicly = AllowPublicly,
                AdditionalNotes = AdditionalNotes,
            };
        }
    }
}

