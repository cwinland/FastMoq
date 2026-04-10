namespace FastMoq.Tests
{
    public class PropertyValueCaptureTests
    {
        [Fact]
        public void Record_ShouldStoreLatestValueAndHistory()
        {
            var capture = new PropertyValueCapture<string?>();

            capture.Record("alpha");
            capture.Record("beta");

            capture.HasValue.Should().BeTrue();
            capture.Value.Should().Be("beta");
            capture.History.Should().Equal("alpha", "beta");
        }

        [Fact]
        public void Clear_ShouldResetState()
        {
            var capture = new PropertyValueCapture<int>();
            capture.Record(5);

            capture.Clear();

            capture.HasValue.Should().BeFalse();
            capture.History.Should().BeEmpty();
            capture.Value.Should().Be(default);
        }
    }
}