namespace FastMoq.TestingExample
{
    public class TestClassMany : ITestClassMany
    {
        #region Fields

        public object? value;

        #endregion

        public TestClassMany() => value = null;

        public TestClassMany(int x) => value = x;

        public TestClassMany(string y) => value = y;

        public TestClassMany(int x, string y) => value = $"{x} {y}";
    }

    public interface ITestClassMany { }
}