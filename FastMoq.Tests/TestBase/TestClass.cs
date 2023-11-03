using System;
using System.Threading.Tasks;

namespace FastMoq.Tests.TestBase
{
#pragma warning disable CS8604, CS8602, CS8625, CS0649, CS8618, CS8974, CS0472
    public class TestClass
    {
        #region Fields

        private static readonly int sField = 123;
        public object field2 = 111;
        public int field3 = 222;

        private int field = sField;

        #endregion

        #region Properties

        public object property4 => property3;
        private object property { get; set; } = sProperty;
        private object property2 { get; } = 789;
        private int property3 => int.Parse(property2.ToString());

        private static int sProperty { get; } = 456;

        #endregion

        internal void TestMethod(int i, string s, TestClass c)
        {
            if (i == null)
            {
                throw new ArgumentNullException(nameof(i));
            }

            if (string.IsNullOrEmpty(s))
            {
                throw new ArgumentNullException(nameof(s));
            }

            if (c == null)
            {
                throw new ArgumentNullException(nameof(c));
            }
        }

        internal void TestMethod2(TestEnum testEnum, string s)
        {
            if (s == null)
            {
                throw new InvalidOperationException(nameof(s));
            }
        }

        internal async Task TestMethod2Async(int i, string s, TestClass c) => await TestMethodAsync(i, s, c);

        internal async Task TestMethodAsync(int? i, string s, TestClass c)
        {
            if (i == null)
            {
                throw new ArgumentNullException(nameof(i));
            }

            if (string.IsNullOrEmpty(s))
            {
                throw new ArgumentNullException(nameof(s));
            }

            if (c == null)
            {
                throw new ArgumentNullException(nameof(c));
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        private object method() => "test";
        private string method2() => "test2";

        internal enum TestEnum
        {
            test1,
            test2,
            test3,
            test4,
            test5,
            test6
        }
    }
}