using FastMoq.Extensions;
using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class ConstructorHistory.
    ///     Implements the <see cref="System.Collections.Generic.IReadOnlyDictionary{Type, IReadonlyList}" />
    /// </summary>
    /// <inheritdoc cref="ILookup{TKey,TElement}" />
    /// <inheritdoc cref="IReadOnlyList{T}" />
    /// <seealso cref="System.Collections.Generic.IReadOnlyDictionary{Type, IReadonlyList}" />
    public class ConstructorHistory :
        ILookup<Type, IHistoryModel>,
        IReadOnlyDictionary<Type, ReadOnlyCollection<IHistoryModel>>

    {
        #region Fields

        /// <summary>
        ///     The constructor history
        /// </summary>
        private readonly Dictionary<Type, List<IHistoryModel>> constructorHistory = new();

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the <see cref="System.Collections.Generic.KeyValuePair{Type, IReadOnlyList{IHistoryModel}}" /> at the
        ///     specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>
        ///     System.Collections.Generic.KeyValuePair&lt;System.Type, System.Collections.Generic.IReadOnlyList&lt;
        ///     FastMoq.Models.IHistoryModel&gt;&gt;.
        /// </returns>
        public KeyValuePair<Type, IReadOnlyList<IHistoryModel>> this[int index]
        {
            get
            {
                var item = constructorHistory.Skip(index - 1).FirstOrDefault();
                return new KeyValuePair<Type, IReadOnlyList<IHistoryModel>>(item.Key, item.Value);
            }
        }

        /// <summary>
        ///     Gets the values.
        /// </summary>
        /// <value>The values.</value>
        public IEnumerable<IReadOnlyList<IHistoryModel>> Values => constructorHistory.Values;

        /// <summary>
        ///     Gets the read only history.
        /// </summary>
        /// <value>The read only history.</value>
        private ILookup<Type, IHistoryModel> ReadOnlyHistory =>
            constructorHistory
                .SelectMany(pair => pair.Value.Select(value => new { pair.Key, Value = value }))
                .ToLookup(item => item.Key, item => item.Value);

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConstructorHistory" /> class.
        /// </summary>
        public ConstructorHistory() { }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:FastMoq.Models.ConstructorHistory" /> class.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        public ConstructorHistory(IDictionary<Type, List<IHistoryModel>> dictionary) : this() =>
            dictionary.ForEach(pair => constructorHistory.Add(pair.Key, pair.Value));

        /// <summary>
        ///     Gets the constructor.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System ofReflection ofConstructorInfo? of the constructor.</returns>
        public ConstructorInfo? GetConstructor(Type type) => ReadOnlyHistory[type]
            .OfType<ConstructorModel>()
            .Select(x => x.ConstructorInfo)
            .LastOrDefault();

        /// <summary>
        ///     Converts to list.
        /// </summary>
        /// <returns>To the list.</returns>
        public IReadOnlyCollection<KeyValuePair<Type, ReadOnlyCollection<IHistoryModel>>> ToList() => this;

        /// <summary>
        ///     Adds or Updates History
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="model">The model.</param>
        /// <returns>Adds the or update.</returns>
        internal bool AddOrUpdate(Type key, IHistoryModel model)
        {
            if (!constructorHistory.ContainsKey(key))
            {
                // Add first instance model to key value's list.
                constructorHistory.Add(key, [model]);
                return true;
            }

            // Add instance model to key value's list.
            if (!constructorHistory[key].Contains(model))
            {
                constructorHistory[key].Add(model);
                return true;
            }

            return false;
        }

        #region IEnumerable

        /// <inheritdoc />
        public IEnumerator GetEnumerator() => new ReadOnlyDictionary<Type, ReadOnlyCollection<IHistoryModel>>(
            constructorHistory.ToDictionary(pair => pair.Key, pair => pair.Value.AsReadOnly())
        ).AsEnumerable().GetEnumerator();

        #endregion

        #region IEnumerable<IGrouping<Type,IHistoryModel>>

        /// <inheritdoc />
        IEnumerator<IGrouping<Type, IHistoryModel>> IEnumerable<IGrouping<Type, IHistoryModel>>.GetEnumerator() => ReadOnlyHistory.GetEnumerator();

        #endregion

        #region IEnumerable<KeyValuePair<Type,ReadOnlyCollection<IHistoryModel>>>

        /// <inheritdoc />
        IEnumerator<KeyValuePair<Type, ReadOnlyCollection<IHistoryModel>>> IEnumerable<KeyValuePair<Type, ReadOnlyCollection<IHistoryModel>>>.
            GetEnumerator()
        {
            var iterator = GetEnumerator();

            while (iterator.MoveNext())
            {
                yield return (KeyValuePair<Type, ReadOnlyCollection<IHistoryModel>>) iterator.Current;
            }

            (iterator as IDisposable)?.Dispose();
        }

        #endregion

        #region ILookup<Type,IHistoryModel>

        /// <inheritdoc />
        public bool Contains(Type key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return ReadOnlyHistory.Select(x => x.Key.Name).Contains(key.Name);
        }

        /// <summary>
        ///     Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public int Count => ReadOnlyHistory.Count;

        /// <inheritdoc />
        public IEnumerable<IHistoryModel> this[Type key] => ReadOnlyHistory[key].ToList();

        #endregion

        #region IReadOnlyDictionary<Type,ReadOnlyCollection<IHistoryModel>>

        /// <summary>
        ///     Determines whether the specified key contains key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Containses the key.</returns>
        public bool ContainsKey(Type key) => constructorHistory.ContainsKey(key);

        /// <inheritdoc />
        ReadOnlyCollection<IHistoryModel> IReadOnlyDictionary<Type, ReadOnlyCollection<IHistoryModel>>.this[Type key] =>
            new(ReadOnlyHistory[key].ToList());

        /// <summary>
        ///     Gets the keys.
        /// </summary>
        /// <value>The keys.</value>
        public IEnumerable<Type> Keys => constructorHistory.Keys;

        /// <inheritdoc />
        public bool TryGetValue(Type key, out ReadOnlyCollection<IHistoryModel> value)
        {
            var result = constructorHistory.TryGetValue(key, out var value2);
            value = value2?.AsReadOnly();
            return result;
        }

        /// <inheritdoc />
        IEnumerable<ReadOnlyCollection<IHistoryModel>> IReadOnlyDictionary<Type, ReadOnlyCollection<IHistoryModel>>.Values =>
            constructorHistory.Values.Select(x => x.AsReadOnly());

        #endregion

        //#region IEnumerable<IGrouping<Type,ReadOnlyCollection<IHistoryModel>>>

        ///// <inheritdoc />
        //IEnumerator<IGrouping<Type, ReadOnlyCollection<IHistoryModel>>> IEnumerable<IGrouping<Type, ReadOnlyCollection<IHistoryModel>>>.
        //    GetEnumerator() => ReadOnlyHistory.GetEnumerator();

        //#endregion

        ///// <inheritdoc />
        //IEnumerator<KeyValuePair<Type, List<IHistoryModel>>> IEnumerable<KeyValuePair<Type, List<IHistoryModel>>>.GetEnumerator() =>
        //    constructorHistory.GetEnumerator();

        /// <inheritdoc />
        //IEnumerable<List<IHistoryModel>> IReadOnlyDictionary<Type, List<IHistoryModel>>.Values => constructorHistory.Values;
    }
}
