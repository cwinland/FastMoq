namespace FastMoq.Tests.TestClasses
{
    public class TestClassDouble1 : ITestClassDouble
    {
        #region ITestClassDouble

        /// <inheritdoc />
        public double Value { get; set; }

        #endregion
    }

    public interface ITestClassDouble
    {
        #region Properties

        double Value { get; set; }

        #endregion
    }

    public class TestClassDouble2 : ITestClassDouble
    {
        #region ITestClassDouble

        /// <inheritdoc />
        public double Value { get; set; }

        #endregion
    }
}