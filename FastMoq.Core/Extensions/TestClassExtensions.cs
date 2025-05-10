using FastMoq.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO.Abstractions.TestingHelpers;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit.Abstractions;

namespace FastMoq.Extensions
{
    /// <summary>
    ///     Helper Methods for testing.
    /// </summary>
    public static class TestClassExtensions
    {
        /// <summary>
        /// The default type mappings are only in effect when a TypeMap is not manually mapped by the test class. When a map item does not exist, it will look here.
        /// </summary>
        private static readonly List<(Type InterfaceType, Type DefaultType)> DefaultTypeMappings =
        [
            (typeof(ILogger), typeof(NullLogger)),
            (typeof(ILogger<>), typeof(NullLogger)),
            (typeof(IEnumerable<>), typeof(List<>)),
            (typeof(IDictionary<,>), typeof(Dictionary<,>)),
            (typeof(IList<>), typeof(List<>)),
            (typeof(ICollection<>), typeof(Collection<>)),
            (typeof(ISet<>), typeof(HashSet<>)),
            (typeof(IReadOnlyCollection<>), typeof(Collection<>)),
            (typeof(IReadOnlyList<>), typeof(List<>)),
            (typeof(IReadOnlyDictionary<,>), typeof(Dictionary<,>)),
            (typeof(IOptions<>), typeof(OptionsWrapper<>)),
        ];

        /// <summary>
        ///     Calls the generic method.
        /// </summary>
        /// <param name="typeParameter">The type parameter.</param>
        /// <param name="obj">The object.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>Calls the generic method.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.MissingMethodException"></exception>
        public static object? CallGenericMethod(this Type typeParameter, object obj, [CallerMemberName] string? methodName = null,
            Type[]? parameterTypes = null, object[]? parameters = null)
        {
            ArgumentNullException.ThrowIfNull(obj);

            parameterTypes ??= [];
            parameters ??= [];

            var method = obj.GetType().GetMethods()
                             .FirstOrDefault(m => m.Name == methodName &&
                                                  m.IsGenericMethodDefinition &&
                                                  m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes)
                             ) ??
                         throw new MissingMethodException(obj.GetType().FullName, methodName);

            var genericMethod = method.MakeGenericMethod(typeParameter);
            return genericMethod.Invoke(obj, parameters);
        }

        /// <summary>
        ///     Ensures the null check thrown.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="constructorName">Name of the constructor.</param>
        /// <param name="output">The output.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void EnsureNullCheckThrown(this Action action, string parameterName, string? constructorName = "",
            Action<string>? output = null)
        {
            ArgumentNullException.ThrowIfNull(action);

            try
            {
                output?.Invoke($"Testing {constructorName}\n - {parameterName}");

                try
                {
                    action();
                }
                catch (ArgumentNullException ex)
                {
                    if (!ex.Message.Contains(parameterName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw;
                    }
                }

                output?.Invoke($"Passed {parameterName}");
            }
            catch
            {
                output?.Invoke($"Failed {parameterName}");
                throw;
            }
        }

        /// <summary>
        ///     Ensures the null check thrown.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="constructorName">Name of the constructor.</param>
        /// <param name="output">The output.</param>
        public static void EnsureNullCheckThrown(this Action action, string parameterName,
            string? constructorName, ITestOutputHelper? output) =>
            action.EnsureNullCheckThrown(parameterName, constructorName, s => output?.WriteLine(s));

        /// <summary>
        ///     Gets the default value.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><see cref="Nullable{T}" />.</returns>
        public static object? GetDefaultValue(this Type type) => type switch
        {
            { FullName: "System.Uri" } => new UriBuilder { Scheme = "http", Host = "localhost" }.Uri,
            { FullName: "System.String" } => string.Empty,
            _ when typeof(IEnumerable).IsAssignableFrom(type) => Array.CreateInstance(type.GetElementType() ?? typeof(object), 0),
            { IsClass: true } => null,
            { IsInterface: true } => null,
            _ => Activator.CreateInstance(type),
        };

        /// <summary>
        ///     Gets the field.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <returns><see cref="Nullable{FieldInfo}" />.</returns>
        public static FieldInfo? GetField<TObject>(this TObject obj, string name) where TObject : class? =>
            obj?.GetType().GetRuntimeFields()
                .FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        /// <summary>
        ///     Gets the field information.
        /// </summary>
        /// <typeparam name="TType">The type of the t type.</typeparam>
        /// <param name="_">The object.</param>
        /// <param name="name">The name.</param>
        /// <returns>System.Nullable&lt;FieldInfo&gt;.</returns>
        public static FieldInfo GetFieldInfo<TType>(this object _, string name)
        {
            var fields = typeof(TType).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            return fields.First(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Gets the field value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TType">The type of the t type.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <returns>System.Nullable&lt;T&gt;.</returns>
        public static T? GetFieldValue<T, TType>(this object obj, string name) => obj.GetFieldValue<T>(obj.GetFieldInfo<TType>(name));

        /// <summary>
        ///     Gets the field value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="field">The field.</param>
        /// <returns>System.Nullable&lt;T&gt;.</returns>
        public static T? GetFieldValue<T>(this object? obj, FieldInfo field) => (T?) field.GetValue(obj);

        /// <summary>
        ///     Gets the field value.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns><see cref="Nullable{Object}" />.</returns>
        public static object? GetFieldValue<TObject>(this TObject obj, string name, TObject? defaultValue = null) where TObject : class?
            => obj.GetField(name)?.GetValue(obj) ?? defaultValue ?? default;

        /// <summary>
        ///     Gets the property value based on lambda.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TValue">The type of the t value.</typeparam>
        /// <param name="_">The object.</param>
        /// <param name="memberLambda">The member lambda.</param>
        /// <returns>System.Nullable&lt;TValue&gt;.</returns>
        public static MemberInfo GetMember<T, TValue>(this T _, Expression<Func<T, TValue>> memberLambda) =>
            memberLambda.GetMemberExpression().Member;

        /// <summary>
        ///     Gets the member expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method">The method.</param>
        /// <returns>MemberExpression.</returns>
        public static MemberExpression GetMemberExpression<T>(this Expression<T> method) => method.GetMemberExpressionInternal();

        /// <summary>
        ///     Gets the member expression.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>MemberExpression.</returns>
        public static MemberExpression GetMemberExpression(this Expression method) => method.GetMemberExpressionInternal();

        /// <summary>
        ///     Gets the name of the member.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TValue">The type of the t value.</typeparam>
        /// <param name="_">The .</param>
        /// <param name="memberLambda">The member lambda.</param>
        /// <returns>System.String.</returns>
        public static string GetMemberName<T, TValue>(this T _, Expression<Func<T, TValue>> memberLambda) =>
            memberLambda.GetMemberExpression().Member.Name;

        /// <summary>
        ///     Gets the name of the member.
        /// </summary>
        /// <param name="memberLambda">The member lambda.</param>
        /// <returns>System.String.</returns>
        public static string GetMemberName(this Expression memberLambda) => memberLambda.GetMemberExpressionInternal().Member.Name;

        /// <summary>
        ///     Gets the method.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <returns><see cref="Nullable{MethodInfo}" />.</returns>
        public static MethodInfo? GetMethod<TObject>(this TObject obj, string name) where TObject : class? =>
            obj?.GetType().GetRuntimeMethods()
                .FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        /// <summary>
        ///     Gets the method value.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>System.Nullable&lt;System.Object&gt;.</returns>
        public static object? GetMethodValue<TObject>(this TObject obj, string name, object? defaultValue = null, params object[] args)
            where TObject : class?
            => obj.GetMethod(name)?.Invoke(obj, args) ?? defaultValue;

        /// <summary>
        ///     Gets the property.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <returns><see cref="Nullable{PropertyInfo}" />.</returns>
        public static PropertyInfo? GetProperty<TObject>(this TObject obj, string name) =>
            obj?.GetType().GetRuntimeProperties()
                .FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

        /// <summary>
        ///     Gets the property value.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns><see cref="Nullable{Object}" />.</returns>
        public static object? GetPropertyValue<TObject>(this TObject obj, string name, object? defaultValue = null)
            where TObject : class? =>
            obj.GetProperty(name)?.GetValue(obj) ?? defaultValue ?? default;

        /// <summary>
        ///     Gets the test data.
        /// </summary>
        /// <param name="testData">The test data.</param>
        /// <param name="i">The i.</param>
        /// <param name="p">The p.</param>
        /// <returns>object of the test data.</returns>
        public static object? GetTestData(this IReadOnlyList<object>? testData, int i, ParameterInfo p) =>
            testData != null && i < testData.Count ? testData[i] : p?.ParameterType.GetDefaultValue();

        /// <summary>
        ///     Gets the type from interface.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="tType">Type of the t.</param>
        /// <param name="typeList">The type list.</param>
        /// <returns>Type.</returns>
        /// <exception cref="System.Runtime.AmbiguousImplementationException"></exception>
        public static Type GetTypeFromInterface(this Mocker mocker, Type tType, List<Type>? typeList = null)
        {
            if (!tType.IsInterface)
            {
                return tType;
            }

            if (tType.Name.StartsWith("IOptions"))
            {
                Console.WriteLine();
            }
            var mappedType = mocker.typeMap.Where(x => x.Key == tType).Select(x => x.Value).FirstOrDefault();
            if (mappedType != null)
            {
                return mappedType.InstanceType;
            }

            foreach (var (interfaceType, defaultType) in DefaultTypeMappings)
            {
                if (TryMapGenericType(tType, interfaceType, defaultType, mocker, out var result))
                {
                    return result;
                }
            }

            // Continue with existing logic if no match is found
            var types = typeList ?? tType.Assembly.GetTypes().ToList();
            return GetTypeFromInterfaceList(mocker, tType, types);
        }

        private static bool TryMapGenericType(Type tType, Type interfaceType, Type defaultType, Mocker mocker, out Type type)
        {
            var sourceType = tType.IsGenericType
                ? tType.GetGenericTypeDefinition()
                : tType;

            var destType = defaultType.IsGenericType
                ? defaultType.GetGenericTypeDefinition()
                : defaultType;

            var warningMessage = $"WARNING: {sourceType.Name} found and not mapped. Assuming {destType.Name}.";

            if (sourceType == interfaceType)
            {
                mocker.ExceptionLog.Add(warningMessage);
                type = defaultType;
            }
            else
            {
                type = tType;
            }

            return sourceType == interfaceType;

        }

        private static Type GetTypeFromInterfaceList(Mocker mocker, Type tType, List<Type> types)
        {
            // Get interfaces that contain T.
            var allInterfaces = types.Where(type => type.IsInterface).ToList();
            var interfaces = allInterfaces.Where(type => ImplementsGenericInterface(type, tType)).ToList();

            // Updated possibleTypes assignment
            var possibleTypes = types.Where(type =>
                interfaces.TrueForAll(iType => type != iType) &&
                !type.IsAbstract &&
                (
                    tType.IsGenericType
                        ? type.IsGenericType && (IsGenericTypeMatch(type, tType) || ImplementsGenericInterface(type, tType))
                        : type.GetInterfaces().Contains(tType) || ImplementsGenericInterface(type, tType)
                ) &&
                !interfaces.Exists(iType => iType.IsAssignableFrom(type))
            ).ToList();

            var result = mocker.FindBestMatch(possibleTypes, tType);

            return result;
        }

        private static Type FindBestMatch(this Mocker mocker, IList<Type> possibleTypes, Type tType)
        {
            Type? match;

            return possibleTypes.Count switch
            {
                1 => possibleTypes.First(),
                0 => tType,
                > 1 when (match = GetSingleMatch(possibleTypes, x => IsGenericTypeMatch(x, tType))) is not null => match,
                > 1 when (match = GetSingleMatch(possibleTypes, x => x.Name == tType.Name)) is not null => match,
                > 1 when (match = GetSingleMatch(possibleTypes, x => x.IsPublic)) is not null => match,
                > 1 when (match = GetSingleMatch(possibleTypes, x => x.GetInterfaces() is { Length: 1 } interfaces && interfaces[0] == tType)) is not
                    null => match,
                > 1 when IsIEnumerableGenericType(tType) => possibleTypes.First(),
                _ => throw mocker.GetAmbiguousImplementationException(tType, possibleTypes)
            };
        }


        private static T? GetSingleMatch<T>(IEnumerable<T> items, Func<T, bool> predicate)
        {
            var matches = items.Where(predicate).Take(2).ToList();

            return matches.Count == 1
                ? matches[0]
                : default;
        }

        private static bool IsIEnumerableGenericType(Type tType)
        {
            return tType.IsGenericType && (tType.GetGenericTypeDefinition() == typeof(IEnumerable<>) || tType.GetInterfaces().Any(t => t == typeof(IEnumerable<>)));
        }

        private static bool ImplementsGenericInterface(Type givenType, Type genericInterfaceType)
        {
            // Ensure we're working with the correct type
            var genericTypeDefinition = genericInterfaceType.IsGenericType
                ? genericInterfaceType.GetGenericTypeDefinition()
                : genericInterfaceType;

            // Check all interfaces implemented by the given type
            var interfaceTypes = givenType.GetInterfaces();

            // Also check if the given type itself is the generic type
            return interfaceTypes.Any(interfaceType => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == genericTypeDefinition) || (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericTypeDefinition);
        }

        private static bool IsGenericTypeMatch(Type x, Type tType)
        {
            if (x.IsGenericType && tType.IsGenericType)
            {
                var xGenericTypeDef = x.GetGenericTypeDefinition();
                var tTypeGenericTypeDef = tType.GetGenericTypeDefinition();
                if (xGenericTypeDef == tTypeGenericTypeDef)
                {
                    return true;
                }

                // Additional check for constructed types
                if (x.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == tTypeGenericTypeDef))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Throws the ambiguous implementation exception.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="message">The message.</param>
        /// <returns>System.Runtime.AmbiguousImplementationException.</returns>
        public static AmbiguousImplementationException GetAmbiguousImplementationException(this Mocker mocker, string message)
        {
            mocker.ExceptionLog.Add(message);
            return new AmbiguousImplementationException(message);
        }

        public static void AddFiles(this MockFileSystem fileSystem, IDictionary<string, MockFileData> files)
        {
            foreach (var mockFileData in files)
            {
                fileSystem.AddFile(mockFileData.Key, mockFileData.Value);
            }
        }

        /// <summary>
        /// Gets the ambiguous implementation exception.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="tType">Type of the t.</param>
        /// <param name="types">The types.</param>
        /// <returns>System.Runtime.AmbiguousImplementationException.</returns>
        public static AmbiguousImplementationException GetAmbiguousImplementationException(this Mocker mocker, Type tType, ICollection<Type>? types = null)
        {
            var builder = new StringBuilder($"Multiple components of type '{tType}' was found. Use Mocker.AddType to specify the correct resolution.");

            if (types?.Count > 1)
            {
                builder.AppendLine("\r\nTypes found:");

                builder.AppendJoin(", ", types.Select(x => x.FullName));
            }
            return mocker.GetAmbiguousImplementationException(builder.ToString());
        }

        /// <summary>
        /// Throws the ambiguous constructor implementation exception.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="tType">Type of the t.</param>
        /// <returns>System.Runtime.AmbiguousImplementationException.</returns>
        public static AmbiguousImplementationException GetAmbiguousConstructorImplementationException(this Mocker mocker, Type tType) =>
            mocker.GetAmbiguousImplementationException($"Multiple parameterized constructors exist of type '{tType}'. Cannot decide which to use.");

        /// <summary>
        ///     Sets the field value.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static void SetFieldValue<TObject>(this TObject obj, string name, object? value) where TObject : class? =>
            obj.GetField(name)?.SetValue(obj, value);

        /// <summary>
        ///     Sets the property value.
        /// </summary>
        /// <typeparam name="TObject">The type of the t object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public static void SetPropertyValue<TObject>(this TObject obj, string name, object? value) where TObject : class? =>
            obj.GetProperty(name)?.SetValue(obj, value);

        /// <summary>
        ///     Verifies the Mock ILogger was invoked.
        /// </summary>
        /// <param name="loggerMock">The logger mock.</param>
        /// <param name="logLevel">The expected log level.</param>
        /// <param name="message">The expected message.</param>
        /// <param name="times">The expected number of invocations.</param>
        public static void VerifyLogger(this Mock<ILogger> loggerMock, LogLevel logLevel, string message, int times = 1) => loggerMock.VerifyLogger(logLevel, message, null, null, times);

        /// <summary>
        ///     Verifies the Mock ILogger of T was invoked.
        /// </summary>
        /// <typeparam name="TLogger"></typeparam>
        /// <param name="loggerMock">The logger mock.</param>
        /// <param name="logLevel">The expected log level.</param>
        /// <param name="message">The expected message.</param>
        /// <param name="times">The expected number of invocations.</param>
        public static void VerifyLogger<TLogger>(this Mock<TLogger> loggerMock, LogLevel logLevel, string message, int times = 1)
        where TLogger : class, ILogger
            => loggerMock.VerifyLogger(logLevel, message, null, null, times);

        /// <summary>
        ///     Verifies the Mock ILogger was invoked.
        /// </summary>
        /// <param name="loggerMock">The logger mock.</param>
        /// <param name="logLevel">The log level.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="times">The expected number of invocations.</param>
        public static void VerifyLogger(this Mock<ILogger> loggerMock, LogLevel logLevel, string message, Exception? exception, int? eventId = null,
                                        int times = 1) => loggerMock.VerifyLogger<Exception>(logLevel, message, exception, eventId, times);

        /// <summary>
        ///     Verifies the Mock ILogger was invoked.
        /// </summary>
        /// <param name="loggerMock">The logger mock.</param>
        /// <param name="logLevel">The log level.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="times">The expected number of invocations.</param>
        public static void VerifyLogger<TLogger>(this Mock<TLogger> loggerMock, LogLevel logLevel, string message, Exception? exception, int? eventId = null,
                                           int times = 1) where TLogger : class, ILogger => loggerMock.VerifyLogger<Exception, TLogger>(logLevel, message, exception, eventId, times);

        /// <summary>
        ///     Verifies the Mock ILogger was invoked.
        /// </summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <param name="loggerMock">The logger mock.</param>
        /// <param name="logLevel">The log level.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="times">The expected number of invocations.</param>
        public static void VerifyLogger<TException>(this Mock<ILogger> loggerMock, LogLevel logLevel, string message, TException? exception, int? eventId = null, int times = 1)
                where TException : Exception
        {
            loggerMock.RaiseIfNull();
            loggerMock.Verify(TestLoggerExpression<TException, ILogger>(logLevel, message, exception, eventId), Times.Exactly(times));
        }

        public static void VerifyLogger<TException>(this Mock<ILogger> loggerMock, LogLevel logLevel, string message, TException? exception, int? eventId, Times times)
            where TException : Exception
        {
            loggerMock.RaiseIfNull();
            loggerMock.Verify(TestLoggerExpression<TException, ILogger>(logLevel, message, exception, eventId), times);
        }

        public static void VerifyLogger<TException>(this Mock<ILogger> loggerMock, LogLevel logLevel, string message, TException? exception, int? eventId, Func<Times> times)
            where TException : Exception
        {
            loggerMock.RaiseIfNull();
            loggerMock.Verify(TestLoggerExpression<TException, ILogger>(logLevel, message, exception, eventId), times);
        }

        /// <summary>
        ///     Verifies the Mock ILogger was invoked.
        /// </summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <typeparam name="TLogger">The type of ILogger.</typeparam>
        /// <param name="loggerMock">The logger mock.</param>
        /// <param name="logLevel">The log level.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="times">The expected number of invocations.</param>
        public static void VerifyLogger<TException, TLogger>(this Mock<TLogger> loggerMock, LogLevel logLevel, string message, TException? exception, int? eventId = null, int times = 1)
            where TException : Exception where TLogger : class, ILogger
        {
            loggerMock.RaiseIfNull();
            loggerMock.Verify(TestLoggerExpression<TException, TLogger>(logLevel, message, exception, eventId), Times.Exactly(times));
        }

        /// <summary>
        /// Setups the logger callback.
        /// </summary>
        /// <typeparam name="TLogger">Class type of ILogger or ILogger{T}</typeparam>
        /// <param name="logger">The mock logger.</param>
        /// <param name="callback">The callback action.</param>
        public static void SetupLoggerCallback<TLogger>(this Mock<TLogger> logger, Action<LogLevel, EventId, string> callback) where TLogger : class, ILogger
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(callback);

            logger
               .Setup(x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>) It.IsAny<object>()))
               .Callback((LogLevel logLevel, EventId eventId, object state, Exception? exception, Delegate formatter) =>
                {
                    // Optional: Capture or inspect log messages here
                    var message = formatter.DynamicInvoke(state, exception);
                    callback.Invoke(logLevel, eventId, message?.ToString() ?? string.Empty);
                });
        }

        /// <summary>
        ///     Tests the logger expression2.
        /// </summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <typeparam name="TLoggerType">The ILogger type.</typeparam>
        /// <param name="logLevel">The log level.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <returns>System.Linq.Expressions.Expression&lt;System.Action&lt;T&gt;&gt;.</returns>
        internal static Expression<Action<TLoggerType>> TestLoggerExpression<TException, TLoggerType>(LogLevel logLevel, string message, TException? exception,
                                                                                             int? eventId) where TException : Exception where TLoggerType : ILogger =>
            logger =>
            logger.Log(
                logLevel,
                It.Is<EventId>(e => CheckEventId(e, eventId)),
                It.Is<It.IsAnyType>((o, t) => CheckMessage(o.ToString() ?? string.Empty, t, message, t)),
                It.Is<Exception>(e => CheckException(e, exception)),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>());

        /// <summary>
        ///     Checks the expectedMessage.
        /// </summary>
        /// <param name="verifyMessage">The object.</param>
        /// <param name="type">The type.</param>
        /// <param name="expectedMessage">The expected expectedMessage.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <returns>System.Boolean.</returns>
        internal static bool CheckMessage(string verifyMessage, Type type, string expectedMessage, Type expectedType) =>
            verifyMessage.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase) &&
            type.IsAssignableTo(expectedType);

        /// <summary>
        ///     Checks the event identifier.
        /// </summary>
        /// <param name="verifyEventId">The event identifier.</param>
        /// <param name="eventId">The expected event identifier.</param>
        /// <returns>System.Boolean.</returns>
        internal static bool CheckEventId(EventId verifyEventId, int? eventId) => eventId == null || verifyEventId == eventId;

        /// <summary>
        ///     Checks the expectedException.
        /// </summary>
        /// <param name="verifyException">The expectedException.</param>
        /// <param name="expectedException">The expected expectedException.</param>
        /// <returns>System.Boolean.</returns>
        internal static bool CheckException(Exception verifyException, Exception? expectedException) => expectedException == null ||
            (verifyException.Message.Contains(
                 expectedException.Message, StringComparison.OrdinalIgnoreCase) &&
             verifyException.GetType().IsAssignableTo(expectedException.GetType()));

        /// <summary>
        ///     ForEach for <see cref="IEnumerable{T}" />.
        /// </summary>
        /// <typeparam name="T">Type of item.</typeparam>
        /// <param name="iEnumerable">The <see cref="IEnumerable{T}" />.</param>
        /// <param name="action">The action.</param>
        internal static void ForEach<T>(this IEnumerable<T> iEnumerable, Action<T> action) => iEnumerable.ToList().ForEach(action);

        /// <summary>
        ///     Gets the argument data.
        /// </summary>
        /// <param name="mocker">The mocker.</param>
        /// <param name="method">The method.</param>
        /// <param name="data">The data.</param>
        /// <returns>object?[] of the argument data.</returns>
        internal static object?[] GetArgData(this Mocker mocker, MethodBase? method, Dictionary<Type, object?>? data)
        {
            var args = new List<object?>();

            method?.GetParameters().ToList().ForEach(p => args.Add(data?.Any(x => x.Key == p.ParameterType) ?? false
                    ? data.First(x => x.Key == p.ParameterType).Value
                    : mocker.GetParameter(p)
                )
            );

            return args.ToArray();
        }

        /// <summary>
        ///     Gets the injection fields.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="attributeType">Override attribute type.</param>
        /// <returns><see cref="IEnumerable{T}" />.</returns>
        internal static IEnumerable<FieldInfo> GetInjectionFields(this Type type, Type? attributeType = null) =>
            type
                .GetRuntimeFields()
                .Where(x => x.CustomAttributes.Any(y =>
                        y.AttributeType == attributeType ||
                        y.AttributeType.Name.Equals("InjectAttribute", StringComparison.OrdinalIgnoreCase)
                    )
                );

        /// <summary>
        ///     Gets the injection properties.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="attributeType">Override attribute type.</param>
        /// <returns><see cref="IEnumerable{T}" />.</returns>
        internal static IEnumerable<PropertyInfo> GetInjectionProperties(this Type type, Type? attributeType = null) =>
            type
                .GetRuntimeProperties()
                .Where(x => x.CustomAttributes.Any(y =>
                        y.AttributeType == attributeType ||
                        y.AttributeType.Name.Equals("InjectAttribute", StringComparison.OrdinalIgnoreCase)
                    )
                );

        /// <summary>
        ///     Gets the member expression internal.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>MemberExpression of the member expression internal.</returns>
        /// <exception cref="ArgumentNullException">nameof(method)</exception>
        internal static MemberExpression GetMemberExpressionInternal(this Expression method)
        {
            if (method is not LambdaExpression lambda)
            {
                throw new ArgumentNullException(nameof(method));
            }

            var memberExpr = lambda.Body.NodeType switch
            {
                ExpressionType.Convert => ((UnaryExpression) lambda.Body).Operand as MemberExpression,
                ExpressionType.MemberAccess => lambda.Body as MemberExpression,
                _ => null,
            };

            return memberExpr ?? throw new ArgumentNullException(nameof(method));
        }

        /// <summary>
        ///     Gets the tested constructors.
        /// </summary>
        /// <param name="mocker">Mocker object</param>
        /// <param name="type">The type to try to create.</param>
        /// <param name="constructors">The constructors to test with the specified type.</param>
        /// <returns>List&lt;FastMoq.Models.ConstructorModel&gt;.</returns>
        internal static List<ConstructorModel> GetTestedConstructors(this Mocker mocker, Type type, List<ConstructorModel>? constructors)
        {
            constructors ??= [];
            var validConstructors = new List<ConstructorModel>();

            if (constructors.Count <= 1)
            {
                return constructors;
            }

            var targetError = new List<ConstructorModel>();

            foreach (var constructor in constructors)
            {
                try
                {
                    // Test Constructor.
                    var mock = mocker.CreateMockInternal(type, constructor.ParameterList, setupMock: false);
                    _ = mock.Object;
                    validConstructors.Add(constructor);
                }
                catch (TargetInvocationException ex)
                {
                    // Track invocation issues to bubble up if a good constructor is not found.
                    mocker.ExceptionLog.Add(ex.Message);
                    targetError.Add(constructor);
                }
                catch (Exception ex)
                {
                    mocker.ExceptionLog.Add(ex.Message);
                }
            }

            return validConstructors.Count > 0 ? validConstructors : targetError;
        }

        /// <summary>
        ///     Determines whether [is nullable type] [the specified type].
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if [is nullable type] [the specified type]; otherwise, <c>false</c>.</returns>
        internal static bool IsNullableType(this Type type) =>
            type.IsClass || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));

        /// <summary>
        ///     Returns true if the argument list == 0 or the types match the constructor exactly.
        /// </summary>
        /// <param name="type">Type which the constructor is from.</param>
        /// <param name="info">Parameter information.</param>
        /// <param name="instanceParameterValues">Optional arguments.</param>
        /// <returns><c>true</c> if [is valid constructor] [the specified information]; otherwise, <c>false</c>.</returns>
        internal static bool IsValidConstructor(this Type type, MethodBase info, params object?[] instanceParameterValues)
        {
            var paramList = info.GetParameters().ToList();

            if (instanceParameterValues.Length == 0)
            {
                return paramList.All(x => x.ParameterType != type);
            }

            if (instanceParameterValues.Length > paramList.Count)
            {
                return false;
            }

            var isValid = true;

            for (var i = 0; i < instanceParameterValues.Length; i++)
            {
                var paramType = paramList[i].ParameterType;
                var instanceType = instanceParameterValues[i]?.GetType();

                isValid &= (instanceType == null && (paramType.IsNullableType() || paramType.IsInterface)) ||
                           (instanceType != null && paramType.IsAssignableFrom(instanceType));
            }

            return isValid;
        }

        /// <summary>
        ///     Returns true if the argument list == 0 or the types match the constructor exactly.
        /// </summary>
        /// <param name="info">Method.</param>
        /// <param name="instanceParameterValues">Optional arguments.</param>
        /// <returns><c>true</c> if [is valid constructor] [the specified information]; otherwise, <c>false</c>.</returns>
        internal static bool IsValidConstructorByType(this MethodBase info, params Type?[] instanceParameterValues)
        {
            if (instanceParameterValues.Length == 0)
            {
                return true;
            }

            var paramList = info.GetParameters().ToList();

            if (instanceParameterValues.Length != paramList.Count)
            {
                return false;
            }

            var isValid = true;

            for (var i = 0; i < paramList.Count; i++)
            {
                var paramType = paramList[i].ParameterType;
                var instanceType = instanceParameterValues[i];

                isValid &= paramType.IsAssignableFrom(instanceType);
            }

            return isValid;
        }

        /// <summary>
        ///     Throws the already exists.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <exception cref="System.ArgumentException"></exception>
        internal static void ThrowAlreadyExists(this Type type) => throw new ArgumentException($"{type} already exists.");

        internal static void ThrowIfCastleMethodAccessException(this Exception ex)
        {
            if (IsCastleMethodAccessException(ex))
            {
                ThrowInternalConstructorException(ex);
            }

            if (IsCastleMethodAccessException(ex.GetBaseException()))
            {
                ThrowInternalConstructorException(ex.GetBaseException());
            }

            bool IsCastleMethodAccessException(Exception innerException) =>
                innerException.Message.Contains("Castle.DynamicProxy.IInterceptor[]", StringComparison.Ordinal);

            void ThrowInternalConstructorException(Exception innerException) => throw new MethodAccessException(
                "The test cannot see the internal constructor. Add [assembly: InternalsVisibleTo(\"DynamicProxyGenAssembly2\")] to the AssemblyInfo or project file.",
                innerException
            );
        }
    }
}
