using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FastMoq.Models
{
    /// <summary>
    /// Exposes a read-only, observable view of <see cref="LogEntry"/> values captured by FastMoq logger interception.
    /// </summary>
    public class ObservableLogEntries : IReadOnlyCollection<LogEntry>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly object syncRoot = new();
        private readonly ObservableCollection<LogEntry> internalCollection = [];
        private readonly ReadOnlyObservableCollection<LogEntry> readOnlyCollection;

        /// <summary>
        /// Initializes an empty observable log entry collection.
        /// </summary>
        public ObservableLogEntries()
        {
            readOnlyCollection = new ReadOnlyObservableCollection<LogEntry>(internalCollection);
        }

        internal void Add(LogEntry item)
        {
            lock (syncRoot)
            {
                internalCollection.Add(item);
            }
        }

        /// <summary>
        /// Gets the captured log entries as a read-only observable collection.
        /// </summary>
        public IReadOnlyCollection<LogEntry> Items => readOnlyCollection;

        /// <summary>
        /// Occurs when the captured log entry collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add => ((INotifyCollectionChanged) readOnlyCollection).CollectionChanged += value;
            remove => ((INotifyCollectionChanged) readOnlyCollection).CollectionChanged -= value;
        }

        /// <summary>
        /// Occurs when one of the collection properties, such as <see cref="Count"/>, changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => ((INotifyPropertyChanged) readOnlyCollection).PropertyChanged += value;
            remove => ((INotifyPropertyChanged) readOnlyCollection).PropertyChanged -= value;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the captured log entries.
        /// </summary>
        /// <returns>An enumerator over the captured log entries.</returns>
        public IEnumerator<LogEntry> GetEnumerator() => readOnlyCollection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) readOnlyCollection).GetEnumerator();

        /// <summary>
        /// Gets the number of captured log entries.
        /// </summary>
        public int Count => readOnlyCollection.Count;
    }
}