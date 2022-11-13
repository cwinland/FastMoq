namespace FastMoq.Tests.TestClasses
{
    public class NestedTestClass : INestedTestClass { }

    public class NestedTestClassBase : INestedTestClassBase { }

    public interface INestedTestClass : INestedTestClassBase { }

    public interface INestedTestClassBase { }
}