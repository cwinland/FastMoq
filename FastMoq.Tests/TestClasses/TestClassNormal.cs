﻿using Microsoft.Extensions.Logging;
using System;
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
    public class TestClassNormal : ITestClassNormal
    {
        public TestClassNormal()
        {
            // Left blank
        }

        public TestClassNormal(IFileSystem fileSystem, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(fileSystem);
            ArgumentNullException.ThrowIfNull(logger);
        }
    }

    public interface ITestClassNormal
    {
        // Left blank
    }
}