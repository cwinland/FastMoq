using System;
using System.Net.Http;

namespace FastMoq.Tests.TestClasses
{
    public class HttpTestClass
    {
        internal HttpClient http;

        public HttpTestClass(HttpClient httpClient)
        {
            http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }
    }
}
