using FastMoq.Providers;
using System.Reflection;

namespace FastMoq
{
    /// <inheritdoc />
    public partial class Mocker
    {
        private static IFastMock CreateManagedFastMock(Type type, object instance)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(instance);

            var method = typeof(Mocker)
                .GetMethod(nameof(CreateManagedFastMockCore), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type);

            return (IFastMock) method.Invoke(null, [instance])!;
        }

        private static IFastMock CreateManagedFastMockCore<T>(object instance) where T : class
        {
            return new ManagedFastMock<T>((T) instance);
        }

        private sealed class ManagedFastMock<T>(T instance) : IFastMock<T> where T : class
        {
            /// <inheritdoc />
            public Type MockedType => typeof(T);

            /// <inheritdoc />
            public T Instance { get; } = instance;

            object IFastMock.Instance => Instance;

            /// <inheritdoc />
            public object NativeMock => Instance;

            /// <inheritdoc />
            public void Reset() { }
        }
    }
}