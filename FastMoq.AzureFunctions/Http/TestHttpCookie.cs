using Microsoft.Azure.Functions.Worker.Http;

namespace FastMoq.AzureFunctions.Http
{
    internal sealed class TestHttpCookie : IHttpCookie
    {
        public TestHttpCookie(
            string name,
            string value,
            string? domain = null,
            DateTimeOffset? expires = null,
            bool? httpOnly = null,
            double? maxAge = null,
            string? path = null,
            SameSite sameSite = default,
            bool? secure = null)
        {
            Name = name;
            Value = value;
            Domain = domain ?? string.Empty;
            Expires = expires;
            HttpOnly = httpOnly;
            MaxAge = maxAge;
            Path = path ?? string.Empty;
            SameSite = sameSite;
            Secure = secure;
        }

        public string Domain { get; }

        public DateTimeOffset? Expires { get; }

        public bool? HttpOnly { get; }

        public double? MaxAge { get; }

        public string Name { get; }

        public string Path { get; }

        public SameSite SameSite { get; }

        public bool? Secure { get; }

        public string Value { get; }
    }
}