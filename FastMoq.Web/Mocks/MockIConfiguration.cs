using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace FastMoq.Web.Mocks
{
    [ExcludeFromCodeCoverage]
    public class MockIConfiguration : IConfiguration
    {
        #region Properties

        public virtual string? this[string key]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        #endregion

        #region IConfiguration

        public virtual IEnumerable<IConfigurationSection> GetChildren() => throw new NotImplementedException();

        public virtual IChangeToken GetReloadToken() => throw new NotImplementedException();

        public virtual IConfigurationSection GetSection(string key) => throw new NotImplementedException();

        #endregion
    }
}