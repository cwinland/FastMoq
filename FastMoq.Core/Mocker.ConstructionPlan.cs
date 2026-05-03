using FastMoq.Extensions;
using FastMoq.Models;
using System.Reflection;
using PublicInstanceConstructionRequest = FastMoq.Models.InstanceConstructionRequest;

namespace FastMoq
{
    public partial class Mocker
    {
        /// <summary>
        /// Resolves constructor-selection metadata for the supplied request without creating the target instance.
        /// </summary>
        /// <param name="request">The constructor-selection request to resolve.</param>
        /// <returns>A read-only constructor plan describing the selected target type and parameter metadata.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="request" /> is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the requested type does not currently use the constructor-selection path.</exception>
        public InstanceConstructionPlan CreateConstructionPlan(PublicInstanceConstructionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var requestedType = CleanType(request.RequestedType);
            var model = GetTypeModel(requestedType);

            if (model.CreateFunc != null)
            {
                throw new InvalidOperationException($"Type '{requestedType}' resolves through a custom factory registration and does not expose constructor-plan metadata.");
            }

            if (KnownTypeRegistry.HasKnownParameterResolution(this, requestedType))
            {
                throw new InvalidOperationException($"Type '{requestedType}' resolves through a known-type path and does not expose constructor-plan metadata.");
            }

            if (model.Arguments.Count > 0 && request.ConstructorParameterTypes == null)
            {
                throw new InvalidOperationException($"Type '{requestedType}' has stored constructor arguments. Specify ConstructorParameterTypes explicitly before requesting constructor-plan metadata.");
            }

            var targetType = model.InstanceType ?? requestedType;
            if (targetType.IsInterface ||
                targetType.IsAbstract ||
                targetType.ContainsGenericParameters)
            {
                throw new InvalidOperationException($"Type '{requestedType}' does not resolve to a concrete constructor path.");
            }

            var constructionRequest = CreateConstructorSelectionRequest(
                request.PublicOnly,
                request.ConstructorParameterTypes,
                request.OptionalParameterResolution,
                request.ConstructorAmbiguityBehavior);
            var selection = SelectConstructionPlanConstructor(targetType, constructionRequest);
            var parameters = selection.Constructor
                .GetParameters()
                .Select(parameter => CreateConstructionParameterPlan(parameter, constructionRequest.OptionalParameterResolution));

            return new InstanceConstructionPlan(
                requestedType,
                targetType,
                usedNonPublicConstructor: !selection.Constructor.IsPublic,
                selection.UsedPreferredConstructorAttribute,
                selection.UsedAmbiguityFallback,
                parameters);
        }

        internal InstanceConstructionGraph CreateConstructionGraph(PublicInstanceConstructionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            var plan = CreateConstructionPlan(request);
            var rootNode = new InstanceConstructionGraphNode(
                id: 0,
                nodeType: plan.ResolvedType,
                kind: InstanceConstructionGraphNodeKind.Root,
                plan: plan);

            var nodes = new List<InstanceConstructionGraphNode> { rootNode };
            var edges = new List<InstanceConstructionGraphEdge>();

            foreach (var parameter in plan.Parameters)
            {
                var dependencyNode = new InstanceConstructionGraphNode(
                    id: nodes.Count,
                    nodeType: parameter.ParameterType,
                    kind: InstanceConstructionGraphNodeKind.Dependency,
                    parameter: parameter);

                nodes.Add(dependencyNode);
                edges.Add(new InstanceConstructionGraphEdge(rootNode.Id, dependencyNode.Id, parameter.Position, parameter.Name));
            }

            return new InstanceConstructionGraph(request, rootNode, nodes, edges);
        }

        internal PublicInstanceConstructionRequest CreateConstructionPlanRequest(Type requestedType, InstanceCreationFlags flags, Type?[]? constructorParameterTypes)
        {
            ArgumentNullException.ThrowIfNull(requestedType);

            var request = CreateConstructorSelectionRequest(flags, constructorParameterTypes);
            return new PublicInstanceConstructionRequest(requestedType)
            {
                ConstructorParameterTypes = constructorParameterTypes,
                PublicOnly = request.PublicOnly,
                OptionalParameterResolution = request.OptionalParameterResolution,
                ConstructorAmbiguityBehavior = request.ConstructorAmbiguityBehavior,
            };
        }

        private ConstructionPlanSelection SelectConstructionPlanConstructor(Type targetType, ConstructorSelectionRequest request)
        {
            if (request.ConstructorParameterTypes != null)
            {
                var exactConstructor = FindConstructorByType(targetType, false, ShouldFallbackToNonPublicConstructors(request.PublicOnly), request.ConstructorParameterTypes);
                return new ConstructionPlanSelection(exactConstructor, UsedPreferredConstructorAttribute: false, UsedAmbiguityFallback: false);
            }

            var constructorModel = FindPreferredConstructor(
                targetType,
                nonPublic: false,
                fallbackToNonPublicConstructors: ShouldFallbackToNonPublicConstructors(request.PublicOnly),
                constructorAmbiguityBehavior: request.ConstructorAmbiguityBehavior,
                optionalParameterResolution: request.OptionalParameterResolution);

            var constructor = constructorModel.ConstructorInfo
                ?? throw GetConstructorResolutionException(targetType, "Constructor selection did not return a constructor.");
            var usedPreferredConstructorAttribute = constructor.IsDefined(typeof(PreferredConstructorAttribute), inherit: false);
            var usedAmbiguityFallback = !usedPreferredConstructorAttribute && WasAmbiguityFallbackUsed(targetType, constructor, request);
            return new ConstructionPlanSelection(constructor, usedPreferredConstructorAttribute, usedAmbiguityFallback);
        }

        private bool WasAmbiguityFallbackUsed(Type targetType, ConstructorInfo selectedConstructor, ConstructorSelectionRequest request)
        {
            if (request.ConstructorAmbiguityBehavior != ConstructorAmbiguityBehavior.PreferParameterlessConstructor ||
                selectedConstructor.GetParameters().Length != 0)
            {
                return false;
            }

            var constructors = selectedConstructor.IsPublic
                ? GetConstructors(targetType, false, request.OptionalParameterResolution)
                : GetConstructors(targetType, true, request.OptionalParameterResolution)
                    .Where(constructor => constructor.ConstructorInfo?.IsPublic == false)
                    .ToList();

            constructors = constructors
                .Where(constructor => constructor.ConstructorInfo?.IsDefined(typeof(PreferredConstructorAttribute), inherit: false) != true)
                .ToList();

            var testedConstructors = this.GetTestedConstructors(targetType, constructors);
            if (testedConstructors.Count == 0)
            {
                return false;
            }

            var largestArity = testedConstructors.Max(constructor => constructor.ParameterList.Length);
            return largestArity > 0 && testedConstructors.Count(constructor => constructor.ParameterList.Length == largestArity) > 1;
        }

        private InstanceConstructionParameterPlan CreateConstructionParameterPlan(ParameterInfo parameter, OptionalParameterResolutionMode optionalParameterResolution)
        {
            var hasServiceKey = TryGetServiceKey(parameter, out var serviceKey);
            var source = ResolveConstructionParameterSource(parameter, optionalParameterResolution, hasServiceKey ? serviceKey : null);

            return new InstanceConstructionParameterPlan(
                parameter.Name ?? string.Empty,
                parameter.ParameterType,
                parameter.Position,
                parameter.IsOptional,
                optionalParameterResolution,
                hasServiceKey ? serviceKey : null,
                source);
        }

        private InstanceConstructionParameterSource ResolveConstructionParameterSource(ParameterInfo parameter, OptionalParameterResolutionMode optionalParameterResolution, object? serviceKey)
        {
            if (serviceKey != null)
            {
                return InstanceConstructionParameterSource.KeyedService;
            }

            if (optionalParameterResolution == OptionalParameterResolutionMode.UseDefaultOrNull && parameter.IsOptional)
            {
                return InstanceConstructionParameterSource.OptionalDefault;
            }

            var parameterType = parameter.ParameterType;
            if (HasCustomRegistrationAffectingResolution(parameterType))
            {
                return InstanceConstructionParameterSource.CustomRegistration;
            }

            if (KnownTypeRegistry.HasKnownParameterResolution(this, parameterType))
            {
                return InstanceConstructionParameterSource.KnownType;
            }

            if (parameterType.IsClass || parameterType.IsInterface)
            {
                return !parameterType.IsSealed
                    ? InstanceConstructionParameterSource.AutoMock
                    : InstanceConstructionParameterSource.TypeDefault;
            }

            return InstanceConstructionParameterSource.TypeDefault;
        }

        private bool HasCustomRegistrationAffectingResolution(Type parameterType)
        {
            if (!HasTypeRegistration(parameterType))
            {
                return false;
            }

            var model = GetTypeModel(parameterType);
            return model.CreateFunc != null ||
                model.Arguments.Count > 0 ||
                model.InstanceType != parameterType;
        }

        private readonly record struct ConstructionPlanSelection(
            ConstructorInfo Constructor,
            bool UsedPreferredConstructorAttribute,
            bool UsedAmbiguityFallback);
    }
}