using System;
using FastMoq.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace FastMoq.Analyzers.Tests
{
    public sealed class GeneratedHarnessSourceGeneratorTests
    {
        private const string RepresentativeConsumingScenarioSource = @"
using FastMoq;
using FastMoq.Generators;
using FastMoq.Models;
using System;
using System.IO.Abstractions;

namespace Demo.Tests;

public sealed class ConstructorSelectionTarget
{
    public ConstructorSelectionTarget()
    {
        ConstructorKind = ""parameterless"";
    }

    public ConstructorSelectionTarget(IFileSystem fileSystem, string value)
    {
        ConstructorKind = ""selected"";
        ValueWasNull = value is null;
    }

    public string ConstructorKind { get; }

    public bool ValueWasNull { get; }
}

[FastMoqGeneratedTestTarget(typeof(ConstructorSelectionTarget), typeof(IFileSystem), typeof(string))]
public partial class GeneratedConstructorHarness : MockerTestBase<ConstructorSelectionTarget>
{
    public string DescribeConstructorKind() => Component.ConstructorKind;

    public bool DescribeValueWasNull() => Component.ValueWasNull;

    public Type?[]? DescribeConstructorTypes() => ComponentConstructorParameterTypes;

    public InstanceConstructionPlan DescribeComponentConstruction() => GetComponentConstructionPlan();
}

public sealed class ManualConstructorHarness : MockerTestBase<ConstructorSelectionTarget>
{
    protected override Type?[]? ComponentConstructorParameterTypes => new Type?[] { typeof(IFileSystem), typeof(string) };

    public string DescribeConstructorKind() => Component.ConstructorKind;

    public bool DescribeValueWasNull() => Component.ValueWasNull;

    public Type?[]? DescribeConstructorTypes() => ComponentConstructorParameterTypes;

    public InstanceConstructionPlan DescribeComponentConstruction() => GetComponentConstructionPlan();
}
";

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldEmitHarnessMetadata_ForSinglePublicConstructorTarget()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public interface IOrderGateway { }

public sealed class OrderSubmitter
{
    public OrderSubmitter(IOrderGateway gateway)
    {
    }
}

[FastMoqGeneratedTestTarget(typeof(OrderSubmitter))]
public partial class OrderSubmitterTests : MockerTestBase<OrderSubmitter>
{
}
";

            var result = await RunGeneratorAsync(source);

            result.DriverDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();
            result.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();

            var generatedSource = result.GeneratedSources.Should().ContainSingle().Subject;
            generatedSource.SourceText.ToString().Should().Contain("protected override global::System.Type?[]? ComponentConstructorParameterTypes =>");
            generatedSource.SourceText.ToString().Should().Contain("typeof(global::Demo.Tests.IOrderGateway)");
            generatedSource.SourceText.ToString().Should().Contain("\"gateway\"");
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldUseExplicitConstructorSignature_ForMultiConstructorTarget()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public interface IOrderGateway { }

public sealed class OrderSubmitter
{
    public OrderSubmitter()
    {
    }

    public OrderSubmitter(IOrderGateway gateway)
    {
    }
}

[FastMoqGeneratedTestTarget(typeof(OrderSubmitter), typeof(IOrderGateway))]
public partial class OrderSubmitterTests : MockerTestBase<OrderSubmitter>
{
}
";

            var result = await RunGeneratorAsync(source);

            result.DriverDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();
            result.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();

            var generatedSource = result.GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
            generatedSource.Should().Contain("typeof(global::Demo.Tests.IOrderGateway)");
            generatedSource.Should().NotContain("new global::System.Type?[]\r\n            {\r\n            };");
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldUseExplicitParameterlessConstructor_ForMultiConstructorTarget()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;
using System;

namespace Demo.Tests;

public interface IOrderGateway { }

public sealed class OrderSubmitter
{
    public OrderSubmitter()
    {
        ConstructorKind = ""parameterless"";
    }

    public OrderSubmitter(IOrderGateway gateway)
    {
        ConstructorKind = ""dependency"";
    }

    public string ConstructorKind { get; }
}

[FastMoqGeneratedTestTarget(typeof(OrderSubmitter), new global::System.Type[] { })]
public partial class OrderSubmitterTests : MockerTestBase<OrderSubmitter>
{
    public string DescribeConstructorKind() => Component.ConstructorKind;

    public Type?[]? DescribeConstructorTypes() => ComponentConstructorParameterTypes;
}
";

            var loadedAssembly = await LoadGeneratedAssemblyAsync(source);
            var generatedHarness = CreateInstance(loadedAssembly, "Demo.Tests.OrderSubmitterTests");

            Invoke<string>(generatedHarness, "DescribeConstructorKind").Should().Be("parameterless");
            Invoke<Type?[]>(generatedHarness, "DescribeConstructorTypes").Should().BeEmpty();
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldNotEmit_WhenMultiplePublicConstructorsRemainAmbiguous()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public interface IOrderGateway { }
public interface IAuditWriter { }

public sealed class OrderSubmitter
{
    public OrderSubmitter(IOrderGateway gateway)
    {
    }

    public OrderSubmitter(IAuditWriter auditWriter)
    {
    }
}

[FastMoqGeneratedTestTarget(typeof(OrderSubmitter))]
public partial class OrderSubmitterTests : MockerTestBase<OrderSubmitter>
{
}
";

            var result = await RunGeneratorAsync(source);

            result.DriverDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();
            result.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();
            result.GeneratedSources.Should().BeEmpty();
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldWorkInRepresentativeCompiledConsumerScenario()
        {
            var loadedAssembly = await LoadGeneratedAssemblyAsync(RepresentativeConsumingScenarioSource);

            var generatedHarness = CreateInstance(loadedAssembly, "Demo.Tests.GeneratedConstructorHarness");

            Invoke<string>(generatedHarness, "DescribeConstructorKind").Should().Be("selected");
            Invoke<Type?[]>(generatedHarness, "DescribeConstructorTypes")
                .Should()
                .Equal([typeof(System.IO.Abstractions.IFileSystem), typeof(string)]);

            var generatedPlan = Invoke<FastMoq.Models.InstanceConstructionPlan>(generatedHarness, "DescribeComponentConstruction");
            generatedPlan.Parameters.Should().HaveCount(2);
            generatedPlan.Parameters[0].Name.Should().Be("fileSystem");
            generatedPlan.Parameters[0].ParameterType.Should().Be(typeof(System.IO.Abstractions.IFileSystem));
            generatedPlan.Parameters[1].Name.Should().Be("value");
            generatedPlan.Parameters[1].ParameterType.Should().Be(typeof(string));
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldMatchManualRuntimeHarness_ForRepresentativeConsumerScenario()
        {
            var loadedAssembly = await LoadGeneratedAssemblyAsync(RepresentativeConsumingScenarioSource);

            var generatedHarness = CreateInstance(loadedAssembly, "Demo.Tests.GeneratedConstructorHarness");
            var manualHarness = CreateInstance(loadedAssembly, "Demo.Tests.ManualConstructorHarness");

            var generatedPlan = Invoke<FastMoq.Models.InstanceConstructionPlan>(generatedHarness, "DescribeComponentConstruction");
            var manualPlan = Invoke<FastMoq.Models.InstanceConstructionPlan>(manualHarness, "DescribeComponentConstruction");

            Invoke<string>(generatedHarness, "DescribeConstructorKind").Should().Be(Invoke<string>(manualHarness, "DescribeConstructorKind"));
            Invoke<bool>(generatedHarness, "DescribeValueWasNull").Should().Be(Invoke<bool>(manualHarness, "DescribeValueWasNull"));
            Invoke<Type?[]>(generatedHarness, "DescribeConstructorTypes").Should().Equal(Invoke<Type?[]>(manualHarness, "DescribeConstructorTypes"));

            generatedPlan.RequestedType.Should().Be(manualPlan.RequestedType);
            generatedPlan.ResolvedType.Should().Be(manualPlan.ResolvedType);
            generatedPlan.UsedNonPublicConstructor.Should().Be(manualPlan.UsedNonPublicConstructor);
            generatedPlan.UsedPreferredConstructorAttribute.Should().Be(manualPlan.UsedPreferredConstructorAttribute);
            generatedPlan.UsedAmbiguityFallback.Should().Be(manualPlan.UsedAmbiguityFallback);
            generatedPlan.Parameters.Select(static parameter => parameter.Name)
                .Should()
                .Equal(manualPlan.Parameters.Select(static parameter => parameter.Name));
            generatedPlan.Parameters.Select(static parameter => parameter.ParameterType)
                .Should()
                .Equal(manualPlan.Parameters.Select(static parameter => parameter.ParameterType));
            generatedPlan.Parameters.Select(static parameter => parameter.Source)
                .Should()
                .Equal(manualPlan.Parameters.Select(static parameter => parameter.Source));

            var metadataType = generatedHarness.GetType().GetNestedType("FastMoqGeneratedHarnessMetadata", BindingFlags.NonPublic);
            metadataType.Should().NotBeNull();
            var dependencyNames = (string[]) metadataType!.GetProperty("DependencyNames", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            var dependencyTypes = (Type[]) metadataType.GetProperty("DependencyTypes", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

            dependencyNames.Should().Equal(generatedPlan.Parameters.Select(static parameter => parameter.Name));
            dependencyTypes.Should().Equal(generatedPlan.Parameters.Select(static parameter => parameter.ParameterType));
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldEmitExecutableScenarioScaffold_ForGeneratedHarnessTarget()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public sealed class ScenarioCounter
{
    public int Count { get; private set; }

    public bool WasVerified { get; private set; }

    public void Increment()
    {
        Count++;
    }

    public void MarkVerified()
    {
        WasVerified = true;
    }
}

[FastMoqGeneratedTestTarget(typeof(ScenarioCounter))]
public partial class GeneratedScenarioHarness : MockerTestBase<ScenarioCounter>
{
    public int DescribeCount() => Component.Count;

    public bool DescribeWasVerified() => Component.WasVerified;

    partial void ActGeneratedScenario(ScenarioBuilder<ScenarioCounter> scenario)
    {
        scenario.When(component => component.Increment());
    }

    partial void AssertGeneratedScenario(ScenarioBuilder<ScenarioCounter> scenario)
    {
        scenario.Then(component =>
        {
            if (component.Count != 1)
            {
                throw new global::System.InvalidOperationException(""Expected exactly one increment."");
            }
        });
    }

    partial void VerifyGeneratedScenario(ScenarioBuilder<ScenarioCounter> scenario)
    {
        scenario.Then(component => component.MarkVerified());
    }
}
";

            var loadedAssembly = await LoadGeneratedAssemblyAsync(source);
            var generatedHarness = CreateInstance(loadedAssembly, "Demo.Tests.GeneratedScenarioHarness");

            Invoke<object?>(generatedHarness, "ExecuteGeneratedScenarioScaffold");

            Invoke<int>(generatedHarness, "DescribeCount").Should().Be(1);
            Invoke<bool>(generatedHarness, "DescribeWasVerified").Should().BeTrue();
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldEmitExecutableAsyncScenarioScaffold_ForGeneratedHarnessTarget()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public sealed class AsyncScenarioCounter
{
    public int Count { get; private set; }

    public bool WasVerified { get; private set; }

    public void Increment()
    {
        Count++;
    }

    public void MarkVerified()
    {
        WasVerified = true;
    }
}

[FastMoqGeneratedTestTarget(typeof(AsyncScenarioCounter))]
public partial class GeneratedAsyncScenarioHarness : MockerTestBase<AsyncScenarioCounter>
{
    public int DescribeCount() => Component.Count;

    public bool DescribeWasVerified() => Component.WasVerified;

    partial void ActGeneratedScenario(ScenarioBuilder<AsyncScenarioCounter> scenario)
    {
        scenario.When(async component =>
        {
            await global::System.Threading.Tasks.Task.CompletedTask;
            component.Increment();
        });
    }

    partial void AssertGeneratedScenario(ScenarioBuilder<AsyncScenarioCounter> scenario)
    {
        scenario.Then(async component =>
        {
            await global::System.Threading.Tasks.Task.CompletedTask;
            if (component.Count != 1)
            {
                throw new global::System.InvalidOperationException(""Expected exactly one increment."");
            }
        });
    }

    partial void VerifyGeneratedScenario(ScenarioBuilder<AsyncScenarioCounter> scenario)
    {
        scenario.Then(async component =>
        {
            await global::System.Threading.Tasks.Task.CompletedTask;
            component.MarkVerified();
        });
    }
}
";

            var loadedAssembly = await LoadGeneratedAssemblyAsync(source);
            var generatedHarness = CreateInstance(loadedAssembly, "Demo.Tests.GeneratedAsyncScenarioHarness");

            await Invoke<global::System.Threading.Tasks.Task>(generatedHarness, "ExecuteGeneratedScenarioScaffoldAsync");

            Invoke<int>(generatedHarness, "DescribeCount").Should().Be(1);
            Invoke<bool>(generatedHarness, "DescribeWasVerified").Should().BeTrue();
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldEmitExecutableExpectedExceptionScenarioScaffold_ForGeneratedHarnessTarget()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public sealed class ThrowingScenarioCounter
{
    public bool AssertedAfterExpectedException { get; private set; }

    public bool VerifiedAfterExpectedException { get; private set; }

    public void Throw()
    {
        throw new global::System.InvalidOperationException(""boom"");
    }

    public void MarkAsserted()
    {
        AssertedAfterExpectedException = true;
    }

    public void MarkVerified()
    {
        VerifiedAfterExpectedException = true;
    }
}

[FastMoqGeneratedTestTarget(typeof(ThrowingScenarioCounter))]
public partial class GeneratedExpectedExceptionScenarioHarness : MockerTestBase<ThrowingScenarioCounter>
{
    public bool DescribeAssertedAfterExpectedException() => Component.AssertedAfterExpectedException;

    public bool DescribeVerifiedAfterExpectedException() => Component.VerifiedAfterExpectedException;

    partial void ExpectedExceptionGeneratedScenario<TException>(ScenarioBuilder<ThrowingScenarioCounter> scenario) where TException : global::System.Exception
    {
        scenario.WhenThrows<TException>(component => component.Throw());
    }

    partial void AssertGeneratedScenario(ScenarioBuilder<ThrowingScenarioCounter> scenario)
    {
        scenario.Then(component => component.MarkAsserted());
    }

    partial void VerifyGeneratedScenario(ScenarioBuilder<ThrowingScenarioCounter> scenario)
    {
        scenario.Then(component => component.MarkVerified());
    }
}
";

            var loadedAssembly = await LoadGeneratedAssemblyAsync(source);
            var generatedHarness = CreateInstance(loadedAssembly, "Demo.Tests.GeneratedExpectedExceptionScenarioHarness");

            InvokeGenericVoid(generatedHarness, "ExecuteGeneratedExpectedExceptionScenarioScaffold", typeof(global::System.InvalidOperationException));

            Invoke<bool>(generatedHarness, "DescribeAssertedAfterExpectedException").Should().BeTrue();
            Invoke<bool>(generatedHarness, "DescribeVerifiedAfterExpectedException").Should().BeTrue();

            await InvokeGeneric<global::System.Threading.Tasks.Task>(generatedHarness, "ExecuteGeneratedExpectedExceptionScenarioScaffoldAsync", typeof(global::System.InvalidOperationException));

            Invoke<bool>(generatedHarness, "DescribeAssertedAfterExpectedException").Should().BeTrue();
            Invoke<bool>(generatedHarness, "DescribeVerifiedAfterExpectedException").Should().BeTrue();
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldEmitSuiteLevelSharedSetupHooks_ForGeneratedHarnessTarget()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public interface ISharedSetupDependency
{
    string Name { get; }
}

public sealed class SharedSetupDependency : ISharedSetupDependency
{
    public string Name => ""shared"";
}

public sealed class SharedSetupTarget
{
    public SharedSetupTarget(ISharedSetupDependency dependency)
    {
        DependencyName = dependency.Name;
    }

    public string DependencyName { get; }

    public bool WasCreatedHookApplied { get; private set; }

    public bool WasScenarioExecuted { get; private set; }

    public void MarkCreatedHookApplied()
    {
        WasCreatedHookApplied = true;
    }

    public void MarkScenarioExecuted()
    {
        WasScenarioExecuted = true;
    }
}

[FastMoqGeneratedTestTarget(typeof(SharedSetupTarget))]
public partial class GeneratedSharedSetupHarness : MockerTestBase<SharedSetupTarget>
{
    public string DescribeDependencyName() => Component.DependencyName;

    public bool DescribeWasCreatedHookApplied() => Component.WasCreatedHookApplied;

    public bool DescribeWasScenarioExecuted() => Component.WasScenarioExecuted;

    public bool? DescribeStrictPolicy() => Mocks.Policy.DefaultStrictMockCreation;

    partial void ConfigureGeneratedMockerPolicy(MockerPolicyOptions options)
    {
        options.DefaultStrictMockCreation = true;
    }

    partial void ConfigureGeneratedMocks(Mocker mocker)
    {
        mocker.AddType<ISharedSetupDependency>(new SharedSetupDependency());
    }

    partial void AfterGeneratedComponentCreated(SharedSetupTarget component)
    {
        component.MarkCreatedHookApplied();
    }

    partial void ActGeneratedScenario(ScenarioBuilder<SharedSetupTarget> scenario)
    {
        scenario.When(component => component.MarkScenarioExecuted());
    }
}
";

            var loadedAssembly = await LoadGeneratedAssemblyAsync(source);
            var generatedHarness = CreateInstance(loadedAssembly, "Demo.Tests.GeneratedSharedSetupHarness");

            Invoke<string>(generatedHarness, "DescribeDependencyName").Should().Be("shared");
            Invoke<bool>(generatedHarness, "DescribeWasCreatedHookApplied").Should().BeTrue();
            Invoke<bool?>(generatedHarness, "DescribeStrictPolicy").Should().BeTrue();

            Invoke<object?>(generatedHarness, "ExecuteGeneratedScenarioScaffold");

            Invoke<bool>(generatedHarness, "DescribeWasScenarioExecuted").Should().BeTrue();
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldPreserveSuiteLevelSharedSetupHookOrder_ForGeneratedHarnessTarget()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public static class SharedSetupEventLog
{
    public static global::System.Collections.Generic.List<string> Events { get; } = new global::System.Collections.Generic.List<string>();

    public static void Record(string eventName)
    {
        Events.Add(eventName);
    }

    public static string Describe()
    {
        return global::System.String.Join(""|"", Events);
    }
}

public interface ISharedSetupDependency
{
    string Name { get; }
}

public sealed class SharedSetupDependency : ISharedSetupDependency
{
    public string Name => ""shared"";
}

public sealed class SharedSetupTarget
{
    public SharedSetupTarget(ISharedSetupDependency dependency)
    {
        SharedSetupEventLog.Record(""constructed"");
        DependencyName = dependency.Name;
    }

    public string DependencyName { get; }

    public bool WasCreatedHookApplied { get; private set; }

    public bool WasScenarioExecuted { get; private set; }

    public void MarkCreatedHookApplied()
    {
        WasCreatedHookApplied = true;
    }

    public void MarkScenarioExecuted()
    {
        WasScenarioExecuted = true;
    }
}

public abstract class SharedSetupHarnessBase : MockerTestBase<SharedSetupTarget>
{
    protected override global::System.Action<MockerPolicyOptions>? ConfigureMockerPolicy =>
        _ => SharedSetupEventLog.Record(""base-policy"");

    protected override global::System.Action<Mocker>? SetupMocksAction =>
        _ => SharedSetupEventLog.Record(""base-setup"");

    protected override global::System.Action<SharedSetupTarget>? CreatedComponentAction =>
        _ => SharedSetupEventLog.Record(""base-created"");
}

[FastMoqGeneratedTestTarget(typeof(SharedSetupTarget))]
public partial class GeneratedSharedSetupHarness : SharedSetupHarnessBase
{
    public string DescribeDependencyName() => Component.DependencyName;

    public bool DescribeWasCreatedHookApplied() => Component.WasCreatedHookApplied;

    public bool DescribeWasScenarioExecuted() => Component.WasScenarioExecuted;

    public bool? DescribeStrictPolicy() => Mocks.Policy.DefaultStrictMockCreation;

    public string DescribeHookOrder() => SharedSetupEventLog.Describe();

    partial void ConfigureGeneratedMockerPolicy(MockerPolicyOptions options)
    {
        SharedSetupEventLog.Record(""generated-policy"");
        options.DefaultStrictMockCreation = true;
    }

    partial void ConfigureGeneratedMocks(Mocker mocker)
    {
        SharedSetupEventLog.Record(""generated-setup"");
        mocker.AddType<ISharedSetupDependency>(new SharedSetupDependency());
    }

    partial void AfterGeneratedComponentCreated(SharedSetupTarget component)
    {
        SharedSetupEventLog.Record(""generated-created"");
        component.MarkCreatedHookApplied();
    }

    partial void ActGeneratedScenario(ScenarioBuilder<SharedSetupTarget> scenario)
    {
        scenario.When(component =>
        {
            SharedSetupEventLog.Record(""act"");
            component.MarkScenarioExecuted();
        });
    }
}
";

            var loadedAssembly = await LoadGeneratedAssemblyAsync(source);
            var generatedHarness = CreateInstance(loadedAssembly, "Demo.Tests.GeneratedSharedSetupHarness");

            Invoke<string>(generatedHarness, "DescribeDependencyName").Should().Be("shared");
            Invoke<bool>(generatedHarness, "DescribeWasCreatedHookApplied").Should().BeTrue();
            Invoke<bool>(generatedHarness, "DescribeWasScenarioExecuted").Should().BeFalse();
            Invoke<bool?>(generatedHarness, "DescribeStrictPolicy").Should().BeTrue();
            Invoke<string>(generatedHarness, "DescribeHookOrder").Should().Be("base-policy|generated-policy|base-setup|generated-setup|constructed|base-created|generated-created");

            Invoke<object?>(generatedHarness, "ExecuteGeneratedScenarioScaffold");

            Invoke<bool>(generatedHarness, "DescribeWasScenarioExecuted").Should().BeTrue();
            Invoke<string>(generatedHarness, "DescribeHookOrder").Should().Be("base-policy|generated-policy|base-setup|generated-setup|constructed|base-created|generated-created|act");
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldEmitExecutableXunitSmokeTests_ForParameterlessPublicMethods()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public sealed class SmokeTarget
{
    public int Count { get; private set; }

    public int GetCount()
    {
        return Count;
    }

    public void Increment()
    {
        Count++;
    }

    public async global::System.Threading.Tasks.Task IncrementAsync()
    {
        await global::System.Threading.Tasks.Task.CompletedTask;
        Count++;
    }
}

[FastMoqGeneratedTestTarget(typeof(SmokeTarget))]
public partial class GeneratedSmokeHarness : MockerTestBase<SmokeTarget>
{
    public int DescribeCount() => Component.Count;
}
";

            var result = await RunGeneratorAsync(source);

            result.DriverDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();
            result.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();

            var generatedSource = result.GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
            generatedSource.Should().Contain("[global::Xunit.Fact]");
            generatedSource.Should().Contain("FastMoqGeneratedSmokeTest_00_Component_ShouldCreateComponent");
            generatedSource.Should().Contain("FastMoqGeneratedSmokeTest_01_GetCount_ShouldExecuteWithoutThrowing");
            generatedSource.Should().Contain("FastMoqGeneratedSmokeTest_02_Increment_ShouldExecuteWithoutThrowing");
            generatedSource.Should().Contain("FastMoqGeneratedSmokeTest_03_IncrementAsync_ShouldExecuteWithoutThrowing");

            var loadedAssembly = await LoadGeneratedAssemblyAsync(source);
            var generatedHarness = CreateInstance(loadedAssembly, "Demo.Tests.GeneratedSmokeHarness");

            InvokeVoid(generatedHarness, "FastMoqGeneratedSmokeTest_00_Component_ShouldCreateComponent");
            InvokeVoid(generatedHarness, "FastMoqGeneratedSmokeTest_01_GetCount_ShouldExecuteWithoutThrowing");
            Invoke<int>(generatedHarness, "DescribeCount").Should().Be(0);

            InvokeVoid(generatedHarness, "FastMoqGeneratedSmokeTest_02_Increment_ShouldExecuteWithoutThrowing");
            Invoke<int>(generatedHarness, "DescribeCount").Should().Be(1);

            await Invoke<global::System.Threading.Tasks.Task>(generatedHarness, "FastMoqGeneratedSmokeTest_03_IncrementAsync_ShouldExecuteWithoutThrowing");
            Invoke<int>(generatedHarness, "DescribeCount").Should().Be(2);
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldEmitExecutableXunitSmokeTests_ForMethodsWithOptionalDefaults_AndValueTaskShapes()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public sealed class OptionalDefaultsTarget
{
    public string LastMessage { get; private set; } = string.Empty;

    public int Count { get; private set; }

    public int Add(int amount = 3)
    {
        Count += amount;
        return Count;
    }

    public global::System.Threading.Tasks.ValueTask<string> LoadAsync(string prefix = ""value"", int suffix = 7)
    {
        LastMessage = prefix + suffix;
        return new global::System.Threading.Tasks.ValueTask<string>(LastMessage);
    }

    public global::System.Threading.Tasks.ValueTask ResetAsync(bool enabled = true)
    {
        if (enabled)
        {
            Count = 0;
        }

        return global::System.Threading.Tasks.ValueTask.CompletedTask;
    }
}

[FastMoqGeneratedTestTarget(typeof(OptionalDefaultsTarget))]
public partial class GeneratedOptionalDefaultsHarness : MockerTestBase<OptionalDefaultsTarget>
{
    public int DescribeCount() => Component.Count;

    public string DescribeLastMessage() => Component.LastMessage;
}
";

            var result = await RunGeneratorAsync(source);

            result.DriverDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();
            result.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();

            var generatedSource = result.GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
            generatedSource.Should().Contain("FastMoqGeneratedSmokeTest_01_Add_ShouldExecuteWithoutThrowing");
            generatedSource.Should().Contain("_ = component.Add((int)3);");
            generatedSource.Should().Contain("FastMoqGeneratedSmokeTest_02_LoadAsync_ShouldExecuteWithoutThrowing");
            generatedSource.Should().Contain("_ = await component.LoadAsync((string)\"value\", (int)7);");
            generatedSource.Should().Contain("FastMoqGeneratedSmokeTest_03_ResetAsync_ShouldExecuteWithoutThrowing");
            generatedSource.Should().Contain("await component.ResetAsync((bool)true);");

            var loadedAssembly = await LoadGeneratedAssemblyAsync(source);
            var generatedHarness = CreateInstance(loadedAssembly, "Demo.Tests.GeneratedOptionalDefaultsHarness");

            InvokeVoid(generatedHarness, "FastMoqGeneratedSmokeTest_01_Add_ShouldExecuteWithoutThrowing");
            Invoke<int>(generatedHarness, "DescribeCount").Should().Be(3);

            await Invoke<global::System.Threading.Tasks.Task>(generatedHarness, "FastMoqGeneratedSmokeTest_02_LoadAsync_ShouldExecuteWithoutThrowing");
            Invoke<string>(generatedHarness, "DescribeLastMessage").Should().Be("value7");

            await Invoke<global::System.Threading.Tasks.Task>(generatedHarness, "FastMoqGeneratedSmokeTest_03_ResetAsync_ShouldExecuteWithoutThrowing");
            Invoke<int>(generatedHarness, "DescribeCount").Should().Be(0);
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldBindOverloadedMethodsUsingTypedOptionalDefaults()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public sealed class OverloadedOptionalDefaultsTarget
{
    public string LastCall { get; private set; } = string.Empty;

    public void Choose(object? value = null)
    {
        LastCall = value is null ? ""object-null"" : value.ToString()!;
    }

    public void Choose(string value = ""text"")
    {
        LastCall = ""string:"" + value;
    }
}

[FastMoqGeneratedTestTarget(typeof(OverloadedOptionalDefaultsTarget))]
public partial class GeneratedOverloadedOptionalDefaultsHarness : MockerTestBase<OverloadedOptionalDefaultsTarget>
{
    public string DescribeLastCall() => Component.LastCall;
}
";

            var result = await RunGeneratorAsync(source);

            result.DriverDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();
            result.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();

            var generatedSource = result.GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
            generatedSource.Should().Contain("component.Choose((object)null);");
            generatedSource.Should().Contain("component.Choose((string)\"text\");");

            var loadedAssembly = await LoadGeneratedAssemblyAsync(source);
            var generatedHarness = CreateInstance(loadedAssembly, "Demo.Tests.GeneratedOverloadedOptionalDefaultsHarness");

            InvokeVoid(generatedHarness, "FastMoqGeneratedSmokeTest_01_Choose_ShouldExecuteWithoutThrowing");
            Invoke<string>(generatedHarness, "DescribeLastCall").Should().Be("object-null");

            InvokeVoid(generatedHarness, "FastMoqGeneratedSmokeTest_02_Choose_ShouldExecuteWithoutThrowing");
            Invoke<string>(generatedHarness, "DescribeLastCall").Should().Be("string:text");
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldEmitDeferredXunitPlaceholders_ForUnsupportedPublicMethods()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public sealed class DeferredSmokeTarget
{
    public global::System.Threading.Tasks.ValueTask<int> LoadAsync()
    {
        return new global::System.Threading.Tasks.ValueTask<int>(1);
    }

    public void Process(string value)
    {
    }
}

[FastMoqGeneratedTestTarget(typeof(DeferredSmokeTarget))]
public partial class GeneratedDeferredSmokeHarness : MockerTestBase<DeferredSmokeTarget>
{
}
";

            var result = await RunGeneratorAsync(source);

            result.DriverDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();
            result.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();

            var generatedSource = result.GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
            generatedSource.Should().Contain("[global::Xunit.Fact]");
            generatedSource.Should().Contain("FastMoqGeneratedSmokeTest_01_LoadAsync_ShouldExecuteWithoutThrowing");
            generatedSource.Should().Contain("[global::Xunit.Fact(Skip = \"FastMoq generated smoke test deferred: method 'void DeferredSmokeTarget.Process(string value)' requires non-optional parameters.\")]");
            generatedSource.Should().Contain("FastMoqGeneratedPlaceholder_02_Process_IsDeferred");

            var loadedAssembly = await LoadGeneratedAssemblyAsync(source);
            var generatedHarness = CreateInstance(loadedAssembly, "Demo.Tests.GeneratedDeferredSmokeHarness");

            await Invoke<global::System.Threading.Tasks.Task>(generatedHarness, "FastMoqGeneratedSmokeTest_01_LoadAsync_ShouldExecuteWithoutThrowing");

            var process = () => InvokeVoid(generatedHarness, "FastMoqGeneratedPlaceholder_02_Process_IsDeferred");
            process.Should().Throw<TargetInvocationException>()
                .WithInnerException<NotSupportedException>()
                .WithMessage("*DeferredSmokeTarget.Process(string value)*requires non-optional parameters*");
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldNotEmitXunitSmokeTests_WhenXunitIsUnavailable()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public sealed class NoXunitTarget
{
    public void Run()
    {
    }
}

[FastMoqGeneratedTestTarget(typeof(NoXunitTarget))]
public partial class GeneratedNoXunitHarness : MockerTestBase<NoXunitTarget>
{
}
";

            var document = AnalyzerTestHelpers.CreateDocumentForTest(
                source,
                includeAzureFunctionsHelpers: false,
                includeMoqProviderPackage: false,
                includeNSubstituteProviderPackage: false,
                includeWebHelpers: false,
                includeDatabaseHelpers: false,
                includeAzureHelpers: false,
                includeAggregatePackage: false,
                includeXunit: false);
            var compilation = await document.Project.GetCompilationAsync();
            compilation.Should().NotBeNull();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                [new GeneratedHarnessSourceGenerator().AsSourceGenerator()],
                parseOptions: (CSharpParseOptions) document.Project.ParseOptions!);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation!, out var outputCompilation, out var diagnostics);

            diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();
            outputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();

            var generatedSource = driver.GetRunResult().Results.SelectMany(static result => result.GeneratedSources)
                .Should()
                .ContainSingle()
                .Subject.SourceText.ToString();
            generatedSource.Should().NotContain("global::Xunit.Fact");
            generatedSource.Should().NotContain("FastMoqGeneratedSmokeTest_");
            generatedSource.Should().NotContain("FastMoqGeneratedPlaceholder_");
        }

        [Fact]
        public async Task GeneratedHarnessSourceGenerator_ShouldEscapeCSharpKeywordsInGeneratedInvocations()
        {
            const string source = @"
using FastMoq;
using FastMoq.Generators;

namespace Demo.Tests;

public sealed class KeywordTarget
{
    public void @class() { }
    public void @interface() { }
    public void @return() { }
    public void NormalMethod() { }
}

[FastMoqGeneratedTestTarget(typeof(KeywordTarget))]
public partial class GeneratedKeywordHarness : MockerTestBase<KeywordTarget>
{
}
";

            var result = await RunGeneratorAsync(source);

            result.DriverDiagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
            result.OutputCompilation.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();

            var generatedSource = result.GeneratedSources.Should().ContainSingle().Subject.SourceText.ToString();
            // Verify keywords are escaped in the generated invocations
            generatedSource.Should().Contain("component.@class()");
            generatedSource.Should().Contain("component.@interface()");
            generatedSource.Should().Contain("component.@return()");
            generatedSource.Should().Contain("component.NormalMethod()");
            // Verify smoke test methods exist
            generatedSource.Should().Contain("[global::Xunit.Fact]");
            generatedSource.Should().Contain("FastMoqGeneratedSmokeTest_");
        }

        private static async Task<GeneratorTestResult> RunGeneratorAsync(string source)
        {
            var document = AnalyzerTestHelpers.CreateDocumentForTest(
                source,
                includeAzureFunctionsHelpers: false,
                includeMoqProviderPackage: false,
                includeNSubstituteProviderPackage: false,
                includeWebHelpers: false,
                includeDatabaseHelpers: false,
                includeAzureHelpers: false,
                includeAggregatePackage: false);
            var compilation = await document.Project.GetCompilationAsync();
            compilation.Should().NotBeNull();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                [new GeneratedHarnessSourceGenerator().AsSourceGenerator()],
                parseOptions: (CSharpParseOptions) document.Project.ParseOptions!);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation!, out var outputCompilation, out var diagnostics);
            var runResult = driver.GetRunResult();

            return new GeneratorTestResult(
                outputCompilation,
                diagnostics,
                runResult.Results.SelectMany(static result => result.GeneratedSources).ToImmutableArray());
        }

        private static async Task<Assembly> LoadGeneratedAssemblyAsync(string source)
        {
            var result = await RunGeneratorAsync(source);
            result.DriverDiagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();
            result.OutputCompilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Should()
                .BeEmpty();

            using var assemblyStream = new MemoryStream();
            var emitResult = result.OutputCompilation
                .WithAssemblyName("GeneratedHarnessTests_" + System.Guid.NewGuid().ToString("N"))
                .Emit(assemblyStream);
            emitResult.Success.Should().BeTrue(string.Join(System.Environment.NewLine, emitResult.Diagnostics));

            return Assembly.Load(assemblyStream.ToArray());
        }

        private static object CreateInstance(Assembly assembly, string typeName)
        {
            var type = assembly.GetType(typeName, throwOnError: true)!;
            return Activator.CreateInstance(type)!;
        }

        private static T Invoke<T>(object instance, string methodName)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            method.Should().NotBeNull();
            return (T) method!.Invoke(instance, null)!;
        }

        private static void InvokeVoid(object instance, string methodName)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            method.Should().NotBeNull();
            method!.Invoke(instance, null);
        }

        private static void InvokeGenericVoid(object instance, string methodName, params Type[] typeArguments)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            method.Should().NotBeNull();
            method!.MakeGenericMethod(typeArguments).Invoke(instance, null);
        }

        private static T InvokeGeneric<T>(object instance, string methodName, params Type[] typeArguments)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            method.Should().NotBeNull();
            return (T) method!.MakeGenericMethod(typeArguments).Invoke(instance, null)!;
        }

        private sealed class GeneratorTestResult
        {
            public GeneratorTestResult(
                Compilation outputCompilation,
                ImmutableArray<Diagnostic> driverDiagnostics,
                ImmutableArray<GeneratedSourceResult> generatedSources)
            {
                OutputCompilation = outputCompilation;
                DriverDiagnostics = driverDiagnostics;
                GeneratedSources = generatedSources;
            }

            public Compilation OutputCompilation { get; }

            public ImmutableArray<Diagnostic> DriverDiagnostics { get; }

            public ImmutableArray<GeneratedSourceResult> GeneratedSources { get; }
        }
    }
}