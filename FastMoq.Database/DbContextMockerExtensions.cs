using FastMoq.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq
{
    /// <summary>
    /// Entity Framework-specific FastMoq helpers.
    /// </summary>
    public static class DbContextMockerExtensions
    {
        /// <summary>
        /// Creates or returns a DbContext test handle using the requested provisioning mode.
        /// </summary>
        public static DbContextHandle<TContext> GetDbContextHandle<TContext>(this Mocker mocker, DbContextHandleOptions<TContext>? options = null)
            where TContext : DbContext
        {
            ArgumentNullException.ThrowIfNull(mocker);

            options ??= new DbContextHandleOptions<TContext>();

            if (TryGetTrackedHandle(mocker, out DbContextHandle<TContext>? existingHandle))
            {
                ArgumentNullException.ThrowIfNull(existingHandle);

                if (existingHandle.Mode != options.Mode)
                {
                    throw new InvalidOperationException($"A DbContext handle for {typeof(TContext).Name} is already tracked in {existingHandle.Mode} mode.");
                }

                return existingHandle;
            }

            return options.Mode switch
            {
                DbContextTestMode.MockedSets => CreateMockHandle<TContext>(mocker),
                DbContextTestMode.RealInMemory => CreateRealHandle<TContext>(mocker, options),
                _ => throw new ArgumentOutOfRangeException(nameof(options.Mode)),
            };
        }

        /// <summary>
        /// Creates or returns the tracked DbContext mock for the requested context type.
        /// </summary>
        public static DbContextMock<TContext> GetMockDbContext<TContext>(this Mocker mocker) where TContext : DbContext
        {
            var handle = mocker.GetDbContextHandle<TContext>();
            return handle.Mock ?? throw new InvalidOperationException($"Unable to create DbContext mock for {typeof(TContext).Name}.");
        }

        /// <summary>
        /// Creates or returns the tracked DbContext mock for the requested context type.
        /// </summary>
        public static Mock GetMockDbContext(this Mocker mocker, Type dbContextType)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(dbContextType);

            var method = typeof(DbContextMockerExtensions)
                .GetMethods()
                .Single(candidate =>
                    candidate.Name == nameof(GetMockDbContext) &&
                    candidate.IsGenericMethodDefinition &&
                    candidate.GetParameters().Length == 1);

            return method.MakeGenericMethod(dbContextType).Invoke(null, [mocker]) as Mock
                ?? throw new InvalidOperationException($"Unable to create DbContext mock for {dbContextType.Name}.");
        }

        public static bool TryCreateManagedDbContextInstance(Mocker mocker, Type requestedType, out object? instance)
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(requestedType);

            instance = null;
            if (!IsDbContextType(requestedType) || mocker.Contains(requestedType))
            {
                return false;
            }

            var mock = mocker.GetMockDbContext(requestedType);
            instance = mock.Object;
            return true;
        }

        private static DbContextHandle<TContext> CreateMockHandle<TContext>(Mocker mocker) where TContext : DbContext
        {
            if (mocker.Contains<TContext>())
            {
                var trackedMock = (DbContextMock<TContext>) mocker.GetMock<TContext>();
                return TrackHandle(mocker, new DbContextHandle<TContext>(DbContextTestMode.MockedSets, trackedMock.Object, trackedMock));
            }

            var mock = CreateLegacyDbContextMock(typeof(TContext), MockBehavior.Default, Array.Empty<object?>()) as DbContextMock<TContext>
                ?? throw new InvalidOperationException($"Unable to create DbContext mock for {typeof(TContext).Name}.");

            mocker.AddMock(mock, overwrite: true, nonPublic: true);
            SetupTrackedMock(mocker, typeof(TContext), mock);
            mock.SetupDbSets(mocker);
            return TrackHandle(mocker, new DbContextHandle<TContext>(DbContextTestMode.MockedSets, mock.Object, mock));
        }

        private static DbContextHandle<TContext> CreateRealHandle<TContext>(Mocker mocker, DbContextHandleOptions<TContext> options) where TContext : DbContext
        {
            if (mocker.Contains<TContext>())
            {
                throw new InvalidOperationException($"A mocked DbContext for {typeof(TContext).Name} is already tracked.");
            }

            if (mocker.HasTypeRegistration(typeof(TContext)))
            {
                throw new InvalidOperationException($"A DbContext registration for {typeof(TContext).Name} already exists. Create the handle before registering the context manually, or reuse the existing registration directly.");
            }

            var dbContextOptions = CreateInMemoryOptionsCore<TContext>(options.DatabaseName);
            var context = options.RealContextFactory != null
                ? options.RealContextFactory(dbContextOptions)
                : CreateRealContext(dbContextOptions);

            context.Database.EnsureCreated();
            mocker.AddType<DbContextOptions<TContext>>(_ => dbContextOptions, replace: true);
            mocker.AddType(_ => context, replace: true);
            return TrackHandle(mocker, new DbContextHandle<TContext>(DbContextTestMode.RealInMemory, context, null));
        }

        private static bool TryGetTrackedHandle<TContext>(Mocker mocker, out DbContextHandle<TContext>? handle) where TContext : DbContext
        {
            handle = null;
            if (!mocker.HasTypeRegistration(typeof(DbContextHandle<TContext>)))
            {
                return false;
            }

            handle = mocker.GetObject<DbContextHandle<TContext>>();
            return handle != null;
        }

        private static DbContextHandle<TContext> TrackHandle<TContext>(Mocker mocker, DbContextHandle<TContext> handle) where TContext : DbContext
        {
            mocker.AddType(handle, replace: true);
            return handle;
        }

        public static Mock? CreateLegacyDbContextMock(Type requestedType, MockBehavior behavior, object?[] constructorArgs)
        {
            ArgumentNullException.ThrowIfNull(requestedType);
            ArgumentNullException.ThrowIfNull(constructorArgs);

            var mockType = typeof(DbContextMock<>).MakeGenericType(requestedType);
            var factoryMethod = typeof(DbContextMockerExtensions)
                .GetMethod(nameof(CreateDbContextFactory), BindingFlags.Static | BindingFlags.NonPublic)
                ?.MakeGenericMethod(requestedType)
                ?? throw new MissingMethodException(nameof(CreateDbContextFactory));

            var normalizedArgs = NormalizeConstructorArguments(requestedType, constructorArgs);
            var factoryExpression = factoryMethod.Invoke(null, [normalizedArgs])
                ?? throw new InvalidOperationException($"Unable to build constructor factory for {requestedType.Name}.");

            var expressionType = typeof(Expression<>).MakeGenericType(typeof(Func<>).MakeGenericType(requestedType));
            var constructor = mockType.GetConstructor([expressionType, typeof(MockBehavior)])
                ?? throw new MissingMethodException(mockType.Name, ".ctor(Expression<Func<TContext>>, MockBehavior)");

            return constructor.Invoke([factoryExpression, behavior]) as Mock;
        }

        private static bool IsDbContextType(Type type)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (string.Equals(current.FullName, "Microsoft.EntityFrameworkCore.DbContext", StringComparison.Ordinal) &&
                    string.Equals(current.Assembly.GetName().Name, "Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void SetupTrackedMock(Mocker mocker, Type mockedType, Mock mock)
        {
            var method = typeof(Mocker).GetMethod("SetupMock", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            method?.Invoke(mocker, [mockedType, mock]);
        }

        private static object?[] NormalizeConstructorArguments(Type requestedType, object?[] constructorArgs)
        {
            var constructors = requestedType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (constructors.Any(constructor => constructor.GetParameters().Length == 0))
            {
                return Array.Empty<object?>();
            }

            var dbContextOptionsConstructor = constructors.FirstOrDefault(constructor =>
            {
                var parameters = constructor.GetParameters();
                return parameters.Length == 1 && IsDbContextOptionsType(parameters[0].ParameterType);
            });

            if (dbContextOptionsConstructor != null)
            {
                return [CreateInMemoryOptions(requestedType)];
            }

            if (constructorArgs.Length > 0 && constructorArgs.All(argument => argument != null))
            {
                return constructorArgs;
            }

            var preferredConstructor = constructors
                .OrderBy(constructor => constructor.GetParameters().Length)
                .FirstOrDefault();

            if (preferredConstructor == null)
            {
                return Array.Empty<object?>();
            }

            return constructorArgs;
        }

        private static bool IsDbContextOptionsType(Type type)
        {
            return type == typeof(DbContextOptions) ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DbContextOptions<>));
        }

        private static object CreateInMemoryOptions(Type requestedType)
        {
            var method = typeof(DbContextMockerExtensions)
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Single(candidate =>
                    candidate.Name == nameof(CreateInMemoryOptionsCore) &&
                    candidate.IsGenericMethodDefinition &&
                    candidate.GetParameters().Length == 0)
                .MakeGenericMethod(requestedType);

            return method.Invoke(null, null)
                ?? throw new InvalidOperationException($"Unable to create DbContext options for {requestedType.Name}.");
        }

        private static DbContextOptions<TContext> CreateInMemoryOptionsCore<TContext>() where TContext : DbContext
        {
            return CreateInMemoryOptionsCore<TContext>(databaseName: null);
        }

        private static DbContextOptions<TContext> CreateInMemoryOptionsCore<TContext>(string? databaseName) where TContext : DbContext
        {
            return new DbContextOptionsBuilder<TContext>()
                .UseInMemoryDatabase(databaseName ?? $"FastMoq_{typeof(TContext).Name}_{Guid.NewGuid():N}")
                .Options;
        }

        private static TContext CreateRealContext<TContext>(DbContextOptions<TContext> options) where TContext : DbContext
        {
            var constructor = typeof(TContext)
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(candidate =>
                {
                    var parameters = candidate.GetParameters();
                    return parameters.Length == 1 && IsDbContextOptionsType(parameters[0].ParameterType);
                });

            if (constructor == null)
            {
                throw new NotSupportedException($"RealInMemory mode requires a {typeof(TContext).Name} constructor that accepts DbContextOptions or a custom RealContextFactory.");
            }

            return (TContext) constructor.Invoke([options]);
        }

        private static Expression<Func<TContext>> CreateDbContextFactory<TContext>(object?[] constructorArgs) where TContext : DbContext
        {
            var constructor = typeof(TContext)
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(candidate => candidate.GetParameters().Length == constructorArgs.Length)
                ?? throw new MissingMethodException(typeof(TContext).Name, ".ctor");

            var parameters = constructor.GetParameters();
            var arguments = new Expression[constructorArgs.Length];
            for (var index = 0; index < constructorArgs.Length; index++)
            {
                arguments[index] = Expression.Constant(constructorArgs[index], parameters[index].ParameterType);
            }

            var body = Expression.New(constructor, arguments);
            return Expression.Lambda<Func<TContext>>(body);
        }
    }
}