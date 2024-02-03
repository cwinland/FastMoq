using FastMoq.Extensions;
using FastMoq.Models;
using Moq;
using System.IO.Abstractions;
using System.Reflection;

namespace FastMoq
{
    /// <summary>
    ///     Class Mocker.
    /// </summary>
    public partial class Mocker
    {
        /// <summary>
        ///     Creates an instance of <c>T</c>. Parameter data allows matching of constructors by type and uses those values in the creation of the instance.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="args">The optional arguments used to create the instance.</param>
        /// <returns><see cref="Nullable{T}" />.</returns>
        /// <example>
        ///   <code><![CDATA[
        /// IFileSystem fileSystem = CreateInstance<IFileSystem>();
        /// ]]></code>
        /// </example>
        public T? CreateInstance<T>(params object?[] args) where T : class => CreateInstance<T>(true, args);

        /// <summary>
        ///     Creates an instance of <c>T</c>. Parameter data allows matching of constructors by type and uses those values in the creation of the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1>(Dictionary<Type, object?> data) where T : class => CreateInstanceInternal<T>(
            model => FindConstructorByType(model.InstanceType, true, typeof(TParam1)), data
        );

        /// <summary>
        ///     Creates an instance of <c>T</c>. Parameter data allows matching of constructors by type and uses those values in the creation of the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <typeparam name="TParam2">The type of the t param2.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1, TParam2>(Dictionary<Type, object?> data) where T : class => CreateInstanceInternal<T>(
            model => FindConstructorByType(model.InstanceType, true, typeof(TParam1), typeof(TParam2)),
            data
        );

        /// <summary>
        ///     Creates an instance of <c>T</c>. Parameter data allows matching of constructors by type and uses those values in the creation of the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <typeparam name="TParam2">The type of the t param2.</typeparam>
        /// <typeparam name="TParam3">The type of the t param3.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1, TParam2, TParam3>(Dictionary<Type, object?> data) where T : class =>
            CreateInstanceInternal<T>(
                model => FindConstructorByType(model.InstanceType, true, typeof(TParam1), typeof(TParam2), typeof(TParam3)),
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
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1, TParam2, TParam3, TParam4>(Dictionary<Type, object?> data) where T : class =>
            CreateInstanceInternal<T>(
                model => FindConstructorByType(model.InstanceType,
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
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1, TParam2, TParam3, TParam4, TParam5>(Dictionary<Type, object?> data) where T : class =>
            CreateInstanceInternal<T>(
                model => FindConstructorByType(model.InstanceType,
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
        ///     Creates an instance of <c>T</c>. Parameter data allows matching of constructors by type and uses those values in the creation of the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <typeparam name="TParam2">The type of the t param2.</typeparam>
        /// <typeparam name="TParam3">The type of the t param3.</typeparam>
        /// <typeparam name="TParam4">The type of the t param4.</typeparam>
        /// <typeparam name="TParam5">The type of the t param5.</typeparam>
        /// <typeparam name="TParam6">The type of the t param6.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6>(Dictionary<Type, object?> data)
            where T : class => CreateInstanceInternal<T>(
            model => FindConstructorByType(model.InstanceType,
                true,
                typeof(TParam1),
                typeof(TParam2),
                typeof(TParam3),
                typeof(TParam4),
                typeof(TParam5),
                typeof(TParam6)
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
        /// <typeparam name="TParam6">The type of the t param6.</typeparam>
        /// <typeparam name="TParam7">The type of the t param7.</typeparam>
        /// <param name="data">The arguments.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7>(Dictionary<Type, object?> data)
            where T : class => CreateInstanceInternal<T>(
            model => FindConstructorByType(model.InstanceType,
                true,
                typeof(TParam1),
                typeof(TParam2),
                typeof(TParam3),
                typeof(TParam4),
                typeof(TParam5),
                typeof(TParam6),
                typeof(TParam7)
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
        /// <typeparam name="TParam6">The type of the t param6.</typeparam>
        /// <typeparam name="TParam7">The type of the t param7.</typeparam>
        /// <typeparam name="TParam8">The type of the t param8.</typeparam>
        /// <param name="data">The arguments.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8>(
            Dictionary<Type, object?> data) where T : class => CreateInstanceInternal<T>(
            model => FindConstructorByType(model.InstanceType,
                true,
                typeof(TParam1),
                typeof(TParam2),
                typeof(TParam3),
                typeof(TParam4),
                typeof(TParam5),
                typeof(TParam6),
                typeof(TParam7),
                typeof(TParam8)
            ),
            data
        );

        /// <summary>
        ///     Creates an instance of <see cref="IFileSystem" />.
        /// </summary>
        /// <typeparam name="T"><see cref="IFileSystem" />.</typeparam>
        /// <param name="usePredefinedFileSystem">if set to <c>true</c> [use predefined file system].</param>
        /// <returns><see cref="Nullable{IFileSystem}" />.</returns>
        public IFileSystem? CreateInstance<T>(bool usePredefinedFileSystem) where T : class, IFileSystem =>
            CreateInstance<T>(usePredefinedFileSystem, Array.Empty<object?>());

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="usePredefinedFileSystem">if set to <c>true</c> [use predefined file system].</param>
        /// <param name="args">The arguments.</param>
        /// <returns><see cref="Nullable{T}" />.</returns>
        public T? CreateInstance<T>(bool usePredefinedFileSystem, params object?[] args) where T : class
        {
            if (IsMockFileSystem<T>(usePredefinedFileSystem))
            {
                return fileSystem as T;
            }

            var tType = typeof(T);
            var typeInstanceModel = GetTypeModel<T>();

            if (!creatingTypeList.Contains(tType))
            {
                if (typeInstanceModel.CreateFunc != null)
                {
                    creatingTypeList.Add(tType);
                    T obj;

                    try
                    {
                        AddToConstructorHistory(tType, typeInstanceModel);
                        obj = (T) typeInstanceModel.CreateFunc(this);
                    }
                    finally
                    {
                        creatingTypeList.Remove(tType);
                    }

                    return obj;
                }

                // Special handling for DbContext types.
                if (tType.IsAssignableTo(typeof(Microsoft.EntityFrameworkCore.DbContext)))
                {
                    var mockObj = GetMockDbContext(tType);
                    return (T?) mockObj.Object;
                }
            }

            args.RaiseIfNull();

            if (typeInstanceModel.Arguments.Count > 0 && args.Length == 0)
            {
                args = typeInstanceModel.Arguments.ToArray();
            }

            var instanceType = typeInstanceModel.InstanceType;

            var constructor =
                args.Length > 0
                    ? FindConstructor(instanceType, false, args)
                    : FindConstructor(false, instanceType, false);

            return CreateInstanceInternal<T>(constructor);
        }

        /// <summary>
        ///     Creates the instance of the given type.
        /// Public and non-public constructors are searched.
        /// Parameters allow matching of constructors and using those values in the creation of the instance.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="args">The arguments.</param>
        /// <returns><see cref="Nullable{T}" /></returns>
        /// <example>
        ///   <code><![CDATA[
        /// IModel model = CreateInstanceNonPublic<IModel>();
        /// ]]></code>
        /// </example>
        public T? CreateInstanceNonPublic<T>(params object?[] args) where T : class
        {
            var type = typeof(T).IsInterface ? GetTypeFromInterface<T>() : new InstanceModel<T>();

            return type.CreateFunc != null
                ? (T) type.CreateFunc(this)
                : CreateInstanceNonPublic(type.InstanceType, args) as T;
        }

        /// <summary>
        ///     Creates the instance of the given type.
        /// Public and non-public constructors are searched.
        /// Parameters allow matching of constructors and using those values in the creation of the instance.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>System.Nullable&lt;System.Object&gt;.</returns>
        public object? CreateInstanceNonPublic(Type type, params object?[] args)
        {
            var constructor =
                args?.Length > 0
                    ? FindConstructor(type, true, args)
                    : FindConstructor(false, type, true);

            return constructor.ConstructorInfo?.Invoke(constructor.ParameterList);
        }

        /// <summary>
        ///     Creates the <see cref="MockModel" /> from the <c>Type</c>. This throws an exception if the mock already exists.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic"><c>true</c> if non-public and public constructors are used.</param>
        /// <param name="args">The arguments used to match to the constructor.</param>
        /// <returns><see cref="List{Mock}" />.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.ApplicationException">Cannot create instance.</exception>
        public List<MockModel> CreateMock(Type type, bool nonPublic = false, params object?[] args)
        {
            type = CleanType(type);

            if (Contains(type))
            {
                type.ThrowAlreadyExists();
            }

            var oMock = CreateMockInstance(type, nonPublic, args);

            AddMock(oMock, type);
            return mockCollection;
        }

        /// <summary>
        ///     Creates the <see cref="MockModel" /> from the type <c>T</c>. This throws an exception if the mock already exists.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="nonPublic">if set to <c>true</c> public and non-public constructors are used.</param>
        /// <param name="args">The arguments used to find the correct constructor for a class.</param>
        /// <returns><see cref="List{T}" />.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.ArgumentException">type already exists. - type</exception>
        /// <exception cref="System.ApplicationException">Cannot create instance.</exception>
        public List<MockModel> CreateMock<T>(bool nonPublic = false, params object?[] args) where T : class => CreateMock(typeof(T), nonPublic, args);

        /// <summary>
        ///     Creates the mock instance that is not automatically injected.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nonPublic">if set to <c>true</c> can use non-public constructor.</param>
        /// <param name="args">The arguments used to find the correct constructor for a class.</param>
        /// <returns>Mock.</returns>
        /// <exception cref="System.ArgumentException">type must be a class. - type</exception>
        /// <exception cref="System.ApplicationException">Cannot create instance.</exception>
        public Mock<T> CreateMockInstance<T>(bool nonPublic = false, params object?[] args) where T : class =>
            (Mock<T>) CreateMockInstance(typeof(T), nonPublic, args);

        /// <summary>
        ///     Creates an instance of the mock.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="nonPublic">if set to <c>true</c> can use non-public constructor.</param>
        /// <param name="args">The arguments used to find the correct constructor for a class.</param>
        /// <returns>Mock.</returns>
        /// <exception cref="ArgumentException">type must be a class or interface., nameof(type)</exception>
        /// <exception cref="ApplicationException">type must be a class or interface., nameof(type)</exception>
        /// <exception cref="System.ArgumentException">type must be a class or interface., nameof(type)</exception>
        /// <exception cref="System.ApplicationException">type must be a class or interface., nameof(type)</exception>
        public Mock CreateMockInstance(Type type, bool nonPublic = false, params object?[] args)
        {
            if (type == null || (!type.IsClass && !type.IsInterface))
            {
                throw new ArgumentException("type must be a class or interface.", nameof(type));
            }

            var constructor = GetTypeConstructor(type, nonPublic, args);

            var oMock = CreateMockInternal(type, constructor, nonPublic);

            if (!Strict)
            {
                InvokeMethod<Mock>(null, "SetupAllProperties", true, oMock);

                if (InnerMockResolution)
                {
                    AddProperties(type, GetSafeMockObject(oMock));
                }
            }

            AddInjections(GetSafeMockObject(oMock), GetTypeModel(type)?.InstanceType ?? type);

            return oMock;
        }

        private object GetSafeMockObject(Mock mock)
        {
            try
            {
                return mock.Object;
            }
            catch (TargetInvocationException ex)
            {
                exceptionLog.Add(ex.Message);
                ex.ThrowIfCastleMethodAccessException();
                throw;
            }
            catch (MethodAccessException ex)
            {
                exceptionLog.Add(ex.Message);
                ex.ThrowIfCastleMethodAccessException();
                throw;
            }
            catch (Exception ex)
            {
                exceptionLog.Add(ex.Message);
                throw;
            }
        }

        private ConstructorModel GetTypeConstructor(Type type, bool nonPublic, object?[] args)
        {
            var constructor = new ConstructorModel(null, args.ToList());

            try
            {
                if (!type.IsInterface)
                {
                    // Find the best constructor and build the parameters.
                    constructor = args.Length > 0 || nonPublic ? FindConstructor(type, true, args) : FindConstructor(true, type, nonPublic);
                }
            }
            catch (Exception ex)
            {
                exceptionLog.Add(ex.Message);
            }

            if (constructor.ConstructorInfo == null && !HasParameterlessConstructor(type))
            {
                try
                {
                    constructor = (nonPublic ? GetConstructorsNonPublic(type) : GetConstructors(type)).MinBy(x => x.ConstructorInfo?.GetParameters().Length ?? 0) ?? constructor;
                }
                catch (Exception ex)
                {
                    // It's okay if this fails.
                    exceptionLog.Add(ex.Message);
                }
            }

            return constructor;
        }

        /// <summary>
        ///     Create an instance using the constructor by the function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="constructorFunc">The constructor function.</param>
        /// <param name="data">The arguments.</param>
        /// <returns>T.</returns>
        internal T? CreateInstanceInternal<T>(Func<IInstanceModel, ConstructorInfo> constructorFunc, Dictionary<Type, object?>? data) where T : class
        {
            var type = typeof(T).IsInterface ? GetTypeFromInterface<T>() : new InstanceModel<T>();

            if (type.CreateFunc != null)
            {
                return (T) type.CreateFunc.Invoke(this);
            }

            data ??= [];
            var constructor = constructorFunc(type);

            var args = GetArgData(constructor, data);

            return CreateInstanceInternal<T>(constructor, args);
        }

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="constructorModel">The constructor model.</param>
        /// <returns>T.</returns>
        internal T? CreateInstanceInternal<T>(ConstructorModel constructorModel) where T : class =>
            CreateInstanceInternal<T>(constructorModel.ConstructorInfo, constructorModel.ParameterList);

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="info">The information.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>T.</returns>
        internal T? CreateInstanceInternal<T>(ConstructorInfo? info, params object?[] args) where T : class =>
            CreateInstanceInternal(typeof(T), info, args) as T;

        /// <summary>
        ///     Creates the instance internal.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="info">The information.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>object?.</returns>
        internal object? CreateInstanceInternal(Type type, ConstructorInfo? info, params object?[] args)
        {
            AddToConstructorHistory(type, info, args.ToList());
            var paramList = info?.GetParameters().ToList() ?? new();
            var newArgs = args.ToList();

            if (args.Length < paramList.Count)
            {
                for (var i = args.Length; i < paramList.Count; i++)
                {
                    var p = paramList[i];
                    newArgs.Add(p.IsOptional ? null : GetParameter(p.ParameterType));
                }
            }

            var obj = AddInjections(info?.Invoke(newArgs.ToArray()));
            return InnerMockResolution ? AddProperties(type, obj) : obj;
        }

    }
}
