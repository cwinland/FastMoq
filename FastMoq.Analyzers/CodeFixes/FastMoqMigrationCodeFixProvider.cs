using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastMoq.Analyzers.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FastMoqMigrationCodeFixProvider)), Shared]
    public sealed class FastMoqMigrationCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticIds.UseProviderFirstObjectAccess,
            DiagnosticIds.UseProviderFirstReset,
            DiagnosticIds.UseVerifyLogged,
            DiagnosticIds.UseConsistentMockRetrieval,
            DiagnosticIds.UseProviderFirstMockRetrieval,
            DiagnosticIds.PreferTypedServiceProviderHelpers,
            DiagnosticIds.UseExplicitOptionalParameterResolution,
            DiagnosticIds.ReplaceInitializeCompatibilityWrapper,
            DiagnosticIds.PreferSetupOptionsHelper);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
            {
                return;
            }

            switch (diagnostic.Id)
            {
                case DiagnosticIds.UseProviderFirstObjectAccess:
                    {
                        var memberAccess = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<MemberAccessExpressionSyntax>();
                        if (memberAccess is null)
                        {
                            return;
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Use provider-first object access",
                                cancellationToken => ReplaceMemberAccessAsync(document, memberAccess, BuildObjectReplacementAsync, cancellationToken),
                                nameof(DiagnosticIds.UseProviderFirstObjectAccess)),
                            diagnostic);
                        break;
                    }

                case DiagnosticIds.UseProviderFirstReset:
                    {
                        var invocationExpression = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>();
                        if (invocationExpression is null)
                        {
                            return;
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Use IFastMock.Reset()",
                                cancellationToken => ReplaceInvocationAsync(document, invocationExpression, BuildResetReplacementAsync, cancellationToken),
                                nameof(DiagnosticIds.UseProviderFirstReset)),
                            diagnostic);
                        break;
                    }

                case DiagnosticIds.UseVerifyLogged:
                    {
                        var invocationExpression = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>();
                        if (invocationExpression is null)
                        {
                            return;
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Use VerifyLogged(...)",
                                cancellationToken => ReplaceInvocationAsync(document, invocationExpression, BuildVerifyLoggedReplacementAsync, cancellationToken),
                                nameof(DiagnosticIds.UseVerifyLogged)),
                            diagnostic);
                        break;
                    }

                case DiagnosticIds.UseConsistentMockRetrieval:
                case DiagnosticIds.UseProviderFirstMockRetrieval:
                    {
                        var memberAccess = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<MemberAccessExpressionSyntax>();
                        if (memberAccess is null || memberAccess.Name.Identifier.ValueText != "GetMock")
                        {
                            return;
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Use GetOrCreateMock<T>()",
                                cancellationToken => ReplaceGetMockAsync(document, memberAccess, cancellationToken),
                                diagnostic.Id),
                            diagnostic);
                        break;
                    }

                case DiagnosticIds.PreferTypedServiceProviderHelpers:
                    {
                        var invocationExpression = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>();
                        if (invocationExpression is null)
                        {
                            return;
                        }

                        var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                        if (semanticModel is null ||
                            !FastMoqAnalysisHelpers.TryBuildFunctionContextInstanceServicesReplacement(invocationExpression, semanticModel, context.CancellationToken, out _, out _))
                        {
                            return;
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Use AddFunctionContextInstanceServices(...)",
                                cancellationToken => ReplaceFunctionContextInstanceServicesInvocationAsync(document, invocationExpression, cancellationToken),
                                nameof(DiagnosticIds.PreferTypedServiceProviderHelpers)),
                            diagnostic);
                        break;
                    }

                case DiagnosticIds.UseExplicitOptionalParameterResolution:
                    {
                        var assignmentExpression = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<AssignmentExpressionSyntax>();
                        if (assignmentExpression is null)
                        {
                            return;
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Use OptionalParameterResolution",
                                cancellationToken => ReplaceAssignmentAsync(document, assignmentExpression, BuildMockOptionalReplacementAsync, cancellationToken),
                                nameof(DiagnosticIds.UseExplicitOptionalParameterResolution)),
                            diagnostic);
                        break;
                    }

                case DiagnosticIds.ReplaceInitializeCompatibilityWrapper:
                    {
                        var invocationExpression = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>();
                        if (invocationExpression is null)
                        {
                            return;
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Use GetMock<T>(...)",
                                cancellationToken => ReplaceInvocationAsync(document, invocationExpression, BuildInitializeReplacementAsync, cancellationToken),
                                nameof(DiagnosticIds.ReplaceInitializeCompatibilityWrapper)),
                            diagnostic);
                        break;
                    }

                case DiagnosticIds.PreferSetupOptionsHelper:
                    {
                        var invocationExpression = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>();
                        if (invocationExpression is null)
                        {
                            return;
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Use SetupOptions(...)",
                                cancellationToken => ReplaceSetupOptionsInvocationAsync(document, invocationExpression, cancellationToken),
                                nameof(DiagnosticIds.PreferSetupOptionsHelper)),
                            diagnostic);
                        break;
                    }
            }
        }

        private static async Task<Document> ReplaceAssignmentAsync(Document document, AssignmentExpressionSyntax assignmentExpression, ReplacementBuilder replacementBuilder, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
            {
                return document;
            }

            var replacementText = replacementBuilder(document, semanticModel, assignmentExpression, cancellationToken);
            if (replacementText is null)
            {
                return document;
            }

            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(assignmentExpression);
            var updatedRoot = root.ReplaceNode(assignmentExpression, replacementExpression);
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static async Task<Document> ReplaceMemberAccessAsync(Document document, MemberAccessExpressionSyntax memberAccess, ReplacementBuilder replacementBuilder, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
            {
                return document;
            }

            var replacementText = replacementBuilder(document, semanticModel, memberAccess, cancellationToken);
            if (replacementText is null)
            {
                return document;
            }

            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(memberAccess);
            var updatedRoot = root.ReplaceNode(memberAccess, replacementExpression);
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static async Task<Document> ReplaceInvocationAsync(Document document, InvocationExpressionSyntax invocationExpression, ReplacementBuilder replacementBuilder, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
            {
                return document;
            }

            var replacementText = replacementBuilder(document, semanticModel, invocationExpression, cancellationToken);
            if (replacementText is null)
            {
                return document;
            }

            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(invocationExpression);
            var updatedRoot = root.ReplaceNode(invocationExpression, replacementExpression);
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static string? BuildObjectReplacementAsync(Document document, SemanticModel semanticModel, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            if (syntaxNode is not MemberAccessExpressionSyntax memberAccess ||
                !FastMoqAnalysisHelpers.TryResolveTrackedMockOrigin(memberAccess.Expression, semanticModel, cancellationToken, out var origin))
            {
                return null;
            }

            return FastMoqAnalysisHelpers.BuildObjectAccessReplacement(origin, semanticModel, memberAccess.SpanStart);
        }

        private static string? BuildResetReplacementAsync(Document document, SemanticModel semanticModel, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            if (syntaxNode is not InvocationExpressionSyntax invocationExpression ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess ||
                !FastMoqAnalysisHelpers.TryResolveTrackedMockOrigin(memberAccess.Expression, semanticModel, cancellationToken, out var origin))
            {
                return null;
            }

            return FastMoqAnalysisHelpers.BuildResetReplacement(origin, semanticModel, invocationExpression.SpanStart);
        }

        private static string? BuildVerifyLoggedReplacementAsync(Document document, SemanticModel semanticModel, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            if (syntaxNode is not InvocationExpressionSyntax invocationExpression ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess ||
                !FastMoqAnalysisHelpers.TryResolveTrackedMockOrigin(memberAccess.Expression, semanticModel, cancellationToken, out var origin))
            {
                return null;
            }

            return FastMoqAnalysisHelpers.TryBuildVerifyLoggedReplacement(origin, semanticModel, invocationExpression, cancellationToken, out var replacement)
                ? replacement
                : null;
        }

        private static string? BuildMockOptionalReplacementAsync(Document document, SemanticModel semanticModel, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            return syntaxNode is AssignmentExpressionSyntax assignmentExpression &&
                FastMoqAnalysisHelpers.TryBuildMockOptionalReplacement(assignmentExpression, semanticModel, cancellationToken, out var replacement)
                ? replacement
                : null;
        }

        private static string? BuildInitializeReplacementAsync(Document document, SemanticModel semanticModel, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            return syntaxNode is InvocationExpressionSyntax invocationExpression &&
                FastMoqAnalysisHelpers.TryBuildInitializeReplacement(invocationExpression, out var replacement)
                ? replacement
                : null;
        }

        private static string? BuildSetupOptionsReplacementAsync(Document document, SemanticModel semanticModel, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            return syntaxNode is InvocationExpressionSyntax invocationExpression &&
                FastMoqAnalysisHelpers.TryBuildSetupOptionsReplacement(invocationExpression, semanticModel, cancellationToken, out var replacement)
                ? replacement
                : null;
        }

        private static async Task<Document> ReplaceSetupOptionsInvocationAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
            {
                return document;
            }

            if (!FastMoqAnalysisHelpers.TryBuildSetupOptionsReplacement(invocationExpression, semanticModel, cancellationToken, out var replacementText))
            {
                return document;
            }

            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(invocationExpression);
            var updatedRoot = root.ReplaceNode(invocationExpression, replacementExpression);

            return document.WithSyntaxRoot(AddUsingDirectiveIfMissing(updatedRoot, "FastMoq.Extensions"));
        }

        private static async Task<Document> ReplaceFunctionContextInstanceServicesInvocationAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
            {
                return document;
            }

            if (!FastMoqAnalysisHelpers.TryBuildFunctionContextInstanceServicesReplacement(invocationExpression, semanticModel, cancellationToken, out var targetInvocation, out var replacementText))
            {
                return document;
            }

            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(targetInvocation);
            var updatedRoot = root.ReplaceNode(targetInvocation, replacementExpression);
            updatedRoot = AddUsingDirectiveIfMissing(updatedRoot, "FastMoq.AzureFunctions.Extensions");
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static async Task<Document> ReplaceGetMockAsync(Document document, MemberAccessExpressionSyntax memberAccess, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
            {
                return document;
            }

            SimpleNameSyntax replacementName = memberAccess.Name is GenericNameSyntax genericName
                ? genericName.WithIdentifier(SyntaxFactory.Identifier("GetOrCreateMock"))
                : SyntaxFactory.IdentifierName("GetOrCreateMock");
            var replacementMemberAccess = memberAccess.WithName(replacementName);
            var updatedRoot = root.ReplaceNode(memberAccess, replacementMemberAccess);
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static SyntaxNode AddUsingDirectiveIfMissing(SyntaxNode root, string namespaceName)
        {
            if (root is CompilationUnitSyntax compilationUnit && !compilationUnit.Usings.Any(@using => @using.Name?.ToString() == namespaceName))
            {
                return compilationUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName)));
            }

            return root;
        }

        private delegate string? ReplacementBuilder(Document document, SemanticModel semanticModel, SyntaxNode syntaxNode, CancellationToken cancellationToken);
    }
}