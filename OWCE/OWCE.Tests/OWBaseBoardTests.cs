using OWCE;
using Xunit;

namespace OWCE.Tests
{
    public class OWBaseBoardTests
    {
        [Theory]
        [InlineData(OWBoardType.V1, 917.66f)]
        [InlineData(OWBoardType.Plus, 917.66f)]
        [InlineData(OWBoardType.XR, 917.66f)]
        [InlineData(OWBoardType.Pint, 837.86f)]
        [InlineData(OWBoardType.PintX, 837.86f)]
        [InlineData(OWBoardType.GT, 917.66f)]
        [InlineData(OWBoardType.Unknown, 0f)]
        public void WheelCircumference_MatchesBoardType(OWBoardType boardType, float expectedCircumference)
        {
            var board = new OWBaseBoard { BoardType = boardType };

            Assert.Equal(expectedCircumference, board.WheelCircumference);
        }

        [Theory]
        [InlineData(OWBoardType.V1, "V1", "Onewheel V1")]
        [InlineData(OWBoardType.Plus, "Plus", "Onewheel+")]
        [InlineData(OWBoardType.XR, "XR", "Onewheel+ XR")]
        [InlineData(OWBoardType.Pint, "Pint", "Onewheel Pint")]
        [InlineData(OWBoardType.PintX, "Pint X", "Onewheel Pint X")]
        [InlineData(OWBoardType.GT, "GT", "Onewheel GT")]
        [InlineData(OWBoardType.Unknown, "", "")]
        public void BoardModelStrings_MatchBoardType(OWBoardType boardType, string expectedShort, string expectedLong)
        {
            var board = new OWBaseBoard { BoardType = boardType };

            Assert.Equal(expectedShort, board.BoardModelStringShort);
            Assert.Equal(expectedLong, board.BoardModelStringLong);
        }

        [Fact]
        public void GetAvailableRideModes_V1_ReturnsThreeModes()
        {
            var board = new OWBaseBoard { BoardType = OWBoardType.V1 };

            var modes = board.GetAvailableRideModes();

            Assert.Equal(new[] { "Classic", "Extreme", "Elevated" }, modes.ConvertAll(m => m.Item1));
            Assert.Equal(new ushort[] { 1, 2, 3 }, modes.ConvertAll(m => m.Item2));
        }

        [Theory]
        [InlineData(OWBoardType.Plus)]
        [InlineData(OWBoardType.XR)]
        public void GetAvailableRideModes_PlusAndXR_ReturnSameSixModes(OWBoardType boardType)
        {
            var board = new OWBaseBoard { BoardType = boardType };

            var modes = board.GetAvailableRideModes();

            Assert.Equal(
                new[] { "Sequoia", "Cruz", "Mission", "Elevated", "Delirium", "Custom" },
                modes.ConvertAll(m => m.Item1));
            Assert.Equal(new ushort[] { 4, 5, 6, 7, 8, 9 }, modes.ConvertAll(m => m.Item2));
        }

        [Theory]
        [InlineData(OWBoardType.Pint)]
        [InlineData(OWBoardType.PintX)]
        public void GetAvailableRideModes_PintAndPintX_ReturnSameFourModes(OWBoardType boardType)
        {
            var board = new OWBaseBoard { BoardType = boardType };

            var modes = board.GetAvailableRideModes();

            Assert.Equal(new[] { "Redwood", "Pacific", "Elevated", "Skyline" }, modes.ConvertAll(m => m.Item1));
            Assert.Equal(new ushort[] { 5, 6, 7, 8 }, modes.ConvertAll(m => m.Item2));
        }

        [Fact]
        public void GetAvailableRideModes_GT_ReturnsSevenModes()
        {
            var board = new OWBaseBoard { BoardType = OWBoardType.GT };

            var modes = board.GetAvailableRideModes();

            Assert.Equal(
                new[] { "Bay", "Roam", "Flow", "Highline", "Elevated", "Apex", "Custom" },
                modes.ConvertAll(m => m.Item1));
            Assert.Equal(new ushort[] { 3, 4, 5, 6, 7, 8, 9 }, modes.ConvertAll(m => m.Item2));
        }

        [Fact]
        public void GetAvailableRideModes_UnknownBoardType_ReturnsEmptyList()
        {
            var board = new OWBaseBoard { BoardType = OWBoardType.Unknown };

            Assert.Empty(board.GetAvailableRideModes());
        }

        [Fact]
        public void Equals_ComparesById_NotReferenceOrOtherFields()
        {
            var first = new OWBaseBoard("shared-id", "Board A");
            var second = new OWBaseBoard("shared-id", "Board B");
            var different = new OWBaseBoard("other-id", "Board A");

            Assert.True(first.Equals(second));
            Assert.False(first.Equals(different));
            Assert.False(first.Equals((OWBaseBoard)null));
        }
    }
}
