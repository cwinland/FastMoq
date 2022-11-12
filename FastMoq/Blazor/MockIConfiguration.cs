using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace FastMoq.Blazor
{
    public class MockIConfiguration : IConfiguration
    {

        public virtual string? this[string key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public virtual IEnumerable<IConfigurationSection> GetChildren()
        {
            throw new NotImplementedException();
        }

        public virtual IChangeToken GetReloadToken()
        {
            throw new NotImplementedException();
        }

        public virtual IConfigurationSection GetSection(string key)
        {
            throw new NotImplementedException();
        }
    }
}
