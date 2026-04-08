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
        public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, params DiagnosticAnalyzer[] analyzers)
        {
            var document = CreateDocument(source);
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

        public static async Task<string> ApplyCodeFixAsync(string source, DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string diagnosticId)
        {
            var document = CreateDocument(source);
            var diagnostics = await GetDiagnosticsAsync(document, analyzer).ConfigureAwait(false);
            var diagnostic = diagnostics.Single(item => item.Id == diagnosticId);

            var actions = new List<CodeAction>();
            var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
            await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

            var action = actions.Single();
            var operations = await action.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
            var changedSolution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
            var changedDocument = changedSolution.GetDocument(document.Id)!;
            var changedRoot = await changedDocument.GetSyntaxRootAsync(CancellationToken.None).ConfigureAwait(false);
            return changedRoot!.NormalizeWhitespace().ToFullString();
        }

        public static string NormalizeCode(string source)
        {
            return CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))
                .GetRoot()
                .NormalizeWhitespace()
                .ToFullString();
        }

        private static Document CreateDocument(string source)
        {
            var workspace = new AdhocWorkspace();
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var solution = workspace.CurrentSolution
                .AddProject(projectId, "AnalyzerTests", "AnalyzerTests", LanguageNames.CSharp)
                .WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.Preview))
                .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            foreach (var metadataReference in GetMetadataReferences())
            {
                solution = solution.AddMetadataReference(projectId, metadataReference);
            }

            solution = solution.AddDocument(documentId, "Test.cs", SourceText.From(source));
            return solution.GetDocument(documentId)!;
        }

        private static IEnumerable<MetadataReference> GetMetadataReferences()
        {
            var references = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedPlatformAssemblies)
            {
                foreach (var assemblyPath in trustedPlatformAssemblies.Split(Path.PathSeparator))
                {
                    references.Add(assemblyPath);
                }
            }

            references.Add(typeof(FastMoq.Mocker).Assembly.Location);
            references.Add(typeof(FastMoq.Providers.MoqProvider.IFastMockMoqExtensions).Assembly.Location);
            references.Add(typeof(FastMoq.Providers.NSubstituteProvider.IFastMockNSubstituteExtensions).Assembly.Location);
            references.Add(typeof(Moq.Mock).Assembly.Location);
            references.Add(typeof(NSubstitute.Substitute).Assembly.Location);
            references.Add(typeof(Microsoft.Extensions.Logging.ILogger).Assembly.Location);
            references.Add(typeof(Microsoft.AspNetCore.Http.DefaultHttpContext).Assembly.Location);
            references.Add(typeof(Microsoft.AspNetCore.Mvc.ControllerContext).Assembly.Location);

            return references.Select(path => MetadataReference.CreateFromFile(path));
        }
    }
}