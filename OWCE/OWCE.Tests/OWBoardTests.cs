using System;
using OWCE;
using Xunit;

namespace OWCE.Tests
{
    public class OWBoardTests
    {
        private static OWBoard CreateBoard(out FakeOWBLE fakeOwble)
        {
            fakeOwble = new FakeOWBLE();
            return new OWBoard(fakeOwble, new OWBaseBoard("test-id", "Test Board"));
        }

        private static byte[] Bytes(ushort value) => BitConverter.GetBytes(value);

        // --- RideModeString: cross-referenced against community BLE projects in
        // issue #36 - this pins that mapping down so a future edit can't silently
        // drift from it. ---

        [Theory]
        [InlineData(OWBoardType.V1, 1, "Classic")]
        [InlineData(OWBoardType.V1, 2, "Extreme")]
        [InlineData(OWBoardType.V1, 3, "Elevated")]
        [InlineData(OWBoardType.V1, 99, "Unknown")]
        [InlineData(OWBoardType.Plus, 4, "Sequoia")]
        [InlineData(OWBoardType.Plus, 5, "Cruz")]
        [InlineData(OWBoardType.Plus, 6, "Mission")]
        [InlineData(OWBoardType.Plus, 7, "Elevated")]
        [InlineData(OWBoardType.Plus, 8, "Delirium")]
        [InlineData(OWBoardType.Plus, 9, "Custom")]
        [InlineData(OWBoardType.XR, 4, "Sequoia")]
        [InlineData(OWBoardType.XR, 9, "Custom")]
        // Pint mode 4 ("Sequoia") isn't a selectable Pint mode in OWCE's own UI
        // (GetAvailableRideModes has no case for it), but the board can in
        // principle report it - confirmed via the rewheel project's mode table
        // (see #36). Was rendering "Unknown" before this fix.
        [InlineData(OWBoardType.Pint, 4, "Sequoia")]
        [InlineData(OWBoardType.Pint, 5, "Redwood")]
        [InlineData(OWBoardType.Pint, 6, "Pacific")]
        [InlineData(OWBoardType.Pint, 7, "Elevated")]
        [InlineData(OWBoardType.Pint, 8, "Skyline")]
        [InlineData(OWBoardType.PintX, 4, "Sequoia")]
        [InlineData(OWBoardType.PintX, 5, "Redwood")]
        [InlineData(OWBoardType.GT, 3, "Bay")]
        [InlineData(OWBoardType.GT, 4, "Roam")]
        [InlineData(OWBoardType.GT, 5, "Flow")]
        [InlineData(OWBoardType.GT, 6, "Highline")]
        [InlineData(OWBoardType.GT, 7, "Elevated")]
        [InlineData(OWBoardType.GT, 8, "Apex")]
        [InlineData(OWBoardType.GT, 9, "Custom")]
        [InlineData(OWBoardType.Unknown, 4, "Unknown")]
        public void RideModeString_MatchesBoardTypeAndMode(OWBoardType boardType, ushort rideMode, string expected)
        {
            var board = CreateBoard(out _);
            board.BoardType = boardType;
            board.RideMode = rideMode;

            Assert.Equal(expected, board.RideModeString);
        }

        [Theory]
        [InlineData(OWBoard.SerialNumberUUID, "SerialNumber")]
        [InlineData(OWBoard.RideModeUUID, "RideMode")]
        [InlineData(OWBoard.BatteryPercentUUID, "BatteryPercent")]
        [InlineData("not-a-real-uuid", "Unknown")]
        public void GetNameFromUUID_MapsKnownUUIDs(string uuid, string expected)
        {
            Assert.Equal(expected, OWBoard.GetNameFromUUID(uuid));
        }

        // --- SetValue, driven through the same BoardValueChanged path a real BLE
        // notification/read uses - this is the actual byte-parsing logic the app
        // relies on for every displayed value. ---

        [Theory]
        [InlineData(new byte[] { 50, 0 }, 50)] // data[0] wins when non-zero.
        [InlineData(new byte[] { 0, 75 }, 75)] // falls back to data[1] when data[0] is 0.
        public void BatteryPercent_ParsesFromEitherByte(byte[] data, int expected)
        {
            var board = CreateBoard(out var fakeOwble);

            fakeOwble.RaiseBoardValueChanged(OWBoard.BatteryPercentUUID, data);

            Assert.Equal(expected, board.BatteryPercent);
        }

        [Fact]
        public void Temperature_SplitsMotorAndController()
        {
            var board = CreateBoard(out var fakeOwble);

            fakeOwble.RaiseBoardValueChanged(OWBoard.TemperatureUUID, new byte[] { 42, 55 });

            Assert.Equal(42, board.MotorTemperature);
            Assert.Equal(55, board.ControllerTemperature);
        }

        [Theory]
        [InlineData(OWBoardType.V1, 1)] // V1/Plus read the second byte.
        [InlineData(OWBoardType.Plus, 1)]
        [InlineData(OWBoardType.XR, 0)] // Everything else reads the first byte.
        [InlineData(OWBoardType.GT, 0)]
        public void BatteryTemperature_ByteIndexDependsOnBoardType(OWBoardType boardType, int expectedSourceByteIndex)
        {
            var board = CreateBoard(out var fakeOwble);
            board.BoardType = boardType;
            byte[] data = { 10, 20 };

            fakeOwble.RaiseBoardValueChanged(OWBoard.BatteryTemperatureUUID, data);

            Assert.Equal((float)data[expectedSourceByteIndex], board.BatteryTemperature);
        }

        [Theory]
        [InlineData((ushort)1, OWBoardType.V1)]
        [InlineData((ushort)2999, OWBoardType.V1)]
        [InlineData((ushort)3000, OWBoardType.Plus)]
        [InlineData((ushort)3999, OWBoardType.Plus)]
        [InlineData((ushort)4000, OWBoardType.XR)]
        [InlineData((ushort)4999, OWBoardType.XR)]
        [InlineData((ushort)5000, OWBoardType.Pint)]
        [InlineData((ushort)5999, OWBoardType.Pint)]
        [InlineData((ushort)6000, OWBoardType.GT)]
        [InlineData((ushort)6999, OWBoardType.GT)]
        [InlineData((ushort)7000, OWBoardType.PintX)]
        [InlineData((ushort)7999, OWBoardType.PintX)]
        public void HardwareRevision_InfersBoardTypeFromRange(ushort hardwareRevision, OWBoardType expectedBoardType)
        {
            var board = CreateBoard(out var fakeOwble);

            fakeOwble.RaiseBoardValueChanged(OWBoard.HardwareRevisionUUID, Bytes(hardwareRevision));

            Assert.Equal(hardwareRevision, board.HardwareRevision);
            Assert.Equal(expectedBoardType, board.BoardType);
        }

        [Fact]
        public void HardwareRevision_ZeroDoesNotInferABoardType()
        {
            var board = CreateBoard(out var fakeOwble);

            fakeOwble.RaiseBoardValueChanged(OWBoard.HardwareRevisionUUID, Bytes(0));

            Assert.Equal(0, board.HardwareRevision);
            Assert.Equal(OWBoardType.Unknown, board.BoardType);
        }

        [Theory]
        [InlineData((ushort)5000)] // Pint
        [InlineData((ushort)6000)] // GT
        [InlineData((ushort)7000)] // PintX
        public void HardwareRevision_DefaultsSimpleStopToFalseOnceKnown(ushort hardwareRevision)
        {
            var board = CreateBoard(out var fakeOwble);

            Assert.Null(board.SimpleStopEnabled);

            fakeOwble.RaiseBoardValueChanged(OWBoard.HardwareRevisionUUID, Bytes(hardwareRevision));

            Assert.False(board.SimpleStopEnabled);
        }

        [Fact]
        public void CurrentAmps_UsesPerBoardScaleFactor()
        {
            var board = CreateBoard(out var fakeOwble);
            board.BoardType = OWBoardType.Plus;

            fakeOwble.RaiseBoardValueChanged(OWBoard.CurrentAmpsUUID, Bytes(1000));

            Assert.Equal(1.8f, board.CurrentAmps, 3);
            Assert.False(board.IsRegen);
        }

        [Fact]
        public void CurrentAmps_NegativeValuesDecodeAsTwosComplementAndSetIsRegen()
        {
            var board = CreateBoard(out var fakeOwble);
            board.BoardType = OWBoardType.Plus;

            // 65036 as an unsigned 16-bit value is -500 in two's complement.
            fakeOwble.RaiseBoardValueChanged(OWBoard.CurrentAmpsUUID, Bytes(65036));

            Assert.Equal(-0.9f, board.CurrentAmps, 3);
            Assert.True(board.IsRegen);
        }

        [Fact]
        public void CurrentAmps_UnknownBoardTypeIsIgnoredRatherThanThrowing()
        {
            var board = CreateBoard(out var fakeOwble);

            var exception = Record.Exception(() =>
                fakeOwble.RaiseBoardValueChanged(OWBoard.CurrentAmpsUUID, Bytes(1000)));

            Assert.Null(exception);
            Assert.Equal(0f, board.CurrentAmps);
        }

        [Theory]
        [InlineData(OWBoardType.V1, 0.00009f)]
        [InlineData(OWBoardType.Plus, 0.00018f)]
        [InlineData(OWBoardType.GT, 0.00018f)]
        public void TripAmpHours_UsesV1OrDefaultScaleFactor(OWBoardType boardType, float scaleFactor)
        {
            var board = CreateBoard(out var fakeOwble);
            board.BoardType = boardType;

            fakeOwble.RaiseBoardValueChanged(OWBoard.TripAmpHoursUUID, Bytes(1000));

            Assert.Equal(1000f * scaleFactor, board.TripAmpHours, 5);
        }

        [Fact]
        public void BatteryVoltage_IsTenthsOfAVolt()
        {
            var board = CreateBoard(out var fakeOwble);

            fakeOwble.RaiseBoardValueChanged(OWBoard.BatteryVoltageUUID, Bytes(632));

            Assert.Equal(63.2f, board.BatteryVoltage, 3);
        }

        [Theory]
        [InlineData((ushort)1800, 0f)]
        [InlineData((ushort)1700, 10f)]
        [InlineData((ushort)1900, -10f)]
        public void Pitch_IsOffsetAndScaledFromRawValue(ushort rawValue, float expected)
        {
            var board = CreateBoard(out var fakeOwble);

            fakeOwble.RaiseBoardValueChanged(OWBoard.PitchUUID, Bytes(rawValue));

            Assert.Equal(expected, board.Pitch, 3);
        }

        [Theory]
        [InlineData((ushort)1, true)]
        [InlineData((ushort)0, false)]
        [InlineData((ushort)2, false)]
        public void LightMode_OnlyTrueWhenValueIsExactlyOne(ushort rawValue, bool expectedLightMode)
        {
            var board = CreateBoard(out var fakeOwble);

            fakeOwble.RaiseBoardValueChanged(OWBoard.LightModeUUID, Bytes(rawValue));

            Assert.Equal(expectedLightMode, board.LightMode);
        }

        [Fact]
        public void BatteryCells_ModernFirmwareDecodesCellIdFromHighNibble()
        {
            var board = CreateBoard(out var fakeOwble);
            // FirmwareRevision must be set first - it gates which of the two
            // BatteryCells decode paths SetValue uses.
            fakeOwble.RaiseBoardValueChanged(OWBoard.FirmwareRevisionUUID, Bytes(4141));

            // Cell 5, raw 12-bit reading of 1000 -> 1000 * 0.0011 volts.
            ushort packed = (ushort)((5 << 12) | 1000);
            fakeOwble.RaiseBoardValueChanged(OWBoard.BatteryCellsUUID, Bytes(packed));

            Assert.Equal(1.1f, board.BatteryCells.GetCell(5), 3);
        }

        [Fact]
        public void BatteryCells_PreModernFirmwareDecodesCellIdFromSecondByte()
        {
            var board = CreateBoard(out var fakeOwble);
            fakeOwble.RaiseBoardValueChanged(OWBoard.FirmwareRevisionUUID, Bytes(4000));

            fakeOwble.RaiseBoardValueChanged(OWBoard.BatteryCellsUUID, new byte[] { 50, 3 });

            Assert.Equal(1.0f, board.BatteryCells.GetCell(3), 3);
        }

        [Fact]
        public void SetValue_IgnoresPayloadsThatArentTwoBytes()
        {
            var board = CreateBoard(out var fakeOwble);

            fakeOwble.RaiseBoardValueChanged(OWBoard.RpmUUID, new byte[] { 1, 2, 3 });

            Assert.Equal(0, board.RPM);
        }

        [Fact]
        public void SetValue_IgnoresNullData()
        {
            var board = CreateBoard(out var fakeOwble);

            var exception = Record.Exception(() => fakeOwble.RaiseBoardValueChanged(OWBoard.RpmUUID, null));

            Assert.Null(exception);
        }
    }
}
