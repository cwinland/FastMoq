using System.Collections.Specialized;
using System.ComponentModel;

namespace FastMoq.Collections
{
    /// <inheritdoc />
    public class MockerObservableCollectionChangedEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        ///     Gets the notify collection changed event arguments.
        /// </summary>
        /// <value>The notify collection changed event arguments.</value>
        public NotifyCollectionChangedEventArgs? NotifyCollectionChangedEventArgs { get; }

        /// <summary>
        ///     Gets the property changed event arguments.
        /// </summary>
        /// <value>The property changed event arguments.</value>
        public PropertyChangedEventArgs? PropertyChangedEventArgs { get; }

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockerObservableCollectionChangedEventArgs" /> class.
        /// </summary>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs" /> instance containing the event data.</param>
        public MockerObservableCollectionChangedEventArgs(NotifyCollectionChangedEventArgs? e) => NotifyCollectionChangedEventArgs = e;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockerObservableCollectionChangedEventArgs" /> class.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedEventArgs" /> instance containing the event data.</param>
        public MockerObservableCollectionChangedEventArgs(PropertyChangedEventArgs? e) => PropertyChangedEventArgs = e;
    }
}
