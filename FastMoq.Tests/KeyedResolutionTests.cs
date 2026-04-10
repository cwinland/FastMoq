using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace FastMoq.Tests
{
    public class KeyedResolutionTests
    {
        [Fact]
        public void GetKeyedObject_ShouldReturnRegisteredInstance_ForMatchingKey()
        {
            var mocker = new Mocker();
            var expected = new Uri("http://alpha.fastmoq/");

            mocker.AddKeyedType<Uri>("alpha", _ => expected);

            var resolved = mocker.GetKeyedObject<Uri>("alpha");

            resolved.Should().BeSameAs(expected);
        }

        [Fact]
        public void GetOrCreateMock_ShouldReturnSameTrackedMock_ForRepeatedKey()
        {
            var mocker = new Mocker();
            var alphaOptions = new MockRequestOptions { ServiceKey = "alpha" };
            var betaOptions = new MockRequestOptions { ServiceKey = "beta" };

            var first = mocker.GetOrCreateMock<IKeyedDependency>(alphaOptions);
            var second = mocker.GetOrCreateMock<IKeyedDependency>(alphaOptions);
            var other = mocker.GetOrCreateMock<IKeyedDependency>(betaOptions);

            second.Should().BeSameAs(first);
            other.Should().NotBeSameAs(first);
        }

        [Fact]
        public void CreateInstance_ShouldResolveKeyedDependencies_FromStandardDiAttributes()
        {
            var mocker = new Mocker();
            var primaryUri = new Uri("http://primary.fastmoq/");
            var secondaryUri = new Uri("http://secondary.fastmoq/");
            var keyedMock = mocker.GetOrCreateMock<IKeyedDependency>(new MockRequestOptions
            {
                ServiceKey = "dep",
            });

            mocker.AddKeyedType<Uri>("primary", _ => primaryUri);
            mocker.AddKeyedType<Uri>("secondary", _ => secondaryUri);

            var instance = mocker.CreateInstance<KeyedConsumer>();

            instance.Should().NotBeNull();
            instance!.PrimaryUri.Should().BeSameAs(primaryUri);
            instance.SecondaryUri.Should().BeSameAs(secondaryUri);
            instance.Dependency.Should().BeSameAs(keyedMock.Instance);
            instance.DefaultHttpClient.Should().BeSameAs(mocker.HttpClient);
            instance.DefaultUri.Should().BeSameAs(mocker.Uri);
        }

        [Fact]
        public void CreateInstance_ShouldFallbackToUnkeyedTrackedMock_WhenKeyedDependencyIsNotRegistered()
        {
            var mocker = new Mocker();
            var unkeyedMock = mocker.GetOrCreateMock<IKeyedDependency>();

            var instance = mocker.CreateInstance<KeyedFallbackConsumer>();

            instance.Should().NotBeNull();
            instance!.Dependency.Should().BeSameAs(unkeyedMock.Instance);
        }

        [Fact]
        public void GetKeyedObject_ShouldFallbackToUnkeyedRegistration_WhenKeyedRegistrationIsMissing()
        {
            var mocker = new Mocker();
            var expected = new Uri("http://fallback.fastmoq/");

            mocker.AddType<Uri>(_ => expected, replace: true);

            var resolved = mocker.GetKeyedObject<Uri>("missing");

            resolved.Should().BeSameAs(expected);
        }
    }

    public interface IKeyedDependency
    {
    }

    public class KeyedConsumer(
        [FromKeyedServices("primary")] Uri primaryUri,
        [FromKeyedServices("secondary")] Uri secondaryUri,
        [FromKeyedServices("dep")] IKeyedDependency dependency,
        HttpClient defaultHttpClient,
        Uri defaultUri)
    {
        public Uri PrimaryUri { get; } = primaryUri;
        public Uri SecondaryUri { get; } = secondaryUri;
        public IKeyedDependency Dependency { get; } = dependency;
        public HttpClient DefaultHttpClient { get; } = defaultHttpClient;
        public Uri DefaultUri { get; } = defaultUri;
    }

    public class KeyedFallbackConsumer([FromKeyedServices("dep")] IKeyedDependency dependency)
    {
        public IKeyedDependency Dependency { get; } = dependency;
    }
}
