using System;
using System.IO.Abstractions;

namespace FastMoq.TestingExample
{
    public class TestClassNormal : ITestClassNormal
    {
        public event EventHandler TestEvent;

        public IFileSystem FileSystem { get; set; }

        public TestClassNormal()
        {

        }

        public TestClassNormal(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        public void CallTestEvent()
        {
            TestEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    public interface ITestClassNormal
    {
    }
}
