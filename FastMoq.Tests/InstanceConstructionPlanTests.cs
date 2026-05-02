using FastMoq.Models;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;
using PublicInstanceConstructionRequest = FastMoq.Models.InstanceConstructionRequest;

namespace FastMoq.Tests
{
    public sealed class InstanceConstructionPlanTests
    {
        [Fact]
        public void CreateConstructionPlan_ShouldUseMappedConcreteType_WhenRequestedTypeIsRegisteredAbstraction()
        {
            var mocker = new Mocker();
            mocker.AddType<IMappedService, MappedService>();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(IMappedService)));

            plan.RequestedType.Should().Be(typeof(IMappedService));
            plan.ResolvedType.Should().Be(typeof(MappedService));
            plan.Parameters.Should().BeEmpty();
        }

        [Fact]
        public void CreateConstructionPlan_ShouldDescribeCustomRegistrationParameter_WhenFactoryRegistrationExists()
        {
            var mocker = new Mocker();
            mocker.AddType<IDependency, RegisteredDependency>(_ => new RegisteredDependency());

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithDependency)));

            plan.Parameters.Should().HaveCount(1);
            plan.Parameters[0].Source.Should().Be(InstanceConstructionParameterSource.CustomRegistration);
        }

        [Fact]
        public void CreateConstructionPlan_ShouldDescribeKnownTypeParameter_WhenBuiltInKnownTypeApplies()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithFileSystem)));

            plan.Parameters.Should().HaveCount(1);
            plan.Parameters[0].ParameterType.Should().Be(typeof(IFileSystem));
            plan.Parameters[0].Source.Should().Be(InstanceConstructionParameterSource.KnownType);
        }

        [Fact]
        public void CreateConstructionPlan_ShouldDescribeKeyedParameter_WhenFromKeyedServicesAttributeIsPresent()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithKeyedDependency)));

            plan.Parameters.Should().HaveCount(1);
            plan.Parameters[0].Source.Should().Be(InstanceConstructionParameterSource.KeyedService);
            plan.Parameters[0].ServiceKey.Should().Be("primary");
        }

        [Fact]
        public void CreateConstructionPlan_ShouldDescribeOptionalDefault_WhenOptionalParametersUseDefaults()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithOptionalDependency)));

            plan.Parameters.Should().HaveCount(1);
            plan.Parameters[0].IsOptional.Should().BeTrue();
            plan.Parameters[0].Source.Should().Be(InstanceConstructionParameterSource.OptionalDefault);
        }

        [Fact]
        public void CreateConstructionPlan_ShouldDescribeAutoMockAndTypeDefaultParameters_WhenNoHigherPriorityResolutionExists()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithMixedDependencies)));

            plan.Parameters.Should().HaveCount(2);
            plan.Parameters[0].Source.Should().Be(InstanceConstructionParameterSource.AutoMock);
            plan.Parameters[1].Source.Should().Be(InstanceConstructionParameterSource.TypeDefault);
        }

        [Fact]
        public void CreateConstructionPlan_ShouldDescribeNonSealedConcreteDependencyAsAutoMock_WhenNoCustomRegistrationExists()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithConcreteDependency)));

            plan.Parameters.Should().HaveCount(1);
            plan.Parameters[0].ParameterType.Should().Be(typeof(ConcreteDependency));
            plan.Parameters[0].Source.Should().Be(InstanceConstructionParameterSource.AutoMock);
        }

        [Fact]
        public void CreateConstructionPlan_ShouldDescribeSealedConcreteDependencyAsTypeDefault_WhenNoCustomRegistrationExists()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithSealedConcreteDependency)));

            plan.Parameters.Should().HaveCount(1);
            plan.Parameters[0].ParameterType.Should().Be(typeof(SealedConcreteDependency));
            plan.Parameters[0].Source.Should().Be(InstanceConstructionParameterSource.TypeDefault);
        }

        [Fact]
        public void CreateConstructionPlan_ShouldFlagPreferredConstructorSelection_WhenAttributeIsPresent()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithPreferredConstructor)));

            plan.UsedPreferredConstructorAttribute.Should().BeTrue();
            plan.Parameters.Should().HaveCount(1);
        }

        [Fact]
        public void CreateConstructionPlan_ShouldFlagAmbiguityFallback_WhenConfiguredToPreferParameterlessConstructor()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithAmbiguousConstructors))
            {
                ConstructorAmbiguityBehavior = ConstructorAmbiguityBehavior.PreferParameterlessConstructor,
            });

            plan.UsedAmbiguityFallback.Should().BeTrue();
            plan.Parameters.Should().BeEmpty();
        }

        [Fact]
        public void CreateConstructionPlan_ShouldFlagNonPublicConstructor_WhenFallbackToNonPublicIsRequested()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithPrivateConstructor))
            {
                PublicOnly = false,
            });

            plan.UsedNonPublicConstructor.Should().BeTrue();
            plan.Parameters.Should().HaveCount(1);
        }

        private interface IMappedService;

        private sealed class MappedService : IMappedService;

        public interface IDependency;

        private sealed class RegisteredDependency : IDependency;

        private sealed class TargetWithDependency
        {
            public TargetWithDependency(IDependency dependency)
            {
            }
        }

        private sealed class TargetWithFileSystem
        {
            public TargetWithFileSystem(IFileSystem fileSystem)
            {
            }
        }

        private sealed class TargetWithKeyedDependency
        {
            public TargetWithKeyedDependency([FromKeyedServices("primary")] IDependency dependency)
            {
            }
        }

        private sealed class TargetWithOptionalDependency
        {
            public TargetWithOptionalDependency(string name = "default")
            {
            }
        }

        private sealed class TargetWithMixedDependencies
        {
            public TargetWithMixedDependencies(IDependency dependency, int retryCount)
            {
            }
        }

        private sealed class TargetWithConcreteDependency
        {
            public TargetWithConcreteDependency(ConcreteDependency dependency)
            {
            }
        }

        private sealed class TargetWithSealedConcreteDependency
        {
            public TargetWithSealedConcreteDependency(SealedConcreteDependency dependency)
            {
            }
        }

        public class ConcreteDependency
        {
        }

        private sealed class SealedConcreteDependency
        {
        }

        private sealed class TargetWithPreferredConstructor
        {
            public TargetWithPreferredConstructor()
            {
            }

            [PreferredConstructor]
            public TargetWithPreferredConstructor(IDependency dependency)
            {
            }
        }

        private sealed class TargetWithAmbiguousConstructors
        {
            public TargetWithAmbiguousConstructors()
            {
            }

            public TargetWithAmbiguousConstructors(IDependency dependency)
            {
            }

            public TargetWithAmbiguousConstructors(string name)
            {
            }
        }

        private sealed class TargetWithPrivateConstructor
        {
            private TargetWithPrivateConstructor(IDependency dependency)
            {
            }
        }
    }
}