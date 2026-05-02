using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FastMoq.Analyzers.CodeFixes
{
    /// <summary>
    /// Provides Roslyn code fixes for FastMoq migration diagnostics.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FastMoqMigrationCodeFixProvider)), Shared]
    public sealed class FastMoqMigrationCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc />
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticIds.UseProviderFirstObjectAccess,
            DiagnosticIds.UseProviderFirstReset,
            DiagnosticIds.UseVerifyLogged,
            DiagnosticIds.PreferSetupLoggerCallbackHelper,
            DiagnosticIds.DirectMockerTestBaseInheritance,
            DiagnosticIds.UseProviderFirstVerify,
            DiagnosticIds.AvoidFastMockVerifyHelperWrappers,
            DiagnosticIds.AvoidProviderSpecificFastMockVerifyHelperWrappers,
            DiagnosticIds.UseFastArgMatcherInProviderFirstVerify,
            DiagnosticIds.UseConsistentMockRetrieval,
            DiagnosticIds.UseProviderFirstMockRetrieval,
            DiagnosticIds.PreferTypedProviderExtensions,
            DiagnosticIds.PreferWebTestHelpers,
            DiagnosticIds.PreferTypedServiceProviderHelpers,
            DiagnosticIds.PreferFunctionContextExecutionHelpers,
            DiagnosticIds.UseExplicitOptionalParameterResolution,
            DiagnosticIds.ReplaceInitializeCompatibilityWrapper,
            DiagnosticIds.PreferSetupOptionsHelper,
            DiagnosticIds.PreferPropertySetterCaptureHelper,
            DiagnosticIds.PreferPropertyStateHelper,
            DiagnosticIds.RequireExplicitMoqOnboarding,
            DiagnosticIds.PreferProviderNeutralHttpHelpers);

        /// <inheritdoc />
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc />
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
                                DiagnosticDescriptors.UseProviderFirstObjectAccess.Title.ToString(),
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

                case DiagnosticIds.PreferSetupLoggerCallbackHelper:
                    {
                        var invocationExpression = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>();
                        if (invocationExpression is null)
                        {
                            return;
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Use SetupLoggerCallback(...)",
                                cancellationToken => ReplaceSetupLoggerCallbackInvocationAsync(document, invocationExpression, cancellationToken),
                                nameof(DiagnosticIds.PreferSetupLoggerCallbackHelper)),
                            diagnostic);
                        break;
                    }

                case DiagnosticIds.DirectMockerTestBaseInheritance:
                    {
                        var classDeclaration = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<ClassDeclarationSyntax>();
                        if (classDeclaration is null)
                        {
                            return;
                        }

                        var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                        if (semanticModel is null ||
                            !TryBuildDirectMockerTestBaseInheritanceFix(classDeclaration, semanticModel, context.CancellationToken, out _))
                        {
                            return;
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Use direct MockerTestBase inheritance",
                                cancellationToken => ReplaceDirectMockerTestBaseInheritanceAsync(document, classDeclaration, cancellationToken),
                                nameof(DiagnosticIds.DirectMockerTestBaseInheritance)),
                            diagnostic);
                        break;
                    }

                case DiagnosticIds.UseProviderFirstVerify:
                    {
                        var invocationExpression = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>();
                        if (invocationExpression is null)
                        {
                            return;
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Use provider-first Verify(...)",
                                cancellationToken => ReplaceVerifyInvocationAsync(document, invocationExpression, cancellationToken),
                                nameof(DiagnosticIds.UseProviderFirstVerify)),
                            diagnostic);
                        break;
                    }

                case DiagnosticIds.AvoidFastMockVerifyHelperWrappers:
                case DiagnosticIds.AvoidProviderSpecificFastMockVerifyHelperWrappers:
                    {
                        var invocationExpression = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>();
                        if (invocationExpression is null)
                        {
                            return;
                        }

                        var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                        if (semanticModel is null ||
                            !FastMoqAnalysisHelpers.TryBuildFastMockVerifyWrapperUsageReplacement(invocationExpression, semanticModel, context.CancellationToken, out _, out _))
                        {
                            return;
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Use explicit FastMoq verification",
                                cancellationToken => ReplaceFastMockVerifyWrapperInvocationAsync(document, invocationExpression, cancellationToken),
                                diagnostic.Id),
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

                case DiagnosticIds.UseFastArgMatcherInProviderFirstVerify:
                    {
                        var invocationExpression = FindInvocationExpression(root, diagnostic.Location.SourceSpan);
                        if (invocationExpression is null)
                        {
                            return;
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Use FastArg matcher",
                                cancellationToken => ReplaceFastArgMatcherAsync(document, invocationExpression, cancellationToken),
                                nameof(DiagnosticIds.UseFastArgMatcherInProviderFirstVerify)),
                            diagnostic);
                        break;
                    }

                case DiagnosticIds.PreferTypedProviderExtensions:
                    {
                        var invocationExpression = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>();
                        if (invocationExpression is not null)
                        {
                            var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                            if (semanticModel is not null &&
                                FastMoqAnalysisHelpers.TryBuildTypedProviderExtensionReplacement(invocationExpression, semanticModel, context.CancellationToken, out _))
                            {
                                context.RegisterCodeFix(
                                    CodeAction.Create(
                                        "Use typed provider extension",
                                        cancellationToken => ReplaceTypedProviderExtensionInvocationAsync(document, invocationExpression, cancellationToken),
                                        nameof(DiagnosticIds.PreferTypedProviderExtensions) + ".invocation"),
                                    diagnostic);
                                break;
                            }
                        }

                        var memberAccess = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<MemberAccessExpressionSyntax>();
                        if (memberAccess is null)
                        {
                            return;
                        }

                        var propertySemanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                        if (propertySemanticModel is not null &&
                            FastMoqAnalysisHelpers.TryBuildTypedProviderExtensionReplacement(memberAccess, propertySemanticModel, context.CancellationToken, out _))
                        {
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Use typed provider extension",
                                    cancellationToken => ReplaceTypedProviderExtensionMemberAccessAsync(document, memberAccess, cancellationToken),
                                    nameof(DiagnosticIds.PreferTypedProviderExtensions) + ".property"),
                                diagnostic);
                        }

                        break;
                    }

                case DiagnosticIds.PreferWebTestHelpers:
                    {
                        var invocationExpression = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>();
                        if (invocationExpression is not null)
                        {
                            var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                            if (semanticModel is not null &&
                                FastMoqAnalysisHelpers.TryBuildWebHelperInvocationReplacement(invocationExpression, semanticModel, context.CancellationToken, out _))
                            {
                                context.RegisterCodeFix(
                                    CodeAction.Create(
                                        "Use FastMoq.Web helper",
                                        cancellationToken => ReplaceWebHelperInvocationAsync(document, invocationExpression, cancellationToken),
                                        nameof(DiagnosticIds.PreferWebTestHelpers) + ".invocation"),
                                    diagnostic);
                                break;
                            }
                        }

                        var assignmentExpression = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<AssignmentExpressionSyntax>();
                        if (assignmentExpression is null)
                        {
                            return;
                        }

                        var assignmentSemanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                        if (assignmentSemanticModel is not null &&
                            FastMoqAnalysisHelpers.TryBuildWebHelperRequestBodyReplacement(assignmentExpression, assignmentSemanticModel, context.CancellationToken, out _, out _, out _))
                        {
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Use FastMoq.Web request-body helper",
                                    cancellationToken => ReplaceWebHelperRequestBodyAssignmentAsync(document, assignmentExpression, cancellationToken),
                                    nameof(DiagnosticIds.PreferWebTestHelpers) + ".requestBody"),
                                diagnostic);
                        }

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

                case DiagnosticIds.PreferFunctionContextExecutionHelpers:
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

                        if (FastMoqAnalysisHelpers.HasFunctionContextInvocationIdMockHelper(semanticModel) &&
                            FastMoqAnalysisHelpers.TryBuildFunctionContextInvocationIdReplacement(invocationExpression, semanticModel, context.CancellationToken, out _, out _))
                        {
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Use AddFunctionContextInvocationId(...)",
                                    cancellationToken => ReplaceFunctionContextInvocationIdInvocationAsync(document, invocationExpression, cancellationToken),
                                    nameof(DiagnosticIds.PreferFunctionContextExecutionHelpers) + ".invocationId"),
                                diagnostic);
                        }

                        break;
                    }

                case DiagnosticIds.PreferPropertySetterCaptureHelper:
                    {
                        var invocationExpression = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>();
                        if (invocationExpression is null)
                        {
                            return;
                        }

                        var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                        if (semanticModel is not null &&
                            FastMoqAnalysisHelpers.TryBuildSetupSetReplacement(invocationExpression, semanticModel, context.CancellationToken, out _))
                        {
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Use AddPropertySetterCapture(...)",
                                    cancellationToken => ReplaceSetupSetInvocationAsync(document, invocationExpression, cancellationToken),
                                    nameof(DiagnosticIds.PreferPropertySetterCaptureHelper)),
                                diagnostic);
                        }

                        break;
                    }

                case DiagnosticIds.PreferPropertyStateHelper:
                    {
                        var invocationExpression = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>();
                        if (invocationExpression is null)
                        {
                            return;
                        }

                        var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                        if (semanticModel is not null &&
                            FastMoqAnalysisHelpers.TryBuildSetupAllPropertiesReplacement(invocationExpression, semanticModel, context.CancellationToken, out _))
                        {
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    "Use AddPropertyState(...)",
                                    cancellationToken => ReplaceSetupAllPropertiesInvocationAsync(document, invocationExpression, cancellationToken),
                                    nameof(DiagnosticIds.PreferPropertyStateHelper)),
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

        private static async Task<Document> ReplaceVerifyInvocationAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess ||
                !FastMoqAnalysisHelpers.TryBuildVerifyReplacement(memberAccess.Expression, semanticModel, invocationExpression, cancellationToken, out var replacementText, out var requiresProvidersNamespace))
            {
                return document;
            }

            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(invocationExpression);
            var updatedRoot = root.ReplaceNode(invocationExpression, replacementExpression);

            if (requiresProvidersNamespace)
            {
                updatedRoot = AddUsingDirectiveIfMissing(updatedRoot, semanticModel.Compilation, FastMoqAnalysisHelpers.FastMoqProvidersNamespace);
            }

            return document.WithSyntaxRoot(updatedRoot);
        }

        private static async Task<Document> ReplaceFastMockVerifyWrapperInvocationAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null ||
                !FastMoqAnalysisHelpers.TryBuildFastMockVerifyWrapperUsageReplacement(invocationExpression, semanticModel, cancellationToken, out var replacementText, out var requiresProvidersNamespace))
            {
                return document;
            }

            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(invocationExpression);
            var updatedRoot = root.ReplaceNode(invocationExpression, replacementExpression);

            if (requiresProvidersNamespace)
            {
                updatedRoot = AddUsingDirectiveIfMissing(updatedRoot, semanticModel.Compilation, FastMoqAnalysisHelpers.FastMoqProvidersNamespace);
            }

            return document.WithSyntaxRoot(updatedRoot);
        }

        private static async Task<Document> ReplaceDirectMockerTestBaseInheritanceAsync(Document document, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null ||
                !TryBuildDirectMockerTestBaseInheritanceFix(classDeclaration, semanticModel, cancellationToken, out var fix))
            {
                return document;
            }

            var outerAnnotation = new SyntaxAnnotation();
            var helperDeclarationAnnotation = new SyntaxAnnotation();
            var helperFieldAnnotation = new SyntaxAnnotation();
            var memberAccessAnnotations = fix.MemberAccessReplacements
                .Select(item => (Replacement: item, Annotation: new SyntaxAnnotation()))
                .ToArray();
            var statementAnnotations = fix.StatementsToRemove
                .Select(item => (Statement: item, Annotation: new SyntaxAnnotation()))
                .ToArray();
            var nodesToAnnotate = new List<SyntaxNode>
            {
                fix.OuterClassDeclaration,
                fix.HelperTypeDeclaration,
                fix.HelperFieldDeclaration,
            };
            nodesToAnnotate.AddRange(memberAccessAnnotations.Select(item => (SyntaxNode) item.Replacement.MemberAccess));
            nodesToAnnotate.AddRange(statementAnnotations.Select(item => (SyntaxNode) item.Statement));

            var updatedRoot = root.ReplaceNodes(
                nodesToAnnotate,
                (originalNode, rewrittenNode) =>
                {
                    if (originalNode == fix.OuterClassDeclaration)
                    {
                        return rewrittenNode.WithAdditionalAnnotations(outerAnnotation);
                    }

                    if (originalNode == fix.HelperTypeDeclaration)
                    {
                        return rewrittenNode.WithAdditionalAnnotations(helperDeclarationAnnotation);
                    }

                    if (originalNode == fix.HelperFieldDeclaration)
                    {
                        return rewrittenNode.WithAdditionalAnnotations(helperFieldAnnotation);
                    }

                    var memberAccessAnnotation = memberAccessAnnotations.SingleOrDefault(item => item.Replacement.MemberAccess == originalNode).Annotation;
                    if (memberAccessAnnotation is not null)
                    {
                        return rewrittenNode.WithAdditionalAnnotations(memberAccessAnnotation);
                    }

                    var statementAnnotation = statementAnnotations.Single(item => item.Statement == originalNode).Annotation;
                    return rewrittenNode.WithAdditionalAnnotations(statementAnnotation);
                });

            foreach (var (replacement, annotation) in memberAccessAnnotations)
            {
                var currentMemberAccess = updatedRoot.GetAnnotatedNodes(annotation).OfType<MemberAccessExpressionSyntax>().SingleOrDefault();
                if (currentMemberAccess is null)
                {
                    continue;
                }

                updatedRoot = updatedRoot.ReplaceNode(
                    currentMemberAccess,
                    SyntaxFactory.ParseExpression(replacement.ReplacementText).WithTriviaFrom(currentMemberAccess));
            }

            foreach (var (_, annotation) in statementAnnotations)
            {
                var currentStatement = updatedRoot.GetAnnotatedNodes(annotation).OfType<StatementSyntax>().SingleOrDefault();
                if (currentStatement is not null)
                {
                    updatedRoot = updatedRoot.RemoveNode(currentStatement, SyntaxRemoveOptions.KeepExteriorTrivia) ?? updatedRoot;
                }
            }

            var helperDeclarationNode = updatedRoot.GetAnnotatedNodes(helperDeclarationAnnotation).SingleOrDefault();
            if (helperDeclarationNode is not null)
            {
                updatedRoot = updatedRoot.RemoveNode(helperDeclarationNode, SyntaxRemoveOptions.KeepExteriorTrivia) ?? updatedRoot;
            }

            var helperFieldNode = updatedRoot.GetAnnotatedNodes(helperFieldAnnotation).SingleOrDefault();
            if (helperFieldNode is not null)
            {
                updatedRoot = updatedRoot.RemoveNode(helperFieldNode, SyntaxRemoveOptions.KeepExteriorTrivia) ?? updatedRoot;
            }

            var annotatedOuter = (ClassDeclarationSyntax) updatedRoot.GetAnnotatedNodes(outerAnnotation).Single();
            var baseTypes = new SeparatedSyntaxList<BaseTypeSyntax>()
                .Add(
                    SyntaxFactory.SimpleBaseType(
                        SyntaxFactory.ParseTypeName($"MockerTestBase<{fix.TargetTypeName}>")));

            if (annotatedOuter.BaseList is not null)
            {
                foreach (var existingBaseType in annotatedOuter.BaseList.Types)
                {
                    baseTypes = baseTypes.Add(existingBaseType);
                }
            }

            var replacementOuter = annotatedOuter.WithBaseList(
                SyntaxFactory.BaseList(baseTypes));
            updatedRoot = updatedRoot.ReplaceNode(annotatedOuter, replacementOuter);
            updatedRoot = AddUsingDirectiveIfMissing(updatedRoot, semanticModel.Compilation, "FastMoq");

            return document.WithSyntaxRoot(updatedRoot);
        }

        private static async Task<Document> ReplaceFastArgMatcherAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null ||
                !FastMoqAnalysisHelpers.TryBuildFastArgMatcherReplacement(invocationExpression, semanticModel, cancellationToken, out var replacementText))
            {
                return document;
            }

            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(invocationExpression);
            var updatedRoot = root.ReplaceNode(invocationExpression, replacementExpression);
            updatedRoot = AddUsingDirectiveIfMissing(updatedRoot, semanticModel.Compilation, FastMoqAnalysisHelpers.FastMoqProvidersNamespace);
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static async Task<Document> ReplaceTypedProviderExtensionInvocationAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
            {
                return document;
            }

            if (!FastMoqAnalysisHelpers.TryBuildTypedProviderExtensionReplacement(invocationExpression, semanticModel, cancellationToken, out var replacementText))
            {
                return document;
            }

            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(invocationExpression);
            var updatedRoot = root.ReplaceNode(invocationExpression, replacementExpression);
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static async Task<Document> ReplaceTypedProviderExtensionMemberAccessAsync(Document document, MemberAccessExpressionSyntax memberAccessExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
            {
                return document;
            }

            if (!FastMoqAnalysisHelpers.TryBuildTypedProviderExtensionReplacement(memberAccessExpression, semanticModel, cancellationToken, out var replacementText))
            {
                return document;
            }

            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(memberAccessExpression);
            var updatedRoot = root.ReplaceNode(memberAccessExpression, replacementExpression);
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static async Task<Document> ReplaceWebHelperInvocationAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
            {
                return document;
            }

            if (!FastMoqAnalysisHelpers.TryBuildWebHelperInvocationReplacement(invocationExpression, semanticModel, cancellationToken, out var replacementText))
            {
                return document;
            }

            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(invocationExpression);
            var updatedRoot = root.ReplaceNode(invocationExpression, replacementExpression);
            updatedRoot = AddUsingDirectiveIfMissing(updatedRoot, semanticModel.Compilation, "FastMoq.Web.Extensions");
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static async Task<Document> ReplaceWebHelperRequestBodyAssignmentAsync(Document document, AssignmentExpressionSyntax assignmentExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
            {
                return document;
            }

            if (!FastMoqAnalysisHelpers.TryBuildWebHelperRequestBodyReplacement(assignmentExpression, semanticModel, cancellationToken, out var targetStatement, out var replacementText, out var linkedStatementToRemove))
            {
                return document;
            }

            var targetAnnotation = new SyntaxAnnotation();
            var removalAnnotation = linkedStatementToRemove is null ? null : new SyntaxAnnotation();
            var nodesToAnnotate = new List<SyntaxNode>
            {
                targetStatement,
            };

            if (linkedStatementToRemove is not null)
            {
                nodesToAnnotate.Add(linkedStatementToRemove);
            }

            var updatedRoot = root.ReplaceNodes(
                nodesToAnnotate,
                (originalNode, rewrittenNode) =>
                {
                    if (originalNode == targetStatement)
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

            var annotatedTargetStatement = updatedRoot.GetAnnotatedNodes(targetAnnotation).Single();
            var replacementStatement = SyntaxFactory.ParseStatement(replacementText)
                .WithTriviaFrom(annotatedTargetStatement);
            updatedRoot = updatedRoot.ReplaceNode(annotatedTargetStatement, replacementStatement);
            updatedRoot = AddUsingDirectiveIfMissing(updatedRoot, semanticModel.Compilation, "FastMoq.Web.Extensions");
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static async Task<Document> ReplaceSetupSetInvocationAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
            {
                return document;
            }

            if (!FastMoqAnalysisHelpers.TryBuildSetupSetReplacement(invocationExpression, semanticModel, cancellationToken, out var replacementText))
            {
                return document;
            }

            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(invocationExpression);
            var updatedRoot = root.ReplaceNode(invocationExpression, replacementExpression);
            updatedRoot = AddUsingDirectiveIfMissing(updatedRoot, semanticModel.Compilation, "FastMoq.Extensions");
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static async Task<Document> ReplaceSetupAllPropertiesInvocationAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
            {
                return document;
            }

            if (!FastMoqAnalysisHelpers.TryBuildSetupAllPropertiesReplacement(invocationExpression, semanticModel, cancellationToken, out var replacementText))
            {
                return document;
            }

            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(invocationExpression);
            var updatedRoot = root.ReplaceNode(invocationExpression, replacementExpression);
            updatedRoot = AddUsingDirectiveIfMissing(updatedRoot, semanticModel.Compilation, "FastMoq.Extensions");
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

            return FastMoqAnalysisHelpers.BuildResetReplacement(origin, semanticModel, invocationExpression.SpanStart, cancellationToken);
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

        private static string? BuildVerifyReplacementAsync(Document document, SemanticModel semanticModel, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            if (syntaxNode is not InvocationExpressionSyntax invocationExpression ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return null;
            }

            return FastMoqAnalysisHelpers.TryBuildVerifyReplacement(memberAccess.Expression, semanticModel, invocationExpression, cancellationToken, out var replacement, out _)
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

        private static string? BuildLoggerFactoryHelperReplacementAsync(Document document, SemanticModel semanticModel, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            return syntaxNode is InvocationExpressionSyntax invocationExpression &&
                FastMoqAnalysisHelpers.TryBuildLoggerFactoryHelperReplacement(invocationExpression, semanticModel, cancellationToken, out var replacement)
                ? replacement
                : null;
        }

        private static string? BuildSetupLoggerCallbackReplacementAsync(Document document, SemanticModel semanticModel, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            return syntaxNode is InvocationExpressionSyntax invocationExpression &&
                FastMoqAnalysisHelpers.TryBuildSetupLoggerCallbackReplacement(invocationExpression, semanticModel, cancellationToken, out var replacement)
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

            return document.WithSyntaxRoot(AddUsingDirectiveIfMissing(updatedRoot, semanticModel.Compilation, "FastMoq.Extensions"));
        }

        private static async Task<Document> ReplaceLoggerFactoryHelperInvocationAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
            {
                return document;
            }

            var replacementText = BuildLoggerFactoryHelperReplacementAsync(document, semanticModel, invocationExpression, cancellationToken);
            if (replacementText is null)
            {
                return document;
            }

            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(invocationExpression);
            var updatedRoot = root.ReplaceNode(invocationExpression, replacementExpression);

            return document.WithSyntaxRoot(AddUsingDirectiveIfMissing(updatedRoot, semanticModel.Compilation, "FastMoq.Extensions"));
        }

        private static async Task<Document> ReplaceSetupLoggerCallbackInvocationAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
            {
                return document;
            }

            var replacementText = BuildSetupLoggerCallbackReplacementAsync(document, semanticModel, invocationExpression, cancellationToken);
            if (replacementText is null)
            {
                return document;
            }

            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(invocationExpression);
            var updatedRoot = root.ReplaceNode(invocationExpression, replacementExpression);

            return document.WithSyntaxRoot(AddUsingDirectiveIfMissing(updatedRoot, semanticModel.Compilation, "FastMoq.Extensions"));
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
            var replacementStatements = ParseReplacementStatements(edit.SetupReplacementStatements, (StatementSyntax) annotatedSetupStatement);
            if (replacementStatements.Count == 1)
            {
                updatedRoot = updatedRoot.ReplaceNode(annotatedSetupStatement, replacementStatements[0]);
            }
            else if (annotatedSetupStatement.Parent is BlockSyntax setupBlock)
            {
                var statementIndex = setupBlock.Statements.IndexOf((StatementSyntax) annotatedSetupStatement);
                var rewrittenStatements = setupBlock.Statements.RemoveAt(statementIndex).InsertRange(statementIndex, replacementStatements);
                updatedRoot = updatedRoot.ReplaceNode(setupBlock, setupBlock.WithStatements(rewrittenStatements));
            }
            else
            {
                return document;
            }

            for (var index = 0; index < clientAnnotations.Length; index++)
            {
                var annotatedClientExpression = updatedRoot.GetAnnotatedNodes(clientAnnotations[index]).Single();
                var replacementExpression = SyntaxFactory.ParseExpression(edit.HttpClientCreations[index].ReplacementText)
                    .WithTriviaFrom(annotatedClientExpression);
                updatedRoot = updatedRoot.ReplaceNode(annotatedClientExpression, replacementExpression);
            }

            updatedRoot = AddUsingDirectiveIfMissing(updatedRoot, semanticModel.Compilation, "FastMoq.Extensions");
            updatedRoot = AddUsingDirectivesIfMissing(updatedRoot, semanticModel.Compilation, edit.RequiredNamespaces);
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
            updatedRoot = AddUsingDirectiveIfMissing(updatedRoot, semanticModel.Compilation, "FastMoq.AzureFunctions.Extensions");
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static async Task<Document> ReplaceFunctionContextInvocationIdInvocationAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || semanticModel is null)
            {
                return document;
            }

            if (!FastMoqAnalysisHelpers.TryBuildFunctionContextInvocationIdReplacement(invocationExpression, semanticModel, cancellationToken, out var targetInvocation, out var replacementText))
            {
                return document;
            }

            var replacementExpression = SyntaxFactory.ParseExpression(replacementText)
                .WithTriviaFrom(targetInvocation);
            var updatedRoot = root.ReplaceNode(targetInvocation, replacementExpression);
            updatedRoot = AddUsingDirectiveIfMissing(updatedRoot, semanticModel.Compilation, "FastMoq.AzureFunctions.Extensions");
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
            updatedRoot = AddUsingDirectivesIfMissing(updatedRoot, semanticModel.Compilation, requiredNamespaces);
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
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || HasAssemblyDefaultProviderAttribute(root, providerName))
            {
                return document;
            }

            var updatedRoot = (CompilationUnitSyntax) AddUsingDirectiveIfMissing(root, compilation, FastMoqAnalysisHelpers.FastMoqProvidersNamespace);
            updatedRoot = updatedRoot.AddAttributeLists(CreateAssemblyDefaultProviderAttribute(providerName));
            return document.WithSyntaxRoot(updatedRoot);
        }

        private static async Task<Document> AddAssemblyRegisteredDefaultProviderAsync(Document document, string providerName, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || HasAssemblyRegisteredDefaultProviderAttribute(root, providerName))
            {
                return document;
            }

            var updatedRoot = (CompilationUnitSyntax) AddUsingDirectivesIfMissing(root, compilation, [FastMoqAnalysisHelpers.FastMoqProvidersNamespace, FastMoqAnalysisHelpers.MoqProviderNamespace]);
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
            return AddUsingDirectiveIfMissing(root, compilation: null, namespaceName);
        }

        private static SyntaxNode AddUsingDirectiveIfMissing(SyntaxNode root, Compilation? compilation, string namespaceName)
        {
            if (root is not CompilationUnitSyntax compilationUnit || IsNamespaceAlreadyImported(compilationUnit, compilation, namespaceName))
            {
                return root;
            }

            return compilationUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName)));
        }

        private static SyntaxNode AddUsingDirectivesIfMissing(SyntaxNode root, IReadOnlyList<string> namespaceNames)
        {
            return AddUsingDirectivesIfMissing(root, compilation: null, namespaceNames);
        }

        private static SyntaxNode AddUsingDirectivesIfMissing(SyntaxNode root, Compilation? compilation, IReadOnlyList<string> namespaceNames)
        {
            foreach (var namespaceName in namespaceNames)
            {
                root = AddUsingDirectiveIfMissing(root, compilation, namespaceName);
            }

            return root;
        }

        private static bool IsNamespaceAlreadyImported(CompilationUnitSyntax compilationUnit, Compilation? compilation, string namespaceName)
        {
            if (compilationUnit.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(@using => @using.Name?.ToString() == namespaceName))
            {
                return true;
            }

            if (compilation is null)
            {
                return false;
            }

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                if (syntaxTree.GetRoot() is not CompilationUnitSyntax candidateCompilationUnit)
                {
                    continue;
                }

                if (candidateCompilationUnit.Usings.Any(@using => @using.GlobalKeyword != default && @using.Name?.ToString() == namespaceName))
                {
                    return true;
                }
            }

            if (compilation.Options is CSharpCompilationOptions csharpCompilationOptions &&
                csharpCompilationOptions.Usings.Any(@using => string.Equals(@using, namespaceName, StringComparison.Ordinal)))
            {
                return true;
            }

            return false;
        }

        private static InvocationExpressionSyntax? FindInvocationExpression(SyntaxNode root, TextSpan span)
        {
            return root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(invocationExpression => invocationExpression.Span == span)
                ?? root.FindNode(span).DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        }

        private static IReadOnlyList<StatementSyntax> ParseReplacementStatements(IReadOnlyList<string> replacementStatements, StatementSyntax originalStatement)
        {
            var parsedStatements = replacementStatements
                .Select(statementText => SyntaxFactory.ParseStatement(statementText))
                .ToArray();

            if (parsedStatements.Length == 0)
            {
                return [];
            }

            parsedStatements[0] = parsedStatements[0].WithLeadingTrivia(originalStatement.GetLeadingTrivia());
            parsedStatements[parsedStatements.Length - 1] = parsedStatements[parsedStatements.Length - 1].WithTrailingTrivia(originalStatement.GetTrailingTrivia());
            return parsedStatements;
        }

        private static bool TryBuildDirectMockerTestBaseInheritanceFix(
            ClassDeclarationSyntax classDeclaration,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out DirectMockerTestBaseInheritanceFix fix)
        {
            fix = default;

            if (!FastMoqAnalysisHelpers.TryGetDirectMockerTestBaseInheritanceCandidate(classDeclaration, semanticModel, cancellationToken, out var candidate) ||
                !candidate.IsFixable ||
                candidate.HelperMember is not IFieldSymbol helperField ||
                helperField.DeclaringSyntaxReferences.SingleOrDefault()?.GetSyntax(cancellationToken) is not VariableDeclaratorSyntax helperVariable ||
                helperVariable.Parent?.Parent is not FieldDeclarationSyntax helperFieldDeclaration ||
                helperFieldDeclaration.Declaration.Variables.Count != 1 ||
                !TryGetHelperAliasMap(candidate.HelperTypeDeclaration, out var aliasMap))
            {
                return false;
            }

            var memberAccessReplacements = new List<DirectMockerTestBaseInheritanceReplacement>();
            var statementsToRemove = new List<StatementSyntax>();

            foreach (var member in classDeclaration.Members)
            {
                if (member == candidate.HelperTypeDeclaration || member == helperFieldDeclaration)
                {
                    continue;
                }

                foreach (var memberAccess in member.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>())
                {
                    if (!SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken).Symbol, helperField))
                    {
                        continue;
                    }

                    if (!aliasMap.TryGetValue(memberAccess.Name.Identifier.ValueText, out var baseMemberName))
                    {
                        return false;
                    }

                    memberAccessReplacements.Add(new DirectMockerTestBaseInheritanceReplacement(memberAccess, $"base.{baseMemberName}"));
                }

                foreach (var assignmentExpression in member.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>())
                {
                    if (!SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(assignmentExpression.Left, cancellationToken).Symbol, helperField))
                    {
                        continue;
                    }

                    if (assignmentExpression.Parent is ExpressionStatementSyntax expressionStatement &&
                        TryIsHelperConstructionAssignment(assignmentExpression.Right, candidate.HelperType, semanticModel, cancellationToken))
                    {
                        statementsToRemove.Add(expressionStatement);
                        continue;
                    }

                    return false;
                }
            }

            fix = new DirectMockerTestBaseInheritanceFix(
                classDeclaration,
                candidate.HelperTypeDeclaration,
                helperFieldDeclaration,
                candidate.TargetType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                memberAccessReplacements,
                statementsToRemove);
            return true;
        }

        private static bool TryGetHelperAliasMap(TypeDeclarationSyntax helperTypeDeclaration, out Dictionary<string, string> aliasMap)
        {
            aliasMap = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var propertyDeclaration in helperTypeDeclaration.Members.OfType<PropertyDeclarationSyntax>())
            {
                if (!FastMoqAnalysisHelpers.TryGetPropertyReturnExpression(propertyDeclaration, out var expression))
                {
                    return false;
                }

                expression = FastMoqAnalysisHelpers.Unwrap(expression);
                string? baseMemberName = expression switch
                {
                    IdentifierNameSyntax identifierName when identifierName.Identifier.ValueText is "Component" or "Mocks" => identifierName.Identifier.ValueText,
                    MemberAccessExpressionSyntax memberAccess when memberAccess.Expression is ThisExpressionSyntax && memberAccess.Name.Identifier.ValueText is "Component" or "Mocks" => memberAccess.Name.Identifier.ValueText,
                    _ => null,
                };

                if (baseMemberName is null)
                {
                    return false;
                }

                aliasMap[propertyDeclaration.Identifier.ValueText] = baseMemberName;
            }

            return aliasMap.Count > 0;
        }

        private static bool TryIsHelperConstructionAssignment(ExpressionSyntax expression, INamedTypeSymbol helperType, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type as INamedTypeSymbol;
            return type is not null && SymbolEqualityComparer.Default.Equals(type, helperType);
        }

        private readonly struct DirectMockerTestBaseInheritanceFix
        {
            public DirectMockerTestBaseInheritanceFix(
                ClassDeclarationSyntax outerClassDeclaration,
                TypeDeclarationSyntax helperTypeDeclaration,
                FieldDeclarationSyntax helperFieldDeclaration,
                string targetTypeName,
                IReadOnlyList<DirectMockerTestBaseInheritanceReplacement> memberAccessReplacements,
                IReadOnlyList<StatementSyntax> statementsToRemove)
            {
                OuterClassDeclaration = outerClassDeclaration;
                HelperTypeDeclaration = helperTypeDeclaration;
                HelperFieldDeclaration = helperFieldDeclaration;
                TargetTypeName = targetTypeName;
                MemberAccessReplacements = memberAccessReplacements;
                StatementsToRemove = statementsToRemove;
            }

            public ClassDeclarationSyntax OuterClassDeclaration { get; }

            public TypeDeclarationSyntax HelperTypeDeclaration { get; }

            public FieldDeclarationSyntax HelperFieldDeclaration { get; }

            public string TargetTypeName { get; }

            public IReadOnlyList<DirectMockerTestBaseInheritanceReplacement> MemberAccessReplacements { get; }

            public IReadOnlyList<StatementSyntax> StatementsToRemove { get; }
        }

        private readonly struct DirectMockerTestBaseInheritanceReplacement
        {
            public DirectMockerTestBaseInheritanceReplacement(MemberAccessExpressionSyntax memberAccess, string replacementText)
            {
                MemberAccess = memberAccess;
                ReplacementText = replacementText;
            }

            public MemberAccessExpressionSyntax MemberAccess { get; }

            public string ReplacementText { get; }
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
            var supportsSequence = method.Name == "SetupSequence";
            if (method.Name is not "Setup" and not "SetupSequence" ||
                method.ContainingNamespace.ToDisplayString() != "Moq.Protected" ||
                method.ContainingType.Name != "IProtectedMock" ||
                invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccess ||
                !FastMoqAnalysisHelpers.TryResolveProtectedTrackedMockOrigin(memberAccess.Expression, semanticModel, cancellationToken, out var origin) ||
                origin.ServiceType.ToDisplayString() != "System.Net.Http.HttpMessageHandler" ||
                invocationExpression.ArgumentList.Arguments.Count != 3 ||
                semanticModel.GetConstantValue(invocationExpression.ArgumentList.Arguments[0].Expression, cancellationToken) is not { HasValue: true, Value: string protectedMemberName } ||
                protectedMemberName != "SendAsync" ||
                !IsAnyCancellationTokenMatcher(invocationExpression.ArgumentList.Arguments[2].Expression, semanticModel, cancellationToken))
            {
                return false;
            }

            if (!TryGetSupportedProviderNeutralHttpReturnsInvocations(invocationExpression, supportsSequence, out var returnsInvocations))
            {
                return false;
            }

            var setupStatement = returnsInvocations[returnsInvocations.Count - 1].FirstAncestorOrSelf<ExpressionStatementSyntax>();
            if (setupStatement is null)
            {
                return false;
            }

            IReadOnlyList<string> setupReplacementStatements;
            IReadOnlyList<string> requiredNamespaces;
            string codeActionTitle;
            if (supportsSequence)
            {
                if (!TryBuildProviderNeutralHttpSequenceReplacement(origin, invocationExpression.ArgumentList.Arguments[1].Expression, returnsInvocations, semanticModel, cancellationToken, setupStatement, out setupReplacementStatements, out codeActionTitle, out requiredNamespaces))
                {
                    return false;
                }
            }
            else if (!TryBuildProviderNeutralHttpSetupReplacement(origin, invocationExpression.ArgumentList.Arguments[1].Expression, returnsInvocations[0], semanticModel, cancellationToken, out var setupReplacementText, out codeActionTitle))
            {
                return false;
            }
            else
            {
                setupReplacementStatements = [setupReplacementText];
                requiredNamespaces = Array.Empty<string>();
            }

            var httpClientCreations = FindHttpClientCreationEdits(origin, setupStatement, semanticModel, cancellationToken);
            if (!TryGetTrackedMockDeclarationToRemove(origin, setupStatement, httpClientCreations, semanticModel, cancellationToken, out var declarationToRemove))
            {
                return false;
            }

            edit = new ProviderNeutralHttpHelperEdit(setupStatement, setupReplacementStatements, codeActionTitle, httpClientCreations, declarationToRemove, requiredNamespaces);
            return true;
        }

        private static bool TryGetSupportedProviderNeutralHttpReturnsInvocations(InvocationExpressionSyntax invocationExpression, bool supportsSequence, out IReadOnlyList<InvocationExpressionSyntax> returnsInvocations)
        {
            var collectedReturnsInvocations = new List<InvocationExpressionSyntax>();
            while (invocationExpression.Parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Parent is InvocationExpressionSyntax parentInvocation)
            {
                if (memberAccess.Name.Identifier.ValueText is not "Returns" and not "ReturnsAsync")
                {
                    returnsInvocations = Array.Empty<InvocationExpressionSyntax>();
                    return false;
                }

                collectedReturnsInvocations.Add(parentInvocation);
                invocationExpression = parentInvocation;
            }

            returnsInvocations = collectedReturnsInvocations;
            return collectedReturnsInvocations.Count > 0 && (supportsSequence || collectedReturnsInvocations.Count == 1);
        }

        private static bool IsAnyCancellationTokenMatcher(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            expression = UnwrapForPatternMatching(expression);
            if (expression is not InvocationExpressionSyntax invocationExpression ||
                !FastMoqAnalysisHelpers.TryGetMethodSymbol(invocationExpression, semanticModel, cancellationToken, out var method) ||
                method is null)
            {
                return false;
            }

            method = method.ReducedFrom ?? method;
            return method.Name == "IsAny" &&
                method.ContainingType.Name == "ItExpr" &&
                method.TypeArguments.Length == 1 &&
                method.TypeArguments[0].ToDisplayString() == "System.Threading.CancellationToken" &&
                invocationExpression.ArgumentList.Arguments.Count == 0;
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

        private static bool TryBuildProviderNeutralHttpSequenceReplacement(TrackedMockOrigin origin, ExpressionSyntax requestMatcherExpression, IReadOnlyList<InvocationExpressionSyntax> returnsInvocations, SemanticModel semanticModel, CancellationToken cancellationToken, StatementSyntax setupStatement, out IReadOnlyList<string> replacementStatements, out string codeActionTitle, out IReadOnlyList<string> requiredNamespaces)
        {
            replacementStatements = Array.Empty<string>();
            codeActionTitle = "Use WhenHttpRequest(...)";
            requiredNamespaces = ["System", "System.Collections.Generic"];

            if (setupStatement.Parent is not BlockSyntax ||
                !TryBuildRequestMatcher(requestMatcherExpression, semanticModel, cancellationToken, out var predicateText, out var methodText, out var requestUriText))
            {
                return false;
            }

            var responseFactories = new List<string>(returnsInvocations.Count);
            foreach (var returnsInvocation in returnsInvocations)
            {
                if (!TryBuildResponseFactory(returnsInvocation, semanticModel, cancellationToken, out var responseFactoryText, out _, out _))
                {
                    return false;
                }

                responseFactories.Add(responseFactoryText);
            }

            var queueVariableName = "fastMoqHttpResponseFactories";
            var queueDeclaration = $"var {queueVariableName} = new Queue<Func<HttpResponseMessage>>(new Func<HttpResponseMessage>[] {{ {string.Join(", ", responseFactories)} }});";
            var dequeueFactory = $"() => {queueVariableName}.Count > 0 ? {queueVariableName}.Dequeue().Invoke() : throw new InvalidOperationException(\"No queued HTTP response remains.\")";
            var mockerText = origin.MockerExpression.ToString();
            var whenHttpRequestCall = methodText is not null && requestUriText is not null
                ? $"{mockerText}.WhenHttpRequest({methodText}, {requestUriText}, {dequeueFactory});"
                : $"{mockerText}.WhenHttpRequest({predicateText}, {dequeueFactory});";

            replacementStatements = [queueDeclaration, whenHttpRequestCall];
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
                if (HasAnonymousFunctionParameters(anonymousFunction))
                {
                    return false;
                }

                responseFactoryText = anonymousFunction.ToString();
                if (!TryGetAnonymousFunctionReturnExpression(anonymousFunction, out var returnedExpression))
                {
                    return true;
                }

                _ = TryExtractJsonResponse(returnedExpression, semanticModel, cancellationToken, out jsonPayloadText, out statusCodeText);
                return true;
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

        private static bool HasAnonymousFunctionParameters(AnonymousFunctionExpressionSyntax anonymousFunction)
        {
            return anonymousFunction switch
            {
                SimpleLambdaExpressionSyntax => true,
                ParenthesizedLambdaExpressionSyntax parenthesizedLambdaExpression => parenthesizedLambdaExpression.ParameterList.Parameters.Count != 0,
                AnonymousMethodExpressionSyntax anonymousMethodExpression => anonymousMethodExpression.ParameterList?.Parameters.Count > 0,
                _ => false,
            };
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

        private static bool TryGetTrackedMockDeclarationToRemove(TrackedMockOrigin origin, StatementSyntax setupStatement, IReadOnlyList<HttpClientCreationEdit> httpClientCreations, SemanticModel semanticModel, CancellationToken cancellationToken, out LocalDeclarationStatementSyntax? declarationToRemove)
        {
            declarationToRemove = null;

            if (origin.TrackedMockExpression is InvocationExpressionSyntax)
            {
                return true;
            }

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

            if (localDeclarationStatement.Parent != containingBlock)
            {
                return false;
            }

            foreach (var candidate in containingBlock.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (!SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(candidate, cancellationToken).Symbol, localSymbol))
                {
                    continue;
                }

                if (setupStatement.Span.Contains(candidate.Span))
                {
                    continue;
                }

                if (httpClientCreations.Any(edit => edit.TargetExpression.Span.Contains(candidate.Span)))
                {
                    continue;
                }

                return false;
            }

            declarationToRemove = localDeclarationStatement;
            return true;
        }

        private readonly struct ProviderNeutralHttpHelperEdit
        {
            public ProviderNeutralHttpHelperEdit(StatementSyntax setupStatement, IReadOnlyList<string> setupReplacementStatements, string codeActionTitle, IReadOnlyList<HttpClientCreationEdit> httpClientCreations, LocalDeclarationStatementSyntax? trackedMockDeclarationToRemove, IReadOnlyList<string> requiredNamespaces)
            {
                SetupStatement = setupStatement;
                SetupReplacementStatements = setupReplacementStatements;
                CodeActionTitle = codeActionTitle;
                HttpClientCreations = httpClientCreations;
                TrackedMockDeclarationToRemove = trackedMockDeclarationToRemove;
                RequiredNamespaces = requiredNamespaces;
            }

            public StatementSyntax SetupStatement { get; }

            public IReadOnlyList<string> SetupReplacementStatements { get; }

            public string CodeActionTitle { get; }

            public IReadOnlyList<HttpClientCreationEdit> HttpClientCreations { get; }

            public LocalDeclarationStatementSyntax? TrackedMockDeclarationToRemove { get; }

            public IReadOnlyList<string> RequiredNamespaces { get; }
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