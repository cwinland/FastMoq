using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastMoq.Analyzers.Tests
{
    internal static class AnalyzerTestHelpers
    {
        private static readonly HashSet<string> ExcludedTrustedPlatformAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "FastMoq.Analyzers.Tests",
            "FastMoq.Tests",
            "FastMoq.Tests.Blazor",
            "FastMoq.Tests.Web",
        };

        private static bool IsXunitAssemblyName(string assemblyName)
        {
            return assemblyName.StartsWith("xunit", StringComparison.OrdinalIgnoreCase);
        }

        public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, params DiagnosticAnalyzer[] analyzers)
        {
            return await GetDiagnosticsAsync(source, includeAzureFunctionsHelpers: false, includeMoqProviderPackage: true, includeNSubstituteProviderPackage: true, includeWebHelpers: true, analyzers).ConfigureAwait(false);
        }

        public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(IReadOnlyList<(string fileName, string source)> sources, params DiagnosticAnalyzer[] analyzers)
        {
            return await GetDiagnosticsAsync(sources, includeAzureFunctionsHelpers: false, includeMoqProviderPackage: true, includeNSubstituteProviderPackage: true, includeWebHelpers: true, analyzers).ConfigureAwait(false);
        }

        public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, bool includeAzureFunctionsHelpers, params DiagnosticAnalyzer[] analyzers)
        {
            return await GetDiagnosticsAsync(source, includeAzureFunctionsHelpers, includeMoqProviderPackage: true, includeNSubstituteProviderPackage: true, includeWebHelpers: true, analyzers).ConfigureAwait(false);
        }

        public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, bool includeAzureFunctionsHelpers, bool includeMoqProviderPackage, bool includeNSubstituteProviderPackage, params DiagnosticAnalyzer[] analyzers)
        {
            return await GetDiagnosticsAsync(source, includeAzureFunctionsHelpers, includeMoqProviderPackage, includeNSubstituteProviderPackage, includeWebHelpers: true, analyzers).ConfigureAwait(false);
        }

        public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, bool includeAzureFunctionsHelpers, bool includeMoqProviderPackage, bool includeNSubstituteProviderPackage, bool includeWebHelpers = true, params DiagnosticAnalyzer[] analyzers)
        {
            var document = CreateDocument(source, includeAzureFunctionsHelpers, includeMoqProviderPackage, includeNSubstituteProviderPackage, includeWebHelpers);
            return await GetDiagnosticsAsync(document, analyzers).ConfigureAwait(false);
        }

        public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(IReadOnlyList<(string fileName, string source)> sources, bool includeAzureFunctionsHelpers, bool includeMoqProviderPackage, bool includeNSubstituteProviderPackage, bool includeWebHelpers = true, params DiagnosticAnalyzer[] analyzers)
        {
            var project = CreateProject(sources, includeAzureFunctionsHelpers, includeMoqProviderPackage, includeNSubstituteProviderPackage, includeWebHelpers);
            var document = project.Documents.First();
            return await GetDiagnosticsAsync(document, analyzers).ConfigureAwait(false);
        }

        public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Document document, params DiagnosticAnalyzer[] analyzers)
        {
            var compilation = await document.Project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);
            if (compilation is null)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            return await compilation
                .WithAnalyzers(ImmutableArray.Create(analyzers))
                .GetAnalyzerDiagnosticsAsync()
                .ConfigureAwait(false);
        }

        public static async Task<string> ApplyCodeFixAsync(string source, DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string diagnosticId, bool includeAzureFunctionsHelpers = false, int diagnosticOccurrence = 0, string? diagnosticMessageContains = null, string? codeFixTitle = null)
        {
            return await ApplyCodeFixAsync(source, analyzer, codeFixProvider, diagnosticId, includeAzureFunctionsHelpers, includeMoqProviderPackage: true, includeNSubstituteProviderPackage: true, includeWebHelpers: true, diagnosticOccurrence, diagnosticMessageContains, codeFixTitle).ConfigureAwait(false);
        }

        public static async Task<string> ApplyCodeFixAsync(string source, DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string diagnosticId, bool includeAzureFunctionsHelpers, bool includeMoqProviderPackage, bool includeNSubstituteProviderPackage, int diagnosticOccurrence = 0, string? diagnosticMessageContains = null, string? codeFixTitle = null)
        {
            return await ApplyCodeFixAsync(source, analyzer, codeFixProvider, diagnosticId, includeAzureFunctionsHelpers, includeMoqProviderPackage, includeNSubstituteProviderPackage, includeWebHelpers: true, diagnosticOccurrence, diagnosticMessageContains, codeFixTitle).ConfigureAwait(false);
        }

        public static async Task<string> ApplyCodeFixAsync(string source, DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string diagnosticId, bool includeAzureFunctionsHelpers, bool includeMoqProviderPackage, bool includeNSubstituteProviderPackage, bool includeWebHelpers = true, int diagnosticOccurrence = 0, string? diagnosticMessageContains = null, string? codeFixTitle = null)
        {
            var document = CreateDocument(source, includeAzureFunctionsHelpers, includeMoqProviderPackage, includeNSubstituteProviderPackage, includeWebHelpers);
            var diagnostics = await GetDiagnosticsAsync(document, analyzer).ConfigureAwait(false);
            var diagnostic = diagnostics
                .Where(item => item.Id == diagnosticId)
                .Where(item => diagnosticMessageContains is null || item.GetMessage().Contains(diagnosticMessageContains, StringComparison.Ordinal))
                .OrderBy(item => item.Location.SourceSpan.Start)
                .ElementAt(diagnosticOccurrence);

            var actions = new List<CodeAction>();
            var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
            await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

            var action = codeFixTitle is null
                ? actions.Single()
                : actions.Single(item => string.Equals(item.Title, codeFixTitle, StringComparison.Ordinal));
            var operations = await action.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
            var changedSolution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
            var changedDocument = changedSolution.GetDocument(document.Id)!;
            var changedRoot = await changedDocument.GetSyntaxRootAsync(CancellationToken.None).ConfigureAwait(false);
            return changedRoot!.NormalizeWhitespace().ToFullString();
        }

        public static async Task<ImmutableArray<string>> GetCodeFixTitlesAsync(string source, DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string diagnosticId, bool includeAzureFunctionsHelpers = false, int diagnosticOccurrence = 0, string? diagnosticMessageContains = null)
        {
            return await GetCodeFixTitlesAsync(source, analyzer, codeFixProvider, diagnosticId, includeAzureFunctionsHelpers, includeMoqProviderPackage: true, includeNSubstituteProviderPackage: true, includeWebHelpers: true, diagnosticOccurrence, diagnosticMessageContains).ConfigureAwait(false);
        }

        public static async Task<ImmutableArray<string>> GetCodeFixTitlesAsync(string source, DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string diagnosticId, bool includeAzureFunctionsHelpers, bool includeMoqProviderPackage, bool includeNSubstituteProviderPackage, int diagnosticOccurrence = 0, string? diagnosticMessageContains = null)
        {
            return await GetCodeFixTitlesAsync(source, analyzer, codeFixProvider, diagnosticId, includeAzureFunctionsHelpers, includeMoqProviderPackage, includeNSubstituteProviderPackage, includeWebHelpers: true, diagnosticOccurrence, diagnosticMessageContains).ConfigureAwait(false);
        }

        public static async Task<ImmutableArray<string>> GetCodeFixTitlesAsync(string source, DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string diagnosticId, bool includeAzureFunctionsHelpers, bool includeMoqProviderPackage, bool includeNSubstituteProviderPackage, bool includeWebHelpers = true, int diagnosticOccurrence = 0, string? diagnosticMessageContains = null)
        {
            var document = CreateDocument(source, includeAzureFunctionsHelpers, includeMoqProviderPackage, includeNSubstituteProviderPackage, includeWebHelpers);
            var diagnostics = await GetDiagnosticsAsync(document, analyzer).ConfigureAwait(false);
            var diagnostic = diagnostics
                .Where(item => item.Id == diagnosticId)
                .Where(item => diagnosticMessageContains is null || item.GetMessage().Contains(diagnosticMessageContains, StringComparison.Ordinal))
                .OrderBy(item => item.Location.SourceSpan.Start)
                .ElementAt(diagnosticOccurrence);

            var actions = new List<CodeAction>();
            var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
            await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

            return actions.Select(action => action.Title).ToImmutableArray();
        }

        public static string NormalizeCode(string source)
        {
            return CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))
                .GetRoot()
                .NormalizeWhitespace()
                .ToFullString();
        }

        public static Document CreateDocumentForTest(
            string source,
            bool includeAzureFunctionsHelpers = false,
            bool includeMoqProviderPackage = true,
            bool includeNSubstituteProviderPackage = true,
            bool includeWebHelpers = true,
            bool includeDatabaseHelpers = false,
            bool includeAzureHelpers = false,
            bool includeAggregatePackage = false,
            bool includeXunit = true)
        {
            return CreateDocument(
                source,
                includeAzureFunctionsHelpers,
                includeMoqProviderPackage,
                includeNSubstituteProviderPackage,
                includeWebHelpers,
                includeDatabaseHelpers,
                includeAzureHelpers,
                includeAggregatePackage,
                includeXunit);
        }

        private static Document CreateDocument(
            string source,
            bool includeAzureFunctionsHelpers = false,
            bool includeMoqProviderPackage = true,
            bool includeNSubstituteProviderPackage = true,
            bool includeWebHelpers = true,
            bool includeDatabaseHelpers = false,
            bool includeAzureHelpers = false,
            bool includeAggregatePackage = false,
            bool includeXunit = true)
        {
            var project = CreateProject(
                [("Test.cs", source)],
                includeAzureFunctionsHelpers,
                includeMoqProviderPackage,
                includeNSubstituteProviderPackage,
                includeWebHelpers,
                includeDatabaseHelpers,
                includeAzureHelpers,
                includeAggregatePackage,
                includeXunit);
            return project.Documents.Single();
        }

        private static Project CreateProject(
            IReadOnlyList<(string fileName, string source)> sources,
            bool includeAzureFunctionsHelpers = false,
            bool includeMoqProviderPackage = true,
            bool includeNSubstituteProviderPackage = true,
            bool includeWebHelpers = true,
            bool includeDatabaseHelpers = false,
            bool includeAzureHelpers = false,
            bool includeAggregatePackage = false,
            bool includeXunit = true)
        {
            var workspace = new AdhocWorkspace();
            var projectId = ProjectId.CreateNewId();

            var solution = workspace.CurrentSolution
                .AddProject(projectId, "AnalyzerTests", "AnalyzerTests", LanguageNames.CSharp)
                .WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.Preview))
                .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            foreach (var metadataReference in GetMetadataReferences(
                includeAzureFunctionsHelpers,
                includeMoqProviderPackage,
                includeNSubstituteProviderPackage,
                includeWebHelpers,
                includeDatabaseHelpers,
                includeAzureHelpers,
                includeAggregatePackage,
                includeXunit))
            {
                solution = solution.AddMetadataReference(projectId, metadataReference);
            }

            foreach (var (fileName, source) in sources)
            {
                var documentId = DocumentId.CreateNewId(projectId);
                solution = solution.AddDocument(documentId, fileName, SourceText.From(source));
            }

            return solution.GetProject(projectId)!;
        }

        private static IEnumerable<MetadataReference> GetMetadataReferences(
            bool includeAzureFunctionsHelpers,
            bool includeMoqProviderPackage,
            bool includeNSubstituteProviderPackage,
            bool includeWebHelpers,
            bool includeDatabaseHelpers,
            bool includeAzureHelpers,
            bool includeAggregatePackage,
            bool includeXunit)
        {
            if (includeAggregatePackage)
            {
                includeAzureFunctionsHelpers = true;
                includeAzureHelpers = true;
                includeDatabaseHelpers = true;
                includeWebHelpers = true;
            }

            var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedPlatformAssemblies)
            {
                foreach (var assemblyPath in trustedPlatformAssemblies.Split(Path.PathSeparator))
                {
                    var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
                    if (ExcludedTrustedPlatformAssemblyNames.Contains(assemblyName))
                    {
                        continue;
                    }

                    if (!includeXunit && IsXunitAssemblyName(assemblyName))
                    {
                        continue;
                    }

                    if (!includeAggregatePackage &&
                        string.Equals(assemblyName, "FastMoq", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!includeDatabaseHelpers &&
                        string.Equals(assemblyName, "FastMoq.Database", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!includeAzureHelpers &&
                        string.Equals(assemblyName, "FastMoq.Azure", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!includeWebHelpers &&
                        string.Equals(assemblyName, "FastMoq.Web", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!includeAzureFunctionsHelpers &&
                        string.Equals(assemblyName, "FastMoq.AzureFunctions", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!includeMoqProviderPackage &&
                        string.Equals(assemblyName, "FastMoq.Provider.Moq", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!includeNSubstituteProviderPackage &&
                        string.Equals(assemblyName, "FastMoq.Provider.NSubstitute", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    references.Add(assemblyPath);
                }
            }

            references.Add(typeof(FastMoq.Mocker).Assembly.Location);
            references.Add(typeof(FastMoq.Providers.FastMoqDefaultProviderAttribute).Assembly.Location);

            if (includeAggregatePackage)
            {
                var aggregateAssemblyPath = Path.Combine(Path.GetDirectoryName(typeof(FastMoq.Mocker).Assembly.Location)!, "FastMoq.dll");
                if (File.Exists(aggregateAssemblyPath))
                {
                    references.Add(aggregateAssemblyPath);
                }
            }

            if (includeWebHelpers)
            {
                references.Add(typeof(FastMoq.Web.Extensions.TestWebExtensions).Assembly.Location);
            }

            if (includeDatabaseHelpers)
            {
                references.Add(typeof(FastMoq.DbContextMockerExtensions).Assembly.Location);
            }

            if (includeAzureHelpers)
            {
                references.Add(typeof(FastMoq.Azure.Pageable.PageableBuilder).Assembly.Location);
            }

            if (includeMoqProviderPackage)
            {
                references.Add(typeof(FastMoq.Providers.MoqProvider.IFastMockMoqExtensions).Assembly.Location);
            }

            if (includeNSubstituteProviderPackage)
            {
                references.Add(typeof(FastMoq.Providers.NSubstituteProvider.IFastMockNSubstituteExtensions).Assembly.Location);
            }

            references.Add(typeof(Moq.Mock).Assembly.Location);
            references.Add(typeof(NSubstitute.Substitute).Assembly.Location);
            if (includeXunit)
            {
                references.Add(typeof(global::Xunit.FactAttribute).Assembly.Location);
            }
            references.Add(typeof(Microsoft.Extensions.Logging.ILogger).Assembly.Location);
            references.Add(typeof(Microsoft.AspNetCore.Http.DefaultHttpContext).Assembly.Location);
            references.Add(typeof(Microsoft.AspNetCore.Mvc.ControllerContext).Assembly.Location);

            if (includeAzureFunctionsHelpers)
            {
                references.Add(typeof(FastMoq.AzureFunctions.Extensions.FunctionContextTestExtensions).Assembly.Location);
                references.Add(typeof(Microsoft.Azure.Functions.Worker.FunctionContext).Assembly.Location);
                references.Add(typeof(global::Azure.Core.Serialization.ObjectSerializer).Assembly.Location);
            }

            return references.Select(path => MetadataReference.CreateFromFile(path));
        }
    }
}