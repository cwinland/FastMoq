using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FastMoq.Models
{
    /// <summary>
    /// Exposes a read-only, observable view of exception messages captured by a <see cref="Mocker"/> during resolution and invocation.
    /// </summary>
    public class ObservableExceptionLog : IReadOnlyCollection<string>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly ObservableCollection<string> internalCollection = [];
        private readonly ReadOnlyObservableCollection<string> readOnlyCollection;

        /// <summary>
        /// Initializes an empty observable exception log.
        /// </summary>
        public ObservableExceptionLog()
        {
            readOnlyCollection = new ReadOnlyObservableCollection<string>(internalCollection);
        }

        // Internal method to add an item
        internal void Add(string item)
        {
            internalCollection.Add(item);
        }

        /// <summary>
        /// Gets the captured exception messages as a read-only observable collection.
        /// </summary>
        public IReadOnlyCollection<string> Items => readOnlyCollection;

        /// <inheritdoc />
        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add => ((INotifyCollectionChanged) readOnlyCollection).CollectionChanged += value;
            remove => ((INotifyCollectionChanged) readOnlyCollection).CollectionChanged -= value;
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => ((INotifyPropertyChanged) readOnlyCollection).PropertyChanged += value;
            remove => ((INotifyPropertyChanged) readOnlyCollection).PropertyChanged -= value;
        }

        /// <inheritdoc />
        public IEnumerator<string> GetEnumerator() => readOnlyCollection.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) readOnlyCollection).GetEnumerator();

        /// <inheritdoc />
        public int Count => readOnlyCollection.Count;
    }
}