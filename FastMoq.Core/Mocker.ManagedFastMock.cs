using System;
using System.Reflection;
using FastMoq.Providers;

namespace FastMoq
{
    public partial class Mocker
    {
        private static IFastMock CreateManagedFastMock(Type type, object instance)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(instance);

            var method = typeof(Mocker)
                .GetMethod(nameof(CreateManagedFastMockCore), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type);

            return (IFastMock)method.Invoke(null, [instance])!;
        }

        private static IFastMock CreateManagedFastMockCore<T>(object instance) where T : class
        {
            return new ManagedFastMock<T>((T)instance);
        }

        private sealed class ManagedFastMock<T>(T instance) : IFastMock<T> where T : class
        {
            public Type MockedType => typeof(T);
            public T Instance { get; } = instance;
            object IFastMock.Instance => Instance;
            public object NativeMock => Instance;
            public void Reset() { }
        }
    }
}