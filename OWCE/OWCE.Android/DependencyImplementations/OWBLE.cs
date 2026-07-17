using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
//using Android.OS;
using Android.Runtime;
using Java.Util;
using OWCE.DependencyInterfaces;
using OWCE.Droid.Extensions;
using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: Dependency(typeof(OWCE.Droid.DependencyImplementations.OWBLE))]

namespace OWCE.Droid.DependencyImplementations
{
    public class OWBLE : Java.Lang.Object, IOWBLE, INotifyPropertyChanged
    {
        private enum OWBLE_QueueItemOperationType
        {
            Read,
            Write,
            Subscribe,
            Unsubscribe,
        }

        private Queue<OWBLE_QueueItem> _gattOperationQueue = new Queue<OWBLE_QueueItem>();
        private bool _gattOperationQueueProcessing = false;

        public event PropertyChangedEventHandler PropertyChanged;


        private class OWBLE_QueueItem
        {
            public OWBLE_QueueItemOperationType OperationType { get; private set; }
            public BluetoothGattCharacteristic Characteristic { get; private set; }
            public byte[] Data { get; set; }

            public OWBLE_QueueItem(BluetoothGattCharacteristic characteristic, OWBLE_QueueItemOperationType operationType, byte[] data = null)
            {
                Characteristic = characteristic;
                OperationType = operationType;
                Data = data; 
            }
        }

        private class OWBLE_ScanCallback : ScanCallback
        {
            private OWBLE _owble;

            public OWBLE_ScanCallback(OWBLE owble)
            {
                _owble = owble;
            }

            public override void OnBatchScanResults(IList<ScanResult> results)
            {
                Debug.WriteLine("OnBatchScanResults");
                base.OnBatchScanResults(results);
            }

            public override void OnScanResult(ScanCallbackType callbackType, ScanResult result)
            {
                Debug.WriteLine("OnScanResult");
                
                var board = new OWBaseBoard()
                {
                    ID = result.Device.Address,
                    Name = result.Device.Name ?? "Onewheel",
                    IsAvailable = true,
                    NativePeripheral = result.Device,
                };

                _owble.BoardDiscovered?.Invoke(board);
            }

            public override void OnScanFailed([GeneratedEnum] ScanFailure errorCode)
            {
                Debug.WriteLine("OnScanFailed");
                base.OnScanFailed(errorCode);
            }
        }
        
        private class OWBLE_LeScanCallback : Java.Lang.Object, BluetoothAdapter.ILeScanCallback
        {
            private OWBLE _owble;

            public OWBLE_LeScanCallback(OWBLE owble)
            {
                _owble = owble;
            }

            public void OnLeScan(BluetoothDevice device, int rssi, byte[] scanRecord)
            {
                Debug.WriteLine("OnLeScan");
                
                var board = new OWBaseBoard()
                {
                    ID = device.JniIdentityHashCode.ToString(),
                    Name = device.Name ?? "Onewheel",
                    IsAvailable = true,
                    NativePeripheral = device,
                };

                _owble.BoardDiscovered?.Invoke(board);
            }
        }

        private class OWBLE_BluetoothGattCallback : BluetoothGattCallback
        {
            private OWBLE _owble;

            public OWBLE_BluetoothGattCallback(OWBLE owble)
            {
                _owble = owble;
            }
                        
            public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
            {
                Debug.WriteLine("OnServicesDiscovered: " + status);
                _owble.OnServicesDiscovered(gatt, status);
            }

            public override void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
            {
                Debug.WriteLine("OnConnectionStateChange: " + status);
                _owble.OnConnectionStateChange(gatt, status, newState);
            }

            // Pre-API 33 callback. Only ever invoked on devices where the OS doesn't
            // know about the API 33+ overload below - Android calls exactly one of the
            // two depending on the device's OS version, never both.
            public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
            {
                Debug.WriteLine("OnCharacteristicRead: " + characteristic.Uuid);
                _owble.OnCharacteristicRead(gatt, characteristic, status);
            }

            // API 33+ callback - delivers the value directly instead of requiring a
            // separate characteristic.GetValue() call after the fact.
            public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, byte[] value, GattStatus status)
            {
                Debug.WriteLine("OnCharacteristicRead: " + characteristic.Uuid);
                _owble.OnCharacteristicRead(gatt, characteristic, value, status);
            }

            public override void OnCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
            {
                Debug.WriteLine("OnCharacteristicWrite: " + characteristic.Uuid);
                _owble.OnCharacteristicWrite(gatt, characteristic, status);
            }

            // Pre-API 33 callback.
            public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
            {
                Debug.WriteLine("OnCharacteristicChanged: " + characteristic.Uuid);
                _owble.OnCharacteristicChanged(gatt, characteristic);
            }

            // API 33+ callback - delivers the value directly.
            public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, byte[] value)
            {
                Debug.WriteLine("OnCharacteristicChanged: " + characteristic.Uuid);
                _owble.OnCharacteristicChanged(gatt, characteristic, value);
            }

            // Pre-API 33 callback.
            public override void OnDescriptorRead(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
            {
                Debug.WriteLine($"OnDescriptorRead: {descriptor.Characteristic.Uuid}, {descriptor.Uuid}");
                _owble.OnDescriptorRead(gatt, descriptor, status);
            }

            // API 33+ callback.
            public override void OnDescriptorRead(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status, byte[] value)
            {
                Debug.WriteLine($"OnDescriptorRead: {descriptor.Characteristic.Uuid}, {descriptor.Uuid}");
                _owble.OnDescriptorRead(gatt, descriptor, status, value);
            }

            public override void OnDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, GattStatus status)
            {
                Debug.WriteLine($"OnDescriptorWrite: {descriptor.Characteristic.Uuid}, {descriptor.Uuid}");
                _owble.OnDescriptorWrite(gatt, descriptor, status);
            }

            public override void OnReadRemoteRssi(BluetoothGatt gatt, int rssi, [GeneratedEnum] GattStatus status)
            {
                Debug.WriteLine($"OnReadRemoteRssi: {rssi}");
                _owble.OnReadRemoteRssi(gatt, rssi, status);
            }
        }


        Dictionary<string, BluetoothGattCharacteristic> _characteristics = new Dictionary<string, BluetoothGattCharacteristic>();
        Dictionary<string, TaskCompletionSource<byte[]>> _readQueue = new Dictionary<string, TaskCompletionSource<byte[]>>();
        List<CharacteristicValueRequest> _writeQueue = new List<CharacteristicValueRequest>();
        Dictionary<string, TaskCompletionSource<byte[]>> _subscribeQueue = new Dictionary<string, TaskCompletionSource<byte[]>>();
        Dictionary<string, TaskCompletionSource<byte[]>> _unsubscribeQueue = new Dictionary<string, TaskCompletionSource<byte[]>>();
        List<string> _notifyList = new List<string>();

        private void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status)
        {
            //BTA_GATTC_CONN_MAX
            //BTA_GATTC_NOTIF_REG_MAX

            var service = gatt.GetService(OWBoard.ServiceUUID.ToUUID());

            if (service == null)
                return;
            
            foreach (var characteristic in service.Characteristics)
            {
                _characteristics[characteristic.Uuid.ToString().ToLower()] = characteristic;
            }

            if (_reconnecting)
            {
                // A reconnect creates a brand new BluetoothGatt (and therefore new
                // BluetoothGattCharacteristic objects), so services need to be
                // rediscovered - the previous connection's cached characteristics are
                // no longer valid - before treating the board as usable again.
                _reconnecting = false;
                BoardReconnected?.Invoke();
                return;
            }

            // TrySetResult (not SetResult): cancelling the "connecting..." popup can
            // race with this callback landing on the GATT thread, and SetResult throws
            // if the task was already cancelled in that window.
            if (_connectTaskCompletionSource?.TrySetResult(true) == true)
            {
                // TODO: Fix this.
                //BoardConnected?.Invoke(new OWBoard(_board));
            }
        }

        /*
        private class OWBLE_BroadcastReceiver : BroadcastReceiver
        {
            private OWBLE _owble;

            public OWBLE_BroadcastReceiver(OWBLE owble)
            {
                _owble = owble;
            }

            public override void OnReceive(Context context, Intent intent)
            {
                Debug.WriteLine("OnReceive: " + intent.Action);

                if (BluetoothAdapter.ActionStateChanged.Equals(intent.Action))
                {
                    var stateInt = intent.GetIntExtra(BluetoothAdapter.ExtraState, -1);

                    Debug.WriteLine("stateInt: " + stateInt);
                    if (stateInt == -1)
                    {
                        return;
                    }

                    var state = (State)stateInt;
                    var bluetoothState = BluetoothState.Unknown;

                    switch (state)
                    {
                        case State.Connected:
                            bluetoothState = BluetoothState.Connected;
                            break;
                        case State.Connecting:
                            bluetoothState = BluetoothState.Connecting;
                            break;
                        case State.Disconnected:
                            bluetoothState = BluetoothState.Disconnected;
                            break;
                        case State.Disconnecting:
                            bluetoothState = BluetoothState.Disconnecting;
                            break;
                        case State.Off:
                            bluetoothState = BluetoothState.Off;
                            break;
                        case State.On:
                            bluetoothState = BluetoothState.On;
                            break;
                        case State.TurningOff:
                            bluetoothState = BluetoothState.TurningOff;
                            break;
                        case State.TurningOn:
                            bluetoothState = BluetoothState.TurningOn;
                            break;
                    }

                    Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _owble?.BLEStateChanged?.Invoke(bluetoothState);
                    });
                }
            }
        }
        */


        private BluetoothAdapter _adapter;
        private BluetoothLeScanner _bleScanner;
        bool _updatingRSSI = false;

        // How long to wait for a GATT operation's native callback before treating it
        // as hung (eg the board was powered off mid-connection, which Android may not
        // report via OnConnectionStateChange for a long time, if at all).
        private static readonly TimeSpan GattOperationTimeout = TimeSpan.FromSeconds(10);
        private int _operationGeneration = 0;

        // How long to keep automatically retrying a lost connection before giving up
        // and telling the UI to treat it as a real disconnect (see
        // GiveUpReconnecting/BoardReconnectFailed) - previously this retried every 2
        // seconds forever with no way to stop short of the user manually cancelling
        // the "Reconnecting..." popup.
        private static readonly TimeSpan ReconnectGiveUpAfter = TimeSpan.FromSeconds(30);
        private DateTime _reconnectDeadlineUtc;


        TaskCompletionSource<bool> _connectTaskCompletionSource = null;
        private OWBaseBoard _board = null;
        private bool _requestingDisconnect = false;
        private bool _reconnecting = false;

        //private OWBLE_BroadcastReceiver _broadcastReceiver;
        private OWBLE_ScanCallback _scanCallback;
        private OWBLE_LeScanCallback _leScanCallback;
        private OWBLE_BluetoothGattCallback _gattCallback;
        private BluetoothGatt _bluetoothGatt;

        // Moved to be its own property for debugging.
        private Android.OS.BuildVersionCodes _sdkInt = Android.OS.Build.VERSION.SdkInt;

        public OWBLE()
        {
            //_sdkInt = BuildVersionCodes.JellyBeanMr1;

            /*
            _broadcastReceiver = new OWBLE_BroadcastReceiver(this);
            IntentFilter filter = new IntentFilter(BluetoothAdapter.ActionStateChanged);
            Xamarin.Essentials.Platform.AppContext.RegisterReceiver(_broadcastReceiver, filter);
            */
            BluetoothManager manager = Xamarin.Essentials.Platform.CurrentActivity.GetSystemService(Context.BluetoothService) as BluetoothManager;
            _adapter = manager.Adapter;
        }

        public bool IsEnabled()
        {
            return !(_adapter == null || !_adapter.IsEnabled);
        }

        public void RequestPermission()
        {
            // TODO: Request location.

            // Ensures Bluetooth is available on the device and it is enabled. If not,
            // displays a dialog requesting user permission to enable Bluetooth.
            if (_adapter == null || !_adapter.IsEnabled)
            {
                Intent enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                Xamarin.Essentials.Platform.CurrentActivity.StartActivityForResult(enableBtIntent, MainActivity.REQUEST_ENABLE_BT);
            }
        }
        


        private void OnConnectionStateChange(BluetoothGatt gatt, GattStatus status, ProfileState newState)
        {
            if (newState == ProfileState.Connected)
            {
                if (status == GattStatus.Success)
                {
                    // Requests a shorter connection interval. Android's default is
                    // fairly conservative, and since the OS's own supervision timeout
                    // (how long it waits without a response before deciding the link
                    // is dead) scales with the connection interval, a slower interval
                    // means a slower-to-notice disconnect too - not just slower data
                    // exchange. Not guaranteed to eliminate that delay (Android
                    // doesn't expose direct control over the supervision timeout
                    // itself), but this is the standard, low-risk lever available.
                    gatt.RequestConnectionPriority(GattConnectionPriority.High);

                    // Re-discover services on a reconnect too (handled below in
                    // OnServicesDiscovered) - Android hands us a fresh BluetoothGatt on
                    // each ConnectGatt() call, so previously cached characteristics
                    // would otherwise be stale and unusable.
                    gatt.DiscoverServices();
                    return;
                }

                if (_reconnecting)
                {
                    // The reconnect attempt itself failed at the connection level. Close
                    // this failed GATT client before retrying - Reconnect() replaces
                    // _bluetoothGatt with a new one via ConnectGatt(), so without this the
                    // failed client would never get closed.
                    gatt.Close();

                    if (gatt != _bluetoothGatt)
                    {
                        // A stale/duplicate callback for a GATT client that's already been
                        // superseded (eg Reconnect() already moved on, or GiveUpReconnecting
                        // already ran) - Android's BLE stack can deliver a late or repeated
                        // callback for an old client. The current connection attempt (if any)
                        // is unaffected, so don't touch shared state or restart the retry
                        // cycle for it.
                        return;
                    }

                    _bluetoothGatt = null;
                    _gattCallback = null;

                    FailAndClearPendingOperations();
                    RetryReconnect();
                    return;
                }

                // TrySetResult (not SetResult): cancelling the "connecting..." popup can
                // race with this callback landing on the GATT thread, and SetResult
                // throws if the task was already cancelled in that window.
                //
                // Initial (non-reconnecting) connect attempt failed at the connection
                // level - close the failed GATT client rather than leaking one of
                // Android's limited pool of concurrent GATT client registrations.
                gatt.Close();
                if (gatt == _bluetoothGatt)
                {
                    _bluetoothGatt = null;
                    _gattCallback = null;
                }

                _connectTaskCompletionSource?.TrySetResult(false);
                return;
            }

            if (newState == ProfileState.Disconnected)
            {
                // Release the native GATT client now that the disconnect is confirmed by
                // the OS. This was previously never called anywhere, leaking Android's
                // limited pool of concurrent GATT client registrations on every
                // connect/disconnect cycle.
                gatt.Close();

                if (gatt != _bluetoothGatt)
                {
                    // Stale/duplicate callback for an already-superseded GATT client (see
                    // the identical check above) - a reconnect or fresh connect has
                    // already replaced it, so don't tear down or restart anything for the
                    // connection that's actually current.
                    return;
                }

                var wasStillConnecting = _reconnecting == false && _connectTaskCompletionSource != null &&
                    _connectTaskCompletionSource.Task.IsCanceled == false && _connectTaskCompletionSource.Task.IsCompleted == false;
                var wasReconnecting = _reconnecting;

                _bluetoothGatt = null;
                _gattCallback = null;

                // Any queued/in-flight operations belong to this now-dead connection -
                // their BluetoothGattCharacteristic references become invalid once it's
                // closed (a reconnect rediscovers fresh ones), and this also unwedges
                // the queue if the disconnect was only detected via a forced
                // Disconnect() from HandleOperationTimeout() rather than the board
                // itself reporting it.
                FailAndClearPendingOperations();

                if (wasStillConnecting)
                {
                    // Failed during the initial connection attempt. TrySetResult (not
                    // SetResult) for the same reason as above.
                    _connectTaskCompletionSource?.TrySetResult(false);
                    return;
                }

                BoardDisconnected?.Invoke();

                if (_requestingDisconnect == false)
                {
                    // Board dropped out of range/lost power unexpectedly - try to recover,
                    // same as the iOS implementation already does. Back off briefly if this
                    // is a retry (a reconnect attempt that itself disconnected again),
                    // rather than hammering ConnectGatt() in a tight loop.
                    if (wasReconnecting)
                    {
                        RetryReconnect();
                    }
                    else
                    {
                        _reconnectDeadlineUtc = DateTime.UtcNow + ReconnectGiveUpAfter;
                        Reconnect();

                        // Reconnect()'s ConnectGatt() attempt may never invoke any
                        // callback at all if the board stays unreachable - unlike an
                        // established connection's supervision timeout, a fresh
                        // ConnectGatt() isn't guaranteed to report failure on its own.
                        // RetryReconnect()'s deadline check only ever runs following an
                        // actual callback, so without this independent watchdog, a
                        // first attempt that never calls back would leave the
                        // "Reconnecting..." popup spinning forever regardless of
                        // ReconnectGiveUpAfter (this is what was actually happening -
                        // confirmed via hardware testing).
                        Device.StartTimer(ReconnectGiveUpAfter, () =>
                        {
                            if (_reconnecting)
                            {
                                GiveUpReconnecting();
                            }
                            return false;
                        });
                    }
                }
            }
        }

        private int _queueNumber = 0;

        private void ProcessQueue()
        {
            var queueNumber = _queueNumber;
            ++_queueNumber;

            Debug.WriteLine($"ProcessQueue {queueNumber}: {_gattOperationQueue.Count}");
            if (_gattOperationQueue.Count == 0)
            {
                return;
            }

            if (_gattOperationQueueProcessing)
                return;

            _gattOperationQueueProcessing = true;

            var item = _gattOperationQueue.Dequeue();

            // If this operation's native callback never fires (eg the board was
            // powered off and Android hasn't noticed the link is dead yet), this is
            // what keeps the queue from being wedged forever.
            var operationGeneration = ++_operationGeneration;
            Device.StartTimer(GattOperationTimeout, () =>
            {
                HandleOperationTimeout(item, operationGeneration);
                return false;
            });

            switch (item.OperationType)
            {
                case OWBLE_QueueItemOperationType.Read:
                    bool didRead = _bluetoothGatt.ReadCharacteristic(item.Characteristic);
                    if (didRead == false)
                    {
                        Debug.WriteLine($"ERROR {queueNumber}: Unable to read {item.Characteristic.Uuid}");
                    }
                    break;

                case OWBLE_QueueItemOperationType.Write:
                    bool didWrite;
                    if (_sdkInt >= Android.OS.BuildVersionCodes.Tiramisu) // 33
                    {
                        didWrite = _bluetoothGatt.WriteCharacteristic(item.Characteristic, item.Data, (int)GattWriteType.Default) == 0;
                    }
                    else
                    {
#pragma warning disable CS0618
                        item.Characteristic.SetValue(item.Data);
                        didWrite = _bluetoothGatt.WriteCharacteristic(item.Characteristic);
#pragma warning restore CS0618
                    }
                    if (didWrite == false)
                    {
                        Debug.WriteLine($"ERROR {queueNumber}: Unable to write {item.Characteristic.Uuid}");
                    }
                    break;

                case OWBLE_QueueItemOperationType.Subscribe:
                    bool didSubscribe = _bluetoothGatt.SetCharacteristicNotification(item.Characteristic, true);
                    if (didSubscribe == false)
                    {
                        Debug.WriteLine($"ERROR {queueNumber}: Unable to subscribe {item.Characteristic.Uuid}");
                    }

                    var subscribeDescriptor = item.Characteristic.GetDescriptor(UUID.FromString("00002902-0000-1000-8000-00805f9b34fb"));
                    var enableNotificationValue = BluetoothGattDescriptor.EnableNotificationValue.ToArray();
                    bool didWriteSubscribeDescriptor;
                    if (_sdkInt >= Android.OS.BuildVersionCodes.Tiramisu) // 33
                    {
                        didWriteSubscribeDescriptor = _bluetoothGatt.WriteDescriptor(subscribeDescriptor, enableNotificationValue) == 0;
                    }
                    else
                    {
#pragma warning disable CS0618
                        subscribeDescriptor.SetValue(enableNotificationValue);
                        didWriteSubscribeDescriptor = _bluetoothGatt.WriteDescriptor(subscribeDescriptor);
#pragma warning restore CS0618
                    }
                    break;

                case OWBLE_QueueItemOperationType.Unsubscribe:
                    bool didUnsubscribe = _bluetoothGatt.SetCharacteristicNotification(item.Characteristic, false);
                    if (didUnsubscribe == false)
                    {
                        Debug.WriteLine($"ERROR {queueNumber}: Unable to unsubscribe {item.Characteristic.Uuid}");
                    }

                    var unsubscribeDescriptor = item.Characteristic.GetDescriptor(UUID.FromString("00002902-0000-1000-8000-00805f9b34fb"));
                    var disableNotificationValue = BluetoothGattDescriptor.DisableNotificationValue.ToArray();
                    bool didWriteUnsubscribeDescriptor;
                    if (_sdkInt >= Android.OS.BuildVersionCodes.Tiramisu) // 33
                    {
                        didWriteUnsubscribeDescriptor = _bluetoothGatt.WriteDescriptor(unsubscribeDescriptor, disableNotificationValue) == 0;
                    }
                    else
                    {
#pragma warning disable CS0618
                        unsubscribeDescriptor.SetValue(disableNotificationValue);
                        didWriteUnsubscribeDescriptor = _bluetoothGatt.WriteDescriptor(unsubscribeDescriptor);
#pragma warning restore CS0618
                    }
                    break;
            }
        }

        // Fires GattOperationTimeout after a dequeued operation is issued. If its
        // native callback already fired normally, either a new operation will have
        // bumped _operationGeneration past this one, or the queue will have gone
        // idle (_gattOperationQueueProcessing false) - either way this is a no-op.
        // Otherwise the operation is presumed hung (eg the board went silent mid-op);
        // this fails it and forces a disconnect rather than leaving the queue wedged.
        private void HandleOperationTimeout(OWBLE_QueueItem item, int operationGeneration)
        {
            if (operationGeneration != _operationGeneration || _gattOperationQueueProcessing == false)
            {
                return;
            }

            var uuid = item.Characteristic.Uuid.ToString().ToLower();
            Debug.WriteLine($"GATT operation timed out ({item.OperationType}): {uuid}");

            switch (item.OperationType)
            {
                case OWBLE_QueueItemOperationType.Read:
                    if (_readQueue.ContainsKey(uuid))
                    {
                        var readItem = _readQueue[uuid];
                        _readQueue.Remove(uuid);
                        readItem.TrySetResult(null);
                    }
                    break;

                case OWBLE_QueueItemOperationType.Write:
                    var writeRequest = _writeQueue.FirstOrDefault(t => t.CharacteristicId.Equals(uuid));
                    if (writeRequest != null)
                    {
                        _writeQueue.Remove(writeRequest);
                        writeRequest.CompletionSource.TrySetResult(null);
                    }
                    break;

                case OWBLE_QueueItemOperationType.Subscribe:
                    if (_subscribeQueue.ContainsKey(uuid))
                    {
                        var subscribeItem = _subscribeQueue[uuid];
                        _subscribeQueue.Remove(uuid);
                        subscribeItem.TrySetResult(null);
                    }
                    break;

                case OWBLE_QueueItemOperationType.Unsubscribe:
                    if (_unsubscribeQueue.ContainsKey(uuid))
                    {
                        var unsubscribeItem = _unsubscribeQueue[uuid];
                        _unsubscribeQueue.Remove(uuid);
                        unsubscribeItem.TrySetResult(null);
                    }
                    break;
            }

            // Do NOT clear _gattOperationQueueProcessing or call ProcessQueue() here.
            // Android's GATT callbacks carry no ID correlating them to a specific
            // ReadCharacteristic()/WriteCharacteristic()/WriteDescriptor() call - if
            // this timed-out op's native callback is merely late rather than truly
            // lost, and we dispatched a new op for the same characteristic in the
            // meantime, there would be no way to tell which op a subsequent callback
            // belongs to (both report the same UUID). Force a disconnect instead:
            // OnConnectionStateChange's Disconnected handling is what actually clears
            // _gattOperationQueueProcessing and resumes the queue, and only once a
            // fresh connection (and therefore an unambiguous callback stream) exists.
            Debug.WriteLine("GATT operation timed out - treating the connection as dead and forcing a disconnect.");

            if (_bluetoothGatt != null)
            {
                _bluetoothGatt.Disconnect();
            }
            else
            {
                // Already disconnected by the time this fired - nothing left to wait
                // on, so unwedge the queue directly instead of leaving
                // _gattOperationQueueProcessing stuck true forever.
                FailAndClearPendingOperations();
            }
        }

        // Fails and clears every pending/queued operation. Used both for a normal
        // manual Disconnect() and for any detected/forced disconnect - in every case
        // the BluetoothGattCharacteristic references queued items hold become invalid
        // once the connection they came from is gone, and this also resets the queue
        // so a fresh connection isn't left permanently wedged behind a dead one.
        private void FailAndClearPendingOperations()
        {
            foreach (var pending in _readQueue.Values)
            {
                pending.TrySetResult(null);
            }
            _readQueue.Clear();

            foreach (var pending in _writeQueue)
            {
                pending.CompletionSource.TrySetResult(null);
            }
            _writeQueue.Clear();

            foreach (var pending in _subscribeQueue.Values)
            {
                pending.TrySetResult(null);
            }
            _subscribeQueue.Clear();

            foreach (var pending in _unsubscribeQueue.Values)
            {
                pending.TrySetResult(null);
            }
            _unsubscribeQueue.Clear();

            _gattOperationQueue.Clear();
            _gattOperationQueueProcessing = false;
        }

        // Pre-API 33 callback: the value isn't delivered directly, so GetValue() is
        // the only way to get it here.
        private void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
#pragma warning disable CS0618
            OnCharacteristicReadValue(characteristic, status, characteristic.GetValue());
#pragma warning restore CS0618
        }

        // API 33+ callback: value is delivered directly, avoiding GetValue() and the
        // race window between the callback firing and a subsequent read of it.
        private void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, byte[] value, GattStatus status)
        {
            OnCharacteristicReadValue(characteristic, status, value);
        }

        private void OnCharacteristicReadValue(BluetoothGattCharacteristic characteristic, GattStatus status, byte[] dataBytes)
        {
            var uuid = characteristic.Uuid.ToString().ToLower();

            if (_readQueue.ContainsKey(uuid) == false)
            {
                // Already resolved - most likely HandleOperationTimeout gave up on
                // this one and moved the queue on before this (late) callback finally
                // arrived. Don't touch queue-advancement state here, or this stale
                // callback could clobber a different, still-legitimately-in-flight
                // operation that's since taken its place.
                return;
            }

            var readItem = _readQueue[uuid];
            _readQueue.Remove(uuid);

            if (status != GattStatus.Success)
            {
                // A failed read used to fall through and hand back whatever
                // characteristic.GetValue() happened to contain (stale/incorrect
                // data) as if the read had succeeded. Resolve with null instead -
                // OWBoard.SetValue() already treats null data as "ignore this update".
                Debug.WriteLine($"OnCharacteristicRead Error: read of {uuid} failed with status {status}");
                readItem.SetResult(null);
            }
            else
            {
                if (OWBoard.SerialWriteUUID.Equals(uuid, StringComparison.InvariantCultureIgnoreCase) == false &&
                    OWBoard.SerialReadUUID.Equals(uuid, StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    // If our system is little endian, reverse the array.
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(dataBytes);
                    }
                }

                readItem.SetResult(dataBytes);
            }

            _gattOperationQueueProcessing = false;
            ProcessQueue();
        }


        private void OnCharacteristicWrite(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status)
        {
            var uuid = characteristic.Uuid.ToString().ToLower();

            var writeCharacteristicValueRequest = _writeQueue.FirstOrDefault(t => t.CharacteristicId.Equals(uuid));

            if (writeCharacteristicValueRequest == null)
            {
                // Already resolved (eg by HandleOperationTimeout) - stale callback for
                // an operation the queue already moved past. See the same guard in
                // OnCharacteristicReadValue for why this must not touch queue state.
                return;
            }

            _writeQueue.Remove(writeCharacteristicValueRequest);

            // Use the bytes we asked to write instead of reading them back via the
            // deprecated characteristic.GetValue() - we already know exactly what
            // was written (this is the same value WriteValue() queued below,
            // before the endian-reversal applied for the wire), so there's no need
            // to round-trip through native state to reconstruct it.
            writeCharacteristicValueRequest.CompletionSource.SetResult(writeCharacteristicValueRequest.Data);

            _gattOperationQueueProcessing = false;
            ProcessQueue();
        }


        // Pre-API 33 callback: the value isn't delivered directly, so GetValue() is
        // the only way to get it here.
        private void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
#pragma warning disable CS0618
            OnCharacteristicChangedValue(characteristic, characteristic.GetValue());
#pragma warning restore CS0618
        }

        // API 33+ callback: value is delivered directly.
        private void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, byte[] value)
        {
            OnCharacteristicChangedValue(characteristic, value);
        }

        private void OnCharacteristicChangedValue(BluetoothGattCharacteristic characteristic, byte[] dataBytes)
        {
            var uuid = characteristic.Uuid.ToString().ToLower();

            if (_notifyList.Contains(uuid))
            {
                if (OWBoard.SerialWriteUUID.Equals(uuid, StringComparison.InvariantCultureIgnoreCase) == false &&
                   OWBoard.SerialReadUUID.Equals(uuid, StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    // If our system is little endian, reverse the array.
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(dataBytes);
                    }
                }

                BoardValueChanged.Invoke(uuid, dataBytes);
            }
        }


        // Pre-API 33 callback. Shared by both signatures below since this is
        // currently a no-op either way.
        public void OnDescriptorRead(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, [GeneratedEnum] GattStatus status, byte[] value = null)
        {
            // TODO: ?
        }

        public void OnDescriptorWrite(BluetoothGatt gatt, BluetoothGattDescriptor descriptor, [GeneratedEnum] GattStatus status)
        {
            var uuid = descriptor.Characteristic.Uuid.ToString().ToLower();

            // Check if its a subscribe or unsubscribe descriptor. Disambiguated by
            // which queue this uuid is sitting in - not by reading the descriptor's
            // value back via the deprecated GetValue() - since we already know
            // exactly which of the two we asked to write.
            bool resolved = true;
            if (descriptor.Uuid.ToString().ToLower() == "00002902-0000-1000-8000-00805f9b34fb")
            {
                if (_subscribeQueue.ContainsKey(uuid))
                {
                    var subscribeItem = _subscribeQueue[uuid];
                    _subscribeQueue.Remove(uuid);
                    subscribeItem.SetResult(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
                }
                else if (_unsubscribeQueue.ContainsKey(uuid))
                {
                    var unsubscribeItem = _unsubscribeQueue[uuid];
                    _unsubscribeQueue.Remove(uuid);
                    unsubscribeItem.SetResult(BluetoothGattDescriptor.DisableNotificationValue.ToArray());
                }
                else
                {
                    // Already resolved (eg by HandleOperationTimeout) - a stale
                    // callback for an operation the queue already moved past. See the
                    // same guard in OnCharacteristicReadValue for why this must not
                    // touch queue state.
                    resolved = false;
                    Debug.WriteLine($"OnDescriptorWrite Error: Unhandled descriptor of {descriptor.Uuid} on {uuid}.");
                }
            }
            else
            {
                // Never issued by this class outside the CCCD subscribe/unsubscribe
                // above - not something to advance the queue for.
                resolved = false;
                Debug.WriteLine($"OnDescriptorWrite Error: Unhandled descriptor of {descriptor.Uuid} on {uuid}.");
            }

            if (resolved)
            {
                _gattOperationQueueProcessing = false;
                ProcessQueue();
            }
        }

        public void OnReadRemoteRssi(BluetoothGatt gatt, int rssi, [GeneratedEnum] GattStatus status)
        {
            _updatingRSSI = false;

            if (status != GattStatus.Success)
            {
                // Previously ignored entirely - a failed read (eg a stale/dying
                // connection) would still invoke RSSIUpdated with whatever garbage
                // value came back, as if it had succeeded.
                Debug.WriteLine($"OnReadRemoteRssi Error: status {status}");
                return;
            }

            RSSIUpdated?.Invoke(rssi);
        }



        #region IOWBLE
        public Action<BluetoothState> BLEStateChanged { get; set; }
        public Action<OWBaseBoard> BoardDiscovered { get; set; }
        public Action<OWBoard> BoardConnected { get; set; }
        public Action<string, byte[]> BoardValueChanged { get; set; }
        public Action<int> RSSIUpdated { get; set; }
        public Action BoardDisconnected { get; set; }
        public Action BoardReconnecting { get; set; }
        public Action BoardReconnected { get; set; }
        public Action BoardReconnectFailed { get; set; }
        public Action<String> ErrorOccurred { get; set; }


        bool _isScanning = false;
        public bool IsScanning
        {
            get
            {
                return _isScanning;
            }
            set
            {
                if (_isScanning == value)
                    return;

                _isScanning = value;
                OnPropertyChanged();
            }
        }

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


       
        public Task<bool> Connect(OWBaseBoard board, CancellationToken cancellationToken)
        {
            _board = board;
            _requestingDisconnect = false;
            _reconnecting = false;
            _gattOperationQueueProcessing = false;

            var connectTaskCompletionSource = new TaskCompletionSource<bool>();
            _connectTaskCompletionSource = connectTaskCompletionSource;

            if (board.NativePeripheral is BluetoothDevice device)
            {
                _gattCallback = new OWBLE_BluetoothGattCallback(this);
                _bluetoothGatt = device.ConnectGatt(Xamarin.Essentials.Platform.CurrentActivity, false, _gattCallback);

                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(() =>
                    {
                        // Was previously never observed at all - cancelling the
                        // "connecting..." popup had no effect on the underlying BLE
                        // connection attempt, which would complete/fail regardless.
                        if (connectTaskCompletionSource.TrySetCanceled(cancellationToken))
                        {
                            _bluetoothGatt?.Disconnect();
                        }
                    });
                }
            }

            return connectTaskCompletionSource.Task;
        }

        public Task Disconnect()
        {
            _requestingDisconnect = true;
            _reconnecting = false;

            // TrySetCanceled: this runs on whatever thread calls Disconnect(), which can
            // race with a GATT callback resolving the same TaskCompletionSource on the
            // Binder callback thread.
            _connectTaskCompletionSource?.TrySetCanceled();

            _connectTaskCompletionSource = null;

            FailAndClearPendingOperations();

            // The native BluetoothGatt.Close() (which releases the GATT client's slot -
            // Android only allows a limited number of these system-wide) happens in
            // OnConnectionStateChange once the disconnect is confirmed by the OS, rather
            // than here, to avoid racing the native disconnect callback.
            if (_bluetoothGatt != null)
            {
                _bluetoothGatt.Disconnect();
            }

            _board = null;

            return Task.CompletedTask;
        }

        // Called when the connection drops without Disconnect() having been requested
        // (eg the board went out of range or lost power). Attempts to re-establish the
        // connection, mirroring the behaviour already implemented on iOS.
        private void Reconnect()
        {
            BoardReconnecting?.Invoke();
            _reconnecting = true;

            if (_board?.NativePeripheral is BluetoothDevice device)
            {
                _gattCallback = new OWBLE_BluetoothGattCallback(this);
                _bluetoothGatt = device.ConnectGatt(Xamarin.Essentials.Platform.CurrentActivity, false, _gattCallback);
            }
        }

        // Used when a reconnect attempt itself fails, to avoid hammering ConnectGatt()
        // in a tight loop (Android's BLE stack does not respond well to rapid repeated
        // connection attempts) if the board is genuinely out of range for a while.
        private void RetryReconnect()
        {
            Device.StartTimer(TimeSpan.FromSeconds(2), () =>
            {
                // _reconnecting is false if the standalone watchdog in
                // OnConnectionStateChange already gave up (or a reconnect
                // succeeded) while this 2s delay was pending - don't resurrect
                // that by dispatching yet another ConnectGatt() attempt.
                if (_requestingDisconnect || _reconnecting == false)
                {
                    return false;
                }

                if (DateTime.UtcNow >= _reconnectDeadlineUtc)
                {
                    GiveUpReconnecting();
                    return false;
                }

                Reconnect();
                return false;
            });
        }

        // Stops retrying and tells the UI to treat this like a real disconnect,
        // instead of leaving the "Reconnecting..." popup spinning forever if the
        // board genuinely isn't coming back (eg it was left powered off).
        private void GiveUpReconnecting()
        {
            _reconnecting = false;

            // A ConnectGatt() attempt may still be pending if we're giving up
            // precisely because it never called back at all (the usual case the
            // standalone watchdog exists for) - close it so a late callback can't
            // land after the UI has already disconnected and moved on.
            _bluetoothGatt?.Close();
            _bluetoothGatt = null;
            _gattCallback = null;

            BoardReconnectFailed?.Invoke();
        }

        public async void StartScanning()
        {
            if (IsScanning)
                return;

            IsScanning = true;

            // TODO: Handle power on state.

            if (_sdkInt >= Android.OS.BuildVersionCodes.Lollipop) // 21
            {
                _bleScanner = _adapter.BluetoothLeScanner;
                _scanCallback = new OWBLE_ScanCallback(this);
                var scanFilters = new List<ScanFilter>();
                var scanSettingsBuilder = new ScanSettings.Builder();

                var scanFilterBuilder = new ScanFilter.Builder();
                scanFilterBuilder.SetServiceUuid(OWBoard.ServiceUUID.ToParcelUuid());
                scanFilters.Add(scanFilterBuilder.Build());
                _bleScanner.StartScan(scanFilters, scanSettingsBuilder.Build(), _scanCallback);
            }
            else if (_sdkInt >= Android.OS.BuildVersionCodes.JellyBeanMr2) // 18
            {
                _leScanCallback = new OWBLE_LeScanCallback(this);
#pragma warning disable 0618
                _adapter.StartLeScan(new Java.Util.UUID[] { OWBoard.ServiceUUID.ToUUID() }, _leScanCallback);
#pragma warning restore 0618
            }
            else
            {
                throw new NotImplementedException("Can't run bluetooth scans on device lower than Android 4.3");
            }

            await Task.Delay(15 * 1000);

            StopScanning();
        }

        public void StopScanning()
        {
            if (IsScanning == false)
                return;


            if (_sdkInt >= Android.OS.BuildVersionCodes.Lollipop) // 21
            {
                _bleScanner.StopScan(_scanCallback);
            }
            else
            {
#pragma warning disable 0618
                _adapter.StopLeScan(_leScanCallback);
#pragma warning restore 0618
            }

            IsScanning = false;
        }


        public Task<byte[]> ReadValue(string characteristicGuid, bool important = false)
        {
            Debug.WriteLine($"ReadValue: {characteristicGuid}");

            if (_bluetoothGatt == null)
                return null;

            var uuid = characteristicGuid.ToLower();

            // TODO: Check for connected devices?
            if (_characteristics.ContainsKey(uuid) == false)
            {
                // TODO Error?
                return null;
            }

            // Already awaiting it.
            if (_readQueue.ContainsKey(uuid))
            {
                return _readQueue[uuid].Task;
            }

            var taskCompletionSource = new TaskCompletionSource<byte[]>();

            if (important)
            {
                // TODO: Put this at the start of the queue.
                _readQueue.Add(uuid, taskCompletionSource);
            }
            else
            {
                _readQueue.Add(uuid, taskCompletionSource);
            }

            _gattOperationQueue.Enqueue(new OWBLE_QueueItem(_characteristics[uuid], OWBLE_QueueItemOperationType.Read));

            ProcessQueue();

            return taskCompletionSource.Task;
        }

        public Task<byte[]> WriteValue(string characteristicGuid, byte[] data, bool overrideExistingQueue = false)
        {
            Debug.WriteLine($"WriteValue: {characteristicGuid}");
            if (_bluetoothGatt == null)
                return null;

            if (data.Length > 20)
            {
                // TODO: Error, some Android BLE devices do not handle > 20byte packets well.
                return null;
            }

            var uuid = characteristicGuid.ToLower();

            // TODO: Check for connected devices?
            if (_characteristics.ContainsKey(uuid) == false)
            {
                // TODO Error?
                return null;
            }

            // TODO: Handle this.
            /*
            if (_readQueue.ContainsKey(uuid))
            {
                return _readQueue[uuid].Task;
            }
            */

            var taskCompletionSource = new TaskCompletionSource<byte[]>();

            CharacteristicValueRequest characteristicValueRequest = new CharacteristicValueRequest(uuid, taskCompletionSource, data);


            if (overrideExistingQueue)
            {
                _writeQueue.RemoveAll(t => t.CharacteristicId.Equals(uuid));
            }
            _writeQueue.Add(characteristicValueRequest);

           
            byte[] dataBytes = null;
            if (data != null)
            {
                dataBytes = new byte[data.Length];
                Array.Copy(data, dataBytes, data.Length);
            
                if (OWBoard.SerialWriteUUID.Equals(uuid, StringComparison.InvariantCultureIgnoreCase) == false &&
                       OWBoard.SerialReadUUID.Equals(uuid, StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    // If our system is little endian, reverse the array.
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(dataBytes);
                    }
                }
            }


            _gattOperationQueue.Enqueue(new OWBLE_QueueItem(_characteristics[uuid], OWBLE_QueueItemOperationType.Write, dataBytes));

            ProcessQueue();

            return taskCompletionSource.Task;
        }

        public Task SubscribeValue(string characteristicGuid, bool important = false)
        {
            Debug.WriteLine($"SubscribeValue: {characteristicGuid}");
            if (_bluetoothGatt == null)
                return null;

            var uuid = characteristicGuid.ToLower();

            // TODO: Check for connected devices?
            if (_characteristics.ContainsKey(uuid) == false)
            {
                // TODO Error?
                return null;
            }

            _notifyList.Add(uuid);

            var taskCompletionSource = new TaskCompletionSource<byte[]>();

            if (important)
            {
                // TODO: Put this at the start of the queue.
                _subscribeQueue.Add(uuid, taskCompletionSource);
            }
            else
            {
                _subscribeQueue.Add(uuid, taskCompletionSource);
            }

            _gattOperationQueue.Enqueue(new OWBLE_QueueItem(_characteristics[uuid], OWBLE_QueueItemOperationType.Subscribe));

            ProcessQueue();

            return taskCompletionSource.Task;
        }

        public Task UnsubscribeValue(string characteristicGuid, bool important = false)
        {
            Debug.WriteLine($"UnsubscribeValue: {characteristicGuid}");
            if (_bluetoothGatt == null)
                return null;

            var uuid = characteristicGuid.ToLower();

            // TODO: Check for connected devices?
            if (_characteristics.ContainsKey(uuid) == false)
            {
                // TODO Error?
                return null;
            }

            _notifyList.RemoveAll(x => x == uuid);

            var taskCompletionSource = new TaskCompletionSource<byte[]>();

            if (important)
            {
                // TODO: Put this at the start of the queue.
                _unsubscribeQueue.Add(uuid, taskCompletionSource);
            }
            else
            {
                _unsubscribeQueue.Add(uuid, taskCompletionSource);
            }

            _gattOperationQueue.Enqueue(new OWBLE_QueueItem(_characteristics[uuid], OWBLE_QueueItemOperationType.Unsubscribe));

            ProcessQueue();

            return taskCompletionSource.Task;
        }

        public bool BluetoothEnabled()
        {
            // Was using the obsolete BluetoothAdapter.DefaultAdapter static - the
            // constructor already resolves the adapter via BluetoothManager into
            // _adapter, so reuse that instead of a second, deprecated lookup.
            if (_adapter == null)
            {
                // Device does not support Bluetooth
                return false;
            }
            else if (_adapter.IsEnabled == false)
            {
                // Bluetooth is not enabled
                return false;
            }

            // Bluetooth is enabled
            return true;
        }

        public async Task<bool> ReadyToScan()
        {
            if ((int)Android.OS.Build.VERSION.SdkInt >= 31)
            {
                var permissionStatus = await Permissions.CheckStatusAsync<BluetoothPermission>();
                if (permissionStatus == PermissionStatus.Granted)
                {
                    return true;
                }
            }
            else
            {
                var permissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (permissionStatus == PermissionStatus.Granted || permissionStatus == PermissionStatus.Restricted)
                {
                    return true;
                }
            }

            return false;
        }

        public void Shutdown()
        {
            // TODO: Handle this.
        }

        public void RequestRSSIUpdate()
        {
            if (_updatingRSSI)
            {
                return;
            }

            _updatingRSSI = true;
            _bluetoothGatt?.ReadRemoteRssi();

            // OnReadRemoteRssi may never fire at all on a dead/dying connection,
            // which would otherwise permanently block every future RSSI poll via the
            // guard above. Reset unconditionally - comparing against the
            // BluetoothGatt that was current when this fired would leave the flag
            // stuck true forever if a reconnect swapped in a new one before this
            // timer ran, since the guard above would then block every subsequent
            // request from ever reaching ReadRemoteRssi() again.
            Device.StartTimer(TimeSpan.FromSeconds(5), () =>
            {
                _updatingRSSI = false;
                return false;
            });
        }
        #endregion
    }
}
