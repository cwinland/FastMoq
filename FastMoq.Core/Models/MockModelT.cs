using System;
using System.Diagnostics.CodeAnalysis;
using FastMoq.Providers;
using Moq; // legacy while migrating

namespace FastMoq.Models
{
    /// <summary>
    ///     Generic mock model wrapper (provider-first, Moq legacy compatible).
    /// </summary>
    public class MockModel<T> : MockModel, IComparable<MockModel<T>>, IEquatable<MockModel<T>>, IEqualityComparer<MockModel<T>> where T : class
    {
        #region Properties
        [Obsolete("Use TypedFastMock / Instance instead. Will be removed in a future major version.")]
        public new Mock<T> Mock
        {
            get => (Mock<T>)base.Mock;
            internal set
            {
                base.Mock = value;
                // Re-hydrate adapter from legacy mock
                RefreshFastMockFromLegacy();
            }
        }

        public IFastMock<T> TypedFastMock => (IFastMock<T>)FastMock;
        public override Type Type => typeof(T);
        public new T Instance => TypedFastMock.Instance;
        #endregion

        #region Construction
        internal MockModel(IFastMock<T> fastMock, bool nonPublic = false) : base(fastMock, nonPublic) { }
        internal MockModel(Mock mock) : base(typeof(T), mock)
        {
            if (mock is not Mock<T> typedMock)
            {
                throw new ArgumentException($"Expected a Mock<{typeof(T).Name}> instance.", nameof(mock));
            }

            FastMock = new Providers.MoqProvider.MoqMockAdapter<T>(typedMock);
        }
        internal MockModel(MockModel baseModel) : base(baseModel.FastMock, baseModel.NonPublic) { }
        #endregion

        #region Comparison / Equality
        public override int CompareTo(object? obj) =>
            obj is MockModel<T> mockModel ? CompareTo(mockModel) : throw new ArgumentException($"Not a MockModel<{typeof(T).Name}> instance");

        public override bool Equals(object? obj) => IsEqual(this, obj as MockModel<T>);

        [ExcludeFromCodeCoverage]
        public override int GetHashCode() => base.GetHashCode();

        [ExcludeFromCodeCoverage]
        public static bool operator ==(MockModel<T>? a, MockModel<T>? b) => IsEqual(a, b);
        [ExcludeFromCodeCoverage]
        public static bool operator !=(MockModel<T>? a, MockModel<T>? b) => !IsEqual(a, b);

        [ExcludeFromCodeCoverage]
        public int CompareTo(MockModel<T>? other) => base.CompareTo(other);

        [ExcludeFromCodeCoverage]
        public bool Equals(MockModel<T>? x, MockModel<T>? y) => IsEqual(x, y);

        [ExcludeFromCodeCoverage]
        public int GetHashCode(MockModel<T> obj) => base.GetHashCode(obj);

        [ExcludeFromCodeCoverage]
        public bool Equals(MockModel<T>? other) => IsEqual(this, other);
        #endregion
    }
}
