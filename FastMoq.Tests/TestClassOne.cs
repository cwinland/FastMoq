using System.IO.Abstractions;

namespace FastMoq.Tests
{
    /// <summary>
    ///     Class TestClassOne.
    /// Implements the <see cref="FastMoq.Tests.ITestClassOne" />
    /// </summary>
    /// <seealso cref="FastMoq.Tests.ITestClassOne" />
    public class TestClassOne : ITestClassOne
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TestClassOne"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        public TestClassOne(IFileSystem fileSystem) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TestClassOne"/> class.
        /// </summary>
        /// <param name="file">The file.</param>
        internal TestClassOne(IFile file) { }

        internal void TestVoid(IFileSystem fileSystem)
        {

        }

        public static object TestStaticObject(IFileSystem fileSystem) => fileSystem;

        public int TestInt(int i) => i;
    }

    /// <summary>
    ///     Interface ITestClassOne
    /// </summary>
    public interface ITestClassOne { }
}