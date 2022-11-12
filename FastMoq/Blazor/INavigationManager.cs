using Microsoft.AspNetCore.Components.Routing;

namespace FastMoq.Blazor
{
    /// <summary>
    /// Blazor Navigation Manager - Wraps Navigation Manager or any navigation implementation used for Blazor and testing.
    /// </summary>
    public interface INavigationManager
    {
        event EventHandler<LocationChangedEventArgs> LocationChanged;
        /// <summary>
        /// Navigate to a path.
        /// </summary>
        /// <param name="uri">path to navigate</param>
        void NavigateTo(string uri);

        /// <summary>
        /// Navigate to a path.
        /// </summary>
        /// <param name="uri">path to navigate</param>
        /// <param name="forceLoad">If true, bypasses client-side routing and forces the browser to load the new page from the server, whether or not the URI would normally be handled by the client-side router.</param>
        void NavigateTo(string uri, bool forceLoad);

        /// <summary>
        /// Returns if the Url contains a specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool UrlContains(string path);

        /// <summary>
        /// Gets or sets the current base URI. The <see cref="BaseUri" /> is always represented as an absolute URI in string form with trailing slash.
        /// Typically this corresponds to the 'href' attribute on the document's &lt;base&gt; element.
        /// </summary>
        /// <value>The base URI.</value>
        /// <remarks>Setting <see cref="BaseUri" /> will not trigger the <see cref="LocationChanged" /> event.</remarks>
        string BaseUri { get; }

        /// <summary>
        /// Gets or sets the current URI. The <see cref="Uri" /> is always represented as an absolute URI in string form.
        /// </summary>
        /// <value>The URI.</value>
        /// <remarks>Setting <see cref="Uri" /> will not trigger the <see cref="LocationChanged" /> event.</remarks>
        string Uri { get; }

        Uri ToAbsoluteUri(string relativeUri);

        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added or updated.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        public string GetUriWithQueryParameter(string name, bool value);

        /// <summary>
        /// Returns a URI constructed from <paramref name="uri"/> except with multiple parameters
        /// added, updated, or removed.
        /// </summary>
        /// <param name="uri">The URI with the query to modify.</param>
        /// <param name="parameters">The values to add, update, or remove.</param>
        string GetUriWithQueryParameters(string uri, IReadOnlyDictionary<string, object?> parameters);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added, updated, or removed.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        ///
        /// Remarks:
        ///     If value is null, the parameter will be removed if it exists in the URI. Otherwise,
        ///     it will be added or updated.
        public string GetUriWithQueryParameter(string name, bool? value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added or updated.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        public string GetUriWithQueryParameter(string name, DateTime value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added, updated, or removed.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        ///
        /// Remarks:
        ///     If value is null, the parameter will be removed if it exists in the URI. Otherwise,
        ///     it will be added or updated.
        public string GetUriWithQueryParameter(string name, DateTime? value);

#if  NET6_0_OR_GREATER
        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added or updated.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        public string GetUriWithQueryParameter(string name, DateOnly value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added, updated, or removed.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        ///
        /// Remarks:
        ///     If value is null, the parameter will be removed if it exists in the URI. Otherwise,
        ///     it will be added or updated.
        public string GetUriWithQueryParameter(string name, DateOnly? value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added or updated.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        public string GetUriWithQueryParameter(string name, TimeOnly value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added, updated, or removed.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        ///
        /// Remarks:
        ///     If value is null, the parameter will be removed if it exists in the URI. Otherwise,
        ///     it will be added or updated.
        public string GetUriWithQueryParameter(string name, TimeOnly? value);
#endif
        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added or updated.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        public string GetUriWithQueryParameter(string name, decimal value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added, updated, or removed.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        ///
        /// Remarks:
        ///     If value is null, the parameter will be removed if it exists in the URI. Otherwise,
        ///     it will be added or updated.
        public string GetUriWithQueryParameter(string name, decimal? value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added or updated.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        public string GetUriWithQueryParameter(string name, double value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added, updated, or removed.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        ///
        /// Remarks:
        ///     If value is null, the parameter will be removed if it exists in the URI. Otherwise,
        ///     it will be added or updated.
        public string GetUriWithQueryParameter(string name, double? value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added or updated.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        public string GetUriWithQueryParameter(string name, float value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added, updated, or removed.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        ///
        /// Remarks:
        ///     If value is null, the parameter will be removed if it exists in the URI. Otherwise,
        ///     it will be added or updated.
        public string GetUriWithQueryParameter(string name, float? value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added or updated.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        public string GetUriWithQueryParameter(string name, Guid value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added, updated, or removed.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        ///
        /// Remarks:
        ///     If value is null, the parameter will be removed if it exists in the URI. Otherwise,
        ///     it will be added or updated.
        public string GetUriWithQueryParameter(string name, Guid? value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added or updated.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        public string GetUriWithQueryParameter(string name, int value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added, updated, or removed.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        ///
        /// Remarks:
        ///     If value is null, the parameter will be removed if it exists in the URI. Otherwise,
        ///     it will be added or updated.
        public string GetUriWithQueryParameter(string name, int? value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added or updated.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        public string GetUriWithQueryParameter(string name, long value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added, updated, or removed.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        ///
        /// Remarks:
        ///     If value is null, the parameter will be removed if it exists in the URI. Otherwise,
        ///     it will be added or updated.
        public string GetUriWithQueryParameter(string name, long? value);

        ///
        /// Summary:
        ///     Returns a URI that is constructed by updating Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with a single parameter added, updated, or removed.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   name:
        ///     The name of the parameter to add or update.
        ///
        ///   value:
        ///     The value of the parameter to add or update.
        ///
        /// Remarks:
        ///     If value is null, the parameter will be removed if it exists in the URI. Otherwise,
        ///     it will be added or updated.
        public string GetUriWithQueryParameter(string name, string? value);

        /// Summary:
        ///     Returns a URI constructed from Microsoft.AspNetCore.Components.NavigationManager.Uri
        ///     with multiple parameters added, updated, or removed.
        ///
        /// Parameters:
        ///   navigationManager:
        ///     The Microsoft.AspNetCore.Components.NavigationManager.
        ///
        ///   parameters:
        ///     The values to add, update, or remove.
        public string GetUriWithQueryParameters(IReadOnlyDictionary<string, object?> parameters);
    }
}
