using System.IO.Abstractions;

#pragma warning disable CS8604 // Possible null reference argument for parameter.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS0649 // Field 'field' is never assigned to, and will always have its default value 'value'.
#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable.
#pragma warning disable CS8974 // Converting method group to non-delegate type
#pragma warning disable CS0472 // The result of the expression is always 'value1' since a value of type 'value2' is never equal to 'null' of type 'value3'.

namespace FastMoq.Tests.TestClasses
{
    /// <summary>
    ///     Class TestClassOne.
    /// Implements the <see cref="ITestClassOne" />
    /// </summary>
    /// <seealso cref="ITestClassOne" />
    public class TestClassOne : ITestClassOne
    {
        [Inject]
        public IFileSystem FileSystem { get; set; }

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
    public interface ITestClassOne
    {
        IFileSystem FileSystem { get; set; }
    }
}