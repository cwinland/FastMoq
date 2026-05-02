using System;
using System.Collections.Generic;

namespace FastMoq.Analyzers
{
    internal enum FastMoqGeneratedTestPackageLayout
    {
        None = 0,
        CoreOnly = 1,
        SplitHelpers = 2,
        Aggregate = 3,
    }

    internal enum GeneratedTestTargetShape
    {
        Core = 0,
        Web = 1,
        Blazor = 2,
        Database = 3,
        Azure = 4,
        AzureFunctions = 5,
    }

    internal sealed class GeneratedTestTargetShapeRule
    {
        public GeneratedTestTargetShapeRule(
            GeneratedTestTargetShape shape,
            string requiredPackageName,
            string defaultTestBaseTypeDisplayName,
            IEnumerable<string> defaultNamespaces)
        {
            Shape = shape;
            RequiredPackageName = requiredPackageName ?? throw new ArgumentNullException(nameof(requiredPackageName));
            DefaultTestBaseTypeDisplayName = defaultTestBaseTypeDisplayName ?? throw new ArgumentNullException(nameof(defaultTestBaseTypeDisplayName));
            DefaultNamespaces = [.. defaultNamespaces ?? throw new ArgumentNullException(nameof(defaultNamespaces))];
        }

        public GeneratedTestTargetShape Shape { get; }

        public string RequiredPackageName { get; }

        public string DefaultTestBaseTypeDisplayName { get; }

        public IReadOnlyList<string> DefaultNamespaces { get; }
    }

    internal sealed class FastMoqGeneratedTestPackageMatrix
    {
        public FastMoqGeneratedTestPackageMatrix(
            FastMoqGeneratedTestPackageLayout layout,
            bool hasAggregatePackage,
            bool hasWebHelpers,
            bool hasDatabaseHelpers,
            bool hasAzureHelpers,
            bool hasAzureFunctionsHelpers,
            IEnumerable<GeneratedTestTargetShapeRule> supportedTargetShapes)
        {
            Layout = layout;
            HasAggregatePackage = hasAggregatePackage;
            HasWebHelpers = hasWebHelpers;
            HasDatabaseHelpers = hasDatabaseHelpers;
            HasAzureHelpers = hasAzureHelpers;
            HasAzureFunctionsHelpers = hasAzureFunctionsHelpers;
            SupportedTargetShapes = [.. supportedTargetShapes ?? throw new ArgumentNullException(nameof(supportedTargetShapes))];
        }

        public FastMoqGeneratedTestPackageLayout Layout { get; }

        public bool HasAggregatePackage { get; }

        public bool HasWebHelpers { get; }

        public bool HasDatabaseHelpers { get; }

        public bool HasAzureHelpers { get; }

        public bool HasAzureFunctionsHelpers { get; }

        public IReadOnlyList<GeneratedTestTargetShapeRule> SupportedTargetShapes { get; }
    }
}