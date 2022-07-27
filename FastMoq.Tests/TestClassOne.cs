using System.IO.Abstractions;

namespace FastMoq.Tests
{
    public class TestClassOne : ITestClassOne
    {
        public TestClassOne(IFileSystem fileSystem) { }

        internal TestClassOne(IFile file) { }
    }

    public interface ITestClassOne { }
}