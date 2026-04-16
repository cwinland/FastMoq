namespace FastMoq
{
    /// <summary>
    /// Adds disposal support for helper-owned registrations created by a <see cref="Mocker" /> instance.
    /// </summary>
    public partial class Mocker
    {
        private readonly List<object> _ownedRegistrations = [];

        private bool _disposed;

        internal void TrackOwnedRegistration(object ownedRegistration)
        {
            ArgumentNullException.ThrowIfNull(ownedRegistration);

            _ownedRegistrations.Add(ownedRegistration);
        }

        /// <summary>
        /// Releases helper-owned disposable registrations created by this <see cref="Mocker" /> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Asynchronously releases helper-owned disposable registrations created by this <see cref="Mocker" /> instance.
        /// </summary>
        /// <returns>A task that completes when owned registrations are disposed.</returns>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases managed resources owned by this <see cref="Mocker" /> instance.
        /// Derived types overriding this method should call the base implementation.
        /// </summary>
        /// <param name="disposing"><see langword="true" /> when called from <see cref="Dispose()" />; otherwise, <see langword="false" />.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                foreach (var ownedRegistration in Enumerable.Reverse(_ownedRegistrations))
                {
                    DisposeOwnedRegistration(ownedRegistration);
                }

                _ownedRegistrations.Clear();
            }

            _disposed = true;
        }

        /// <summary>
        /// Asynchronously releases resources owned by this <see cref="Mocker" /> instance.
        /// Derived types overriding this method should call the base implementation.
        /// </summary>
        /// <returns>A task that completes when asynchronous cleanup has finished.</returns>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var ownedRegistration in Enumerable.Reverse(_ownedRegistrations))
            {
                await DisposeOwnedRegistrationAsync(ownedRegistration).ConfigureAwait(false);
            }

            _ownedRegistrations.Clear();
            _disposed = true;
        }

        private static void DisposeOwnedRegistration(object ownedRegistration)
        {
            ArgumentNullException.ThrowIfNull(ownedRegistration);

            if (ownedRegistration is IDisposable disposable)
            {
                disposable.Dispose();
                return;
            }

            if (ownedRegistration is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        private static async ValueTask DisposeOwnedRegistrationAsync(object ownedRegistration)
        {
            ArgumentNullException.ThrowIfNull(ownedRegistration);

            if (ownedRegistration is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                return;
            }

            if (ownedRegistration is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}