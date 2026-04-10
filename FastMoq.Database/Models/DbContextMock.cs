using FastMoq.Extensions;
using System.Linq.Expressions;
using System.Reflection;

namespace FastMoq.Models
{
    /// <summary>
    ///     Wrapper for Mock.
    ///     Implements the <see cref="Mock{TEntity}" />
    /// </summary>
    /// <typeparam name="TEntity">The type of the t entity.</typeparam>
    /// <seealso cref="Mock{TEntity}" />
    public class DbContextMock<TEntity> : Mock<TEntity>, IDbContextMock<TEntity> where TEntity : DbContext
    {
        /// <inheritdoc />
        public override bool CallBase { get; set; } = true;

        internal static IEnumerable<PropertyInfo> DbSets => typeof(TEntity).GetProperties()
            .Where(x => (x.GetGetMethod()?.IsVirtual ?? false) &&
                        x.CanRead &&
                        x.PropertyType.IsGenericType &&
                        x.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

        /// <inheritdoc />
        public DbContextMock() : this(MockBehavior.Default) { }

        /// <inheritdoc />
        public DbContextMock(MockBehavior behavior) : this(behavior, []) { }

        /// <inheritdoc />
        public DbContextMock(params object[] args) : this(MockBehavior.Default, args) { }

        /// <inheritdoc />
        public DbContextMock(Expression<Func<TEntity>> newExpression, MockBehavior behavior = MockBehavior.Default) :
            base(newExpression, behavior)
        { }

        /// <inheritdoc />
        public DbContextMock(MockBehavior behavior, params object[] args) : base(behavior, args) { }

        private static object? GetValue(Type x, Mocker mocks)
        {
            var genericType = typeof(DbSetMock<>).MakeGenericType(x.GenericTypeArguments[0]);
            var value = Activator.CreateInstance(genericType) as Mock ?? throw new InvalidOperationException("Cannot create Mock.");
            var addMockMethod = typeof(Mocker).GetMethods()
                .Single(method =>
                    method.Name == nameof(Mocker.AddMock) &&
                    method.IsGenericMethodDefinition &&
                    method.GetGenericArguments().Length == 1 &&
                    method.GetParameters().Length == 3);

            addMockMethod.MakeGenericMethod(x).Invoke(mocks, [value, true, x.IsNotPublic]);
            var obj = value.Object;

            var dbSetMock = (IDbSetMock) value;
            dbSetMock.SetupMockMethods();

            return obj;
        }

        /// <inheritdoc />
        public void SetupDbContextSetMethods(PropertyInfo propertyInfo)
        {
            ArgumentNullException.ThrowIfNull(propertyInfo);

            var setType = propertyInfo.PropertyType;
            var funcType = typeof(Func<>).MakeGenericType(setType);

            var propValueDelegate = Delegate.CreateDelegate(
                funcType,
                Object,
                propertyInfo.GetGetMethod() ?? throw new MissingMethodException("Unable to get Set method."));

            if (setType.IsGenericType && setType.GetGenericTypeDefinition() == typeof(DbSet<>))
            {
                setType = setType.GetGenericArguments()[0];
            }

            SetupSetMethod(setType, propValueDelegate);
            SetupSetMethod(setType, propValueDelegate, [typeof(string)], [propertyInfo.Name]);
        }

        /// <inheritdoc />
        public virtual void SetupDbSetProperties(PropertyInfo propertyInfo, object value)
        {
            SetupDbSetPropertyGet(propertyInfo, value);
            SetupDbContextSetMethods(propertyInfo);
        }

        /// <inheritdoc />
        public void SetupDbSetPropertyGet(PropertyInfo propertyInfo, object value)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var body = Expression.Property(parameter, propertyInfo);
            var lambda = Expression.Lambda<Func<TEntity, object>>(body, parameter);
            Setup(lambda)?.Returns(value);
        }

        /// <inheritdoc />
        public void SetupSetMethod(Type setType, Delegate propValueDelegate, Type[]? types = null, object?[]? parameters = null)
        {
            types ??= [];
            parameters ??= [];

            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var method = typeof(TEntity).GetMethod("Set", types) ?? throw new MissingMethodException("Unable to get Set method.");
            var genericMethod = method.MakeGenericMethod(setType);
            var args = new List<Expression>();
            foreach (var type in types)
            {
                args.Add(Expression.Call(typeof(It), "IsAny", [type]));
            }

            parameters.RaiseIfNull();
            types.RaiseIfNull();

            for (var i = 0; i < parameters.Length && i < types.Length; i++)
            {
                args[i] = Expression.Constant(parameters[i]);
            }

            var body = Expression.Call(parameter, genericMethod, args.ToArray());
            var expression = Expression.Lambda<Func<TEntity, object>>(body, parameter);

            var setup = new FastMoqNonVoidSetupPhrase<TEntity>(Setup(expression) ?? throw new InvalidOperationException("Unable to Get Setup."));
            setup.Returns(propValueDelegate, setType);
        }

        /// <inheritdoc />
        public DbContextMock<TEntity> SetupDbSets(Mocker mocks)
        {
            foreach (var dbSet in DbSets)
            {
                var value = GetValue(dbSet.PropertyType, mocks) ?? throw new InvalidOperationException($"Unable to create Dbset for {dbSet.Name}");
                SetupDbSetProperties(dbSet, value);
            }

            return this;
        }
    }
}