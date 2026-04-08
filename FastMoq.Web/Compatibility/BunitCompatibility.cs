using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Security.Claims;

namespace Bunit
{
    /// <summary>
    /// Backward-compatible wrapper around <see cref="BunitServiceProvider"/> for FastMoq Blazor helpers.
    /// </summary>
    /// <remarks>
    /// Keep this wrapper when migrating existing FastMoq Blazor tests with minimal churn.
    /// It preserves the older helper name while delegating to the current bUnit 2 service-provider implementation.
    /// </remarks>
    public sealed class TestServiceProvider : IServiceCollection, IServiceProvider, IDisposable, IAsyncDisposable
    {
        private readonly BunitServiceProvider _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestServiceProvider"/> class.
        /// </summary>
        /// <param name="inner">The underlying bUnit service provider.</param>
        public TestServiceProvider(BunitServiceProvider inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>
        /// Gets the underlying bUnit service provider.
        /// </summary>
        public BunitServiceProvider InnerProvider => _inner;

        /// <summary>
        /// Gets a value indicating whether the underlying provider has been initialized.
        /// </summary>
        public bool IsProviderInitialized => _inner.IsProviderInitialized;

        /// <inheritdoc />
        public int Count => _inner.Count;

        /// <inheritdoc />
        public bool IsReadOnly => _inner.IsReadOnly;

        /// <inheritdoc />
        public ServiceDescriptor this[int index]
        {
            get => _inner[index];
            set => _inner[index] = value;
        }

        /// <summary>
        /// Gets or sets the service provider options.
        /// </summary>
        public ServiceProviderOptions Options
        {
            get => _inner.Options;
            set => _inner.Options = value;
        }

        /// <summary>
        /// Configures the underlying service provider factory.
        /// </summary>
        /// <param name="serviceProviderFactory">The factory used to create the provider.</param>
        public void UseServiceProviderFactory(Func<IServiceCollection, IServiceProvider> serviceProviderFactory)
        {
            _inner.UseServiceProviderFactory(serviceProviderFactory);
        }

        /// <summary>
        /// Configures the underlying service provider factory.
        /// </summary>
        /// <typeparam name="TContainerBuilder">The container builder type.</typeparam>
        /// <param name="serviceProviderFactory">The service provider factory.</param>
        /// <param name="configureContainer">The container configuration callback.</param>
        public void UseServiceProviderFactory<TContainerBuilder>(
            IServiceProviderFactory<TContainerBuilder> serviceProviderFactory,
            Action<TContainerBuilder> configureContainer)
            where TContainerBuilder : notnull
        {
            _inner.UseServiceProviderFactory(serviceProviderFactory, configureContainer);
        }

        /// <summary>
        /// Initializes the underlying provider.
        /// </summary>
        /// <remarks>
        /// bUnit 2 no longer exposes the exact same initialization path as earlier versions.
        /// FastMoq keeps this method so existing helper code can continue to force provider initialization when needed.
        /// </remarks>
        public void InitializeProvider()
        {
            var initializeProvider = _inner.GetType().GetMethod("InitializeProvider", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic) ??
                                     throw new MissingMethodException(_inner.GetType().FullName, "InitializeProvider");

            initializeProvider.Invoke(_inner, null);
        }

        /// <summary>
        /// Adds a fallback service provider.
        /// </summary>
        /// <param name="fallbackServiceProvider">The fallback provider.</param>
        public void AddFallbackServiceProvider(IServiceProvider fallbackServiceProvider)
        {
            _inner.AddFallbackServiceProvider(fallbackServiceProvider);
        }

        /// <summary>
        /// Gets a service from the underlying provider.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <returns>The resolved service, if available.</returns>
        public TService? GetService<TService>()
        {
            return _inner.GetService<TService>();
        }

        /// <inheritdoc />
        public object? GetService(Type serviceType)
        {
            return _inner.GetService(serviceType);
        }

        /// <summary>
        /// Gets a keyed service from the underlying provider.
        /// </summary>
        /// <param name="serviceType">The service type.</param>
        /// <param name="serviceKey">The service key.</param>
        /// <returns>The resolved service, if available.</returns>
        public object? GetKeyedService(Type serviceType, object serviceKey)
        {
            return _inner.GetKeyedService(serviceType, serviceKey);
        }

        /// <summary>
        /// Gets a required keyed service from the underlying provider.
        /// </summary>
        /// <param name="serviceType">The service type.</param>
        /// <param name="serviceKey">The service key.</param>
        /// <returns>The resolved service.</returns>
        public object GetRequiredKeyedService(Type serviceType, object serviceKey)
        {
            return _inner.GetRequiredKeyedService(serviceType, serviceKey);
        }

        /// <inheritdoc />
        public int IndexOf(ServiceDescriptor item)
        {
            return _inner.IndexOf(item);
        }

        /// <inheritdoc />
        public void Insert(int index, ServiceDescriptor item)
        {
            _inner.Insert(index, item);
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            _inner.RemoveAt(index);
        }

        /// <inheritdoc />
        public void Add(ServiceDescriptor item)
        {
            _inner.Add(item);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _inner.Clear();
        }

        /// <inheritdoc />
        public bool Contains(ServiceDescriptor item)
        {
            return _inner.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(ServiceDescriptor[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public bool Remove(ServiceDescriptor item)
        {
            return _inner.Remove(item);
        }

        /// <inheritdoc />
        public IEnumerator<ServiceDescriptor> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _inner).GetEnumerator();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _inner.Dispose();
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            return _inner.DisposeAsync();
        }
    }

    /// <summary>
    /// Compatibility extensions that preserve bUnit 1.x helper names used by FastMoq.
    /// </summary>
    public static class TestAuthorizationExtensions
    {
        /// <summary>
        /// Adds bUnit authorization services and returns a compatibility wrapper around the authorization context.
        /// </summary>
        /// <param name="context">The bUnit context.</param>
        /// <returns>A compatibility wrapper over the bUnit authorization context.</returns>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// var authContext = this.AddTestAuthorization();
        /// authContext.SetAuthorized("migration.user");
        /// ]]></code>
        /// </example>
        public static TestDoubles.TestAuthorizationContext AddTestAuthorization(this BunitContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            return new TestDoubles.TestAuthorizationContext(context.AddAuthorization());
        }
    }
}

namespace Bunit.TestDoubles
{
    /// <summary>
    /// Backward-compatible wrapper around <see cref="BunitAuthorizationContext"/>.
    /// </summary>
    /// <remarks>
    /// This wrapper preserves the older FastMoq-facing helper name while the underlying bUnit authorization implementation
    /// comes from the current bUnit 2 package line.
    /// </remarks>
    public sealed class TestAuthorizationContext
    {
        private readonly BunitAuthorizationContext _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAuthorizationContext"/> class.
        /// </summary>
        /// <param name="inner">The underlying bUnit authorization context.</param>
        public TestAuthorizationContext(BunitAuthorizationContext inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>
        /// Gets the underlying bUnit authorization context.
        /// </summary>
        public BunitAuthorizationContext InnerContext => _inner;

        /// <summary>
        /// Gets a value indicating whether the current user is authenticated.
        /// </summary>
        public bool IsAuthenticated => _inner.IsAuthenticated;

        /// <summary>
        /// Gets the current user name.
        /// </summary>
        public string UserName => _inner.UserName;

        /// <summary>
        /// Gets the current authorization state.
        /// </summary>
        public AuthorizationState State => _inner.State;

        /// <summary>
        /// Gets the active roles.
        /// </summary>
        public IEnumerable<string> Roles => _inner.Roles;

        /// <summary>
        /// Gets the active policies.
        /// </summary>
        public IEnumerable<string> Policies => _inner.Policies;

        /// <summary>
        /// Gets the active claims.
        /// </summary>
        public IEnumerable<Claim> Claims => _inner.Claims;

        /// <summary>
        /// Gets the configured policy scheme name.
        /// </summary>
        public string PolicySchemeName => _inner.PolicySchemeName;

        /// <summary>
        /// Sets an authenticated user with the default authorized state.
        /// </summary>
        /// <param name="userName">The user name.</param>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// AuthContext.SetAuthorized("migration.user");
        /// ]]></code>
        /// </example>
        public void SetAuthorized(string userName)
        {
            _inner.SetAuthorized(userName, AuthorizationState.Authorized);
        }

        /// <summary>
        /// Sets an authenticated user with the specified authorization state.
        /// </summary>
        /// <param name="userName">The user name.</param>
        /// <param name="state">The authorization state.</param>
        public void SetAuthorized(string userName, AuthorizationState state)
        {
            _inner.SetAuthorized(userName, state);
        }

        /// <summary>
        /// Puts the authorization services into the authorizing state.
        /// </summary>
        public void SetAuthorizing()
        {
            _inner.SetAuthorizing();
        }

        /// <summary>
        /// Puts the authorization services into the unauthenticated state.
        /// </summary>
        public void SetNotAuthorized()
        {
            _inner.SetNotAuthorized();
        }

        /// <summary>
        /// Sets the active roles.
        /// </summary>
        /// <param name="roles">The roles to apply.</param>
        public void SetRoles(params string[] roles)
        {
            _inner.SetRoles(roles);
        }

        /// <summary>
        /// Sets the active policies.
        /// </summary>
        /// <param name="policies">The policies to apply.</param>
        public void SetPolicies(params string[] policies)
        {
            _inner.SetPolicies(policies);
        }

        /// <summary>
        /// Sets the active claims.
        /// </summary>
        /// <param name="claims">The claims to apply.</param>
        public void SetClaims(params Claim[] claims)
        {
            _inner.SetClaims(claims);
        }

        /// <summary>
        /// Sets the authentication type for the current identity.
        /// </summary>
        /// <param name="authenticationType">The authentication type.</param>
        public void SetAuthenticationType(string authenticationType)
        {
            _inner.SetAuthenticationType(authenticationType);
        }
    }

    /// <summary>
    /// Backward-compatible wrapper around <see cref="BunitNavigationManager"/>.
    /// </summary>
    /// <remarks>
    /// This wrapper keeps older FastMoq navigation assertions stable while delegating to bUnit 2's navigation manager.
    /// </remarks>
    public sealed class FakeNavigationManager
    {
        private readonly BunitNavigationManager _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeNavigationManager"/> class.
        /// </summary>
        /// <param name="inner">The underlying bUnit navigation manager.</param>
        public FakeNavigationManager(BunitNavigationManager inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>
        /// Gets the underlying bUnit navigation manager.
        /// </summary>
        public BunitNavigationManager InnerManager => _inner;

        /// <summary>
        /// Gets the base URI.
        /// </summary>
        public string BaseUri => _inner.BaseUri;

        /// <summary>
        /// Gets the current URI.
        /// </summary>
        public string Uri => _inner.Uri;

        /// <summary>
        /// Gets the captured navigation history.
        /// </summary>
        /// <example>
        /// <code language="csharp"><![CDATA[
        /// ClickButton("#review-orders", () => NavigationManager.History.Count == 1);
        /// NavigationManager.Uri.Should().Contain("/orders/review");
        /// ]]></code>
        /// </example>
        public IReadOnlyCollection<NavigationHistory> History => _inner.History;

        /// <summary>
        /// Navigates to the specified URI.
        /// </summary>
        /// <param name="uri">The destination URI.</param>
        /// <param name="forceLoad">Whether to force a full page load.</param>
        /// <param name="replace">Whether to replace the current history entry.</param>
        public void NavigateTo(string uri, bool forceLoad = false, bool replace = false)
        {
            _inner.NavigateTo(uri, forceLoad, replace);
        }

        /// <summary>
        /// Navigates to the specified URI using navigation options.
        /// </summary>
        /// <param name="uri">The destination URI.</param>
        /// <param name="options">The navigation options.</param>
        public void NavigateTo(string uri, NavigationOptions options)
        {
            _inner.NavigateTo(uri, options);
        }

        /// <summary>
        /// Converts a URI to an absolute URI.
        /// </summary>
        /// <param name="relativeUri">The relative URI.</param>
        /// <returns>The absolute URI.</returns>
        public Uri ToAbsoluteUri(string relativeUri)
        {
            return _inner.ToAbsoluteUri(relativeUri);
        }

        /// <summary>
        /// Converts a URI to a base-relative path.
        /// </summary>
        /// <param name="uri">The URI to convert.</param>
        /// <returns>The base-relative path.</returns>
        public string ToBaseRelativePath(string uri)
        {
            return _inner.ToBaseRelativePath(uri);
        }
    }
}