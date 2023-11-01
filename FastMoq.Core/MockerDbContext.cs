using FastMoq.Extensions;
using FastMoq.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq
{
    /// <summary>
    ///     Class Mocker.
    /// </summary>
    public partial class Mocker
    {
        /// <summary>
        ///     Gets the database context using a SqlLite DB or provided options and DbConnection.
        /// </summary>
        /// <typeparam name="TContext">The type of the t context.</typeparam>
        /// <param name="options">The options.</param>
        /// <param name="connection">The connection.</param>
        /// <returns>TContext of the database context.</returns>
        public TContext GetDbContext<TContext>(DbContextOptions<TContext>? options = null, DbConnection? connection = null)
            where TContext : DbContext =>
            GetDbContext(contextOptions =>
                {
                    AddType(_ => contextOptions, true);
                    return CreateInstance<TContext>() ?? throw new InvalidOperationException("Unable to create DbContext.");
                },
                options,
                connection
            );

        /// <summary>
        ///     Gets the database context using a SqlLite DB or provided options and DbConnection.
        /// </summary>
        /// <typeparam name="TContext">The type of the t context.</typeparam>
        /// <param name="newObjectFunc">The new object function.</param>
        /// <param name="options">The options.</param>
        /// <param name="connection">The connection.</param>
        /// <returns>TContext.</returns>
        public TContext GetDbContext<TContext>(Func<DbContextOptions<TContext>, TContext> newObjectFunc, DbContextOptions<TContext>? options = null,
            DbConnection? connection = null) where TContext : DbContext
        {
            DbConnection = connection ?? new SqliteConnection("DataSource=:memory:");
            DbConnection.Open();

            var dbContextOptions = options ??
                                   new DbContextOptionsBuilder<TContext>()
                                       .UseSqlite(DbConnection)
                                       .Options;

            var context = newObjectFunc(dbContextOptions);
            context.Database.EnsureCreated();
            context.SaveChanges();

            return context;
        }

        /// <summary>
        ///     Gets the mock database context.
        /// </summary>
        /// <typeparam name="TDbContext">The type of the t database context.</typeparam>
        /// <returns>Mock&lt;TDbContext&gt; of the mock database context.</returns>
        public Mock<TDbContext> GetMockDbContext<TDbContext>() where TDbContext : DbContext
        {
            AddType(_ => new DbContextOptions<TDbContext>(), true);

            var genericDbSets = typeof(TDbContext).GetProperties()
                .Where(x => x.CanRead && x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            var mock = (Mock<TDbContext>) GetProtectedMock(typeof(TDbContext));
            mock.CallBase = true;

            genericDbSets.ForEach(x =>
                {
                    var value = GetValue(x.PropertyType);
                    SetupDbSetPropertyGet(mock, x, value);
                    SetupDbContextSetMethods(mock, x);
                }
            );

            return mock;

            object? GetValue(Type x)
            {
                var genericType = typeof(DbSetMock<>).MakeGenericType(x.GenericTypeArguments.First());
                var value = (Mock) Activator.CreateInstance(genericType);
                AddMock(value, genericType, true, x.IsNotPublic);
                return value.Object;
            }
        }

        /// <summary>
        ///     Gets the set expression.
        /// </summary>
        /// <typeparam name="TContext">The type of the t context.</typeparam>
        /// <param name="methodInfo">The method information.</param>
        /// <returns>Expression of the set expression.</returns>
        public Expression GetSetExpression<TContext>(MethodInfo methodInfo)
        {
            // Create an instance of the class
            var targetExpr = Expression.Parameter(typeof(TContext), "target");

            // Get parameters info from the MethodInfo
            var parametersInfo = methodInfo.GetParameters();

            // Create an array of ParameterExpression objects for the arguments
            List<ParameterExpression> arguments = new();

            parametersInfo.ForEach(parameter => arguments.Add(Expression.Parameter(parameter.ParameterType, parameter.Name)));
            var argArray = arguments.Cast<Expression>().ToArray();

            // Create an expression to call the MethodInfo instance with arguments
            var callExpr = Expression.Call(targetExpr, methodInfo, argArray);

            // Wrap the MethodCallExpression in a LambdaExpression
            var lambdaExpr = Expression.Lambda(callExpr, targetExpr);

            return lambdaExpr;
        }

        /// <summary>
        ///     Gets the set method.
        /// </summary>
        /// <typeparam name="TContext">The type of the t context.</typeparam>
        /// <param name="setType">Type of the set.</param>
        /// <param name="types">The types.</param>
        /// <returns>MethodInfo of the set method.</returns>
        public MethodInfo GetSetMethod<TContext>(Type setType, Type[]? types = null)
        {
            types ??= new Type[] { };

            // Get the MethodInfo for the parameterless Set method
            var setMethod = typeof(TContext).GetMethod("Set", BindingFlags.Public | BindingFlags.Instance, null, types, null);

            // Make sure setType is not already a DbSet
            if (setType.IsGenericType && setType.GetGenericTypeDefinition() == typeof(DbSet<>))
            {
                setType = setType.GetGenericArguments()[0];
            }

            // Get the generic version of the Set method for your setType
            var genericSetMethod = setMethod.MakeGenericMethod(setType);

            return genericSetMethod;
        }

        /// <summary>
        ///     Setups the mock.
        /// </summary>
        /// <typeparam name="TContext">The type of the t context.</typeparam>
        /// <param name="mockDbContext">The mock database context.</param>
        /// <param name="propertyInfo">The property information.</param>
        public void SetupDbContextSetMethods<TContext>(Mock<TContext> mockDbContext, PropertyInfo propertyInfo)
            where TContext : DbContext
        {
            var setType = propertyInfo.PropertyType;

            // Create a Func<T> at runtime
            var funcType = typeof(Func<>).MakeGenericType(setType);
            var propValueDelegate = Delegate.CreateDelegate(funcType, mockDbContext.Object, propertyInfo.GetGetMethod());

            if (setType.IsGenericType && setType.GetGenericTypeDefinition() == typeof(DbSet<>))
            {
                setType = setType.GetGenericArguments()[0];
            }

            SetupSetMethod(mockDbContext, setType, propValueDelegate);
            SetupSetMethod(mockDbContext, setType, propValueDelegate, new[] {typeof(string)});
        }

        /// <summary>
        ///     Setups the get.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TProperty">The type of the t property.</typeparam>
        /// <param name="mock">The mock.</param>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="value">The value.</param>
        public static void SetupDbSetPropertyGet<T, TProperty>(Mock<T> mock, PropertyInfo propertyInfo, TProperty value) where T : class
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var body = Expression.Property(parameter, propertyInfo);
            var lambda = Expression.Lambda<Func<T, TProperty>>(body, parameter);
            mock.Setup(lambda).Returns(value);
        }

        /// <summary>
        ///     Setups the set method.
        /// </summary>
        /// <typeparam name="TContext">The type of the t context.</typeparam>
        /// <param name="mockDbContext">The mock database context.</param>
        /// <param name="setType">Type of the set.</param>
        /// <param name="propValueDelegate">The property value delegate.</param>
        /// <param name="types">The types.</param>
        public void SetupSetMethod<TContext>(Mock<TContext> mockDbContext, Type setType, Delegate propValueDelegate, Type[]? types = null)
            where TContext : DbContext
        {
            types ??= new Type[] { };

            // Create an expression that represents x => x.Set<setType>(It.IsAny<string>())
            var parameter = Expression.Parameter(typeof(TContext), "x");
            var method = typeof(TContext).GetMethod("Set", types);
            var genericMethod = method.MakeGenericMethod(setType);
            var args = new List<Expression>();
            types.ForEach(x => args.Add(Expression.Call(typeof(It), "IsAny", new[] {x})));
            var body = Expression.Call(parameter, genericMethod, args.ToArray());
            var expression = Expression.Lambda<Func<TContext, object>>(body, parameter);

            // Use the expression to setup mockDbContext
            var setup = new FastMoqNonVoidSetupPhrase<TContext>(mockDbContext.Setup(expression));
            setup.Returns(propValueDelegate, setType);
        }
    }
}
