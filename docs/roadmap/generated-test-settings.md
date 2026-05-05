# FastMoq Generated Test Settings Design

This page captures the current `#162` design direction for generated-test authoring settings. It is design-level only. It does not imply shipped settings resolution, generator behavior changes, analyzer behavior changes, or external scaffolding support in the repo.

## Purpose And Non-Goals

Purpose:

- define one shared authoring-time settings contract for later generated scenario scaffolds, full generated tests, analyzer-guided entry points, and any helper-builder output that explicitly opts into the same contract
- keep `#123`, `#124`, `#126`, `#136`, and conditionally `#137` aligned to one vocabulary for naming, platform targeting, regeneration, and extensibility
- settle where those settings live, how they override one another, and which parts remain capability-gated by existing package-shape rules

Non-goals:

- no generator, analyzer, runtime, schema parser, or settings-loader implementation work in this slice
- no reopening of the `#122` harness MVP contract
- no reopening of the `#127` package-layout and target-shape authority
- no machine-local IDE-profile settings model
- no arbitrary user-authored template system, AST injection point, or plugin callback model in the first design

## Current Baseline

The current repo state that drives this design is:

- `#122` is complete for the narrow harness MVP, and `#136` now builds on that base for explicit partial `MockerTestBase<TComponent>` targets: the repo can emit constructor-signature metadata, harness bootstrap, generated scenario execution entry points, and suite-level shared setup hooks, but it still does not emit full generated tests, settings-driven scaffold variants, or framework-helper builders.
- `#127` is complete for package detection and target-shape rules: the analyzer layer already knows the supported generated target shapes for the referenced FastMoq package layout.
- `GeneratedHarnessSourceGenerator` currently hard-codes the target attribute metadata name, the `MockerTestBase<TComponent>` requirement, the generated metadata type name, the `ComponentConstructorParameterTypes` override, the auto-generated header, `#nullable enable`, and the `.FastMoq.GeneratedHarness.g.cs` hint-name suffix.
- `GeneratedTestTargetShapeRule` and `FastMoqAnalysisHelpers` currently own the package matrix, target-shape list, required package per shape, default base type name, and default namespaces for each supported generated test shape.
- `Directory.Packages.props` currently carries `xunit` `2.9.3` and `xunit.runner.visualstudio` `3.0.2`, while the repo documentation remains framework-agnostic. That mixed baseline is one reason syntax targeting and runner or bootstrap targeting must stay separate settings.
- `FastMoq.Generators.csproj` does not currently declare `CompilerVisibleProperty`, `CompilerVisibleItemMetadata`, or any equivalent custom bridge for generated-test authoring settings.

## Consumer Map

- `#126` consumes the shared settings contract for regeneration-safe scenario-scaffolding hooks and customization boundaries, but it still owns the `ScenarioBuilder` contract surface rather than the settings carrier.
- `#136` consumes the shared settings contract for concrete scenario and suite scaffolding implementation after `#126` settles the hook model.
- `#123` consumes the shared settings contract for naming, scaffold choice, framework syntax targeting, runner or bootstrap targeting, and regeneration behavior when widening from harness metadata into full generated tests.
- `#124` consumes the shared settings contract so analyzer-guided generation suggestions can reflect the same naming and platform choices without inventing analyzer-local defaults.
- `#137` is conditional: if helper-builder output needs shared naming, output, or regeneration rules, it should consume this contract later. This design does not assume that relationship unless a later implementation explicitly chooses it.

## Settings Taxonomy

### Package Acquisition And Capability Settings

- Auto-add package policy. Classification: capability-gated user setting. First consumer: `#124` or a later external scaffolder. Notes: the in-compilation source generator must not mutate project files.
- Helper-family opt-in or opt-out. Classification: capability-gated user setting. First consumer: `#137` if it opts into the shared contract. Notes: the setting can only choose among helper families already supported by the `#127` package matrix.

### Output Placement And Naming Settings

- Destination project. Classification: deferred value placeholder. First consumer: a future external scaffolder, not the current in-compilation source generator.
- Destination folder. Classification: deferred value placeholder. First consumer: a future external scaffolder, not the current in-compilation source generator.
- Namespace template. Classification: true user setting. First consumer: `#123`, `#136`, and `#124`.
- Type and member naming templates. Classification: true user setting. First consumer: `#123`, `#136`, and conditionally `#137`.

### Provider And Bootstrap Settings

- Preferred provider. Classification: true user setting. First consumer: `#123` and `#136`. Notes: provider choice remains constrained by the FastMoq packages referenced by the consuming project.
- Provider bootstrap style. Classification: true user setting. First consumer: `#123` and `#136`. Notes: this covers how generated tests express or discover provider selection; it is separate from framework syntax or runner choice.

### Test Syntax And Assertion Settings

- Test framework syntax target. Classification: true user setting with explicit deferred values. First consumer: `#123`, `#136`, and `#124`.
- Runner or bootstrap target. Classification: true user setting with explicit deferred values. First consumer: `#123`, `#136`, and `#124`.
- Assertion style. Classification: true user setting. First consumer: `#123` and `#136`. Notes: the repo already uses more than one assertion style in documentation, so generated output should not silently hard-code one style forever.

### Scaffold-Shape Settings

- Preferred scaffold shape. Classification: true user setting. First consumer: `#123` and `#136`.
- Hook emission style. Classification: structured extensibility point. First consumer: `#126` and `#136`.
- Helper-builder generation preference. Classification: capability-gated user setting. First consumer: conditionally `#137`.

### Regeneration-Safety Settings

- Regeneration policy. Classification: true user setting. First consumer: `#123`, `#136`, and conditionally `#137`.
- Partial-extension boundary policy. Classification: structured extensibility point. First consumer: `#126` and `#136`.
- Hand-edited safe-zone policy. Classification: deferred value placeholder until a non-generator file-emission workflow exists.

## Settings Carrier, Scope, And Precedence Model

The first persisted carrier should be MSBuild-backed configuration authored at repo or project scope. The authoritative path is:

1. built-in defaults derived from package matrix and target shape
2. repo defaults in `Directory.Build.props`
3. consuming project overrides in the project file
4. future explicit tool or code-action arguments for external scaffolding flows only

The first-wave property family should use a single `FastMoqGeneratedTest...` prefix so the settings are easy to discover in MSBuild and easy to surface through analyzer-config global options. Recommended first-wave property names are:

- `FastMoqGeneratedTestFramework`
- `FastMoqGeneratedTestRunner`
- `FastMoqGeneratedTestProvider`
- `FastMoqGeneratedTestProviderBootstrap`
- `FastMoqGeneratedTestAssertionStyle`
- `FastMoqGeneratedTestScaffold`
- `FastMoqGeneratedTestHookEmission`
- `FastMoqGeneratedTestRegenerationPolicy`
- `FastMoqGeneratedTestAutoAddPackages`
- `FastMoqGeneratedTestNamespace`
- `FastMoqGeneratedTestTypeNamePattern`
- `FastMoqGeneratedTestMethodNamePattern`
- `FastMoqGeneratedTestHelperFamilies`

Reserved later properties for external scaffolding flows are:

- `FastMoqGeneratedTestOutputProject`
- `FastMoqGeneratedTestOutputFolder`

`.editorconfig` stays secondary in the first design. It can participate later for path-scoped analyzer behavior if a real need appears, but it should not replace repo/project-scoped MSBuild settings as the authoritative source for generated-test authoring choices.

JSON or `AdditionalFiles` manifests are explicitly deferred. They should only be introduced if the structured settings shape demonstrably outgrows practical MSBuild properties or simple semicolon-delimited list values.

Implementation note: although Roslyn analyzers and generators can observe `build_property.*` values through generated analyzer config, this repo does not currently expose any custom generated-test settings bridge in `FastMoq.Generators.csproj`. A future implementation slice must add the required compiler-visible-property or equivalent build plumbing before any custom settings can flow into analyzer or generator code. The current `#136` scaffold implementation therefore consumes the `#162` vocabulary as fixed generator-owned defaults rather than as user-configurable settings values.

## Extensibility Model

The first design should treat extensibility as constrained authoring control, not as an open plugin system.

The surface breaks into three buckets:

- Closed choices. Examples: framework syntax target, runner target, assertion style, provider bootstrap style, scaffold preference, regeneration policy.
- Structured override points. Examples: hook emission style, named partial companion hooks, and regeneration-safe setup or verify seams that map back to existing FastMoq runtime surfaces.
- Deferred open extensibility. Examples: arbitrary user-authored templates, AST injection, or plugin callbacks. These are intentionally out of scope for the first design.

The first override mechanism should be companion partial hooks only. Do not introduce multiple parallel extension systems in the first design.

The first `GeneratedHookEmissionStyle` choice set is:

- `None`
- `CompanionPartialHooks`

The first named hook roles should stay minimal and map back to existing runtime surfaces instead of inventing a second runtime model:

- `ConfigureGeneratedMockerPolicy` for `MockerTestBase<TComponent>.ConfigureMockerPolicy`
- `ConfigureGeneratedMocks` for `MockerTestBase<TComponent>.SetupMocksAction`
- `SelectGeneratedConstructor` for `ComponentConstructorParameterTypes` and `CreateComponentConstructionRequest()`-based customization
- `AfterGeneratedComponentCreated` for `MockerTestBase<TComponent>.CreatedComponentAction`
- `ArrangeGeneratedScenario` for `ScenarioBuilder<T>.With(...)` and `When(...)`
- `AssertGeneratedScenario` for `ScenarioBuilder<T>.Then(...)`, `WhenThrows(...)`, and provider-neutral `Verify(...)` flows

Custom overrides on test structure should come through scaffold selection plus those regeneration-safe named hooks, not through free-form template injection.

## Supported And Deferred Framework And Runner Matrix

Framework syntax targets:

| Target | Status | Notes |
| --- | --- | --- |
| `XUnitV2` | Supported baseline | Matches the current repo package baseline most closely. |
| `XUnitV3` | Supported with differences | Syntax stays similar for many tests, but fixture and runner choices differ enough that it must remain explicit. |
| `NUnit` | Supported with differences | Attribute names, lifecycle hooks, and assertion guidance differ. |
| `MSTest` | Supported with differences | Attribute names and suite structure differ. |
| `Deferred` | Explicit placeholder | Use when the consuming flow cannot yet emit the requested framework cleanly. |

Runner or bootstrap targets:

| Target | Status | Notes |
| --- | --- | --- |
| `ProjectDefault` | Supported baseline | Uses the consuming test project's established runner/bootstrap story without trying to infer more than the project already declares. |
| `VSTest` | Supported | Runner choice remains separate from syntax choice. |
| `MTP` | Supported | Runner choice remains separate from syntax choice. |
| `ModuleInitializer` | Deferred | Bootstrap-specific flow that should stay explicit instead of inferred. |
| `AssemblyFixture` | Deferred | Framework-specific bootstrap behavior should remain explicit. |
| `Deferred` | Explicit placeholder | Use when the requested bootstrap shape is not yet implemented. |

The design requirement behind both tables is simple: syntax target and runner or bootstrap mode are related, but they are not the same setting and should not be collapsed into one inferred value.

## Candidate API And Design Outline

The first descriptive model should use authoring-time names rather than runtime metadata names:

```csharp
public sealed record GeneratedTestAuthoringSettings
{
    public GeneratedTestPackagePolicySettings PackagePolicy { get; init; }
    public GeneratedTestPlacementSettings Placement { get; init; }
    public GeneratedTestNamingTemplateSettings Naming { get; init; }
    public GeneratedTestProviderSelectionSettings ProviderSelection { get; init; }
    public GeneratedTestSyntaxTargetSettings SyntaxTarget { get; init; }
    public GeneratedTestScaffoldPreferenceSettings ScaffoldPreference { get; init; }
    public GeneratedTestRegenerationSettings Regeneration { get; init; }
}

public sealed record GeneratedTestPackagePolicySettings
{
    public GeneratedTestPackagePolicy AutoAddPackages { get; init; }
    public string[] HelperFamilies { get; init; }
}

public sealed record GeneratedTestPlacementSettings
{
    public string? Namespace { get; init; }
    public string? OutputProject { get; init; }
    public string? OutputFolder { get; init; }
}

public sealed record GeneratedTestNamingTemplateSettings
{
    public string TestTypeNamePattern { get; init; }
    public string TestMethodNamePattern { get; init; }
    public string ScenarioTypeNamePattern { get; init; }
}

public sealed record GeneratedTestProviderSelectionSettings
{
    public string PreferredProvider { get; init; }
    public GeneratedTestProviderBootstrapStyle BootstrapStyle { get; init; }
}

public sealed record GeneratedTestSyntaxTargetSettings
{
    public GeneratedTestFrameworkSyntaxTarget Framework { get; init; }
    public GeneratedTestRunnerBootstrapMode Runner { get; init; }
    public string AssertionStyle { get; init; }
}

public sealed record GeneratedTestScaffoldPreferenceSettings
{
    public GeneratedTestScaffoldKind PreferredScaffold { get; init; }
    public GeneratedHookEmissionStyle HookEmission { get; init; }
    public bool GenerateHelperBuildersWhenSupported { get; init; }
}

public sealed record GeneratedTestRegenerationSettings
{
    public GeneratedTestRegenerationPolicy Policy { get; init; }
    public bool PreserveCompanionPartials { get; init; }
    public string[] SafeZones { get; init; }
}
```

Related enum and value-object families should stay separate for clarity:

- `GeneratedTestFrameworkSyntaxTarget`
- `GeneratedTestRunnerBootstrapMode`
- `GeneratedTestProviderBootstrapStyle`
- `GeneratedTestScaffoldKind`
- `GeneratedHookEmissionStyle`
- `GeneratedTestPackagePolicy`
- `GeneratedTestRegenerationPolicy`

Intended first-use classification by type:

- `GeneratedTestAuthoringSettings`: top-level true user-setting aggregate; first carrier is MSBuild-backed analyzer-config values.
- `GeneratedTestPackagePolicySettings`: capability-gated user settings; primary consumer is analyzer-guided or external scaffolding flows.
- `GeneratedTestPlacementSettings`: mixed user setting plus deferred placeholders; `OutputProject` and `OutputFolder` are not first-class source-generator controls yet.
- `GeneratedTestNamingTemplateSettings`: true user settings; consumed by generated scenario scaffolds and full generated tests.
- `GeneratedTestProviderSelectionSettings`: true user settings; constrained by referenced FastMoq provider packages.
- `GeneratedTestSyntaxTargetSettings`: true user settings with explicit deferred values.
- `GeneratedTestScaffoldPreferenceSettings`: mixed user settings and structured extensibility point.
- `GeneratedTestRegenerationSettings`: mixed user settings and structured extensibility point.

## Illustrative MSBuild Configuration Examples

Repo-level defaults in `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <FastMoqGeneratedTestFramework>XUnitV2</FastMoqGeneratedTestFramework>
    <FastMoqGeneratedTestRunner>ProjectDefault</FastMoqGeneratedTestRunner>
    <FastMoqGeneratedTestProvider>reflection</FastMoqGeneratedTestProvider>
    <FastMoqGeneratedTestProviderBootstrap>ProjectDefault</FastMoqGeneratedTestProviderBootstrap>
    <FastMoqGeneratedTestAssertionStyle>AwesomeAssertions</FastMoqGeneratedTestAssertionStyle>
    <FastMoqGeneratedTestScaffold>ScenarioScaffold</FastMoqGeneratedTestScaffold>
    <FastMoqGeneratedTestHookEmission>CompanionPartialHooks</FastMoqGeneratedTestHookEmission>
    <FastMoqGeneratedTestRegenerationPolicy>PreserveCompanionPartials</FastMoqGeneratedTestRegenerationPolicy>
    <FastMoqGeneratedTestAutoAddPackages>SuggestOnly</FastMoqGeneratedTestAutoAddPackages>
    <FastMoqGeneratedTestNamespace>$(RootNamespace).GeneratedTests</FastMoqGeneratedTestNamespace>
    <FastMoqGeneratedTestTypeNamePattern>{ComponentName}GeneratedTests</FastMoqGeneratedTestTypeNamePattern>
    <FastMoqGeneratedTestMethodNamePattern>{MemberName}_Should_{Outcome}</FastMoqGeneratedTestMethodNamePattern>
    <FastMoqGeneratedTestHelperFamilies>Web;Database</FastMoqGeneratedTestHelperFamilies>
  </PropertyGroup>
</Project>
```

Project-level override in a consuming test project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <FastMoqGeneratedTestFramework>NUnit</FastMoqGeneratedTestFramework>
    <FastMoqGeneratedTestRunner>MTP</FastMoqGeneratedTestRunner>
    <FastMoqGeneratedTestScaffold>FullTestClass</FastMoqGeneratedTestScaffold>
    <FastMoqGeneratedTestNamespace>$(RootNamespace).Generated</FastMoqGeneratedTestNamespace>
  </PropertyGroup>
</Project>
```

These examples are illustrative only. They document the intended first carrier and naming family, not working current implementation.

## Regeneration And Safe-Zone Rules

- Generated `.g.cs` output remains generator-owned and should be fully replaceable.
- User customization should live in companion partials or named hook seams, not in edited generated output.
- `PreserveCompanionPartials` should be the default regeneration stance when a file-emitting flow exists.
- When a requested scaffold shape cannot provide a safe regeneration seam, the implementation should fail clearly or document the unsupported case instead of overwriting user edits silently.
- Destination project and destination folder are future external-scaffolder concerns. They do not become meaningful first-class controls for the in-compilation source generator by themselves.
- Hand-edited safe zones remain a deferred concept until a non-generator file-emission workflow exists.

## Package-Aware Gating Rules

Settings may choose among supported outputs, but settings must not override the package-shape authority already owned by `#127`.

That means:

- `FastMoqAnalysisHelpers.GetGeneratedTestPackageMatrix(...)` remains the authoritative package-layout summary.
- `FastMoqAnalysisHelpers.SupportsGeneratedTestTargetShape(...)` remains the authoritative check for whether a requested generated target shape is valid for the current project.
- `ResolveGeneratedTestPackageLayout(...)` and `BuildSupportedGeneratedTestTargetShapes(...)` remain analyzer-owned capability logic.
- helper-family settings can only request helper output that is already valid for the referenced FastMoq package layout.
- naming, scaffold, provider, or runner settings must never bypass unsupported target-shape or helper-package combinations.

## Settings Versus Invariants

The following items are explicitly not user settings in `#162`.

| Item | Classification | Rationale |
| --- | --- | --- |
| `FastMoq.Generators.FastMoqGeneratedTestTargetAttribute` metadata name | Not a setting | This is the current generator target identity, not an authoring preference. |
| Explicit partial-class requirement and `MockerTestBase<TComponent>` target eligibility | Not a setting | These are current MVP target-shape rules from `#122`. |
| `FastMoqGeneratedHarnessMetadata` type name | Not a setting | Generated metadata naming is part of the current harness contract. |
| `ComponentConstructorParameterTypes` hook name | Not a setting | This is the existing runtime and generated harness hook from `MockerTestBase<TComponent>`. |
| `ConfigureGeneratedMockerPolicy`, `ConfigureGeneratedMocks`, `AfterGeneratedComponentCreated`, `ArrangeGeneratedScenario`, `ActGeneratedScenario`, `ExpectedExceptionGeneratedScenario<TException>`, `AssertGeneratedScenario`, and `VerifyGeneratedScenario` hook names | Not a setting | Current companion partial hook names are generator-owned implementation details today. |
| `ExecuteGeneratedScenarioScaffold`, `ExecuteGeneratedScenarioScaffoldAsync`, `ExecuteGeneratedExpectedExceptionScenarioScaffold<TException>`, and `ExecuteGeneratedExpectedExceptionScenarioScaffoldAsync<TException>` member names | Not a setting | Current generated scenario entry-point names are generator-owned implementation details today. |
| `.FastMoq.GeneratedHarness.g.cs` hint-name suffix | Not a setting | File hint naming is generator-owned implementation detail today. |
| Current `// <auto-generated/>` header and `#nullable enable` boilerplate | Not a setting | Generator-owned implementation detail today. |
| `FastMoqGeneratedTestPackageLayout` enum values | Not a setting | Analyzer-owned capability boundary from `#127`. |
| `GeneratedTestTargetShape` enum values | Not a setting | Analyzer-owned target-shape boundary from `#127`. |
| Default required package per target shape | Not a setting | Capability mapping remains analyzer-owned. |
| Default base-type and namespace lists per target shape | Not a setting | Capability mapping remains analyzer-owned. |

The following items are candidate future settings or implementation concerns rather than settled invariants:

| Item | Classification | Rationale |
| --- | --- | --- |
| Naming templates | Candidate future setting | Authoring-time concern consumed by later scaffold and full-test flows. |
| Framework syntax target | Candidate future setting | Authoring-time concern, distinct from runner/bootstrap. |
| Runner/bootstrap mode | Candidate future setting | Authoring-time concern, distinct from syntax. |
| Provider bootstrap style | Candidate future setting | Authoring-time concern for generated output shape. |
| Compiler-visible settings bridge | Future implementation concern | Required before custom settings can flow into Roslyn code, but not itself a user setting. |

This section exists to keep `#162` from silently reopening `#122` or `#127`.

## Explicit Follow-On Boundaries

- `#162` ends once the shared settings contract, carrier model, precedence rules, support matrix, extensibility model, and invariants are documented and mirrored into the roadmap.
- `#126` should then focus only on regeneration-safe scenario/scaffold hooks that consume this contract.
- the first `#136` slice now implements scenario and suite scaffolding against the `#126` hook contract plus the `#162` settings vocabulary, but future settings wiring still needs the compiler-visible-property bridge above before consumers can override those defaults.
- `#123` should implement full generated tests against this contract without inventing local defaults for framework syntax, runner/bootstrap mode, naming, or regeneration behavior.
- `#124` should route analyzer suggestions into already-supported generation layers while using this contract for defaults and choices.
- `#137` should only adopt this contract if helper-builder output actually needs the same naming, placement, regeneration, or framework-targeting choices.
- provider-optimized generation evaluation in `#138` and narrower fake-generation evaluation in `#139` remain intentionally later.
