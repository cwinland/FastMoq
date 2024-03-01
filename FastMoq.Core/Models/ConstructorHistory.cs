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
        IReadOnlyCollection<KeyValuePair<Type, ReadOnlyCollection<IHistoryModel>>>

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

        public IEnumerable<Type> Keys => constructorHistory.Keys;

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

        public bool ContainsKey(Type key) => constructorHistory.ContainsKey(key);

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

        public bool TryGetValue(Type key, out List<IHistoryModel> value) => constructorHistory.TryGetValue(key, out value);

        /// <summary>
        ///     Adds or Updates History
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="model">The model.</param>
        internal void AddOrUpdate(Type key, IHistoryModel model)
        {
            if (!constructorHistory.ContainsKey(key))
            {
                // Add first instance model to key value's list.
                constructorHistory.Add(key, [model]);
            }
            else
            {
                // Add instance model to key value's list.
                constructorHistory[key].Add(model);
            }
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
