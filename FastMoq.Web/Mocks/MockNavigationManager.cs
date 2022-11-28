using Bunit.Rendering;
using Microsoft.AspNetCore.Components;

namespace FastMoq.Web.Mocks
{
    /// <inheritdoc />
    public class MockNavigationManager : NavigationManager
    {
        #region Fields

        private readonly ITestRenderer renderer;

        #endregion

        public MockNavigationManager(string baseUri, string uri, ITestRenderer renderer)
        {
            Initialize(baseUri, uri);
            this.renderer = renderer;
        }

        /// <inheritdoc />
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri = ToAbsoluteUri(uri).ToString();
            renderer.Dispatcher.InvokeAsync(() => NotifyLocationChanged(false));
        }
    }
}
