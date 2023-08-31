using FastMoq.Models;
using System.IO.Abstractions;
using System.Reflection;

namespace FastMoq
{
    public partial class Mocker
    {
        /// <summary>
        ///     Creates an instance of <c>T</c>. Parameters allow matching of constructors and using those values in the creation
        ///     of the instance.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="args">The optional arguments used to create the instance.</param>
        /// <returns><see cref="Nullable{T}" />.</returns>
        /// <example>
        ///     <code><![CDATA[
        /// IFileSystem fileSystem = CreateInstance<IFileSystem>();
        /// ]]></code>
        /// </example>
        public T? CreateInstance<T>(params object?[] args) where T : class => CreateInstance<T>(true, args);

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TParam1">The type of the t param1.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>T.</returns>
        public T? CreateInstance<T, TParam1>(Dictionary<Type, object?> data) where T : class => CreateInstanceInternal<T>(
            model => FindConstructorByType(model.InstanceType, true, typeof(TParam1)),
            data
        );

        /// <summary>
        ///     Creates the instance.
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
        ///     Creates the instance.
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
        ///     Creates the instance.
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
        ///     Creates the instance.
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
        ///     Creates the instance.
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
        ///     Creates the instance.
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
        ///     Creates the instance.
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
            CreateInstance<T>(usePredefinedFileSystem, Array.Empty<object>());

        /// <summary>
        ///     Creates the instance.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="usePredefinedFileSystem">if set to <c>true</c> [use predefined file system].</param>
        /// <param name="args">The arguments. These arguments will override the type map, if present.</param>
        /// <returns><see cref="Nullable{T}" />.</returns>
        public T? CreateInstance<T>(bool usePredefinedFileSystem, params object?[] args) where T : class
        {
            if (IsMockFileSystem<T>(usePredefinedFileSystem))
            {
                return fileSystem as T;
            }

            var tType = typeof(T);
            var typeInstanceModel = GetTypeModel<T>();

            if (typeInstanceModel.CreateFunc != null && !creatingTypeList.Contains(tType))
            {
                creatingTypeList.Add(tType);
                T obj;

                try
                {
                    AddToConstructorHistory(tType, typeInstanceModel);
                    obj = (T) typeInstanceModel.CreateFunc.Invoke(this);
                }
                finally
                {
                    creatingTypeList.Remove(tType);
                }

                return obj;
            }

            // Ensure arguments are valid or empty.
            args ??= Array.Empty<object>();

            // if the arguments are not present, but the type map has arguments, use those instead.
            if (args.Length == 0 && typeInstanceModel.Arguments.Count > 0)
            {
                args = new object?[typeInstanceModel.Arguments.Count];
                typeInstanceModel.Arguments.CopyTo(args);
            }

            var constructor =
                args.Length > 0
                    ? FindConstructor(typeInstanceModel.InstanceType, false, args)
                    : FindConstructor(false, typeInstanceModel.InstanceType, false);

            return CreateInstanceInternal<T>(constructor);
        }

        /// <summary>
        ///     Creates an instance of <c>T</c>.
        ///     Non public constructors are included as options for creating the instance.
        ///     Parameters allow matching of constructors and using those values in the creation of the instance.
        /// </summary>
        /// <typeparam name="T">The Mock <see cref="T:Type" />, usually an interface.</typeparam>
        /// <param name="args">The arguments.</param>
        /// <returns>
        ///     <see cref="Nullable{T}" />
        /// </returns>
        /// <example>
        ///     <code><![CDATA[
        /// IModel model = CreateInstanceNonPublic<IModel>();
        /// ]]></code>
        /// </example>
        public T? CreateInstanceNonPublic<T>(params object?[] args) where T : class
        {
            var type = typeof(T).IsInterface ? GetTypeFromInterface<T>() : new InstanceModel<T>();

            return type.CreateFunc != null
                ? (T) type.CreateFunc.Invoke(this)
                : CreateInstanceNonPublic(type.InstanceType, args) as T;
        }

        /// <summary>
        ///     Creates the instance non public.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>System.Nullable&lt;System.Object&gt;.</returns>
        public object? CreateInstanceNonPublic(Type type, params object?[] args)
        {
            var constructor =
                args.Length > 0
                    ? FindConstructor(type, true, args)
                    : FindConstructor(false, type, true);

            return constructor.ConstructorInfo?.Invoke(constructor.ParameterList);
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

            data ??= new Dictionary<Type, object?>();
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