using System;
using System.Threading;
using System.Threading.Tasks;
using OWCE.DependencyInterfaces;

// Types like OWBaseBoard and BluetoothState live in the OWCE namespace itself,
// not OWCE.Tests - not implicitly visible just because this file is nested
// under it.
using OWCE;

namespace OWCE.Tests
{
    // Minimal IOWBLE stand-in for driving OWBoard's parsing/state logic in tests
    // without a real BLE stack. Only BoardValueChanged is actually exercised today
    // (via RaiseBoardValueChanged) - the rest are no-op stubs to satisfy the
    // interface.
    public class FakeOWBLE : IOWBLE
    {
        public bool IsScanning => false;

        public Action<string> ErrorOccurred { get; set; }
        public Action<OWBaseBoard> BoardDiscovered { get; set; }
        public Action<BluetoothState> BLEStateChanged { get; set; }
        public Action<string, byte[]> BoardValueChanged { get; set; }
        public Action<int> RSSIUpdated { get; set; }
        public Action BoardDisconnected { get; set; }
        public Action BoardReconnecting { get; set; }
        public Action BoardReconnected { get; set; }
        public Action BoardReconnectFailed { get; set; }

        public void StartScanning() { }
        public void StopScanning() { }
        public Task<bool> ReadyToScan() => Task.FromResult(true);
        public void Shutdown() { }
        public void RequestRSSIUpdate() { }

        public Task<bool> Connect(OWBaseBoard board, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task Disconnect() => Task.CompletedTask;

        public Task<byte[]> ReadValue(string characteristicGuid, bool important = false) => Task.FromResult<byte[]>(null);
        public Task<byte[]> WriteValue(string characteristicGuid, byte[] data, bool overrideExistingQueue = false) => Task.FromResult<byte[]>(null);
        public Task SubscribeValue(string characteristicGuid, bool important = false) => Task.CompletedTask;
        public Task UnsubscribeValue(string characteristicGuid, bool important = false) => Task.CompletedTask;

        // Simulates a characteristic notification/read arriving from the board -
        // this is what actually drives OWBoard's private SetValue parsing logic.
        public void RaiseBoardValueChanged(string characteristicGuid, byte[] data)
        {
            BoardValueChanged?.Invoke(characteristicGuid, data);
        }
    }
}
