using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FastMoq.Models
{
    public class ObservableLogEntries : IReadOnlyCollection<LogEntry>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly ObservableCollection<LogEntry> internalCollection = [];
        private readonly ReadOnlyObservableCollection<LogEntry> readOnlyCollection;

        public ObservableLogEntries()
        {
            readOnlyCollection = new ReadOnlyObservableCollection<LogEntry>(internalCollection);
        }

        internal void Add(LogEntry item)
        {
            internalCollection.Add(item);
        }

        public IReadOnlyCollection<LogEntry> Items => readOnlyCollection;

        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add => ((INotifyCollectionChanged) readOnlyCollection).CollectionChanged += value;
            remove => ((INotifyCollectionChanged) readOnlyCollection).CollectionChanged -= value;
        }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => ((INotifyPropertyChanged) readOnlyCollection).PropertyChanged += value;
            remove => ((INotifyPropertyChanged) readOnlyCollection).PropertyChanged -= value;
        }

        public IEnumerator<LogEntry> GetEnumerator() => readOnlyCollection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) readOnlyCollection).GetEnumerator();

        public int Count => readOnlyCollection.Count;
    }
}