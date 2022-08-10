using System.IO.Abstractions;

namespace FastMoq.Tests
{
    public class TestClassMany : ITestClassMany
    {
        #region Fields

        public object? value;

        #endregion

        public object? Value => value;

        public TestClassMany() => value = null;

        public TestClassMany(int x) => value = x;

        public TestClassMany(string y) => value = y;

        public TestClassMany(int x, string y) => value = $"{x} {y}";
    }

    public interface ITestClassMany
    {
        object? Value { get; }
    }

    public class TestClassMultiple : ITestClassMultiple
    {
        public IFileSystem Fs { get; }
        public IFile F { get; }
        public TestClassMultiple(IFileSystem fs, IFile f)
        {
            Fs = fs;
            F = f;
        }
    }

    public interface ITestClassMultiple
    {
        IFileSystem Fs { get; }
        IFile F { get; }
    }
}