using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;

namespace FastMoq.Tests.TestClasses
{
    public class InjectAttribute : Attribute { }

    internal class TestClassParameters
    {
        [Inject] internal IFileSystem anotherFileSystem;

        [Inject] internal IFileSystem anotherFileSystem2 { get; set; }

        [Inject] internal IFileSystem anotherFileSystem3 { get; private set; }

        [Inject] internal int invalidInjection;

        [Inject] internal int? invalidInjection2;

        [Inject] internal string invalidInjection3;

        [Inject] internal string? invalidInjection4;

        internal TestClassParameters(int x, string y, IFileSystem fileSystem)
        {
            if (fileSystem == null) throw new ArgumentNullException(nameof(fileSystem));
        }
    }
}
