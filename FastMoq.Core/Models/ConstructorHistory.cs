using FastMoq.Extensions;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace FastMoq.Models
{
    /// <summary>
    ///     Class ConstructorHistory.
    /// Implements the <see cref="System.Collections.Generic.IReadOnlyDictionary{Type, IReadonlyList}" />
    /// </summary>
    /// <inheritdoc cref="ILookup{TKey,TElement}" />
    /// <inheritdoc cref="IReadOnlyList{T}" />
    /// <seealso cref="System.Collections.Generic.IReadOnlyDictionary{Type, IReadonlyList}" />
    public class ConstructorHistory :
        ILookup<Type, IHistoryModel>,
        IReadOnlyDictionary<Type, ReadOnlyCollection<IHistoryModel>>,
        IEnumerable<KeyValuePair<Type, IEnumerable<IHistoryModel>>>

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
        /// specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>System.Collections.Generic.KeyValuePair&lt;System.Type, System.Collections.Generic.IReadOnlyList&lt;
        /// FastMoq.Models.IHistoryModel&gt;&gt;.</returns>
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
        ///     Converts to list.
        /// </summary>
        /// <returns><see cref="IEnumerable{KeyValuePair}"/></returns>
        public IEnumerable<KeyValuePair<Type, IEnumerable<IHistoryModel>>> AsEnumerable() => this;

        /// <summary>
        ///     Converts to a Lookup.
        /// </summary>
        /// <returns><see cref="ILookup{Type, IHistoryModel}"/></returns>
        public ILookup<Type, IHistoryModel> AsLookup() => this;

        /// <summary>
        ///     Converts to read only dictionary.
        /// </summary>
        /// <returns><see cref="IReadOnlyDictionary{Type, ReadOnlyCollection}"/></returns>
        public IReadOnlyDictionary<Type, ReadOnlyCollection<IHistoryModel>> AsReadOnlyDictionary() => this;

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
        ///     Adds or Updates History
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="model">The model.</param>
        /// <returns>Adds or updates the model to the construction history.</returns>
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
        [ExcludeFromCodeCoverage]
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<Type, IEnumerable<IHistoryModel>>>) this).GetEnumerator();

        #endregion

        #region IEnumerable<IGrouping<Type,IHistoryModel>>

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        IEnumerator<IGrouping<Type, IHistoryModel>> IEnumerable<IGrouping<Type, IHistoryModel>>.GetEnumerator() => ReadOnlyHistory.GetEnumerator();

        #endregion

        #region IEnumerable<KeyValuePair<Type,IEnumerable<IHistoryModel>>>

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<Type, IEnumerable<IHistoryModel>>> GetEnumerator()
        {
            var source = constructorHistory.Select(kv => new KeyValuePair<Type, IEnumerable<IHistoryModel>>(kv.Key, kv.Value)).ToList();

            var iterator = source.GetEnumerator();

            while (iterator.MoveNext())
            {
                yield return iterator.Current;
            }

            iterator.Dispose();
        }

        #endregion

        #region IEnumerable<KeyValuePair<Type,ReadOnlyCollection<IHistoryModel>>>

        /// <inheritdoc />
        IEnumerator<KeyValuePair<Type, ReadOnlyCollection<IHistoryModel>>> IEnumerable<KeyValuePair<Type, ReadOnlyCollection<IHistoryModel>>>.
            GetEnumerator()
        {
            var source = constructorHistory.Select(kv => new KeyValuePair<Type, ReadOnlyCollection<IHistoryModel>>(kv.Key, kv.Value.AsReadOnly()))
                .ToList();

            var iterator = source.GetEnumerator();

            while (iterator.MoveNext())
            {
                yield return iterator.Current;
            }

            iterator.Dispose();
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
        /// <inheritdoc cref="ILookup{TKey,TElement}" />
        public int Count => ReadOnlyHistory.Count;

        /// <inheritdoc />
        public IEnumerable<IHistoryModel> this[Type key] => ReadOnlyHistory[key].ToList();

        #endregion

        #region IReadOnlyDictionary<Type,ReadOnlyCollection<IHistoryModel>>

        /// <inheritdoc />
        public bool ContainsKey(Type key) => constructorHistory.ContainsKey(key);

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        ReadOnlyCollection<IHistoryModel> IReadOnlyDictionary<Type, ReadOnlyCollection<IHistoryModel>>.this[Type key] =>
            new(ReadOnlyHistory[key].ToList());

        /// <inheritdoc />
        public IEnumerable<Type> Keys => constructorHistory.Keys;

        /// <inheritdoc />
        public bool TryGetValue(Type key, out ReadOnlyCollection<IHistoryModel> value)
        {
            var result = constructorHistory.TryGetValue(key, out var value2);
            value = value2?.AsReadOnly() ?? new List<IHistoryModel>().AsReadOnly();
            return result;
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        IEnumerable<ReadOnlyCollection<IHistoryModel>> IReadOnlyDictionary<Type, ReadOnlyCollection<IHistoryModel>>.Values =>
            constructorHistory.Values.Select(x => x.AsReadOnly());

        #endregion
    }
}
