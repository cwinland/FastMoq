using System.Threading.Tasks;

namespace FastMoq.Tests
{
    public class MockerDisposalTests
    {
        [Fact]
        public void Dispose_ShouldInvokeVirtualDisposeHook()
        {
            var mocker = new DisposalProbeMocker();

            mocker.Dispose();

            mocker.DisposeCallCount.Should().Be(1);
            mocker.LastDisposeDisposing.Should().BeTrue();
            mocker.DisposeAsyncCoreCallCount.Should().Be(0);
        }

        [Fact]
        public async Task DisposeAsync_ShouldInvokeVirtualDisposeAsyncHook()
        {
            var mocker = new DisposalProbeMocker();

            await mocker.DisposeAsync();

            mocker.DisposeAsyncCoreCallCount.Should().Be(1);
            mocker.DisposeCallCount.Should().Be(0);
        }

        private sealed class DisposalProbeMocker : Mocker
        {
            public int DisposeCallCount { get; private set; }

            public int DisposeAsyncCoreCallCount { get; private set; }

            public bool? LastDisposeDisposing { get; private set; }

            protected override void Dispose(bool disposing)
            {
                DisposeCallCount++;
                LastDisposeDisposing = disposing;
                base.Dispose(disposing);
            }

            protected override async ValueTask DisposeAsyncCore()
            {
                DisposeAsyncCoreCallCount++;
                await base.DisposeAsyncCore().ConfigureAwait(false);
            }
        }
    }
}