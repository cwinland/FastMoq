using System;
using System.IO.Abstractions;

namespace FastMoq.TestingExample
{
    public class TestClassNormal : ITestClassNormal
    {
        #region Properties

        public event EventHandler? TestEvent;
        public IFileSystem? FileSystem { get; set; }

        #endregion

        public TestClassNormal() { }
        public TestClassNormal(IFileSystem fileSystem) => FileSystem = fileSystem;
        public void CallTestEvent() => TestEvent?.Invoke(this, EventArgs.Empty);
    }

    public interface ITestClassNormal { }
}