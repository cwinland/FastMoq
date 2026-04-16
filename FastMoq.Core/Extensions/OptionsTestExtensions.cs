using Microsoft.Extensions.Options;

namespace FastMoq.Extensions
{
    /// <summary>
    /// Provides convenience helpers for registering <see cref="IOptions{TOptions}" /> dependencies in tests.
    /// </summary>
    public static class OptionsTestExtensions
    {
        /// <summary>
        /// Registers a default <see cref="IOptions{TOptions}" /> value created from a new instance of <typeparamref name="T" />.
        /// </summary>
        /// <typeparam name="T">The options type to register.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="replace">True to replace an existing options registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var mocker = new Mocker()
        ///     .SetupOptions<MyOptions>();
        /// ]]></code>
        /// </example>
        public static Mocker SetupOptions<T>(this Mocker mocker, bool replace = false)
            where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(mocker);

            return mocker.SetupOptions(static () => new T(), replace);
        }

        /// <summary>
        /// Registers a concrete <see cref="IOptions{TOptions}" /> value.
        /// </summary>
        /// <typeparam name="T">The options type to register.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="value">The options value to wrap in <see cref="Options.Create{TOptions}(TOptions)" />.</param>
        /// <param name="replace">True to replace an existing options registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var mocker = new Mocker()
        ///     .SetupOptions(new CheckoutOptions { TimeoutSeconds = 30 });
        /// ]]></code>
        /// </example>
        public static Mocker SetupOptions<T>(this Mocker mocker, T value, bool replace = false)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(value);

            return mocker.AddType<IOptions<T>>(Options.Create(value), replace);
        }

        /// <summary>
        /// Creates and registers a concrete <see cref="IOptions{TOptions}" /> value from a factory.
        /// </summary>
        /// <typeparam name="T">The options type to register.</typeparam>
        /// <param name="mocker">The current <see cref="Mocker" /> instance.</param>
        /// <param name="create">The factory used to create the options value each time <see cref="IOptions{TOptions}" /> is resolved.</param>
        /// <param name="replace">True to replace an existing options registration.</param>
        /// <returns>The current <see cref="Mocker" /> instance.</returns>
        public static Mocker SetupOptions<T>(this Mocker mocker, Func<T> create, bool replace = false)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(mocker);
            ArgumentNullException.ThrowIfNull(create);

            return mocker.AddType(typeof(IOptions<T>), typeof(OptionsWrapper<T>), _ => Options.Create(create()), replace);
        }
    }
}