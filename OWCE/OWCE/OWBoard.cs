using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using MvvmHelpers;
using OWCE.DependencyInterfaces;
using OWCE.Models;
using OWCE.Network;
using OWCE.PropertyChangeHandlers;
using OWCE.Protobuf;
using Rg.Plugins.Popup.Services;
//using Plugin.Geolocator;
//using Plugin.Geolocator.Abstractions;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace OWCE
{
    public enum OWBoardType
    {
        Unknown,
        V1,
        Plus,
        XR,
        Pint,
        PintX,
        GT
    };

    public struct RideModes
    {
        public const int V1Classic = 1;
        public const int V1Extreme = 2;
        public const int V1Elevated = 3;

        public const int PlusXRSequoia = 4;
        public const int PlusXRCruz = 5;
        public const int PlusXRMission = 6;
        public const int PlusXRElevated = 7;
        public const int PlusXRDelirium = 8;
        public const int PlusXRCustom = 9;

        // Same value/name as PlusXRSequoia - confirmed via the rewheel project's
        // mode table (see #36). OWCE's own UI never offers this as a selectable
        // Pint mode, but the board can in principle report it.
        public const int PintSequoia = 4;
        public const int PintRedwood = 5;
        public const int PintPacific = 6;
        public const int PintElevated = 7;
        public const int PintSkyline = 8;

        public const int GTBay = 3;
        public const int GTRoam = 4;
        public const int GTFlow = 5;
        public const int GTHighLine = 6;
        public const int GTElevated = 7;
        public const int GTApex = 8;
        public const int GTCustom = 9;


    }

    public class OWBoard : OWBaseBoard
    {
        public static readonly Guid ServiceUUID = new Guid("E659F300-EA98-11E3-AC10-0800200C9A66");
        public const string SerialNumberUUID = "E659F301-EA98-11E3-AC10-0800200C9A66";
        public const string RideModeUUID = "E659F302-EA98-11E3-AC10-0800200C9A66";
        public const string BatteryPercentUUID = "E659F303-EA98-11E3-AC10-0800200C9A66";
        public const string BatteryLow5UUID = "E659F304-EA98-11E3-AC10-0800200C9A66";
        public const string BatteryLow20UUID = "E659F305-EA98-11E3-AC10-0800200C9A66";
        public const string BatterySerialUUID = "E659F306-EA98-11E3-AC10-0800200C9A66";
        public const string PitchUUID = "E659F307-EA98-11E3-AC10-0800200C9A66";
        public const string RollUUID = "E659F308-EA98-11E3-AC10-0800200C9A66";
        public const string YawUUID = "E659F309-EA98-11E3-AC10-0800200C9A66";
        public const string TripOdometerUUID = "E659F30A-EA98-11E3-AC10-0800200C9A66";
        public const string RpmUUID = "E659F30B-EA98-11E3-AC10-0800200C9A66";
        public const string LightModeUUID = "E659F30C-EA98-11E3-AC10-0800200C9A66";
        public const string LightsFrontUUID = "E659F30D-EA98-11E3-AC10-0800200C9A66";
        public const string LightsBackUUID = "E659F30E-EA98-11E3-AC10-0800200C9A66";
        public const string StatusErrorUUID = "E659F30F-EA98-11E3-AC10-0800200C9A66";
        public const string TemperatureUUID = "E659F310-EA98-11E3-AC10-0800200C9A66";
        public const string FirmwareRevisionUUID = "E659F311-EA98-11E3-AC10-0800200C9A66";
        public const string CurrentAmpsUUID = "E659F312-EA98-11E3-AC10-0800200C9A66";
        public const string TripAmpHoursUUID = "E659F313-EA98-11E3-AC10-0800200C9A66";
        public const string TripRegenAmpHoursUUID = "E659F314-EA98-11E3-AC10-0800200C9A66";
        public const string BatteryTemperatureUUID = "E659F315-EA98-11E3-AC10-0800200C9A66";
        public const string BatteryVoltageUUID = "E659F316-EA98-11E3-AC10-0800200C9A66";
        public const string SafetyHeadroomUUID = "E659F317-EA98-11E3-AC10-0800200C9A66";
        public const string HardwareRevisionUUID = "E659F318-EA98-11E3-AC10-0800200C9A66";
        public const string LifetimeOdometerUUID = "E659F319-EA98-11E3-AC10-0800200C9A66";
        public const string LifetimeAmpHoursUUID = "E659F31A-EA98-11E3-AC10-0800200C9A66";
        public const string BatteryCellsUUID = "E659F31B-EA98-11E3-AC10-0800200C9A66";
        public const string LastErrorCodeUUID = "E659F31C-EA98-11E3-AC10-0800200C9A66";
        public const string SerialReadUUID = "E659F3FE-EA98-11E3-AC10-0800200C9A66";
        public const string SerialWriteUUID = "E659F3FF-EA98-11E3-AC10-0800200C9A66";
        public const string UNKNOWN1UUID = "E659F31D-EA98-11E3-AC10-0800200C9A66";
        public const string UNKNOWN2UUID = "E659F31E-EA98-11E3-AC10-0800200C9A66";
        public const string UNKNOWN3UUID = "E659F31F-EA98-11E3-AC10-0800200C9A66";
        public const string UNKNOWN4UUID = "E659F320-EA98-11E3-AC10-0800200C9A66";

        private int _serialNumber;
        public int SerialNumber
        {
            get { return _serialNumber; }
            set { if (_serialNumber != value) { _serialNumber = value; OnPropertyChanged(); } }
        }

        private int _batteryPercent;
        public int BatteryPercent
        {
            get { return _batteryPercent; }
            set { if (_batteryPercent != value) { _batteryPercent = value; OnPropertyChanged(); } }
        }

        private int _batteryLow5;
        public int BatteryLow5
        {
            get { return _batteryLow5; }
            set { if (_batteryLow5 != value) { _batteryLow5 = value; OnPropertyChanged(); } }
        }

        private int _batteryLow20;
        public int BatteryLow20
        {
            get { return _batteryLow20; }
            set { if (_batteryLow20 != value) { _batteryLow20 = value; OnPropertyChanged(); } }
        }

        private int _batterySerial;
        public int BatterySerial
        {
            get { return _batterySerial; }
            set { if (_batterySerial != value) { _batterySerial = value; OnPropertyChanged(); } }
        }

        float _pitch;
        public float Pitch
        {
            get { return _pitch; }
            set { if (_pitch.AlmostEqualTo(value) == false) { _pitch = value; OnPropertyChanged(); } }
        }

        float _yaw;
        public float Yaw
        {
            get { return _yaw; }
            set { if (_yaw.AlmostEqualTo(value) == false) { _yaw = value; OnPropertyChanged(); } }
        }

        float _roll;
        public float Roll
        {
            get { return _roll; }
            set { if (_roll.AlmostEqualTo(value) == false) { _roll = value; OnPropertyChanged(); } }
        }

        /*
        float _yyy;
        public float YYY
        {
            get { return _yyy; }
            set { if (_yyy.AlmostEqualTo(value) == false) { _yyy = value; OnPropertyChanged(); } }
        }

        private int _xxx;
        public int XXX
        {
            get { return _xxx; }
            set { if (_xxx != value) { _xxx = value; OnPropertyChanged(); } }
        }
        */



        private ushort _tripOdometer;
        public ushort TripOdometer
        {
            get { return _tripOdometer; }
            set { if (_tripOdometer != value) { _tripOdometer = value; OnPropertyChanged(); OnPropertyChanged(nameof(TripOdometerDescription)); } }
        }

        // Trip distance converted from rotations using this board's actual wheel
        // circumference. See SpeedValue for why this is a plain computed property
        // instead of a ConverterParameter-based binding.
        public string TripOdometerDescription => Converters.RotationsToDistanceConverter.ConvertRotationsToDistance(TripOdometer, WheelCircumference);

        private int _statusError;
        public int StatusError
        {
            get { return _statusError; }
            set { if (_statusError != value) { _statusError = value; OnPropertyChanged(); } }
        }

        float _controllerTemperature;
        public float ControllerTemperature
        {
            get { return _controllerTemperature; }
            set { if (_controllerTemperature.AlmostEqualTo(value) == false) { _controllerTemperature = value; OnPropertyChanged(); } }
        }


        float _motorTemperature;
        public float MotorTemperature
        {
            get { return _motorTemperature; }
            set { if (_motorTemperature.AlmostEqualTo(value) == false) { _motorTemperature = value; OnPropertyChanged(); } }
        }

        private int _firmwareRevision;
        public int FirmwareRevision
        {
            get { return _firmwareRevision; }
            set { if (_firmwareRevision != value) { _firmwareRevision = value; OnPropertyChanged(); } }
        }

        float _currentAmps;
        public float CurrentAmps
        {
            get { return _currentAmps; }
            set { if (_currentAmps.AlmostEqualTo(value) == false) { _currentAmps = value; OnPropertyChanged(); } }
        }

        bool _isRegen;
        public bool IsRegen
        {
            get { return _isRegen; }
            set { if (_isRegen != value) { _isRegen = value; OnPropertyChanged(); } }
        }


        float _tripAmpHours;
        public float TripAmpHours
        {
            get { return _tripAmpHours; }
            set { if (_tripAmpHours.AlmostEqualTo(value) == false) { _tripAmpHours = value; OnPropertyChanged(); } }
        }

        float _tripRegenAmpHours;
        public float TripRegenAmpHours
        {
            get { return _tripRegenAmpHours; }
            set { if (_tripRegenAmpHours.AlmostEqualTo(value) == false) { _tripRegenAmpHours = value; OnPropertyChanged(); } }
        }

        float _batteryTemperature;
        public float BatteryTemperature
        {
            get { return _batteryTemperature; }
            set { if (_batteryTemperature.AlmostEqualTo(value) == false) { _batteryTemperature = value; OnPropertyChanged(); } }
        }

        float _batteryVoltage;
        public float BatteryVoltage
        {
            get { return _batteryVoltage; }
            set { if (_batteryVoltage.AlmostEqualTo(value) == false) { _batteryVoltage = value; OnPropertyChanged(); } }
        }

        private int _safetyHeadroom;
        public int SafetyHeadroom
        {
            get { return _safetyHeadroom; }
            set { if (_safetyHeadroom != value) { _safetyHeadroom = value; OnPropertyChanged(); } }
        }

        float _lifetimeOdometer;
        public float LifetimeOdometer
        {
            get { return _lifetimeOdometer; }
            set { if (_lifetimeOdometer.AlmostEqualTo(value) == false) { _lifetimeOdometer = value; OnPropertyChanged(); } }
        }

        float _lifetimeAmpHours;
        public float LifetimeAmpHours
        {
            get { return _lifetimeAmpHours; }
            set { if (_lifetimeAmpHours.AlmostEqualTo(value) == false) { _lifetimeAmpHours = value; OnPropertyChanged(); } }
        }

        float _lastErrorCode;
        public float LastErrorCode
        {
            get { return _lastErrorCode; }
            set { if (_lastErrorCode.AlmostEqualTo(value) == false) { _lastErrorCode = value; OnPropertyChanged(); } }
        }

        readonly BatteryCells _batteryCells = new BatteryCells();
        public BatteryCells BatteryCells
        {
            get { return _batteryCells; }
        }

        private int _rpm;
        public int RPM
        {
            get { return _rpm; }
            set { if (_rpm != value) { _rpm = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpeedValue)); } }
        }

        // Speed converted from RPM using this board's actual wheel circumference.
        // Exposed as a plain computed property (rather than binding through
        // RpmToSpeedConverter with a WheelCircumference ConverterParameter) because
        // Xamarin.Forms' ConverterParameter cannot carry a live {Binding} value - it's
        // a plain object property, not a BindableProperty, so a nested {Binding} there
        // never actually resolves.
        public float SpeedValue => Converters.RpmToSpeedConverter.ConvertFromRpm(RPM, WheelCircumference);

        // Value is stored in meters per second.
        float _speed;
        public float Speed
        {
            get { return _speed; }
            set { if (_speed.AlmostEqualTo(value) == false) { _speed = value; OnPropertyChanged(); } }
        }

        private ushort _hardwareRevision;
        public ushort HardwareRevision
        {
            get { return _hardwareRevision; }
            set { if (_hardwareRevision != value) { _hardwareRevision = value; OnPropertyChanged(); } }
        }

        private ushort _rideMode;
        public ushort RideMode
        {
            get { return _rideMode; }
            set { if (_rideMode != value) { _rideMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(RideModeString)); } }
        }

        private ushort _incomingRideMode;
        public ushort IncomingRideMode
        {
            get { return _incomingRideMode; }
            set { if (_incomingRideMode != value) { _incomingRideMode = value; OnPropertyChanged(); } }
        }

        public string RideModeString
        {
            get
            {
                if (BoardType == OWBoardType.V1)
                {
                    return _rideMode switch
                    {
                        1 => "Classic",
                        2 => "Extreme",
                        3 => "Elevated",
                        _ => "Unknown",
                    };
                }
                else if (BoardType == OWBoardType.Plus || BoardType == OWBoardType.XR)
                {
                    return _rideMode switch
                    {
                        4 => "Sequoia",
                        5 => "Cruz",
                        6 => "Mission",
                        7 => "Elevated",
                        8 => "Delirium",
                        9 => "Custom",
                        _ => "Unknown",
                    };
                }
                else if (BoardType == OWBoardType.Pint || BoardType == OWBoardType.PintX)
                {
                    return _rideMode switch
                    {
                        4 => "Sequoia",
                        5 => "Redwood",
                        6 => "Pacific",
                        7 => "Elevated",
                        8 => "Skyline",
                        _ => "Unknown",
                    };
                }
                else if (BoardType == OWBoardType.GT)
                {
                    return _rideMode switch
                    {
                        3 => "Bay",
                        4 => "Roam",
                        5 => "Flow",
                        6 => "Highline",
                        7 => "Elevated",
                        8 => "Apex",
                        9 => "Custom",
                        _ => "Unknown",
                    };
                }

                return "Unknown";
            }
        }

        private bool? _simpleStopEnabled;
        public bool? SimpleStopEnabled
        {
            get { return _simpleStopEnabled; }
            set { if (_simpleStopEnabled != value) { _simpleStopEnabled = value; OnPropertyChanged(); } }
        }

        /*
        public int MaxRecommendedSpeed
        {
            get
            {
                if (App.Current.MetricDisplay)
                {
                    if (BoardType == OWBoardType.V1)
                    {
                        return _rideMode switch
                        {
                            1 => 19, // Classic
                            2 => 24, // Extreme
                            3 => 24, // Elevated
                            _ => 24, // Unknown
                        };
                    }
                    else if (BoardType == OWBoardType.Plus || BoardType == OWBoardType.XR)
                    {
                        return _rideMode switch
                        {
                            4 => 19, // Sequoia
                            5 => 24, // Cruz
                            6 => 30, // Mission
                            7 => 30, // Elevated
                            8 => 32, // Delirium
                            9 => 32, // Custom
                            _ => 32, // Unknown
                        };
                    }
                    else if (BoardType == OWBoardType.Pint)
                    {
                        return _rideMode switch
                        {
                            5 => 19, // Redwood
                            6 => 26, // Pacific
                            7 => 26, // Elevated
                            8 => 26, // Skyline
                            _ => 26, // Unknown
                        };
                    }

                    return 32;
                }
                else
                {
                    if (BoardType == OWBoardType.V1)
                    {
                        return _rideMode switch
                        {
                            1 => 12, // Classic
                            2 => 15, // Extreme
                            3 => 15, // Elevated
                            _ => 15, // Unknown
                        };
                    }
                    else if (BoardType == OWBoardType.Plus || BoardType == OWBoardType.XR)
                    {
                        return _rideMode switch
                        {
                            4 => 12, // Sequoia
                            5 => 15, // Cruz
                            6 => 19, // Mission
                            7 => 19, // Elevated
                            8 => 20, // Delirium
                            9 => 20, // Custom
                            _ => 20, // Unknown
                        };
                    }
                    else if (BoardType == OWBoardType.Pint)
                    {
                        return _rideMode switch
                        {
                            5 => 12, // Redwood
                            6 => 16, // Pacific
                            7 => 16, // Elevated
                            8 => 16, // Skyline
                            _ => 16, // Unknown
                        };
                    }

                    // New ride mode?
                    return 20;
                }
            }
        }
        */





        private bool _lightMode;
        public bool LightMode
        {
            get { return _lightMode; }
            set { if (_lightMode != value) { _lightMode = value; OnPropertyChanged(); } }
        }

        private int _frontLightMode;
        public int FrontLightMode
        {
            get { return _frontLightMode; }
            set { if (_frontLightMode != value) { _frontLightMode = value; OnPropertyChanged(); } }
        }

        private int _rearLightMode;
        public int RearLightMode
        {
            get { return _rearLightMode; }
            set { if (_rearLightMode != value) { _rearLightMode = value; OnPropertyChanged(); } }
        }

        private float _unknown1;
        public float UNKNOWN1
        {
            get { return _unknown1; }
            set { if (_unknown1.AlmostEqualTo(value) == false) { _unknown1 = value; OnPropertyChanged(); } }
        }

        private float _unknown2;
        public float UNKNOWN2
        {
            get { return _unknown2; }
            set { if (_unknown2.AlmostEqualTo(value) == false) { _unknown2 = value; OnPropertyChanged(); } }
        }

        private float _unknown3;
        public float UNKNOWN3
        {
            get { return _unknown3; }
            set { if (_unknown3.AlmostEqualTo(value) == false) { _unknown3 = value; OnPropertyChanged(); } }
        }

        private float _unknown4;
        public float UNKNOWN4
        {
            get { return _unknown4; }
            set { if (_unknown4.AlmostEqualTo(value) == false) { _unknown4 = value; OnPropertyChanged(); } }
        }

        int _rssi;
        public int RSSI
        {
            get { return _rssi; }
            set { if (_rssi != value) { _rssi = value; OnPropertyChanged(); } }
        }


        bool _isRecordingRide;
        public bool IsRecordingRide
        {
            get { return _isRecordingRide; }
            set { if (_isRecordingRide != value) { _isRecordingRide = value; OnPropertyChanged(); } }
        }

        readonly IOWBLE _owble;

        OWBoardEventList _events = new OWBoardEventList();
        List<OWBoardEvent> _initialEvents;
        Ride _currentRide;
        bool _keepHandshakeBackgroundRunning;
        bool _keepRSSIMonitorRunning;
        List<byte> _handshakeBuffer;
        bool _isHandshaking;
        TaskCompletionSource<byte[]> _handshakeTaskCompletionSource;
        bool _isTornDown;

        public OWBoard(IOWBLE owble, OWBaseBoard baseBoard) : base(baseBoard)
        {
            MessagingCenter.Subscribe<App>(this, App.UnitDisplayUpdatedKey, (app) =>
            {
                OnPropertyChanged(nameof(RPM));
                OnPropertyChanged(nameof(LifetimeOdometer));
                OnPropertyChanged(nameof(TripOdometer));
                OnPropertyChanged(nameof(SpeedValue));
                OnPropertyChanged(nameof(TripOdometerDescription));
            });

            _owble = owble;
            ID = baseBoard.ID;
            Name = baseBoard.Name;
            IsAvailable = baseBoard.IsAvailable;
            NativePeripheral = baseBoard.NativePeripheral;
            _owble.BoardValueChanged += OWBLE_BoardValueChanged;
            _owble.RSSIUpdated += OWBLE_RSSIUpdated;
            _owble.BoardReconnected += OWBLE_BoardReconnected;

            // Subscribe to property changes to keep watch app in sync
            // (eg speed, battery percent changes)
            this.PropertyChanged += WatchSyncHandler.HandlePropertyChanged;
        }

        // Undoes everything done in the constructor/SubscribeToBLE, and stops any
        // background timers. Must be called when the board is disconnected/discarded,
        // otherwise this instance (and its timers) will keep running forever even
        // though nothing references it anymore.
        public void Teardown()
        {
            if (_isTornDown)
            {
                return;
            }

            _isTornDown = true;

            _keepHandshakeBackgroundRunning = false;
            _keepRSSIMonitorRunning = false;

            _owble.BoardValueChanged -= OWBLE_BoardValueChanged;
            _owble.RSSIUpdated -= OWBLE_RSSIUpdated;
            _owble.BoardReconnected -= OWBLE_BoardReconnected;

            this.PropertyChanged -= WatchSyncHandler.HandlePropertyChanged;

            MessagingCenter.Unsubscribe<App>(this, App.UnitDisplayUpdatedKey);
        }

        public virtual void Init()
        {
            bool autoRideRecording = Preferences.Get("auto_ride_recording", false);
            if (autoRideRecording)
            {
                StartLogging();
            }
            /*
#if DEBUG
            if (DeviceInfo.DeviceType == DeviceType.Physical)
            {
                StartLogging();
            }
#endif
            */
        }

        void LogData(string characteristicGuid, byte[] data)
        {
            var byteString = ByteString.CopyFrom(data);
#if DEBUG
            // Remove serials from debug builds recording data.
            if (characteristicGuid.Equals(SerialNumberUUID, StringComparison.InvariantCultureIgnoreCase))
            {
                byteString = ByteString.CopyFrom(BitConverter.GetBytes(123456));
            }
            else if (characteristicGuid.Equals(BatterySerialUUID, StringComparison.InvariantCultureIgnoreCase))
            {
                byteString = ByteString.CopyFrom(BitConverter.GetBytes(789123));
            }
#endif

            _events.BoardEvents.Add(new OWBoardEvent()
            {
                Uuid = characteristicGuid,
                Data = byteString,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            });

            if (_events.BoardEvents.Count > 1000)
            {
                SaveEvents();
            }
        }

        private void OWBLE_BoardValueChanged(string characteristicGuid, byte[] data)
        {
            //Debug.WriteLine($"{characteristicGuid} {BitConverter.ToString(data)}");

            if (IsRecordingRide)
            {
                LogData(characteristicGuid, data);
            }


            if (_isHandshaking && characteristicGuid.Equals(SerialReadUUID, StringComparison.OrdinalIgnoreCase))
            {
                _handshakeBuffer.AddRange(data);
                if (_handshakeBuffer.Count == 20)
                {
                    _isHandshaking = false;
                    _handshakeTaskCompletionSource.SetResult(_handshakeBuffer.ToArray<byte>());
                }

                return;
            }


            SetValue(characteristicGuid, data);
        }

        private void OWBLE_RSSIUpdated(int rssi)
        {
            RSSI = rssi;
        }


        // TODO: Restore, Dictionary<string, ICharacteristic> _characteristics = new Dictionary<string, ICharacteristic>();

        private void RSSIMonitor()
        {
            _keepRSSIMonitorRunning = true;

            Device.StartTimer(TimeSpan.FromSeconds(0.5), () =>
            {
                if (_keepRSSIMonitorRunning == false)
                {
                    return false;
                }

                try
                {
                    App.Current.OWBLE.RequestRSSIUpdate();
                }
                catch (Exception err)
                {
                    System.Diagnostics.Debug.WriteLine("RSSI fetch error: " + err.Message);
                }
                return _keepRSSIMonitorRunning;
            });
        }

        // Read once at initial connect, and again after a reconnect to refresh the
        // UI immediately rather than waiting on the next notification for each.
        private static readonly List<string> _characteristicsToReadNow = new List<string>()
        {
            SerialNumberUUID,
            BatteryPercentUUID,
            //BatteryLow5UUID,
            //BatteryLow20UUID,
            BatterySerialUUID,
            //PitchUUID,
            //RollUUID,
            //YawUUID,
            TripOdometerUUID,
            RpmUUID,
            LightModeUUID,
            LightsFrontUUID,
            LightsBackUUID,
            //StatusErrorUUID,
            TemperatureUUID,
            //FirmwareRevisionUUID,
            CurrentAmpsUUID,
            TripAmpHoursUUID,
            TripRegenAmpHoursUUID,
            BatteryTemperatureUUID,
            BatteryVoltageUUID,
            //SafetyHeadroomUUID,
            //HardwareRevisionUUID,
            LifetimeOdometerUUID,
            LifetimeAmpHoursUUID,
            RideModeUUID,
            //BatteryCellsUUID,
            LastErrorCodeUUID,
            //SerialRead,
            //SerialWrite,
            //UNKNOWN1UUID,
            //UNKNOWN2UUID,
            //UNKNOWN3UUID,
            //UNKNOWN4UUID,
        };

        // Android can subscribe up to 15 things at once. Subscriptions are
        // per-connection at the BLE level - a reconnect gets a brand new GATT
        // session where none of these are armed anymore, so this list is also
        // used to re-subscribe after a reconnect, not just on the initial connect.
        private static readonly List<string> _characteristicsToSubscribeTo = new List<string>()
        {
            //SerialNumberUUID,
            BatteryPercentUUID,
            //BatteryLow5UUID,
            //BatteryLow20UUID,
            //BatterySerialUUID,
            //PitchUUID,
            //RollUUID,
            //YawUUID,
            TripOdometerUUID,
            RpmUUID,
            //LightModeUUID,
            //LightsFrontUUID,
            //LightsBackUUID,
            StatusErrorUUID,
            TemperatureUUID,
            //FirmwareRevisionUUID,
            CurrentAmpsUUID,
            TripAmpHoursUUID,
            TripRegenAmpHoursUUID,
            BatteryTemperatureUUID,
            BatteryVoltageUUID,
            //SafetyHeadroomUUID,
            RideModeUUID,
            //HardwareRevisionUUID,
            //LifetimeOdometerUUID,
            //LifetimeAmpHoursUUID,
            BatteryCellsUUID,
            //LastErrorCodeUUID,
            //SerialRead,
            //SerialWrite,
            //UNKNOWN1UUID,
            //UNKNOWN2UUID,
            //UNKNOWN3UUID,
            //UNKNOWN4UUID,
        };

        // Re-arms live-data notifications and refreshes current values. Used both
        // for the initial connect (below) and after a reconnect (OWBLE_BoardReconnected)
        // - deliberately does not redo the one-time hardware/firmware handshake or
        // Jumpstart flow below, since re-triggering that popup on every reconnect
        // would be its own bug, and the already-running KeepBoardAlive() keep-alive
        // timer (started once below and never stopped across a reconnect) continues
        // to re-assert the board's unlock state on its own.
        private async Task RestoreLiveDataSync()
        {
            foreach (string characteristic in _characteristicsToSubscribeTo)
            {
                // SubscribeValue returns null (rather than a Task) if the connection
                // has already dropped again - eg a second disconnect landing while
                // this loop is mid-flight after a reconnect. Awaiting null throws an
                // NRE that SafeFireAndForget (see OWBLE_BoardReconnected below) would
                // otherwise swallow silently, abandoning the rest of the resync.
                // Bail out instead; the next reconnect will call this again.
                Task subscribeTask = _owble.SubscribeValue(characteristic);
                if (subscribeTask == null)
                {
                    return;
                }
                await subscribeTask;
            }

            foreach (string characteristic in _characteristicsToReadNow)
            {
                Task<byte[]> readTask = _owble.ReadValue(characteristic);
                if (readTask == null)
                {
                    return;
                }
                byte[] data = await readTask;
                SetValue(characteristic, data, true);
            }
        }

        private void OWBLE_BoardReconnected()
        {
            RestoreLiveDataSync().SafeFireAndForget();
        }

        internal async Task SubscribeToBLE()
        {
#if DEBUG
            if (NativePeripheral == null)
                return;
#endif
            RSSIMonitor();

            // ReadValue returns null (rather than a Task) if the connection has
            // already dropped - see the identical guard in RestoreLiveDataSync.
            // Awaiting null throws an NRE that would otherwise abort this method
            // (and, since it's invoked fire-and-forget, become an unobserved task
            // fault instead of a reported error). Bail out instead; a reconnect
            // will pick the resync back up via RestoreLiveDataSync.
            Task<byte[]> hardwareRevisionTask = _owble.ReadValue(HardwareRevisionUUID);
            if (hardwareRevisionTask == null)
            {
                return;
            }
            SetValue(HardwareRevisionUUID, await hardwareRevisionTask, true);

            Task<byte[]> firmwareRevisionTask = _owble.ReadValue(FirmwareRevisionUUID);
            if (firmwareRevisionTask == null)
            {
                return;
            }
            SetValue(FirmwareRevisionUUID, await firmwareRevisionTask, true);

            if (HardwareRevision > 3000 && FirmwareRevision > 4000) // Requires Gemini handshake
            {
                Task<byte[]> rideModeTask = _owble.ReadValue(RideModeUUID);
                if (rideModeTask == null)
                {
                    return;
                }
                byte[] rideMode = await rideModeTask;
                ushort rideModeInt = BitConverter.ToUInt16(rideMode, 0);

                if (rideModeInt > 0)
                {
                    // NOOP: Board is active 😜
                }
                else if (FirmwareRevision >= 4142) // Pint or XR with 4210 hardware 
                {
                    if (FirmwareRevision >= 4155 && HardwareRevision < 5000) // XR with 4155 FW.
                    {
                        await App.Current.MainPage.DisplayAlert("Oh no!", "Some features of this app currently will not work with board firmware 4155 and higher.\n\nFuture Motion has locked some features down and as a result prevents apps like OWCE reporting valuable data to you.\n\nSorry about that.", "Ok");
                    }

                    // No longer using the handshake with web connection.
                    var jumpstartAlert = new Pages.Popup.JumpstartAlert(new Command(async () =>
                    {
                        await PopupNavigation.Instance.PopAllAsync();
                        if (App.Current.MainPage.Navigation.ModalStack.Count == 1 && App.Current.MainPage.Navigation.ModalStack[0] is NavigationPage modalNavigationPage && modalNavigationPage.CurrentPage is Pages.BoardPage boardPage)
                        {
                            await boardPage.DisconnectAndPop();
                            return;
                        }
                    }));
                    await PopupNavigation.Instance.PushAsync(jumpstartAlert, true);
                    return;
                }
                else // XR 4209 and below
                {
                    try
                    {
                        await Handshake();
                    }
                    catch (Exceptions.HandshakeException handshakeException)
                    {
                        await App.Current.MainPage.DisplayAlert("Error", handshakeException.Message, "Ok");
                        if (handshakeException.ShouldDisconnect)
                        {
                            if (App.Current.MainPage.Navigation.ModalStack.Count == 1 && App.Current.MainPage.Navigation.ModalStack[0] is Pages.CustomNavigationPage modalNavigationPage && modalNavigationPage.CurrentPage is Pages.BoardPage boardPage)
                            {
                                await boardPage.DisconnectAndPop();
                            }
                            return;
                        }
                    }
                }

                // Turns out the below timer does not fire immedaitly, it fires after the first 15sec have passed.
                // Calling this before we start the timer should make it work more reliably.

                if (!(HardwareRevision >= 6000 && HardwareRevision <= 6999)) // GT only works with OWCE if Rewheel'd with BLE Handshake patched, no need to keep alive.
                {
                    KeepBoardAlive().SafeFireAndForget();

                    _keepHandshakeBackgroundRunning = true;
                    Device.StartTimer(TimeSpan.FromSeconds(15), () =>
                    {
                        KeepBoardAlive().SafeFireAndForget();
                        return _keepHandshakeBackgroundRunning;
                    });
                }
            }

            await RestoreLiveDataSync();
        }

        // This should be called to keep the board in its unlocked state.
        async Task KeepBoardAlive()
        {
            try
            {
                Debug.WriteLine("KeepBoardAlive");
                byte[] firmwareRevision = GetBytesForBoardFromUInt16((UInt16)FirmwareRevision, FirmwareRevisionUUID);
                await _owble.WriteValue(OWBoard.FirmwareRevisionUUID, firmwareRevision);
            }
            catch (Exception err)
            {
                // TODO: Couldnt update firmware revision.
                Debug.WriteLine("ERROR: " + err.Message);
            }
        }

        private async Task<bool> Handshake()
        {
            _isHandshaking = true;
            _handshakeTaskCompletionSource = new TaskCompletionSource<byte[]>();
            _handshakeBuffer = new List<byte>();

            // SubscribeValue/WriteValue return null (rather than a Task) if the
            // connection has already dropped - same guard as SubscribeToBLE and
            // RestoreLiveDataSync. Treat it the same as the timeout guard below:
            // the board went away mid-handshake, so surface it via the same
            // HandshakeException path SubscribeToBLE already catches, instead of
            // an uncaught NRE from awaiting null.
            Task subscribeTask = _owble.SubscribeValue(OWBoard.SerialReadUUID, true);
            if (subscribeTask == null)
            {
                _isHandshaking = false;
                throw new Exceptions.HandshakeException("Board disconnected before the handshake could start.", true);
            }
            await subscribeTask;

            // Data does not send until this is triggered.
            byte[] firmwareRevision = GetBytesForBoardFromUInt16((UInt16)FirmwareRevision, FirmwareRevisionUUID);

            Task<byte[]> writeTask = _owble.WriteValue(OWBoard.FirmwareRevisionUUID, firmwareRevision, true);
            if (writeTask == null)
            {
                _isHandshaking = false;
                throw new Exceptions.HandshakeException("Board disconnected before the handshake could start.", true);
            }
            await writeTask;

            // Guard against the board never sending the full handshake response (eg it
            // disconnected mid-handshake) which would otherwise hang this task forever.
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
            Task completedTask = await Task.WhenAny(_handshakeTaskCompletionSource.Task, timeoutTask);
            if (completedTask == timeoutTask)
            {
                _isHandshaking = false;
                await _owble.UnsubscribeValue(OWBoard.SerialReadUUID, true);
                throw new Exceptions.HandshakeException("Timed out waiting for a response from the board.", true);
            }

            byte[] byteArray = await _handshakeTaskCompletionSource.Task;

            await _owble.UnsubscribeValue(OWBoard.SerialReadUUID, true);
            // TODO: Restore _characteristics[OWBoard.SerialReadUUID].ValueUpdated -= SerialRead_ValueUpdated;
            if (byteArray.Length == 20)
            {
                if (FirmwareRevision >= 4141) // Pint or XR with 4210 hardware 
                {
                    // Get bytes 3 through to 19 (start 3, length 16)
                    byte[] apiKeyArray = new byte[16];
                    Array.Copy(byteArray, 3, apiKeyArray, 0, 16);

                    // Convert to base16 string.
                    string apiKey = BitConverter.ToString(apiKeyArray).Replace("-", "");

                    // Exchange this apiKey for key from server.
                    byte[] tokenArray = await FetchToken(apiKey);
                    if (tokenArray != null)
                    {
                        // Feed it back to the app how we normally would.
                        await _owble.WriteValue(OWBoard.SerialWriteUUID, tokenArray);
                    }
                }
                else
                {
                    byte[] outputArray = new byte[20];
                    Array.Copy(byteArray, 0, outputArray, 0, 3);

                    // Take almost all of the bytes from the input array. This is almost the same as the last part as
                    // we are ignoring the first 3 and the last bytes.
                    byte[] arrayToMD5_part1 = new byte[16];
                    Array.Copy(byteArray, 3, arrayToMD5_part1, 0, 16);

                    // This appears to be a static value from the board.
                    byte[] arrayToMD5_part2 = new byte[] {
                         217,    // D9
                         37,     // 25
                         95,     // 5F
                         15,     // 0F
                         35,     // 23
                         53,     // 35
                         78,     // 4E
                         25,     // 19
                         186,    // BA
                         115,    // 73
                         156,    // 9C
                         205,    // CD
                         196,    // C4
                         169,    // A9
                         23,     // 17
                         101,    // 65
                     };


                    // New byte array we are going to MD5 hash. Part of the input string, part of this static string.
                    byte[] arrayToMD5 = new byte[arrayToMD5_part1.Length + arrayToMD5_part2.Length];
                    arrayToMD5_part1.CopyTo(arrayToMD5, 0);
                    arrayToMD5_part2.CopyTo(arrayToMD5, arrayToMD5_part1.Length);

                    // Start prepping the MD5 hash
                    // MD5 is mandated by the board's own handshake protocol, not a security choice
                    // made here - swapping algorithms would break the challenge/response with real hardware.
#pragma warning disable CA5351
                    byte[] md5Hash = null;
                    using (var md5 = System.Security.Cryptography.MD5.Create())
                    {
                        md5Hash = md5.ComputeHash(arrayToMD5);
                    }
#pragma warning restore CA5351

                    // Add it to the 3 bytes we already have.
                    Array.Copy(md5Hash, 0, outputArray, 3, md5Hash.Length);

                    // Validate the check byte.
                    outputArray[19] = 0;
                    for (int i = 0; i < outputArray.Length - 1; ++i)
                    {
                        outputArray[19] = ((byte)(outputArray[i] ^ outputArray[19]));
                    }

                    string inputString = BitConverter.ToString(byteArray).Replace("-", ":").ToLowerInvariant();
                    string outputString = BitConverter.ToString(outputArray).Replace("-", ":").ToLowerInvariant();

                    Debug.WriteLine($"Input: {inputString}");
                    Debug.WriteLine($"Output: {outputString}");

                    await _owble.WriteValue(OWBoard.SerialWriteUUID, outputArray);
                }
            }
            return false;
        }

        private async Task<byte[]> FetchToken(string apiKey)
        {
            if (String.IsNullOrWhiteSpace(Name))
            {
                return null;
            }
            string deviceName = Name.ToLowerInvariant();
            deviceName = deviceName.Replace("ow", String.Empty);


            //SecureStorage.Remove($"board_{deviceName}_token");
            //SecureStorage.Remove($"board_{deviceName}_key");

            string key = await SecureStorage.GetAsync($"board_{deviceName}_key");

            // If the API key has changed delete the stored token.
            if (key != apiKey)
            {
                SecureStorage.Remove($"board_{deviceName}_token");
            }


            // If we already have a token lets use it.
            string token = await SecureStorage.GetAsync($"board_{deviceName}_token");
            if (String.IsNullOrEmpty(token) == false)
            {
                byte[] tokenArray = token.StringToByteArray();
                return tokenArray;
            }

            try
            {
                // First lets fetch it from OWCE servers.
                using var handler = new HttpClientHandler();
                handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip;

                using var client = new HttpClient();
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");

                HttpResponseMessage response = await client.GetAsync($"https://{App.OWCEApiServer}/v1/handshake/{deviceName}");

                // We only care if we were successful, otherwise fallback to FM.
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    KeyResponse keyResponse = JsonSerializer.Deserialize<KeyResponse>(responseBody);

                    if (String.IsNullOrWhiteSpace(keyResponse.Key) == false)
                    {
                        await SecureStorage.SetAsync($"board_{deviceName}_key", apiKey);
                        await SecureStorage.SetAsync($"board_{deviceName}_token", keyResponse.Key);

                        byte[] tokenArray = keyResponse.Key.StringToByteArray();
                        return tokenArray;
                    }
                }

                HttpResponseMessage statusResponse = await client.GetAsync($"https://{App.OWCEApiServer}/v1/status/handshake");
                if (statusResponse.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return null;
                }

                string handshakeStatusResponseBody = await statusResponse.Content.ReadAsStringAsync();
                HandshakeStatusResponse handshakeStatusResponse = JsonSerializer.Deserialize<HandshakeStatusResponse>(handshakeStatusResponseBody);

                if (handshakeStatusResponse.Online == false)
                {
                    throw new Exceptions.HandshakeException(handshakeStatusResponse.Message, true);
                }
            }
            catch (Exceptions.HandshakeException err)
            {
                throw;
            }
            catch (Exception err)
            {
                Debug.WriteLine($"ERROR: {err.Message}");

                SecureStorage.Remove($"board_{deviceName}_token");
                SecureStorage.Remove($"board_{deviceName}_key");
            }

            return null;
        }


        private static byte[] GetBytesForBoardFromUInt16(UInt16 value, string uuidHint = null)
        {
            if (uuidHint == null)
            {

            }
            else
            {


            }

            byte[] bytes = BitConverter.GetBytes(value);
            return bytes;
        }

        private void SetValue(string uuid, byte[] data, bool initialData = false)
        {
            if (data == null)
                return;

            uuid = uuid.ToUpperInvariant();

            if (initialData)
            {
                if (IsRecordingRide)
                {
                    LogData(uuid, data);
                }
                else
                {
                    _initialEvents ??= new List<OWBoardEvent>();

                    _initialEvents.Add(new OWBoardEvent()
                    {
                        Uuid = uuid,
                        Data = ByteString.CopyFrom(data),
                        Timestamp = DateTime.UtcNow.Ticks,
                    });
                }
            }


            if (data.Length != 2)
                return;

            if (uuid == TemperatureUUID)
            {
                MotorTemperature = data[0];
                ControllerTemperature = data[1];

                return;
            }
            else if (uuid == BatteryTemperatureUUID)
            {
                if (BoardType == OWBoardType.V1 || BoardType == OWBoardType.Plus)
                {
                    BatteryTemperature = data[1];
                }
                else
                {
                    BatteryTemperature = data[0];
                }

                return;
            }
            else if (uuid == BatteryPercentUUID)
            {
                if (data[0] > 0)
                {
                    BatteryPercent = data[0];
                }
                else if (data[1] > 0)
                {
                    BatteryPercent = data[1];
                }

                return;
            }


            ushort value = BitConverter.ToUInt16(data, 0);


            switch (uuid)
            {
                case SerialNumberUUID:
                    SerialNumber = value;
                    break;
                case BatteryLow5UUID:
                    BatteryLow5 = value;
                    break;
                case BatteryLow20UUID:
                    BatteryLow20 = value;
                    break;
                case BatterySerialUUID:
                    BatterySerial = value;
                    break;
                case PitchUUID:
                    Pitch = 0.1f * (1800 - value);
                    break;
                case RollUUID:
                    Roll = 0.1f * (1800 - value);
                    break;
                case YawUUID:
                    Yaw = 0.1f * (1800 - value);
                    break;
                case TripOdometerUUID:
                    TripOdometer = value;
                    break;
                case RpmUUID:
                    RPM = value;
                    break;
                case RideModeUUID:
                    RideMode = value;
                    break;
                case LightModeUUID:
                    LightMode = (value == 1);
                    break;
                case LightsFrontUUID:
                    FrontLightMode = value;
                    break;
                case LightsBackUUID:
                    RearLightMode = value;
                    break;
                case StatusErrorUUID:
                    StatusError = value;
                    break;
                case FirmwareRevisionUUID:
                    FirmwareRevision = value;
                    break;
                case CurrentAmpsUUID:
                    if (BoardType == OWBoardType.Unknown)
                    {
                        break;
                    }

                    float scaleFactor = BoardType switch
                    {
                        OWBoardType.V1 => 0.0009f,
                        OWBoardType.Plus => 0.0018f,
                        OWBoardType.XR => 0.002f,
                        OWBoardType.Pint => 0.002f,
                        OWBoardType.PintX => 0.002f,
                        OWBoardType.GT => 0.002f,
                        _ => throw new InvalidOperationException("Unknown board type: " + BoardType),
                    };

                    /// https://en.wikipedia.org/wiki/Two's_complement
                    int ampsValue = (value > 32767) ? (int)value - 65536 : value;

                    CurrentAmps = (float)ampsValue * scaleFactor;
                    IsRegen = (CurrentAmps < 0);

                    break;
                case TripAmpHoursUUID:
                    if (BoardType == OWBoardType.V1)
                    {
                        TripAmpHours = (float)value * 0.00009f;
                    }
                    else
                    {
                        TripAmpHours = (float)value * 0.00018f;
                    }
                    break;
                case TripRegenAmpHoursUUID:
                    if (BoardType == OWBoardType.V1)
                    {
                        TripRegenAmpHours = (float)value * 0.00009f;
                    }
                    else
                    {
                        TripRegenAmpHours = (float)value * 0.00018f;
                    }
                    break;
                case BatteryVoltageUUID:
                    BatteryVoltage = 0.1f * value;
                    break;
                case SafetyHeadroomUUID:
                    SafetyHeadroom = value;
                    break;
                case HardwareRevisionUUID:
                    HardwareRevision = value;

                    if (value >= 1 && value <= 2999)
                    {
                        BoardType = OWBoardType.V1;
                        SimpleStopEnabled = null;

                        BatteryCells.CellCount = 16;
                    }
                    else if (value >= 3000 && value <= 3999)
                    {
                        BoardType = OWBoardType.Plus;
                        SimpleStopEnabled = null;

                        BatteryCells.CellCount = 16;
                    }
                    else if (value >= 4000 && value <= 4999)
                    {
                        BoardType = OWBoardType.XR;
                        SimpleStopEnabled = null;

                        BatteryCells.CellCount = 15;
                        BatteryCells.IgnoreCell(15);
                        OnPropertyChanged(nameof(BatteryCells));
                    }
                    else if (value >= 5000 && value <= 5999)
                    {
                        BoardType = OWBoardType.Pint;
                        SimpleStopEnabled ??= false;

                        BatteryCells.CellCount = 15;
                        BatteryCells.IgnoreCell(15);
                        OnPropertyChanged(nameof(BatteryCells));
                    }
                    else if (value >= 7000 && value <= 7999)
                    {
                        BoardType = OWBoardType.PintX;
                        SimpleStopEnabled ??= false;

                        BatteryCells.CellCount = 15;
                        BatteryCells.IgnoreCell(15);
                        OnPropertyChanged(nameof(BatteryCells));
                    }
                    else if (value >= 6000 && value <= 6999)
                    {
                        BoardType = OWBoardType.GT;
                        SimpleStopEnabled ??= false;

                        BatteryCells.CellCount = 18;
                        OnPropertyChanged(nameof(BatteryCells));
                    }
                    break;
                case LifetimeOdometerUUID:
                    LifetimeOdometer = value;
                    break;
                case LifetimeAmpHoursUUID:
                    LifetimeAmpHours = value;
                    break;
                case BatteryCellsUUID:

                    // Different battery cell logic for XR 4210+ and Pint.
                    if (FirmwareRevision >= 4141)
                    {
                        uint cellID = (uint)((value & 0xF000) >> 12);
                        float batteryVoltage = (value & 0x0FFF) * 0.0011f;
                        BatteryCells.SetCell(cellID, batteryVoltage, "F3");
                    }
                    else
                    {
                        uint cellID = (uint)data[1];
                        float batteryVoltage = (float)data[0] * 0.02f;
                        BatteryCells.SetCell(cellID, batteryVoltage);
                    }

                    OnPropertyChanged(nameof(BatteryCells));

                    break;
                case LastErrorCodeUUID:

                    break;
                case UNKNOWN1UUID:
                    UNKNOWN1 = value;
                    break;
                case UNKNOWN2UUID:
                    UNKNOWN2 = value;
                    break;
                case UNKNOWN3UUID:
                    UNKNOWN3 = value;
                    break;
                case UNKNOWN4UUID:
                    UNKNOWN4 = value;
                    break;
            }
        }

        /*
        public async Task Disconnect()
        {
            _owble.BoardValueChanged -= OWBLE_BoardValueChanged;
            _keepHandshakeBackgroundRunning = false;
            await _owble.Disconnect();
            //await CrossBluetoothLE.Current.Adapter.DisconnectDeviceAsync(_device);
        }
        */


        public void StartLogging()
        {
            if (IsRecordingRide)
            {
                return;
            }

            _currentRide = Ride.CreateNewRide();

            //_currentLogFile = Path.Combine(App.Current.LogsDirectory, filename);


            IsRecordingRide = true;
            _events = new OWBoardEventList();
            if (_initialEvents != null)
            {
                _events.BoardEvents.AddRange(_initialEvents);
            }

            /*
            if (CrossGeolocator.Current.IsGeolocationAvailable)
            {
                CrossGeolocator.Current.DesiredAccuracy = 1;

                await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(5), 10, true);

                CrossGeolocator.Current.PositionChanged += PositionChanged;
                CrossGeolocator.Current.PositionError += PositionError;

            }
            */
        }

        public void StopLogging()
        {
            if (IsRecordingRide == false)
            {
                return;
            }

            IsRecordingRide = false;

            /*
            await CrossGeolocator.Current.StopListeningAsync(); 
            CrossGeolocator.Current.PositionChanged -= PositionChanged;
            CrossGeolocator.Current.PositionError -= PositionError;
            */

            SaveEvents();

            if (_currentRide != null)
            {
                _currentRide.EndTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _currentRide.EndTime = DateTime.Now;
                _currentRide.Save();
            }
        }


        /*
        double _oldLat = 0;
        double _oldLon = 0;
        private void PositionChanged(object sender, PositionEventArgs e)
        {
            if (_oldLat.Equals(e.Position.Latitude) == false || _oldLon.Equals(e.Position.Longitude) == false)
            {
                _oldLat = e.Position.Latitude;
                _oldLon = e.Position.Longitude;

                _events.BoardEvents.Add(new OWBoardEvent()
                {
                    Uuid = "gps_latitude",
                    Data = ByteString.CopyFrom(BitConverter.GetBytes(e.Position.Latitude)),
                    Timestamp = DateTime.UtcNow.Ticks,
                });

                _events.BoardEvents.Add(new OWBoardEvent()
                {
                    Uuid = "gps_longitude",
                    Data = ByteString.CopyFrom(BitConverter.GetBytes(e.Position.Longitude)),
                    Timestamp = DateTime.UtcNow.Ticks,
                });

                _events.BoardEvents.Add(new OWBoardEvent()
                {
                    Uuid = "gps_altitude",
                    Data = ByteString.CopyFrom(BitConverter.GetBytes(e.Position.Altitude)),
                    Timestamp = DateTime.UtcNow.Ticks,
                });

                _events.BoardEvents.Add(new OWBoardEvent()
                {
                    Uuid = "gps_speed",
                    Data = ByteString.CopyFrom(BitConverter.GetBytes(e.Position.Speed)),
                    Timestamp = DateTime.UtcNow.Ticks,
                });

                _events.BoardEvents.Add(new OWBoardEvent()
                {
                    Uuid = "gps_accuracy",
                    Data = ByteString.CopyFrom(BitConverter.GetBytes(e.Position.Accuracy)),
                    Timestamp = DateTime.UtcNow.Ticks,
                });

                _events.BoardEvents.Add(new OWBoardEvent()
                {
                    Uuid = "gps_heading",
                    Data = ByteString.CopyFrom(BitConverter.GetBytes(e.Position.Heading)),
                    Timestamp = DateTime.UtcNow.Ticks,
                });

                //If updating the UI, ensure you invoke on main thread
                var position = e.Position;
                var output = "Full: Lat: " + position.Latitude + " Long: " + position.Longitude;
                output += "\n Full: Lat: " + ((float)position.Latitude) + " Long: " + ((float)position.Longitude);
                output += "\n" + $"Time: {position.Timestamp}";
                output += "\n" + $"Heading: {position.Heading}";
                output += "\n" + $"Speed: {position.Speed}";
                output += "\n" + $"Accuracy: {position.Accuracy}";
                output += "\n" + $"Altitude: {position.Altitude}";
                output += "\n" + $"Altitude Accuracy: {position.AltitudeAccuracy}";
                System.Diagnostics.Debug.WriteLine(output);
            }
        }

        private void PositionError(object sender, PositionErrorEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.Error);
            //Handle event here for errors
        }
        */

        private void SaveEvents()
        {
            try
            {
                OWBoardEventList oldEvents = _events;
                _events = new OWBoardEventList();
                using (var fileStream = new FileStream(Path.Combine(App.Current.LogsDirectory, _currentRide.DataFileName), FileMode.Append, FileAccess.Write))
                {
                    foreach (OWBoardEvent owBoardEvent in oldEvents.BoardEvents)
                    {
                        owBoardEvent.WriteDelimitedTo(fileStream);
                    }
                }

                if (_currentRide != null)
                {
                    _currentRide.EndTime = DateTime.Now;
                    _currentRide.EndTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    _currentRide.Save();
                }
                //long currentRunEnd = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                // var outputFile = Path.Combine(_logDirectory, $"{currentRunEnd}.dat");
            }
            catch (Exception err)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: " + err.Message);
            }
        }

        public static string GetNameFromUUID(string uuid)
        {
            uuid = uuid.ToUpperInvariant();

            return uuid switch
            {
                SerialNumberUUID => "SerialNumber",
                RideModeUUID => "RideMode",
                BatteryPercentUUID => "BatteryPercent",
                BatteryLow5UUID => "BatteryLow5",
                BatteryLow20UUID => "BatteryLow20",
                BatterySerialUUID => "BatterySerial",
                PitchUUID => "Pitch",
                RollUUID => "Roll",
                YawUUID => "Yaw",
                TripOdometerUUID => "TripOdometer",
                RpmUUID => "Rpm",
                LightModeUUID => "LightMode",
                LightsFrontUUID => "LightsFront",
                LightsBackUUID => "LightsBack",
                StatusErrorUUID => "StatusError",
                TemperatureUUID => "Temperature",
                FirmwareRevisionUUID => "FirmwareRevision",
                CurrentAmpsUUID => "CurrentAmps",
                TripAmpHoursUUID => "TripAmpHours",
                TripRegenAmpHoursUUID => "TripRegenAmpHours",
                BatteryTemperatureUUID => "BatteryTemperature",
                BatteryVoltageUUID => "BatteryVoltage",
                SafetyHeadroomUUID => "SafetyHeadroom",
                HardwareRevisionUUID => "HardwareRevision",
                LifetimeOdometerUUID => "LifetimeOdometer",
                LifetimeAmpHoursUUID => "LifetimeAmpHours",
                BatteryCellsUUID => "BatteryCells",
                LastErrorCodeUUID => "LastErrorCode",
                SerialReadUUID => "SerialRead",
                SerialWriteUUID => "SerialWrite",
                UNKNOWN1UUID => "UNKNOWN1",
                UNKNOWN2UUID => "UNKNOWN2",
                UNKNOWN3UUID => "UNKNOWN3",
                UNKNOWN4UUID => "UNKNOWN4",
                _ => "Unknown",
            };
        }



        public async void ChangeRideMode(ushort rideMode)
        {
            try
            {
                _incomingRideMode = rideMode;
                byte[] rideModeBytes = BitConverter.GetBytes(rideMode);
                byte[] result = await App.Current.OWBLE.WriteValue(OWBoard.RideModeUUID, rideModeBytes, true);
            }
            catch (Exception err)
            {
                // Board likely disconnected mid-write. Swallow rather than crash, same
                // as every other BLE write failure path in this class.
                Debug.WriteLine("ChangeRideMode error: " + err.Message);
            }
        }
    }
}
