using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
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
            DiagnosticIds.PreferSetupOptionsHelper,
            DiagnosticIds.RequireExplicitMoqOnboarding);

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
                        if (semanticModel is null)
                        {
                            return;
                        }

                        if (FastMoqAnalysisHelpers.TryBuildTypedServiceProviderHelperEdit(invocationExpression, semanticModel, context.CancellationToken, out _, out _, out _, out _))
                        {
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Use typed service-provider helper",
                                    cancellationToken => ReplaceTypedServiceProviderHelperInvocationAsync(document, invocationExpression, cancellationToken),
                                    nameof(DiagnosticIds.PreferTypedServiceProviderHelpers) + ".typed"),
                                diagnostic);
                        }

                        if (FastMoqAnalysisHelpers.HasFunctionContextInstanceServicesMockHelper(semanticModel) &&
                            FastMoqAnalysisHelpers.TryBuildFunctionContextInstanceServicesReplacement(invocationExpression, semanticModel, context.CancellationToken, out _, out _))
                        {
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Use AddFunctionContextInstanceServices(...)",
                                    cancellationToken => ReplaceFunctionContextInstanceServicesInvocationAsync(document, invocationExpression, cancellationToken),
                                    nameof(DiagnosticIds.PreferTypedServiceProviderHelpers) + ".functions"),
                                diagnostic);
                        }

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

                case DiagnosticIds.RequireExplicitMoqOnboarding:
                    {
                        var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                        if (semanticModel is null || !FastMoqAnalysisHelpers.HasMoqProviderPackage(semanticModel.Compilation))
                        {
                            return;
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Add [assembly: FastMoqDefaultProvider(\"moq\")]",
                                cancellationToken => AddAssemblyDefaultProviderAsync(document, FastMoqAnalysisHelpers.MoqProviderName, cancellationToken),
                                nameof(DiagnosticIds.RequireExplicitMoqOnboarding) + ".default"),
                            diagnostic);

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Add [assembly: FastMoqRegisterProvider(\"moq\", typeof(MoqMockingProvider), SetAsDefault = true)]",
                                cancellationToken => AddAssemblyRegisteredDefaultProviderAsync(document, FastMoqAnalysisHelpers.MoqProviderName, cancellationToken),
                                nameof(DiagnosticIds.RequireExplicitMoqOnboarding) + ".register"),
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

        private static async Task<Document> ReplaceTypedServiceProviderHelperInvocationAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
            {
                return document;
            }

            if (!FastMoqAnalysisHelpers.TryBuildTypedServiceProviderHelperEdit(invocationExpression, semanticModel, cancellationToken, out var targetInvocation, out var replacementText, out var requiredNamespaces, out var linkedInvocationToRemove))
            {
                return document;
            }

            var targetAnnotation = new SyntaxAnnotation();
            var removalNode = linkedInvocationToRemove?.FirstAncestorOrSelf<ExpressionStatementSyntax>() as SyntaxNode ?? linkedInvocationToRemove;
            var removalAnnotation = removalNode is null ? null : new SyntaxAnnotation();
            var nodesToAnnotate = new List<SyntaxNode>
            {
                targetInvocation,
            };
            if (removalNode is not null)
            {
                nodesToAnnotate.Add(removalNode);
            }

            var updatedRoot = root.ReplaceNodes(
                nodesToAnnotate,
                (originalNode, rewrittenNode) =>
                {
                    if (originalNode == targetInvocation)
                    {
                        return rewrittenNode.WithAdditionalAnnotations(targetAnnotation);
                    }

                    return rewrittenNode.WithAdditionalAnnotations(removalAnnotation!);
                });
            if (removalAnnotation is not null)
            {
                var annotatedRemovalNode = updatedRoot.GetAnnotatedNodes(removalAnnotation).Single();
                updatedRoot = updatedRoot.RemoveNode(annotatedRemovalNode, SyntaxRemoveOptions.KeepExteriorTrivia) ?? updatedRoot;
            }

            var annotatedTargetInvocation = updatedRoot.GetAnnotatedNodes(targetAnnotation).Single();
            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(annotatedTargetInvocation);
            updatedRoot = updatedRoot.ReplaceNode(annotatedTargetInvocation, replacementExpression);
            updatedRoot = AddUsingDirectivesIfMissing(updatedRoot, requiredNamespaces);
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

        private static async Task<Document> AddAssemblyDefaultProviderAsync(Document document, string providerName, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            if (root is null || HasAssemblyDefaultProviderAttribute(root, providerName))
            {
                return document;
            }

            var updatedRoot = (CompilationUnitSyntax) AddUsingDirectiveIfMissing(root, FastMoqAnalysisHelpers.FastMoqProvidersNamespace);
            updatedRoot = updatedRoot.AddAttributeLists(CreateAssemblyDefaultProviderAttribute(providerName));
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static async Task<Document> AddAssemblyRegisteredDefaultProviderAsync(Document document, string providerName, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            if (root is null || HasAssemblyRegisteredDefaultProviderAttribute(root, providerName))
            {
                return document;
            }

            var updatedRoot = (CompilationUnitSyntax) AddUsingDirectivesIfMissing(root, [FastMoqAnalysisHelpers.FastMoqProvidersNamespace, FastMoqAnalysisHelpers.MoqProviderNamespace]);
            updatedRoot = updatedRoot.AddAttributeLists(CreateAssemblyRegisteredDefaultProviderAttribute(providerName));
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static bool HasAssemblyDefaultProviderAttribute(CompilationUnitSyntax compilationUnit, string providerName)
        {
            foreach (var attributeList in compilationUnit.AttributeLists)
            {
                if (attributeList.Target?.Identifier.IsKind(SyntaxKind.AssemblyKeyword) != true)
                {
                    continue;
                }

                foreach (var attribute in attributeList.Attributes)
                {
                    var attributeName = attribute.Name.ToString();
                    if (attributeName is not "FastMoqDefaultProvider" and not "FastMoqDefaultProviderAttribute" and not "FastMoq.Providers.FastMoqDefaultProvider" and not "FastMoq.Providers.FastMoqDefaultProviderAttribute")
                    {
                        continue;
                    }

                    var firstArgument = attribute.ArgumentList?.Arguments.FirstOrDefault();
                    if (firstArgument?.Expression is LiteralExpressionSyntax literalExpression &&
                        literalExpression.IsKind(SyntaxKind.StringLiteralExpression) &&
                        string.Equals(literalExpression.Token.ValueText, providerName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HasAssemblyRegisteredDefaultProviderAttribute(CompilationUnitSyntax compilationUnit, string providerName)
        {
            foreach (var attributeList in compilationUnit.AttributeLists)
            {
                if (attributeList.Target?.Identifier.IsKind(SyntaxKind.AssemblyKeyword) != true)
                {
                    continue;
                }

                foreach (var attribute in attributeList.Attributes)
                {
                    var attributeName = attribute.Name.ToString();
                    if (attributeName is not "FastMoqRegisterProvider" and not "FastMoqRegisterProviderAttribute" and not "FastMoq.Providers.FastMoqRegisterProvider" and not "FastMoq.Providers.FastMoqRegisterProviderAttribute")
                    {
                        continue;
                    }

                    var argumentList = attribute.ArgumentList;
                    var firstArgument = argumentList?.Arguments.FirstOrDefault();
                    if (firstArgument?.Expression is not LiteralExpressionSyntax literalExpression ||
                        !literalExpression.IsKind(SyntaxKind.StringLiteralExpression) ||
                        !string.Equals(literalExpression.Token.ValueText, providerName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (argumentList is not null && argumentList.Arguments.Any(argument =>
                            argument.NameEquals?.Name.Identifier.ValueText == FastMoqAnalysisHelpers.RegisterProviderSetAsDefaultPropertyName &&
                            argument.Expression is LiteralExpressionSyntax boolLiteral &&
                            boolLiteral.IsKind(SyntaxKind.TrueLiteralExpression)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static AttributeListSyntax CreateAssemblyDefaultProviderAttribute(string providerName)
        {
            return SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(
                            SyntaxFactory.IdentifierName("FastMoqDefaultProvider"),
                            SyntaxFactory.AttributeArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.AttributeArgument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            SyntaxFactory.Literal(providerName))))))))
                .WithTarget(SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.AssemblyKeyword)));
        }

        private static AttributeListSyntax CreateAssemblyRegisteredDefaultProviderAttribute(string providerName)
        {
            return SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(
                            SyntaxFactory.IdentifierName("FastMoqRegisterProvider"),
                            SyntaxFactory.AttributeArgumentList(
                                SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(
                                [
                                    SyntaxFactory.AttributeArgument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            SyntaxFactory.Literal(providerName))),
                                    SyntaxFactory.AttributeArgument(
                                        SyntaxFactory.TypeOfExpression(
                                            SyntaxFactory.IdentifierName(FastMoqAnalysisHelpers.MoqProviderTypeName))),
                                    SyntaxFactory.AttributeArgument(
                                        SyntaxFactory.NameEquals(
                                            SyntaxFactory.IdentifierName(FastMoqAnalysisHelpers.RegisterProviderSetAsDefaultPropertyName)),
                                        null,
                                        SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)),
                                ])))))
                .WithTarget(SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.AssemblyKeyword)));
        }

        private static SyntaxNode AddUsingDirectiveIfMissing(SyntaxNode root, string namespaceName)
        {
            if (root is CompilationUnitSyntax compilationUnit && !compilationUnit.Usings.Any(@using => @using.Name?.ToString() == namespaceName))
            {
                return compilationUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName)));
            }

            return root;
        }

        private static SyntaxNode AddUsingDirectivesIfMissing(SyntaxNode root, IReadOnlyList<string> namespaceNames)
        {
            foreach (var namespaceName in namespaceNames)
            {
                root = AddUsingDirectiveIfMissing(root, namespaceName);
            }

            return root;
        }

        private delegate string? ReplacementBuilder(Document document, SemanticModel semanticModel, SyntaxNode syntaxNode, CancellationToken cancellationToken);
    }
}