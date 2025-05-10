using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FastMoq.Collections
{
    /// <exclude />
    public class MockerObservableCollection<T> : ObservableCollection<T>
    {
        #region Fields

        /// <summary>
        ///     Occurs when the collection changes, either by adding or removing an item.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public EventHandler<MockerObservableCollectionChangedEventArgs?>? Changed;

        #endregion

        /// <inheritdoc />
        public MockerObservableCollection()
        {
            base.PropertyChanged += OnChanged;
            base.CollectionChanged += OnChanged;
        }

        /// <exception cref="ArgumentNullException"> collection is a null reference </exception>
        /// <inheritdoc />
        public MockerObservableCollection(IEnumerable<T> collection) : base(
            [..collection ?? throw new ArgumentNullException(nameof(collection))]
        )
        {
            base.PropertyChanged += OnChanged;
            base.CollectionChanged += OnChanged;
        }

        /// <summary>
        ///     Handles the <see cref="E:Changed" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs" /> instance containing the event data.</param>
        protected virtual void OnChanged(object? sender, NotifyCollectionChangedEventArgs? e) =>
            Changed?.Invoke(this, new MockerObservableCollectionChangedEventArgs(e));

        /// <summary>
        ///     Handles the <see cref="E:Changed" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="PropertyChangedEventArgs" /> instance containing the event data.</param>
        protected virtual void OnChanged(object? sender, PropertyChangedEventArgs? e) =>
            Changed?.Invoke(this, new MockerObservableCollectionChangedEventArgs(e));
    }
}
