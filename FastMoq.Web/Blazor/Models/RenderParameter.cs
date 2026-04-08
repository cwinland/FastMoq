namespace FastMoq.Web.Blazor.Models
{
    /// <summary>
    /// Represents a render parameter used by <see cref="MockerBlazorTestBase{T}"/>.
    /// </summary>
    /// <remarks>
    /// This type replaces direct consumer use of bUnit's older <c>ComponentParameter</c> surface.
    /// Use <see cref="Create(string, object?)"/> for ordinary parameters and <see cref="CreateCascading(string, object?)"/>
    /// when the value must be supplied as a named cascading parameter.
    /// </remarks>
    /// <example>
    /// <code language="csharp"><![CDATA[
    /// RenderParameters.Add(RenderParameter.Create(nameof(OrdersPage.Title), "Queued Orders"));
    /// RenderParameters.Add(RenderParameter.CreateCascading("Accent", "Ocean"));
    /// ]]></code>
    /// </example>
    public readonly record struct RenderParameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RenderParameter"/> struct.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <param name="isCascadingValue">Indicates whether the parameter should be applied as a cascading value.</param>
        public RenderParameter(string name, object? value, bool isCascadingValue = false)
        {
            Name = name;
            Value = value;
            IsCascadingValue = isCascadingValue;
        }

        /// <summary>
        /// Gets the parameter name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the parameter value.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Gets a value indicating whether the parameter should be treated as a cascading value.
        /// </summary>
        public bool IsCascadingValue { get; }

        /// <summary>
        /// Creates a direct render parameter.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        /// <returns>A direct render parameter.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var parameter = RenderParameter.Create(nameof(OrdersPage.Title), "Queued Orders");
        /// ]]></code>
        /// </example>
        public static RenderParameter Create(string name, object? value)
        {
            return new RenderParameter(name, value);
        }

        /// <summary>
        /// Creates a cascading render parameter.
        /// </summary>
        /// <param name="name">The cascading value name.</param>
        /// <param name="value">The cascading value.</param>
        /// <returns>A cascading render parameter.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var parameter = RenderParameter.CreateCascading("Accent", "Ocean");
        /// ]]></code>
        /// </example>
        public static RenderParameter CreateCascading(string name, object? value)
        {
            return new RenderParameter(name, value, true);
        }

        /// <summary>
        /// Converts a tuple into a direct render parameter.
        /// </summary>
        /// <param name="parameter">The tuple to convert.</param>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// RenderParameters.Add((nameof(OrdersPage.Title), "Queued Orders"));
        /// ]]></code>
        /// </example>
        public static implicit operator RenderParameter((string Name, object? Value) parameter)
        {
            return new RenderParameter(parameter.Name, parameter.Value);
        }

        /// <summary>
        /// Converts a tuple into a render parameter.
        /// </summary>
        /// <param name="parameter">The tuple to convert.</param>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// RenderParameters.Add(("Accent", "Ocean", true));
        /// ]]></code>
        /// </example>
        public static implicit operator RenderParameter((string Name, object? Value, bool IsCascadingValue) parameter)
        {
            return new RenderParameter(parameter.Name, parameter.Value, parameter.IsCascadingValue);
        }
    }
}