using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace FastMoq.Tests.TestBase
{
#pragma warning disable CS8604, CS8602, CS8625, CS0649, CS8618, CS8974, CS0472
    public class TestBaseTestClass : MockerTestBase<TestClass>
    {
        public void TestMethodParameters(MethodInfo methodInfo, Action<Func<Task>?, string?, List<object?>?, ParameterInfo> resultAction, params object?[]? args) =>
            TestMethodParametersAsync(methodInfo, resultAction, args);
    }
}