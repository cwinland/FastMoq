using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

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
        public void GetKeyedMock_ShouldReturnSameTrackedMock_ForRepeatedKey()
        {
            var mocker = new Mocker();

            var first = mocker.GetKeyedMock<IKeyedDependency>("alpha");
            var second = mocker.GetKeyedMock<IKeyedDependency>("alpha");
            var other = mocker.GetKeyedMock<IKeyedDependency>("beta");

            second.Should().BeSameAs(first);
            other.Should().NotBeSameAs(first);
        }

        [Fact]
        public void CreateInstance_ShouldResolveKeyedDependencies_FromStandardDiAttributes()
        {
            var mocker = new Mocker();
            var primaryUri = new Uri("http://primary.fastmoq/");
            var secondaryUri = new Uri("http://secondary.fastmoq/");
            var keyedMock = mocker.GetKeyedMock<IKeyedDependency>("dep");

            mocker.AddKeyedType<Uri>("primary", _ => primaryUri);
            mocker.AddKeyedType<Uri>("secondary", _ => secondaryUri);

            var instance = mocker.CreateInstance<KeyedConsumer>();

            instance.Should().NotBeNull();
            instance!.PrimaryUri.Should().BeSameAs(primaryUri);
            instance.SecondaryUri.Should().BeSameAs(secondaryUri);
            instance.Dependency.Should().BeSameAs(keyedMock.Object);
            instance.DefaultHttpClient.Should().BeSameAs(mocker.HttpClient);
            instance.DefaultUri.Should().BeSameAs(mocker.Uri);
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
}
