namespace FastMoq
{
    /// <summary>
    /// Captures property values assigned by a fake or stub during a test.
    /// </summary>
    /// <typeparam name="TValue">The captured value type.</typeparam>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// var modeCapture = new PropertyValueCapture<string?>();
    ///
    /// var sink = new FakeModeSink(modeCapture);
    /// Mocks.AddType<IModeSink>(sink);
    ///
    /// modeCapture.Value.Should().Be("updated");
    /// ]]></code>
    /// </example>
    public sealed class PropertyValueCapture<TValue>
    {
        private readonly List<TValue> _history = [];

        /// <summary>
        /// Gets the most recently recorded value.
        /// </summary>
        public TValue Value { get; private set; } = default!;

        /// <summary>
        /// Gets a value indicating whether any value has been recorded.
        /// </summary>
        public bool HasValue { get; private set; }

        /// <summary>
        /// Gets the recorded history in assignment order.
        /// </summary>
        public IReadOnlyList<TValue> History => _history;

        /// <summary>
        /// Records a property assignment.
        /// </summary>
        /// <param name="value">The assigned value.</param>
        public void Record(TValue value)
        {
            Value = value;
            HasValue = true;
            _history.Add(value);
        }

        /// <summary>
        /// Clears the recorded history and current value marker.
        /// </summary>
        public void Clear()
        {
            _history.Clear();
            Value = default!;
            HasValue = false;
        }
    }
}