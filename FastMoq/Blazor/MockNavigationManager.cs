#if NETCOREAPP3_1_OR_GREATER

using Microsoft.AspNetCore.Components.Routing;

namespace FastMoq.Blazor
{
    /// <inheritdoc />
    public sealed class MockNavigationManager : INavigationManager
    {
        public event EventHandler<LocationChangedEventArgs> LocationChanged;

        /// <inheritdoc />
        public void NavigateTo(string uri) { }

        /// <inheritdoc />
        public void NavigateTo(string uri, bool forceLoad) { }

        /// <inheritdoc />
        public string BaseUri { get; }

        /// <inheritdoc />
        public string Uri { get; }

        /// <inheritdoc />
        public bool UrlContains(string path) => Uri.ToUpperInvariant().Contains(path);

        public Uri ToAbsoluteUri(string relativeUri)
        {
            return new Uri(new Uri(BaseUri, UriKind.Absolute), relativeUri);
        }

        public string GetUriWithQueryParameter(string name, bool value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, bool? value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, DateTime value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, DateTime? value)
        {
            throw new NotImplementedException();
        }

#if  NET6_0_OR_GREATER
        public string GetUriWithQueryParameter(string name, DateOnly value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, DateOnly? value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, TimeOnly value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, TimeOnly? value)
        {
            throw new NotImplementedException();
        }
#endif
        public string GetUriWithQueryParameter(string name, decimal value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, decimal? value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, double value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, double? value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, float value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, float? value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, Guid value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, Guid? value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, int value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, int? value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, long value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, long? value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameter(string name, string value)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameters(IReadOnlyDictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        public string GetUriWithQueryParameters(string uri, IReadOnlyDictionary<string, object?> parameters) => throw new NotImplementedException();
    }
}
#endif