using FastMoq.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq.Extensions
{
    /// <summary>
    ///     Mocker Create Extensions
    /// </summary>
    public static class MockerCreationExtensions
    {
        /// <summary>
        ///     Creates an instance of <c>T</c>. Parameter data allows matching of constructors by type and uses those values in the creation of the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <param name="mocker">The mocker.</param>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public static T? CreateInstance<T, TParam1>(this Mocker mocker, Dictionary<Type, object?> data) where T : class => mocker.CreateInstanceInternal<T>(
            model => mocker.FindConstructorByType(model.InstanceType, true, typeof(TParam1)), data
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
        public static T? CreateInstance<T, TParam1, TParam2>(this Mocker mocker, Dictionary<Type, object?> data) where T : class => mocker.CreateInstanceInternal<T>(
            model => mocker.FindConstructorByType(model.InstanceType, true, typeof(TParam1), typeof(TParam2)),
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
            mocker.CreateInstanceInternal<T>(
                model => mocker.FindConstructorByType(model.InstanceType, true, typeof(TParam1), typeof(TParam2), typeof(TParam3)),
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
            mocker.CreateInstanceInternal<T>(
                model => mocker.FindConstructorByType(model.InstanceType,
                    true,
                    typeof(TParam1),
                    typeof(TParam2),
                    typeof(TParam3),
                    typeof(TParam4)
                ),
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
            mocker.CreateInstanceInternal<T>(
                model => mocker.FindConstructorByType(model.InstanceType,
                    true,
                    typeof(TParam1),
                    typeof(TParam2),
                    typeof(TParam3),
                    typeof(TParam4),
                    typeof(TParam5)
                ),
                data
            );

        /// <summary>
        ///     Creates a mock given the <typeparam name="T">Type of Mock</typeparam>. Properties will be stubbed and have default setups.
        /// </summary>
        /// <typeparam name="T">Type of Mock</typeparam>
        /// <param name="mocker">The mocker.</param>
        /// <param name="isNonPublic">if set to <c>true</c>, indicates if non-public constructors should be searched.</param>
        /// <remarks>This is designed for interface mocks or concrete mocks without parameters.</remarks>
        /// <exception cref="System.ApplicationException">Cannot create instance of Mock.</exception>
        public static Mock<T> CreateMockInternal<T>(this Mocker mocker, bool isNonPublic = true) where T : class =>
            (Mock<T>)mocker.CreateMockInternal(typeof(T), new List<object?>(), true);

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

            var isDbContext = type.IsAssignableTo(typeof(DbContext));
            var newType = isDbContext ? typeof(DbContextMock<>).MakeGenericType(type) : typeof(Mock<>).MakeGenericType(type);

            // Execute new Mock with Loose Behavior and arguments from constructor, if applicable.
            var parameters = new List<object?> { mocker.Strict ? MockBehavior.Strict : MockBehavior.Loose };
            parameterList ??= new List<object>();
            parameterList.ForEach(parameters.Add);

            var instance = Activator.CreateInstance(newType,
                               flags,
                               null,
                               parameters.ToArray(),
                               null,
                               null
                           ) as Mock ??
                           throw CannotCreateMock(type);

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
                    constructor = args.Length > 0 ||
                                  nonPublic
                        ? mocker.FindConstructor(type, true, args)
                        : mocker.FindConstructor(true, type, nonPublic);
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
        internal static T? CreateInstanceInternal<T>(this Mocker mocker, Func<IInstanceModel, ConstructorInfo> constructorFunc, Dictionary<Type, object?>? data) where T : class
        {
            var type = typeof(T).IsInterface ? mocker.GetTypeFromInterface<T>() : new InstanceModel<T>();

            if (type.CreateFunc != null)
            {
                return (T?)type.CreateFunc.Invoke(mocker, type.InstanceType);
            }

            data ??= new();
            var constructor = constructorFunc(type);

            var args = mocker.GetArgData(constructor, data);

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
            var paramList = info?.GetParameters().ToList() ?? new();
            var newArgs = args.ToList();

            if (args.Length < paramList.Count)
            {
                for (var i = args.Length; i < paramList.Count; i++)
                {
                    var p = paramList[i];
                    newArgs.Add(p.IsOptional ? null : mocker.GetParameter(p));
                }
            }

            var obj = mocker.AddInjections(info?.Invoke(newArgs.ToArray()));
            return mocker.InnerMockResolution ? mocker.AddProperties(type, obj) : obj;
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

        /// <summary>
        /// Setups the mock for given property name.
        /// </summary>
        /// <typeparam name="TMock">The type of the t mock.</typeparam>
        /// <param name="mock">The mock.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">The value.</param>
        public static void SetupMockProperty<TMock>(this Mock<TMock> mock, string propertyName, object value) where TMock : class
        {
            var propertyInfo = typeof(TMock).GetProperty("Headers");
            mock.SetupMockProperty(propertyInfo, value);
        }
    }
}
