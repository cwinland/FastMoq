using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
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
            DiagnosticIds.RequireExplicitMoqOnboarding,
            DiagnosticIds.PreferProviderNeutralHttpHelpers);

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

                case DiagnosticIds.PreferProviderNeutralHttpHelpers:
                    {
                        var invocationExpression = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>();
                        if (invocationExpression is null)
                        {
                            return;
                        }

                        var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                        if (semanticModel is null ||
                            !TryBuildProviderNeutralHttpHelperEdit(invocationExpression, semanticModel, context.CancellationToken, out var edit))
                        {
                            return;
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                edit.CodeActionTitle,
                                cancellationToken => ReplaceProviderNeutralHttpHelperInvocationAsync(document, invocationExpression, cancellationToken),
                                nameof(DiagnosticIds.PreferProviderNeutralHttpHelpers)),
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

        private static async Task<Document> ReplaceProviderNeutralHttpHelperInvocationAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null ||
                !TryBuildProviderNeutralHttpHelperEdit(invocationExpression, semanticModel, cancellationToken, out var edit))
            {
                return document;
            }

            var setupAnnotation = new SyntaxAnnotation();
            var removalAnnotation = edit.TrackedMockDeclarationToRemove is null ? null : new SyntaxAnnotation();
            var clientAnnotations = edit.HttpClientCreations.Select(_ => new SyntaxAnnotation()).ToArray();

            var nodesToAnnotate = new List<SyntaxNode>
            {
                edit.SetupStatement,
            };
            if (edit.TrackedMockDeclarationToRemove is not null)
            {
                nodesToAnnotate.Add(edit.TrackedMockDeclarationToRemove);
            }

            nodesToAnnotate.AddRange(edit.HttpClientCreations.Select(item => item.TargetExpression));

            var clientIndex = 0;
            var updatedRoot = root.ReplaceNodes(
                nodesToAnnotate,
                (originalNode, rewrittenNode) =>
                {
                    if (originalNode == edit.SetupStatement)
                    {
                        return rewrittenNode.WithAdditionalAnnotations(setupAnnotation);
                    }

                    if (edit.TrackedMockDeclarationToRemove is not null && originalNode == edit.TrackedMockDeclarationToRemove)
                    {
                        return rewrittenNode.WithAdditionalAnnotations(removalAnnotation!);
                    }

                    return rewrittenNode.WithAdditionalAnnotations(clientAnnotations[clientIndex++]);
                });

            if (removalAnnotation is not null)
            {
                var annotatedRemovalNode = updatedRoot.GetAnnotatedNodes(removalAnnotation).Single();
                updatedRoot = updatedRoot.RemoveNode(annotatedRemovalNode, SyntaxRemoveOptions.KeepExteriorTrivia) ?? updatedRoot;
            }

            var annotatedSetupStatement = updatedRoot.GetAnnotatedNodes(setupAnnotation).Single();
            var replacementStatement = SyntaxFactory.ParseStatement(edit.SetupReplacementText)
                .WithTriviaFrom(annotatedSetupStatement);
            updatedRoot = updatedRoot.ReplaceNode(annotatedSetupStatement, replacementStatement);

            for (var index = 0; index < clientAnnotations.Length; index++)
            {
                var annotatedClientExpression = updatedRoot.GetAnnotatedNodes(clientAnnotations[index]).Single();
                var replacementExpression = SyntaxFactory.ParseExpression(edit.HttpClientCreations[index].ReplacementText)
                    .WithTriviaFrom(annotatedClientExpression);
                updatedRoot = updatedRoot.ReplaceNode(annotatedClientExpression, replacementExpression);
            }

            updatedRoot = AddUsingDirectiveIfMissing(updatedRoot, "FastMoq.Extensions");
            return document.WithSyntaxRoot(updatedRoot);
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

        private static bool TryBuildProviderNeutralHttpHelperEdit(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out ProviderNeutralHttpHelperEdit edit)
        {
            edit = default;

            if (!FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            if (method.Name != "Setup" ||
                method.ContainingNamespace.ToDisplayString() != "Moq.Protected" ||
                method.ContainingType.Name != "IProtectedMock" ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess ||
                !FastMoqAnalysisHelpers.TryResolveProtectedTrackedMockOrigin(memberAccess.Expression, semanticModel, cancellationToken, out var origin) ||
                origin.ServiceType.ToDisplayString() != "System.Net.Http.HttpMessageHandler" ||
                invocationExpression.ArgumentList.Arguments.Count < 2 ||
                semanticModel.GetConstantValue(invocationExpression.ArgumentList.Arguments[0].Expression, cancellationToken) is not { HasValue: true, Value: string protectedMemberName } ||
                protectedMemberName != "SendAsync")
            {
                return false;
            }

            var topLevelInvocation = GetOutermostInvocation(invocationExpression);
            var setupStatement = topLevelInvocation.FirstAncestorOrSelf<ExpressionStatementSyntax>();
            if (setupStatement is null ||
                !TryFindReturnsInvocation(topLevelInvocation, out var returnsInvocation) ||
                !TryBuildProviderNeutralHttpSetupReplacement(origin, invocationExpression.ArgumentList.Arguments[1].Expression, returnsInvocation, semanticModel, cancellationToken, out var setupReplacementText, out var codeActionTitle))
            {
                return false;
            }

            var httpClientCreations = FindHttpClientCreationEdits(origin, setupStatement, semanticModel, cancellationToken);
            _ = TryGetTrackedMockDeclarationToRemove(origin, setupStatement, httpClientCreations.Count, semanticModel, cancellationToken, out var declarationToRemove);

            edit = new ProviderNeutralHttpHelperEdit(setupStatement, setupReplacementText, codeActionTitle, httpClientCreations, declarationToRemove);
            return true;
        }

        private static InvocationExpressionSyntax GetOutermostInvocation(InvocationExpressionSyntax invocationExpression)
        {
            while (invocationExpression.Parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Parent is InvocationExpressionSyntax parentInvocation)
            {
                invocationExpression = parentInvocation;
            }

            return invocationExpression;
        }

        private static bool TryFindReturnsInvocation(InvocationExpressionSyntax invocationExpression, out InvocationExpressionSyntax returnsInvocation)
        {
            returnsInvocation = invocationExpression;
            while (true)
            {
                if (returnsInvocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    if (memberAccess.Name.Identifier.ValueText is "Returns" or "ReturnsAsync")
                    {
                        return true;
                    }

                    if (memberAccess.Expression is InvocationExpressionSyntax innerInvocation)
                    {
                        returnsInvocation = innerInvocation;
                        continue;
                    }
                }

                returnsInvocation = null!;
                return false;
            }
        }

        private static bool TryBuildProviderNeutralHttpSetupReplacement(TrackedMockOrigin origin, ExpressionSyntax requestMatcherExpression, InvocationExpressionSyntax returnsInvocation, SemanticModel semanticModel, CancellationToken cancellationToken, out string replacementText, out string codeActionTitle)
        {
            replacementText = string.Empty;
            codeActionTitle = "Use WhenHttpRequest(...)";

            if (!TryBuildRequestMatcher(requestMatcherExpression, semanticModel, cancellationToken, out var predicateText, out var methodText, out var requestUriText) ||
                !TryBuildResponseFactory(returnsInvocation, semanticModel, cancellationToken, out var responseFactoryText, out var jsonPayloadText, out var statusCodeText))
            {
                return false;
            }

            var mockerText = origin.MockerExpression.ToString();
            if (methodText is not null && requestUriText is not null && jsonPayloadText is not null)
            {
                codeActionTitle = "Use WhenHttpRequestJson(...)";
                replacementText = statusCodeText == "HttpStatusCode.OK" || statusCodeText == "System.Net.HttpStatusCode.OK"
                    ? $"{mockerText}.WhenHttpRequestJson({methodText}, {requestUriText}, {jsonPayloadText});"
                    : $"{mockerText}.WhenHttpRequestJson({methodText}, {requestUriText}, {jsonPayloadText}, {statusCodeText});";
                return true;
            }

            if (methodText is not null && requestUriText is not null)
            {
                replacementText = $"{mockerText}.WhenHttpRequest({methodText}, {requestUriText}, {responseFactoryText});";
                return true;
            }

            replacementText = $"{mockerText}.WhenHttpRequest({predicateText}, {responseFactoryText});";
            return true;
        }

        private static bool TryBuildRequestMatcher(ExpressionSyntax requestMatcherExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string predicateText, out string? methodText, out string? requestUriText)
        {
            predicateText = string.Empty;
            methodText = null;
            requestUriText = null;

            requestMatcherExpression = UnwrapForPatternMatching(requestMatcherExpression);
            if (requestMatcherExpression is not InvocationExpressionSyntax matcherInvocation ||
                !FastMoqAnalysisHelpers.TryGetMethodSymbol(matcherInvocation, semanticModel, cancellationToken, out var matcherMethod) ||
                matcherMethod is null)
            {
                return false;
            }

            matcherMethod = matcherMethod.ReducedFrom ?? matcherMethod;
            if (matcherMethod.Name == "IsAny" && matcherMethod.ContainingType.Name == "ItExpr")
            {
                predicateText = "_ => true";
                return true;
            }

            if (matcherMethod.Name != "Is" || matcherMethod.ContainingType.Name != "ItExpr" || matcherInvocation.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            var predicateExpression = matcherInvocation.ArgumentList.Arguments[0].Expression;
            if (predicateExpression is not AnonymousFunctionExpressionSyntax anonymousFunction)
            {
                return false;
            }

            predicateText = anonymousFunction.ToString();
            _ = TryExtractRequestMethodAndUri(anonymousFunction, semanticModel, cancellationToken, out methodText, out requestUriText);
            return true;
        }

        private static bool TryBuildResponseFactory(InvocationExpressionSyntax returnsInvocation, SemanticModel semanticModel, CancellationToken cancellationToken, out string responseFactoryText, out string? jsonPayloadText, out string statusCodeText)
        {
            responseFactoryText = string.Empty;
            jsonPayloadText = null;
            statusCodeText = "HttpStatusCode.OK";

            if (returnsInvocation.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            var responseExpression = returnsInvocation.ArgumentList.Arguments[0].Expression;
            if (responseExpression is AnonymousFunctionExpressionSyntax anonymousFunction)
            {
                responseFactoryText = anonymousFunction.ToString();
                if (!TryGetAnonymousFunctionReturnExpression(anonymousFunction, out var returnedExpression))
                {
                    return true;
                }

                return TryExtractJsonResponse(returnedExpression, semanticModel, cancellationToken, out jsonPayloadText, out statusCodeText) || true;
            }

            var convertedType = semanticModel.GetTypeInfo(responseExpression, cancellationToken).ConvertedType as INamedTypeSymbol;
            if (convertedType?.DelegateInvokeMethod?.ReturnType.ToDisplayString() == "System.Net.Http.HttpResponseMessage")
            {
                responseFactoryText = responseExpression.ToString();
                return true;
            }

            responseFactoryText = $"() => {responseExpression}";
            _ = TryExtractJsonResponse(responseExpression, semanticModel, cancellationToken, out jsonPayloadText, out statusCodeText);
            return true;
        }

        private static bool TryExtractJsonResponse(ExpressionSyntax responseExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string? jsonPayloadText, out string statusCodeText)
        {
            jsonPayloadText = null;
            statusCodeText = "HttpStatusCode.OK";
            responseExpression = UnwrapForPatternMatching(responseExpression);
            if (responseExpression is not ObjectCreationExpressionSyntax responseCreation ||
                semanticModel.GetTypeInfo(responseCreation, cancellationToken).Type?.ToDisplayString() != "System.Net.Http.HttpResponseMessage")
            {
                return false;
            }

            if (responseCreation.ArgumentList?.Arguments.Count > 1)
            {
                return false;
            }

            if (responseCreation.ArgumentList?.Arguments.Count == 1)
            {
                statusCodeText = responseCreation.ArgumentList.Arguments[0].Expression.ToString();
            }

            ObjectCreationExpressionSyntax? stringContentCreation = null;
            if (responseCreation.Initializer is null)
            {
                return false;
            }

            foreach (var initializerExpression in responseCreation.Initializer.Expressions)
            {
                if (initializerExpression is not AssignmentExpressionSyntax assignmentExpression)
                {
                    return false;
                }

                var propertyName = assignmentExpression.Left switch
                {
                    IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
                    MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                    _ => string.Empty,
                };

                if (propertyName == "StatusCode")
                {
                    statusCodeText = assignmentExpression.Right.ToString();
                    continue;
                }

                if (propertyName == "Content")
                {
                    stringContentCreation = UnwrapForPatternMatching(assignmentExpression.Right) as ObjectCreationExpressionSyntax;
                    continue;
                }

                return false;
            }

            if (stringContentCreation is null ||
                semanticModel.GetTypeInfo(stringContentCreation, cancellationToken).Type?.ToDisplayString() != "System.Net.Http.StringContent" ||
                stringContentCreation.ArgumentList?.Arguments.Count != 3)
            {
                return false;
            }

            var mediaTypeExpression = stringContentCreation.ArgumentList.Arguments[2].Expression;
            var mediaTypeConstant = semanticModel.GetConstantValue(mediaTypeExpression, cancellationToken);
            if (!mediaTypeConstant.HasValue || !string.Equals(mediaTypeConstant.Value as string, "application/json", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            jsonPayloadText = stringContentCreation.ArgumentList.Arguments[0].Expression.ToString();
            return true;
        }

        private static bool TryExtractRequestMethodAndUri(AnonymousFunctionExpressionSyntax anonymousFunction, SemanticModel semanticModel, CancellationToken cancellationToken, out string? methodText, out string? requestUriText)
        {
            methodText = null;
            requestUriText = null;

            if (!TryGetAnonymousFunctionReturnExpression(anonymousFunction, out var predicateBodyExpression))
            {
                return false;
            }

            foreach (var condition in GetLogicalAndConditions(predicateBodyExpression))
            {
                if (methodText is null && TryExtractMethodCondition(condition, semanticModel, cancellationToken, out var methodExpressionText))
                {
                    methodText = methodExpressionText;
                }

                if (requestUriText is null && TryExtractRequestUriCondition(condition, semanticModel, cancellationToken, out var requestUriExpressionText))
                {
                    requestUriText = requestUriExpressionText;
                }
            }

            return methodText is not null && requestUriText is not null;
        }

        private static bool TryExtractMethodCondition(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, out string methodExpressionText)
        {
            methodExpressionText = string.Empty;
            expression = UnwrapForPatternMatching(expression);
            if (expression is not BinaryExpressionSyntax binaryExpression || !binaryExpression.IsKind(SyntaxKind.EqualsExpression))
            {
                return false;
            }

            if (IsHttpRequestMethodAccess(binaryExpression.Left, semanticModel, cancellationToken))
            {
                methodExpressionText = binaryExpression.Right.ToString();
                return true;
            }

            if (IsHttpRequestMethodAccess(binaryExpression.Right, semanticModel, cancellationToken))
            {
                methodExpressionText = binaryExpression.Left.ToString();
                return true;
            }

            return false;
        }

        private static bool TryExtractRequestUriCondition(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, out string requestUriExpressionText)
        {
            requestUriExpressionText = string.Empty;
            expression = UnwrapForPatternMatching(expression);
            if (expression is not BinaryExpressionSyntax binaryExpression || !binaryExpression.IsKind(SyntaxKind.EqualsExpression))
            {
                return false;
            }

            if (IsHttpRequestUriAccess(binaryExpression.Left, semanticModel, cancellationToken) &&
                TryExtractUriStringExpression(binaryExpression.Right, semanticModel, cancellationToken, out requestUriExpressionText))
            {
                return true;
            }

            if (IsHttpRequestUriAccess(binaryExpression.Right, semanticModel, cancellationToken) &&
                TryExtractUriStringExpression(binaryExpression.Left, semanticModel, cancellationToken, out requestUriExpressionText))
            {
                return true;
            }

            if (IsHttpRequestAbsoluteUriAccess(binaryExpression.Left, semanticModel, cancellationToken) &&
                semanticModel.GetTypeInfo(binaryExpression.Right, cancellationToken).Type?.SpecialType == SpecialType.System_String)
            {
                requestUriExpressionText = binaryExpression.Right.ToString();
                return true;
            }

            if (IsHttpRequestAbsoluteUriAccess(binaryExpression.Right, semanticModel, cancellationToken) &&
                semanticModel.GetTypeInfo(binaryExpression.Left, cancellationToken).Type?.SpecialType == SpecialType.System_String)
            {
                requestUriExpressionText = binaryExpression.Left.ToString();
                return true;
            }

            return false;
        }

        private static bool TryExtractUriStringExpression(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken, out string requestUriExpressionText)
        {
            requestUriExpressionText = string.Empty;
            expression = UnwrapForPatternMatching(expression);
            if (expression is ObjectCreationExpressionSyntax uriCreation &&
                semanticModel.GetTypeInfo(uriCreation, cancellationToken).Type?.ToDisplayString() == "System.Uri" &&
                uriCreation.ArgumentList?.Arguments.Count == 1)
            {
                requestUriExpressionText = uriCreation.ArgumentList.Arguments[0].Expression.ToString();
                return true;
            }

            return false;
        }

        private static bool IsHttpRequestMethodAccess(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            expression = UnwrapForPatternMatching(expression);
            return expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.ValueText == "Method" &&
                semanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken).Type?.ToDisplayString() == "System.Net.Http.HttpRequestMessage";
        }

        private static bool IsHttpRequestUriAccess(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            expression = UnwrapForPatternMatching(expression);
            return expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.ValueText == "RequestUri" &&
                semanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken).Type?.ToDisplayString() == "System.Net.Http.HttpRequestMessage";
        }

        private static bool IsHttpRequestAbsoluteUriAccess(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            expression = UnwrapForPatternMatching(expression);
            if (expression is MemberAccessExpressionSyntax memberAccess && memberAccess.Name.Identifier.ValueText == "AbsoluteUri")
            {
                return IsHttpRequestUriAccess(memberAccess.Expression, semanticModel, cancellationToken);
            }

            if (expression is InvocationExpressionSyntax invocationExpression &&
                invocationExpression.Expression is MemberAccessExpressionSyntax toStringAccess &&
                toStringAccess.Name.Identifier.ValueText == "ToString")
            {
                return IsHttpRequestUriAccess(toStringAccess.Expression, semanticModel, cancellationToken);
            }

            return false;
        }

        private static bool TryGetAnonymousFunctionReturnExpression(AnonymousFunctionExpressionSyntax anonymousFunction, out ExpressionSyntax expression)
        {
            if (anonymousFunction.Body is ExpressionSyntax expressionBody)
            {
                expression = expressionBody;
                return true;
            }

            if (anonymousFunction.Body is BlockSyntax block &&
                block.Statements.Count == 1 &&
                block.Statements[0] is ReturnStatementSyntax returnStatement &&
                returnStatement.Expression is not null)
            {
                expression = returnStatement.Expression;
                return true;
            }

            expression = null!;
            return false;
        }

        private static IEnumerable<ExpressionSyntax> GetLogicalAndConditions(ExpressionSyntax expression)
        {
            expression = UnwrapForPatternMatching(expression);
            if (expression is BinaryExpressionSyntax binaryExpression && binaryExpression.IsKind(SyntaxKind.LogicalAndExpression))
            {
                foreach (var leftCondition in GetLogicalAndConditions(binaryExpression.Left))
                {
                    yield return leftCondition;
                }

                foreach (var rightCondition in GetLogicalAndConditions(binaryExpression.Right))
                {
                    yield return rightCondition;
                }

                yield break;
            }

            yield return expression;
        }

        private static ExpressionSyntax UnwrapForPatternMatching(ExpressionSyntax expression)
        {
            expression = FastMoqAnalysisHelpers.Unwrap(expression);
            while (expression is PostfixUnaryExpressionSyntax postfixUnaryExpression &&
                postfixUnaryExpression.IsKind(SyntaxKind.SuppressNullableWarningExpression))
            {
                expression = FastMoqAnalysisHelpers.Unwrap(postfixUnaryExpression.Operand);
            }

            return expression;
        }

        private static List<HttpClientCreationEdit> FindHttpClientCreationEdits(TrackedMockOrigin origin, StatementSyntax setupStatement, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var edits = new List<HttpClientCreationEdit>();
            var block = setupStatement.FirstAncestorOrSelf<BlockSyntax>();
            if (block is null)
            {
                return edits;
            }

            foreach (var objectCreation in block.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                if (TryBuildCreateHttpClientReplacement(origin, objectCreation, semanticModel, cancellationToken, out var replacementText))
                {
                    edits.Add(new HttpClientCreationEdit(objectCreation, replacementText));
                }
            }

            return edits;
        }

        private static bool TryBuildCreateHttpClientReplacement(TrackedMockOrigin origin, ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, CancellationToken cancellationToken, out string replacementText)
        {
            replacementText = string.Empty;
            if (semanticModel.GetTypeInfo(objectCreation, cancellationToken).Type?.ToDisplayString() != "System.Net.Http.HttpClient" ||
                objectCreation.ArgumentList?.Arguments.Count != 1 ||
                !IsTrackedHttpHandlerObjectReference(objectCreation.ArgumentList.Arguments[0].Expression, origin, semanticModel, cancellationToken))
            {
                return false;
            }

            if (objectCreation.Initializer is null || objectCreation.Initializer.Expressions.Count == 0)
            {
                replacementText = $"{origin.MockerExpression}.CreateHttpClient()";
                return true;
            }

            if (TryExtractBaseAddressInitializer(objectCreation.Initializer, semanticModel, cancellationToken, out var baseAddressText))
            {
                replacementText = $"{origin.MockerExpression}.CreateHttpClient(baseAddress: {baseAddressText})";
                return true;
            }

            return false;
        }

        private static bool IsTrackedHttpHandlerObjectReference(ExpressionSyntax expression, TrackedMockOrigin expectedOrigin, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            expression = UnwrapForPatternMatching(expression);
            if (expression is not MemberAccessExpressionSyntax memberAccess || memberAccess.Name.Identifier.ValueText != "Object" ||
                !FastMoqAnalysisHelpers.TryResolveTrackedMockOrigin(memberAccess.Expression, semanticModel, cancellationToken, out var actualOrigin))
            {
                return false;
            }

            return actualOrigin.Kind == expectedOrigin.Kind &&
                SymbolEqualityComparer.Default.Equals(actualOrigin.ServiceType, expectedOrigin.ServiceType) &&
                actualOrigin.MockerExpression.ToString() == expectedOrigin.MockerExpression.ToString() &&
                actualOrigin.TrackedMockExpression.ToString() == expectedOrigin.TrackedMockExpression.ToString();
        }

        private static bool TryExtractBaseAddressInitializer(InitializerExpressionSyntax initializerExpression, SemanticModel semanticModel, CancellationToken cancellationToken, out string baseAddressText)
        {
            baseAddressText = string.Empty;
            if (initializerExpression.Expressions.Count != 1 || initializerExpression.Expressions[0] is not AssignmentExpressionSyntax assignmentExpression)
            {
                return false;
            }

            var propertyName = assignmentExpression.Left switch
            {
                IdentifierNameSyntax identifierName => identifierName.Identifier.ValueText,
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                _ => string.Empty,
            };

            if (propertyName != "BaseAddress")
            {
                return false;
            }

            var rightExpression = UnwrapForPatternMatching(assignmentExpression.Right);
            if (rightExpression is not ObjectCreationExpressionSyntax uriCreation ||
                semanticModel.GetTypeInfo(uriCreation, cancellationToken).Type?.ToDisplayString() != "System.Uri" ||
                uriCreation.ArgumentList?.Arguments.Count != 1)
            {
                return false;
            }

            baseAddressText = uriCreation.ArgumentList.Arguments[0].Expression.ToString();
            return true;
        }

        private static bool TryGetTrackedMockDeclarationToRemove(TrackedMockOrigin origin, StatementSyntax setupStatement, int httpClientReplacementCount, SemanticModel semanticModel, CancellationToken cancellationToken, out LocalDeclarationStatementSyntax? declarationToRemove)
        {
            declarationToRemove = null;
            if (origin.TrackedMockExpression is not IdentifierNameSyntax identifierName ||
                semanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol is not ILocalSymbol localSymbol ||
                localSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) is not VariableDeclaratorSyntax variableDeclarator ||
                variableDeclarator.Parent?.Parent is not LocalDeclarationStatementSyntax localDeclarationStatement)
            {
                return false;
            }

            var containingBlock = setupStatement.FirstAncestorOrSelf<BlockSyntax>();
            if (containingBlock is null)
            {
                return false;
            }

            var referenceCount = containingBlock.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Count(candidate => SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(candidate, cancellationToken).Symbol, localSymbol));

            if (referenceCount != 1 + httpClientReplacementCount)
            {
                return false;
            }

            declarationToRemove = localDeclarationStatement;
            return true;
        }

        private readonly struct ProviderNeutralHttpHelperEdit
        {
            public ProviderNeutralHttpHelperEdit(StatementSyntax setupStatement, string setupReplacementText, string codeActionTitle, IReadOnlyList<HttpClientCreationEdit> httpClientCreations, LocalDeclarationStatementSyntax? trackedMockDeclarationToRemove)
            {
                SetupStatement = setupStatement;
                SetupReplacementText = setupReplacementText;
                CodeActionTitle = codeActionTitle;
                HttpClientCreations = httpClientCreations;
                TrackedMockDeclarationToRemove = trackedMockDeclarationToRemove;
            }

            public StatementSyntax SetupStatement { get; }

            public string SetupReplacementText { get; }

            public string CodeActionTitle { get; }

            public IReadOnlyList<HttpClientCreationEdit> HttpClientCreations { get; }

            public LocalDeclarationStatementSyntax? TrackedMockDeclarationToRemove { get; }
        }

        private readonly struct HttpClientCreationEdit
        {
            public HttpClientCreationEdit(ExpressionSyntax targetExpression, string replacementText)
            {
                TargetExpression = targetExpression;
                ReplacementText = replacementText;
            }

            public ExpressionSyntax TargetExpression { get; }

            public string ReplacementText { get; }
        }

        private delegate string? ReplacementBuilder(Document document, SemanticModel semanticModel, SyntaxNode syntaxNode, CancellationToken cancellationToken);
    }
}