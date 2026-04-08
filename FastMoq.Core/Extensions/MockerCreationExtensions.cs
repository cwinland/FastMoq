using FastMoq.Models;
using Moq;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq.Extensions
{
    /// <summary>
    /// Helpers for creating components or raw mocks when you need constructor selection to be explicit in a test.
    /// </summary>
    /// <example>
    /// <para>Use these overloads when the constructor choice matters and you want to supply only a subset of arguments explicitly.</para>
    /// <code language="csharp"><![CDATA[
    /// var mocker = new Mocker();
    ///
    /// var processor = mocker.CreateInstance<OrderProcessor, string>(
    ///     new Dictionary<Type, object?>
    ///     {
    ///         [typeof(string)] = "orders-eu"
    ///     });
    ///
    /// processor.Should().NotBeNull();
    /// ]]></code>
    /// </example>
    public static class MockerCreationExtensions
    {
        /// <summary>
        /// Creates an instance of <c>T</c> by selecting the constructor that matches <typeparamref name="TParam1"/> and injecting the supplied value from <paramref name="data"/>.
        /// </summary>
        /// <typeparam name="T">The concrete type to create.</typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <param name="mocker">The mocker.</param>
        /// <param name="data">The data.</param>
        /// <returns>The created instance or <see langword="null"/> when resolution cannot produce one.</returns>
        /// <example>
        /// <para>This is useful when a component has multiple constructors and your test needs to target the overload that accepts a tenant or region value.</para>
        /// <code language="csharp"><![CDATA[
        /// var mocker = new Mocker();
        ///
        /// var handler = mocker.CreateInstance<InvoiceHandler, string>(
        ///     new Dictionary<Type, object?>
        ///     {
        ///         [typeof(string)] = "contoso"
        ///     });
        ///
        /// handler.Should().NotBeNull();
        /// handler!.TenantName.Should().Be("contoso");
        /// ]]></code>
        /// </example>
        public static T? CreateInstance<T, TParam1>(this Mocker mocker, Dictionary<Type, object?> data) where T : class =>
            mocker.CreateInstance<T, TParam1>(InstanceCreationFlags.None, data);

        public static T? CreateInstance<T, TParam1>(this Mocker mocker, InstanceCreationFlags flags, Dictionary<Type, object?> data) where T : class =>
            mocker.CreateInstanceInternal<T>(
                model => mocker.FindConstructorByType(model.InstanceType, ResolvePublicOnlyOverride(flags), typeof(TParam1)),
                ResolvePublicOnlyOverride(flags),
                ResolveOptionalParameterResolution(mocker, flags),
                data
            );

        /// <summary>
        ///     Creates an instance of <c>T</c>. Parameter data allows matching of constructors by type and uses those values in the creation of the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <typeparam name="TParam2">The type of the t param2.</typeparam>
        /// <param name="mocker">The mocker.</param>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public static T? CreateInstance<T, TParam1, TParam2>(this Mocker mocker, Dictionary<Type, object?> data) where T : class =>
            mocker.CreateInstance<T, TParam1, TParam2>(InstanceCreationFlags.None, data);

        public static T? CreateInstance<T, TParam1, TParam2>(this Mocker mocker, InstanceCreationFlags flags, Dictionary<Type, object?> data) where T : class =>
            mocker.CreateInstanceInternal<T>(
                model => mocker.FindConstructorByType(model.InstanceType, ResolvePublicOnlyOverride(flags), typeof(TParam1), typeof(TParam2)),
                ResolvePublicOnlyOverride(flags),
                ResolveOptionalParameterResolution(mocker, flags),
                data
            );

        /// <summary>
        ///     Creates an instance of <c>T</c>. Parameter data allows matching of constructors by type and uses those values in the creation of the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <typeparam name="TParam2">The type of the t param2.</typeparam>
        /// <typeparam name="TParam3">The type of the t param3.</typeparam>
        /// <param name="mocker">The mocker.</param>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public static T? CreateInstance<T, TParam1, TParam2, TParam3>(this Mocker mocker, Dictionary<Type, object?> data) where T : class =>
            mocker.CreateInstance<T, TParam1, TParam2, TParam3>(InstanceCreationFlags.None, data);

        public static T? CreateInstance<T, TParam1, TParam2, TParam3>(this Mocker mocker, InstanceCreationFlags flags, Dictionary<Type, object?> data) where T : class =>
            mocker.CreateInstanceInternal<T>(
                model => mocker.FindConstructorByType(model.InstanceType, ResolvePublicOnlyOverride(flags), typeof(TParam1), typeof(TParam2), typeof(TParam3)),
                ResolvePublicOnlyOverride(flags),
                ResolveOptionalParameterResolution(mocker, flags),
                data
            );

        /// <summary>
        ///     Creates an instance of <c>T</c>. Parameter data allows matching of constructors by type and uses those values in the creation of the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <typeparam name="TParam2">The type of the t param2.</typeparam>
        /// <typeparam name="TParam3">The type of the t param3.</typeparam>
        /// <typeparam name="TParam4">The type of the t param4.</typeparam>
        /// <param name="mocker">The mocker.</param>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public static T? CreateInstance<T, TParam1, TParam2, TParam3, TParam4>(this Mocker mocker, Dictionary<Type, object?> data) where T : class =>
            mocker.CreateInstance<T, TParam1, TParam2, TParam3, TParam4>(InstanceCreationFlags.None, data);

        public static T? CreateInstance<T, TParam1, TParam2, TParam3, TParam4>(this Mocker mocker, InstanceCreationFlags flags, Dictionary<Type, object?> data) where T : class =>
            mocker.CreateInstanceInternal<T>(
                model => mocker.FindConstructorByType(model.InstanceType, ResolvePublicOnlyOverride(flags), typeof(TParam1), typeof(TParam2), typeof(TParam3), typeof(TParam4)),
                ResolvePublicOnlyOverride(flags),
                ResolveOptionalParameterResolution(mocker, flags),
                data
            );

        /// <summary>
        ///     Creates an instance of <c>T</c>. Parameter data allows matching of constructors by type and uses those values in the creation of the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <typeparam name="TParam2">The type of the t param2.</typeparam>
        /// <typeparam name="TParam3">The type of the t param3.</typeparam>
        /// <typeparam name="TParam4">The type of the t param4.</typeparam>
        /// <typeparam name="TParam5">The type of the t param5.</typeparam>
        /// <param name="mocker">The mocker.</param>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public static T? CreateInstance<T, TParam1, TParam2, TParam3, TParam4, TParam5>(this Mocker mocker, Dictionary<Type, object?> data) where T : class =>
            mocker.CreateInstance<T, TParam1, TParam2, TParam3, TParam4, TParam5>(InstanceCreationFlags.None, data);

        public static T? CreateInstance<T, TParam1, TParam2, TParam3, TParam4, TParam5>(this Mocker mocker, InstanceCreationFlags flags, Dictionary<Type, object?> data) where T : class =>
            mocker.CreateInstanceInternal<T>(
                model => mocker.FindConstructorByType(model.InstanceType, ResolvePublicOnlyOverride(flags), typeof(TParam1), typeof(TParam2), typeof(TParam3), typeof(TParam4), typeof(TParam5)),
                ResolvePublicOnlyOverride(flags),
                ResolveOptionalParameterResolution(mocker, flags),
                data
            );

        private static bool? ResolvePublicOnlyOverride(InstanceCreationFlags flags)
        {
            var publicOnly = flags.HasFlag(InstanceCreationFlags.PublicConstructorsOnly);
            var allowNonPublicFallback = flags.HasFlag(InstanceCreationFlags.AllowNonPublicConstructorFallback);

            if (publicOnly && allowNonPublicFallback)
            {
                throw new ArgumentException("InstanceCreationFlags cannot combine PublicConstructorsOnly with AllowNonPublicConstructorFallback.", nameof(flags));
            }

            if (publicOnly)
            {
                return true;
            }

            if (allowNonPublicFallback)
            {
                return false;
            }

            return null;
        }

        private static OptionalParameterResolutionMode ResolveOptionalParameterResolution(Mocker mocker, InstanceCreationFlags flags)
        {
            var resolveViaMocker = flags.HasFlag(InstanceCreationFlags.ResolveOptionalParametersViaMocker);
            var useDefaultOrNull = flags.HasFlag(InstanceCreationFlags.UseDefaultOrNullOptionalParameters);

            if (resolveViaMocker && useDefaultOrNull)
            {
                throw new ArgumentException("InstanceCreationFlags cannot combine ResolveOptionalParametersViaMocker with UseDefaultOrNullOptionalParameters.", nameof(flags));
            }

            if (resolveViaMocker)
            {
                return OptionalParameterResolutionMode.ResolveViaMocker;
            }

            if (useDefaultOrNull)
            {
                return OptionalParameterResolutionMode.UseDefaultOrNull;
            }

            return mocker.OptionalParameterResolution;
        }

        /// <summary>
        ///     Creates a mock given the Type of Mock. Properties will be stubbed and have default setups.
        /// </summary>
        /// <typeparam name="T">Type of Mock</typeparam>
        /// <param name="mocker">The mocker.</param>
        /// <param name="isNonPublic">if set to <c>true</c>, indicates if non-public constructors should be searched.</param>
        /// <remarks>This is designed for interface mocks or concrete mocks without parameters.</remarks>
        /// <exception cref="System.ApplicationException">Cannot create instance of Mock.</exception>
        public static Mock<T> CreateMockInternal<T>(this Mocker mocker, bool isNonPublic = true) where T : class =>
            (Mock<T>) mocker.CreateMockInternal(typeof(T), new List<object?>(), true);

        /// <summary>
        ///     Creates the mock internal.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="type">The type.</param>
        /// <param name="parameterList">The constructor parameters.</param>
        /// <param name="isNonPublic">if set to <c>true</c>, indicates if non-public constructors should be searched.</param>
        /// <param name="setupMock">if set to <c>true</c>, attempts to setup internal mocks and properties.</param>
        /// <remarks>Parameter list only works if the type is concrete. Otherwise, pass an empty list.</remarks>
        /// <exception cref="System.ApplicationException">Cannot create instance of Mock.</exception>
        public static Mock CreateMockInternal(this Mocker mocker, Type type, IReadOnlyCollection<object?>? parameterList = null, bool isNonPublic = false, bool setupMock = true)
        {
            var flags = isNonPublic
                ? BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance
                : BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

            // Execute new Mock with Loose Behavior and arguments from constructor, if applicable.
            var parameters = new List<object?>
            {
                mocker.ShouldCreateStrictMocks() ? MockBehavior.Strict : MockBehavior.Loose,
            };
            parameterList ??= new List<object>();
            parameterList.ForEach(parameters.Add);

            Mock? instance = null;
            var isEntityFrameworkDbContextType = DatabaseSupportBridge.IsEntityFrameworkDbContextType(type);
            if (!isEntityFrameworkDbContextType ||
                !DatabaseSupportBridge.TryCreateLegacyDbContextMock(type, (MockBehavior) parameters[0]!, parameterList, out instance))
            {
                var newType = typeof(Mock<>).MakeGenericType(type);
                instance = Activator.CreateInstance(newType,
                                   flags,
                                   null,
                                   parameters.ToArray(),
                                   null,
                                   null
                               ) as Mock;
            }

            if (instance == null)
            {
                throw CannotCreateMock(type);
            }

            if (setupMock)
            {
                mocker.SetupMock(type, instance);
            }

            instance.RaiseIfNull();
            return instance;
        }

        private static ApplicationException CannotCreateMock(Type type)
        {
            return new ApplicationException($"Cannot create instance of 'Mock<{type.Name}>'.");
        }

        internal static object GetSafeMockObject(this Mocker mocker, Mock mock)
        {
            try
            {
                return mock.Object.RaiseIfNull();
            }
            catch (TargetInvocationException ex)
            {
                mocker.ExceptionLog.Add(ex.Message);
                ex.ThrowIfCastleMethodAccessException(); // Throw actual error.
                throw; // Bubble up since not a CastleAccessException
            }
            catch (MethodAccessException ex)
            {
                mocker.ExceptionLog.Add(ex.Message);
                ex.ThrowIfCastleMethodAccessException(); // Throw actual error.
                throw; // Bubble up since not a CastleAccessException
            }
            catch (Exception ex)
            {
                mocker.ExceptionLog.Add(ex.Message);
                throw;
            }
        }

        internal static ConstructorModel GetTypeConstructor(this Mocker mocker, Type type, bool nonPublic, object?[] args)
        {
            var constructor = new ConstructorModel(null, args);

            try
            {
                if (!type.IsInterface)
                {
                    // Find the best constructor and build the parameters.
                    constructor = args.Length > 0
                        ? mocker.FindConstructor(type, nonPublic, args)
                        : mocker.FindPreferredConstructor(type, nonPublic);
                }
            }
            catch (Exception ex)
            {
                mocker.ExceptionLog.Add(ex.Message);
            }

            if (constructor.ConstructorInfo == null && !mocker.HasParameterlessConstructor(type))
            {
                try
                {
                    constructor = mocker.GetConstructors(type, nonPublic).MinBy(x => x.ConstructorInfo?.GetParameters().Length ?? 0) ?? constructor;
                }
                catch (Exception ex)
                {
                    // It's okay if this fails.
                    mocker.ExceptionLog.Add(ex.Message);
                }
            }

            return constructor;
        }

        /// <summary>
        ///     Create an instance using the constructor by the function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mocker">The mocker.</param>
        /// <param name="constructorFunc">The constructor function.</param>
        /// <param name="data">The arguments.</param>
        /// <returns>T.</returns>
        internal static T? CreateInstanceInternal<T>(this Mocker mocker, Func<IInstanceModel, ConstructorInfo> constructorFunc, Dictionary<Type, object?>? data) where T : class =>
            mocker.CreateInstanceInternal<T>(constructorFunc, publicOnly: null, mocker.OptionalParameterResolution, data);

        internal static T? CreateInstanceInternal<T>(this Mocker mocker, Func<IInstanceModel, ConstructorInfo> constructorFunc, bool? publicOnly, OptionalParameterResolutionMode optionalParameterResolution, Dictionary<Type, object?>? data) where T : class
        {
            var type = typeof(T).IsInterface ? mocker.GetTypeFromInterface<T>() : new InstanceModel<T>();

            if (type.CreateFunc != null)
            {
                return (T?) type.CreateFunc.Invoke(mocker, type.InstanceType);
            }

            data ??= new();
            var constructor = constructorFunc(type);

            var args = mocker.GetArgData(constructor, optionalParameterResolution, data);

            return mocker.CreateInstanceInternal<T>(constructor, args);
        }

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mocker">The mocker.</param>
        /// <param name="constructorModel">The constructor model.</param>
        /// <returns>T.</returns>
        internal static T? CreateInstanceInternal<T>(this Mocker mocker, ConstructorModel constructorModel) where T : class =>
            mocker.CreateInstanceInternal<T>(constructorModel.ConstructorInfo, constructorModel.ParameterList);

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mocker">The mocker.</param>
        /// <param name="info">The information.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>T.</returns>
        internal static T? CreateInstanceInternal<T>(this Mocker mocker, ConstructorInfo? info, params object?[] args) where T : class =>
            mocker.CreateInstanceInternal(typeof(T), info, args) as T;

        /// <summary>
        ///     Creates the instance internal.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="type">The type.</param>
        /// <param name="info">The information.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>object?.</returns>
        internal static object? CreateInstanceInternal(this Mocker mocker, Type type, ConstructorInfo? info, params object?[] args)
        {
            mocker.ConstructorHistory.AddOrUpdate(type, new ConstructorModel(info, args));
            var paramList = info?.GetParameters().ToList() ?? [];
            var newArgs = args.ToList();

            if (args.Length < paramList.Count)
            {
                for (var i = args.Length; i < paramList.Count; i++)
                {
                    var p = paramList[i];
                    newArgs.Add(mocker.ResolveParameter(p, mocker.OptionalParameterResolution));
                }
            }

            var obj = mocker.AddInjections(info?.Invoke(newArgs.ToArray()));
            return mocker.Behavior.Has(MockFeatures.ResolveNestedMembers) ? mocker.AddProperties(type, obj) : obj;
        }

        /// <summary>
        /// Setups the mock for given property name.
        /// </summary>
        /// <typeparam name="TMock">The type of the t mock.</typeparam>
        /// <param name="mock">The mock.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The value.</param>
        public static void SetupMockProperty<TMock>(this Mock<TMock> mock, string propertyName, object value) where TMock : class
        {
            var propertyInfo = typeof(TMock).GetProperty(propertyName) ?? throw new InvalidFilterCriteriaException($"{propertyName} not found in SetupMockProperty.");
            mock.SetupMockProperty(propertyInfo, value);
        }

        /// <summary>
        /// Setups the mock for given property info.
        /// </summary>
        /// <typeparam name="TMock">The type of mock.</typeparam>
        /// <param name="mock">The mock.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="value">The value.</param>
        public static void SetupMockProperty<TMock>(this Mock<TMock> mock, PropertyInfo propertyInfo, object value) where TMock : class
        {
            mock.RaiseIfNull();
            // Create a parameter expression for the object instance of type TMock
            var instanceParam = Expression.Parameter(typeof(TMock), "instance");

            // Create an expression to access the property
            var propertyAccess = Expression.Property(instanceParam, propertyInfo);

            // Create a lambda expression that represents the getter
            var getterExpression = Expression.Lambda<Func<TMock, object>>(propertyAccess, instanceParam);

            // Set up the mock to return the provided value for the property getter
            mock.Setup(getterExpression).Returns(value);
        }

        public static void SetupMockProperty<TMock>(this Mock<TMock> mock, Expression<Func<TMock, object>> propertyExpression, object value)
            where TMock : class
        {
            var propertyInfo = propertyExpression.GetPropertyInfo();
            mock.SetupMockProperty(propertyInfo, value);
        }

        internal static PropertyInfo GetPropertyInfo<TSource, TProperty>(this Expression<Func<TSource, TProperty>> propertyExpression)
        {
            if (propertyExpression.Body is MemberExpression memberExpression)
            {
                if (memberExpression.Member is PropertyInfo propertyInfo)
                {
                    return propertyInfo;
                }
            }

            throw new ArgumentException("Expression is not a property access.", nameof(propertyExpression));
        }
    }
}
