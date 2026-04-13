using System.Runtime;

namespace FastMoq
{
    /// <summary>
    /// Controls how FastMoq resolves constructor ambiguity when multiple equally viable constructors remain after candidate filtering.
    /// </summary>
    public enum ConstructorAmbiguityBehavior
    {
        /// <summary>
        /// Throw an <see cref="AmbiguousImplementationException"/> when FastMoq cannot reduce the candidate set to a single constructor.
        /// </summary>
        Throw = 0,

        /// <summary>
        /// When ambiguity remains, prefer the parameterless constructor from the current visibility scope if one exists; otherwise throw.
        /// </summary>
        PreferParameterlessConstructor = 1,
    }
}