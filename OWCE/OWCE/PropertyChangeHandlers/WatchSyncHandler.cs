using System;
using System.Collections.Generic;
using System.ComponentModel;
using OWCE.Converters;
using OWCE.DependencyInterfaces;
using Xamarin.Forms;

namespace OWCE.PropertyChangeHandlers
{
    public class WatchSyncHandler
    {
        private static readonly HashSet<String> _propertiesToWatch =
            new HashSet<string> { "BatteryPercent", "BatteryVoltage", "RPM", "TripOdometer" };

        public static readonly WatchSyncHandler Instance = new WatchSyncHandler();

        private Dictionary<WatchMessage, object> _watchUpdates = new Dictionary<WatchMessage, object>();

        // Updates the watch with the given property
        // - propertyName: null if updating all properties
        private void UpdateProperty(string propertyName, OWBoard board)
        {
            if (propertyName == null || propertyName.Equals("BatteryVoltage", StringComparison.Ordinal))
            {
                float voltage = board.BatteryVoltage;
                _watchUpdates[WatchMessage.Voltage] = voltage;

                // For Quart, should add battery percent here
            }

            if (propertyName == null || propertyName.Equals("RPM", StringComparison.Ordinal))
            {
                int rpm = board.RPM;
                int speed = (int)RpmToSpeedConverter.ConvertFromRpm(rpm, board.WheelCircumference);
                _watchUpdates[WatchMessage.Speed] = speed;
            }

            if (propertyName == null || propertyName.Equals("BatteryPercent", StringComparison.Ordinal))
            {
                int batteryPercent = board.BatteryPercent;
                _watchUpdates[WatchMessage.BatteryPercent] = batteryPercent;
            }

            if (propertyName == null || propertyName.Equals("TripOdometer", StringComparison.Ordinal))
            {
                ushort tripOdometer = board.TripOdometer;
                string tripDescription = RotationsToDistanceConverter.ConvertRotationsToDistance(tripOdometer, board.WheelCircumference);
                _watchUpdates[WatchMessage.Distance] = tripDescription;
            }

            if (propertyName == null)
            {
                _watchUpdates[WatchMessage.SpeedUnitsLabel] = App.Current.MetricDisplay ? "km/h" : "mph";
            }

            // TODO: In future, consider calling FlushMessages() after a delay
            // to accumulate more messages and reduce the traffic to the watch.
            FlushMessages();
        }

        // Sends all outstanding updates to the watch, and reset the update Dictionary
        private void FlushMessages()
        {
            Dictionary<WatchMessage, object> updates = _watchUpdates;
            _watchUpdates = new Dictionary<WatchMessage, object>();

            IWatch watchService = DependencyService.Get<IWatch>();
            watchService.SendWatchMessages(updates);
        }

        // Invoked when the OWBoard has properties changed (eg Speed, Voltage) that we need
        // to update the watch on
        public static void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (!_propertiesToWatch.Contains(e.PropertyName)) { return; }

                Instance.UpdateProperty(e.PropertyName, (sender as OWBoard));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception Handling Watch Property Change: {ex.Message}");
                //(sender as OWBoard).ErrorMessage = $"Exception Handling Watch Property Change: {ex.Message}";
            }

        }

        // Invoked when the watch sends messages to the phone (eg when the watch wakes up)
        public static void HandleWatchMessage(Dictionary<WatchMessage, object> message, OWBoard board)
        {
            try
            {
                if (message.ContainsKey(WatchMessage.Awake))
                {
                    if (board == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Board not initialized yet. Returning");
                        return;
                    }
                    // Watch just woke up -- send all current data to bring
                    // the watch up to speed
                    Instance.UpdateProperty(null, board);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception Handling Watch Message: {ex.Message}");
            }
        }
    }
}
