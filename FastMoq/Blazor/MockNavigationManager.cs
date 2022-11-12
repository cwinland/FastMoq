#if NETCOREAPP3_1_OR_GREATER

using Microsoft.AspNetCore.Components.Routing;

namespace FastMoq.Blazor
{
    /// <inheritdoc />
    public class MockNavigationManager : INavigationManager
    {
        #region Events

        public virtual event EventHandler<LocationChangedEventArgs>? LocationChanged;

        #endregion

        #region Properties

        /// <inheritdoc />
        public virtual string BaseUri { get; set; } = "https://localhost";

        /// <inheritdoc />
        public virtual string Uri { get; set; } = "/";

        #endregion

        #region INavigationManager

        public virtual string GetUriWithQueryParameter(string name, bool value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, bool? value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, DateTime value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, DateTime? value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, decimal value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, decimal? value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, double value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, double? value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, float value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, float? value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, Guid value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, Guid? value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, int value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, int? value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, long value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, long? value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, string? value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameters(IReadOnlyDictionary<string, object?> parameters) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameters(string uri, IReadOnlyDictionary<string, object?> parameters) => throw new NotImplementedException();

        /// <inheritdoc />
        public virtual void NavigateTo(string uri) { }

        /// <inheritdoc />
        public virtual void NavigateTo(string uri, bool forceLoad) { }

        public virtual Uri ToAbsoluteUri(string relativeUri) => new(new Uri(BaseUri, UriKind.Absolute), relativeUri);

        /// <inheritdoc />
        public virtual bool UrlContains(string path) => Uri.ToUpperInvariant().Contains(path);

        #endregion

#if NET6_0_OR_GREATER
        public virtual string GetUriWithQueryParameter(string name, DateOnly value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, DateOnly? value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, TimeOnly value) => throw new NotImplementedException();

        public virtual string GetUriWithQueryParameter(string name, TimeOnly? value) => throw new NotImplementedException();
#endif
    }
}
#endif