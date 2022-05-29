namespace FastMoq.Tests
{
    public class TestClassDouble1 : ITestClassDouble
    {
        #region Implementation of ITestClassDouble

        /// <inheritdoc />
        public double Value { get; set; }

        #endregion
    }

    public interface ITestClassDouble
    {
        double Value { get; set; }
    }

    public class TestClassDouble2 : ITestClassDouble
    {
        #region Implementation of ITestClassDouble

        /// <inheritdoc />
        public double Value { get; set; }

        #endregion
    }
}
