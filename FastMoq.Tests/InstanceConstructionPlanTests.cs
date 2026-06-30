using FastMoq.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
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
        public void CreateConstructionPlan_ShouldRejectKnownTypeRequests_WhenRuntimeUsesDirectKnownTypeResolution()
        {
            var mocker = new Mocker();

            var action = () => mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(Uri)));

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*known-type path*");
        }

        [Fact]
        public void CreateConstructionPlan_ShouldRejectAbstractTypeRequests_WhenResolvedTypeIsNotConstructible()
        {
            var mocker = new Mocker();

            var action = () => mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(AbstractDependency)));

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*does not resolve to a concrete constructor path*");
        }

        [Fact]
        public void CreateConstructionPlan_ShouldRejectOpenGenericRequests_WhenResolvedTypeIsNotConstructible()
        {
            var mocker = new Mocker();

            var action = () => mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(GenericDependency<>)));

            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*does not resolve to a concrete constructor path*");
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
        public void CreateConstructionPlan_ShouldDescribeMappedTypeRegistrationParameter_AsCustomRegistration()
        {
            var mocker = new Mocker();
            mocker.AddType<IDependency, RegisteredDependency>();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithDependency)));

            plan.Parameters.Should().HaveCount(1);
            plan.Parameters[0].ParameterType.Should().Be(typeof(IDependency));
            plan.Parameters[0].Source.Should().Be(InstanceConstructionParameterSource.CustomRegistration);
        }

        [Fact]
        public void CreateConstructionPlan_ShouldDescribeStoredConstructorArguments_AsCustomRegistration()
        {
            var mocker = new Mocker();
            mocker.AddType<ConcreteDependencyWithValue>(replace: false, args: 42);

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithConfiguredConcreteDependency)));

            plan.Parameters.Should().HaveCount(1);
            plan.Parameters[0].ParameterType.Should().Be(typeof(ConcreteDependencyWithValue));
            plan.Parameters[0].Source.Should().Be(InstanceConstructionParameterSource.CustomRegistration);
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

        private sealed class TargetWithConfiguredConcreteDependency
        {
            public TargetWithConfiguredConcreteDependency(ConcreteDependencyWithValue dependency)
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

        public abstract class AbstractDependency
        {
            protected AbstractDependency()
            {
            }
        }

        public class ConcreteDependencyWithValue
        {
            public ConcreteDependencyWithValue(int value)
            {
            }
        }

        public class GenericDependency<TValue>
        {
            public GenericDependency(TValue value)
            {
            }
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