using FastMoq.Extensions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq.Models
{
    /// <summary>
    ///     Wrapper for Mock.
    ///     Implements the <see cref="Mock{TEntity}" />
    /// </summary>
    /// <typeparam name="TEntity">The type of the t entity.</typeparam>
    /// <inheritdoc />
    /// <seealso cref="Mock{TEntity}" />
    public class DbContextMock<TEntity> : Mock<TEntity> where TEntity : DbContext
    {
        #region Properties

        /// <inheritdoc />
        public override bool CallBase { get; set; } = true;

        internal static IEnumerable<PropertyInfo> DbSets => typeof(TEntity).GetProperties()
            .Where(x => (x.GetGetMethod()?.IsVirtual ?? false) &&
                        x.CanRead &&
                        x.PropertyType.IsGenericType &&
                        x.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)
            );

        #endregion

        /// <inheritdoc />
        public DbContextMock() : this(MockBehavior.Default) { }

        /// <inheritdoc />
        public DbContextMock(MockBehavior behavior) : this(behavior, Array.Empty<object>()) { }

        /// <inheritdoc />
        public DbContextMock(params object[] args) : this(MockBehavior.Default, args) { }

        /// <inheritdoc />
        public DbContextMock(Expression<Func<TEntity>> newExpression, MockBehavior behavior = MockBehavior.Default) :
            base(newExpression, behavior) { }

        /// <inheritdoc />
        public DbContextMock(MockBehavior behavior, params object[] args) : base(behavior, args) { }

        /// <summary>
        ///     Setups the mock.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <exception cref="System.MissingMethodException">Unable to get Set method.</exception>
        /// <exception cref="InvalidOperationException">Unable to get Set method.</exception>
        public void SetupDbContextSetMethods(PropertyInfo propertyInfo)
        {
            var setType = propertyInfo.PropertyType;

            // Create a Func<T> at runtime
            var funcType = typeof(Func<>).MakeGenericType(setType);

            var propValueDelegate = Delegate.CreateDelegate(funcType,
                Object,
                propertyInfo.GetGetMethod() ?? throw new MissingMethodException("Unable to get Set method.")
            );

            // if the PropertyType is a DbSet, get the entity out of the DbSet.
            if (setType.IsGenericType && setType.GetGenericTypeDefinition() == typeof(DbSet<>))
            {
                setType = setType.GetGenericArguments()[0];
            }

            SetupSetMethod(setType, propValueDelegate);
            SetupSetMethod(setType, propValueDelegate, new[] { typeof(string) }, new object?[] { propertyInfo.Name });
        }

        /// <summary>
        ///     Setups the database set properties.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="value">The value.</param>
        public virtual void SetupDbSetProperties(PropertyInfo propertyInfo, object value)
        {
            SetupDbSetPropertyGet(propertyInfo, value);
            SetupDbContextSetMethods(propertyInfo);
        }

        /// <summary>
        ///     Setups the get.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="value">The value.</param>
        public void SetupDbSetPropertyGet(PropertyInfo propertyInfo, object value)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var body = Expression.Property(parameter, propertyInfo);
            var lambda = Expression.Lambda<Func<TEntity, object>>(body, parameter);
            Setup(lambda)?.Returns(value);
        }

        /// <summary>
        ///     Setup the database sets that are marked as virtual.
        /// </summary>
        /// <param name="mocks">The <see cref="Mocker" /> from test context.</param>
        /// <returns>DbContext Mock.</returns>
        public DbContextMock<TEntity> SetupDbSets(Mocker mocks)
        {
            // Go through the DbSets and attempt to map each property and the set methods to their properties.
            DbSets.ForEach(x =>
                {
                    var value = GetValue(x.PropertyType, mocks) ?? throw new InvalidOperationException($"Unable to create Dbset for {x.Name}");
                    SetupDbSetProperties(x, value);
                }
            );

            return this;
        }

        /// <summary>
        ///     Setup the set method.
        /// </summary>
        /// <param name="setType">Type of the set.</param>
        /// <param name="propValueDelegate">The property value delegate.</param>
        /// <param name="types">The types.</param>
        /// <param name="parameters">The parameters.</param>
        /// <exception cref="System.MissingMethodException">Unable to get Set method.</exception>
        /// <exception cref="System.InvalidOperationException">Unable to Get Setup.</exception>
        public void SetupSetMethod(Type setType, Delegate propValueDelegate, Type[]? types = null, object?[]? parameters = null)
        {
            types ??= Array.Empty<Type>();

            // Create an expression that represents x => x.Set<setType>(It.IsAny<string>())
            var parameter = Expression.Parameter(typeof(TEntity), "x");

            // Get Set method for given paramer type. Either the one without parameters or the string parameter.
            var method = typeof(TEntity).GetMethod("Set", types) ?? throw new MissingMethodException("Unable to get Set method.");
            var genericMethod = method.MakeGenericMethod(setType);
            var args = new List<Expression>();
            types.ForEach(x => args.Add(Expression.Call(typeof(It), "IsAny", new[] { x })));

            // Setup parameters
            parameters ??= Array.Empty<object?>();

            for (var i = 0; i < parameters.Length && i < types.Length; i++)
            {
                // Replace IsAny with specified parameter.
                args[i] = Expression.Constant(parameters[i]);
            }

            var body = Expression.Call(parameter, genericMethod, args.ToArray());
            var expression = Expression.Lambda<Func<TEntity, object>>(body, parameter);

            // Use the expression to setup mockDbContext
            var setup = new FastMoqNonVoidSetupPhrase<TEntity>(Setup(expression) ?? throw new InvalidOperationException("Unable to Get Setup."));
            setup.Returns(propValueDelegate, setType);
        }

        private static object? GetValue(Type x, Mocker mocks)
        {
            var genericType = typeof(DbSetMock<>).MakeGenericType(x.GenericTypeArguments.First());
            var value = Activator.CreateInstance(genericType) as Mock ?? throw new InvalidOperationException("Cannot create Mock.");
            mocks.AddMock(value, genericType, true, x.IsNotPublic);
            var obj = value.Object;

            dynamic dbSetMock = value;

            // Setup Async List methods.
            dbSetMock.SetupAsyncListMethods();

            // Setup List methods.
            dbSetMock.SetupListMethods();

            return obj;
        }
    }
}
