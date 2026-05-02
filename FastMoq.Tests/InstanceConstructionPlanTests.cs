using FastMoq.Models;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;
using AwesomeAssertionExtensions = AwesomeAssertions.AssertionExtensions;
using AwesomeEnumAssertionExtensions = AwesomeAssertions.EnumAssertionsExtensions;
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

            AwesomeAssertionExtensions.Should(plan.RequestedType).Be(typeof(IMappedService));
            AwesomeAssertionExtensions.Should(plan.ResolvedType).Be(typeof(MappedService));
            AwesomeAssertionExtensions.Should(plan.Parameters).BeEmpty();
        }

        [Fact]
        public void CreateConstructionPlan_ShouldDescribeCustomRegistrationParameter_WhenFactoryRegistrationExists()
        {
            var mocker = new Mocker();
            mocker.AddType<IDependency, RegisteredDependency>(_ => new RegisteredDependency());

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithDependency)));

            AwesomeAssertionExtensions.Should(plan.Parameters).HaveCount(1);
            AwesomeEnumAssertionExtensions.Should(plan.Parameters[0].Source).Be(InstanceConstructionParameterSource.CustomRegistration);
        }

        [Fact]
        public void CreateConstructionPlan_ShouldDescribeKnownTypeParameter_WhenBuiltInKnownTypeApplies()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithFileSystem)));

            AwesomeAssertionExtensions.Should(plan.Parameters).HaveCount(1);
            AwesomeAssertionExtensions.Should(plan.Parameters[0].ParameterType).Be(typeof(IFileSystem));
            AwesomeEnumAssertionExtensions.Should(plan.Parameters[0].Source).Be(InstanceConstructionParameterSource.KnownType);
        }

        [Fact]
        public void CreateConstructionPlan_ShouldDescribeKeyedParameter_WhenFromKeyedServicesAttributeIsPresent()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithKeyedDependency)));

            AwesomeAssertionExtensions.Should(plan.Parameters).HaveCount(1);
            AwesomeEnumAssertionExtensions.Should(plan.Parameters[0].Source).Be(InstanceConstructionParameterSource.KeyedService);
            AwesomeAssertionExtensions.Should(plan.Parameters[0].ServiceKey).Be("primary");
        }

        [Fact]
        public void CreateConstructionPlan_ShouldDescribeOptionalDefault_WhenOptionalParametersUseDefaults()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithOptionalDependency)));

            AwesomeAssertionExtensions.Should(plan.Parameters).HaveCount(1);
            AwesomeAssertionExtensions.Should(plan.Parameters[0].IsOptional).BeTrue();
            AwesomeEnumAssertionExtensions.Should(plan.Parameters[0].Source).Be(InstanceConstructionParameterSource.OptionalDefault);
        }

        [Fact]
        public void CreateConstructionPlan_ShouldDescribeAutoMockAndTypeDefaultParameters_WhenNoHigherPriorityResolutionExists()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithMixedDependencies)));

            AwesomeAssertionExtensions.Should(plan.Parameters).HaveCount(2);
            AwesomeEnumAssertionExtensions.Should(plan.Parameters[0].Source).Be(InstanceConstructionParameterSource.AutoMock);
            AwesomeEnumAssertionExtensions.Should(plan.Parameters[1].Source).Be(InstanceConstructionParameterSource.TypeDefault);
        }

        [Fact]
        public void CreateConstructionPlan_ShouldDescribeNonSealedConcreteDependencyAsAutoMock_WhenNoCustomRegistrationExists()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithConcreteDependency)));

            AwesomeAssertionExtensions.Should(plan.Parameters).HaveCount(1);
            AwesomeAssertionExtensions.Should(plan.Parameters[0].ParameterType).Be(typeof(ConcreteDependency));
            AwesomeEnumAssertionExtensions.Should(plan.Parameters[0].Source).Be(InstanceConstructionParameterSource.AutoMock);
        }

        [Fact]
        public void CreateConstructionPlan_ShouldDescribeSealedConcreteDependencyAsTypeDefault_WhenNoCustomRegistrationExists()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithSealedConcreteDependency)));

            AwesomeAssertionExtensions.Should(plan.Parameters).HaveCount(1);
            AwesomeAssertionExtensions.Should(plan.Parameters[0].ParameterType).Be(typeof(SealedConcreteDependency));
            AwesomeEnumAssertionExtensions.Should(plan.Parameters[0].Source).Be(InstanceConstructionParameterSource.TypeDefault);
        }

        [Fact]
        public void CreateConstructionPlan_ShouldFlagPreferredConstructorSelection_WhenAttributeIsPresent()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithPreferredConstructor)));

            AwesomeAssertionExtensions.Should(plan.UsedPreferredConstructorAttribute).BeTrue();
            AwesomeAssertionExtensions.Should(plan.Parameters).HaveCount(1);
        }

        [Fact]
        public void CreateConstructionPlan_ShouldFlagAmbiguityFallback_WhenConfiguredToPreferParameterlessConstructor()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithAmbiguousConstructors))
            {
                ConstructorAmbiguityBehavior = ConstructorAmbiguityBehavior.PreferParameterlessConstructor,
            });

            AwesomeAssertionExtensions.Should(plan.UsedAmbiguityFallback).BeTrue();
            AwesomeAssertionExtensions.Should(plan.Parameters).BeEmpty();
        }

        [Fact]
        public void CreateConstructionPlan_ShouldFlagNonPublicConstructor_WhenFallbackToNonPublicIsRequested()
        {
            var mocker = new Mocker();

            var plan = mocker.CreateConstructionPlan(new PublicInstanceConstructionRequest(typeof(TargetWithPrivateConstructor))
            {
                PublicOnly = false,
            });

            AwesomeAssertionExtensions.Should(plan.UsedNonPublicConstructor).BeTrue();
            AwesomeAssertionExtensions.Should(plan.Parameters).HaveCount(1);
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