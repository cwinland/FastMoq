using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using FastMoq.Providers;

namespace FastMoq.Core.Providers.Reflection
{
    internal sealed class ReflectionMockingProvider : IMockingProvider
    {
        public IMockingProviderCapabilities Capabilities { get; } = ReflectionCapabilities.Instance;

        public IFastMock<T> CreateMock<T>(MockCreationOptions? options = null) where T : class
        {
            var instance = CreateInstance(typeof(T), options) as T
                           ?? throw new InvalidOperationException($"Unable to create instance of {typeof(T)} using reflection provider.");
            return new ReflectionFastMock<T>(instance);
        }

        public IFastMock CreateMock(Type type, MockCreationOptions? options = null)
        {
            var instance = CreateInstance(type, options)
                           ?? throw new InvalidOperationException($"Unable to create instance of {type} using reflection provider.");
            var wrapperType = typeof(ReflectionFastMock<>).MakeGenericType(type);
            return (IFastMock)Activator.CreateInstance(wrapperType, instance)!;
        }

        public void SetupAllProperties(IFastMock mock)
        {
            // No-op: properties already have their default backing values.
        }

        public void SetCallBase(IFastMock mock, bool value)
        {
            // Always call base (there is no interception layer), so ignore.
        }

        public void Verify<T>(IFastMock<T> mock, Expression<Action<T>> expression, TimesSpec? times = null) where T : class
        {
            // Intentionally no-op – reflection provider does not record invocations.
        }

        public void VerifyNoOtherCalls(IFastMock mock)
        {
            // No-op.
        }

        private static object? CreateInstance(Type type, MockCreationOptions? options)
        {
            // If abstract or interface, attempt dynamic proxy that returns default values.
            if (type.IsInterface || type.IsAbstract)
            {
                return InterfaceProxyFactory.CreateProxy(type);
            }

            // Try parameterless constructor first.
            if (Activator.CreateInstance(type) is { } inst)
                return inst;

            // If there are constructor args provided, try them.
            if (options?.ConstructorArgs is { Length: > 0 })
            {
                try
                {
                    return Activator.CreateInstance(type, options.ConstructorArgs);
                }
                catch { /* swallow and fallthrough */ }
            }

            // Last resort – create uninitialized (may leave object in unusable state, so avoided unless necessary)
            return FormatterServices.GetUninitializedObject(type);
        }

        private sealed class ReflectionFastMock<T> : IFastMock<T> where T : class
        {
            public ReflectionFastMock(T instance) => Instance = instance;
            public Type MockedType => typeof(T);
            public T Instance { get; }
            object IFastMock.Instance => Instance;
            public void Reset() { /* nothing to reset */ }
        }

        private static class InterfaceProxyFactory
        {
            private static readonly ConcurrentDictionary<Type, Type> Cache = new();

            public static object CreateProxy(Type interfaceOrAbstractType)
            {
                if (!interfaceOrAbstractType.IsInterface)
                {
                    // For abstract classes attempt to create using uninitialized object (methods will just call base returning defaults)
                    return FormatterServices.GetUninitializedObject(interfaceOrAbstractType);
                }

                var implType = Cache.GetOrAdd(interfaceOrAbstractType, BuildType);
                return Activator.CreateInstance(implType)!;
            }

            private static Type BuildType(Type contract)
            {
                var asmName = new AssemblyName($"FastMoq.ReflectionProxy.{contract.Name}.{Guid.NewGuid():N}");
#if NET8_0_OR_GREATER
                var assembly = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
#else
                var assembly = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
#endif
                var module = assembly.DefineDynamicModule("Main");
                var typeBuilder = module.DefineType(contract.Name + "_ReflectionProxy", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);
                typeBuilder.AddInterfaceImplementation(contract);

                foreach (var prop in contract.GetProperties())
                {
                    var field = typeBuilder.DefineField("_" + prop.Name, prop.PropertyType, FieldAttributes.Private);
                    var pb = typeBuilder.DefineProperty(prop.Name, PropertyAttributes.None, prop.PropertyType, null);

                    if (prop.CanRead)
                    {
                        var getM = typeBuilder.DefineMethod("get_" + prop.Name, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig, prop.PropertyType, Type.EmptyTypes);
                        var il = getM.GetILGenerator();
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldfld, field);
                        il.Emit(OpCodes.Ret);
                        pb.SetGetMethod(getM);
                        typeBuilder.DefineMethodOverride(getM, prop.GetMethod!);
                    }

                    if (prop.CanWrite)
                    {
                        var setM = typeBuilder.DefineMethod("set_" + prop.Name, MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new[] { prop.PropertyType });
                        var il = setM.GetILGenerator();
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldarg_1);
                        il.Emit(OpCodes.Stfld, field);
                        il.Emit(OpCodes.Ret);
                        pb.SetSetMethod(setM);
                        typeBuilder.DefineMethodOverride(setM, prop.SetMethod!);
                    }
                }

                foreach (var method in contract.GetMethods().Where(m => !m.IsSpecialName))
                {
                    var parms = method.GetParameters();
                    var mb = typeBuilder.DefineMethod(method.Name,
                        MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                        method.ReturnType,
                        parms.Select(p => p.ParameterType).ToArray());
                    var il = mb.GetILGenerator();
                    if (method.ReturnType != typeof(void))
                    {
                        if (method.ReturnType.IsValueType)
                        {
                            var local = il.DeclareLocal(method.ReturnType);
                            il.Emit(OpCodes.Ldloca_S, local);
                            il.Emit(OpCodes.Initobj, method.ReturnType);
                            il.Emit(OpCodes.Ldloc_0);
                        }
                        else
                        {
                            il.Emit(OpCodes.Ldnull);
                        }
                    }
                    il.Emit(OpCodes.Ret);
                    typeBuilder.DefineMethodOverride(mb, method);
                }
                return typeBuilder.CreateType();
            }
        }
    }

    internal sealed class ReflectionCapabilities : IMockingProviderCapabilities
    {
        public static IMockingProviderCapabilities Instance { get; } = new ReflectionCapabilities();
        private ReflectionCapabilities() { }
        public bool SupportsCallBase => false;
        public bool SupportsStrict => false;
        public bool SupportsSetupAllProperties => false;
        public bool SupportsProtectedMembers => false;
        public bool SupportsInvocationTracking => false;
    }
}
