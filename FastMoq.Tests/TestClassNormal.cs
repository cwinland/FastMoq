using System.IO.Abstractions;

namespace FastMoq.Tests
{
    public class TestClassNormal : ITestClassNormal
    {
        public TestClassNormal()
        {

        }

        public TestClassNormal(IFileSystem fileSystem)
        {

        }
    }

    public interface ITestClassNormal
    {
    }
}
