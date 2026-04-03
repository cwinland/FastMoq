using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FastMoq.Web.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FastMoq.Tests.Web
{
    public class TestWebExtensionsTests
    {
        [Fact]
        public void SetupClaimsPrincipal_ShouldCreateAuthenticatedPrincipal_WithRolesAndName()
        {
            var mocker = new Mocker();

            var principal = mocker.SetupClaimsPrincipal("Admin", "FaasUser");

            principal.Identity.Should().NotBeNull();
            principal.Identity!.IsAuthenticated.Should().BeTrue();
            principal.Identity.AuthenticationType.Should().Be("TestAuth");
            principal.Identity.Name.Should().Be("Test User");
            principal.IsInRole("Admin").Should().BeTrue();
            principal.IsInRole("FaasUser").Should().BeTrue();
        }

        [Fact]
        public void CreateControllerContext_ShouldStampClaimsPrincipal_OnHttpContext()
        {
            var mocker = new Mocker();

            var controllerContext = mocker.CreateControllerContext("Admin");

            controllerContext.HttpContext.Should().NotBeNull();
            controllerContext.HttpContext.User.Identity.Should().NotBeNull();
            controllerContext.HttpContext.User.Identity!.Name.Should().Be("Test User");
            controllerContext.HttpContext.User.IsInRole("Admin").Should().BeTrue();
        }

        [Fact]
        public void CreateControllerContext_ShouldReuseProvidedHttpContext()
        {
            var mocker = new Mocker();
            var httpContext = mocker.CreateHttpContext("Admin").SetRequestHeader("X-Test", "value");

            var controllerContext = mocker.CreateControllerContext(httpContext);

            controllerContext.HttpContext.Should().BeSameAs(httpContext);
            controllerContext.HttpContext.Request.Headers["X-Test"].ToString().Should().Be("value");
            controllerContext.HttpContext.User.IsInRole("Admin").Should().BeTrue();
        }

        [Fact]
        public void CreateHttpContext_ShouldStampClaimsPrincipal_OnHttpContext()
        {
            var mocker = new Mocker();

            var httpContext = mocker.CreateHttpContext("Admin", "Support");

            httpContext.User.Identity.Should().NotBeNull();
            httpContext.User.Identity!.IsAuthenticated.Should().BeTrue();
            httpContext.User.Identity.Name.Should().Be("Test User");
            httpContext.User.IsInRole("Admin").Should().BeTrue();
            httpContext.User.IsInRole("Support").Should().BeTrue();
        }

        [Fact]
        public void AddHttpContext_ShouldRegisterProvidedContext()
        {
            var mocker = new Mocker();
            var httpContext = new DefaultHttpContext();
            httpContext.SetRequestHeader("X-Test", "abc123");

            mocker.AddHttpContext(httpContext);

            var resolved = mocker.GetObject<HttpContext>();

            resolved.Should().BeSameAs(httpContext);
            resolved.Request.Headers["X-Test"].ToString().Should().Be("abc123");
        }

        [Fact]
        public void AddHttpContextAccessor_ShouldRegisterAccessor_AndHttpContext()
        {
            var mocker = new Mocker();

            mocker.AddHttpContextAccessor(roleNames: ["Admin"]);

            var accessor = mocker.GetObject<IHttpContextAccessor>();
            var httpContext = mocker.GetObject<HttpContext>();

            accessor.Should().NotBeNull();
            accessor!.HttpContext.Should().NotBeNull();
            accessor.HttpContext.Should().BeSameAs(httpContext);
            accessor.HttpContext!.User.IsInRole("Admin").Should().BeTrue();
        }

        [Fact]
        public void SetRequestHeaders_ShouldApplyMultipleHeaderValues()
        {
            var httpContext = new DefaultHttpContext();

            httpContext
                .SetRequestHeader("X-Correlation-Id", "corr-123")
                .SetRequestHeaders(
                [
                    new KeyValuePair<string, string[]>("X-Tenant", ["tenant-a"]),
                    new KeyValuePair<string, string[]>("X-Roles", ["Admin", "Writer"]),
                ]);

            httpContext.Request.Headers["X-Correlation-Id"].ToString().Should().Be("corr-123");
            httpContext.Request.Headers["X-Tenant"].ToString().Should().Be("tenant-a");
            httpContext.Request.Headers["X-Roles"].Should().BeEquivalentTo(["Admin", "Writer"]);
        }

        [Fact]
        public void SetQueryHelpers_ShouldKeepQueryCollection_AndRawQueryStringInSync()
        {
            var httpContext = new DefaultHttpContext();

            httpContext
                .SetQueryParameters(
                [
                    new KeyValuePair<string, string?>("customerId", "42"),
                    new KeyValuePair<string, string?>("includeInactive", "true"),
                ])
                .SetQueryParameter("page", "3");

            httpContext.Request.Query["customerId"].ToString().Should().Be("42");
            httpContext.Request.Query["includeInactive"].ToString().Should().Be("true");
            httpContext.Request.Query["page"].ToString().Should().Be("3");
            httpContext.Request.QueryString.Value.Should().Contain("customerId=42");
            httpContext.Request.QueryString.Value.Should().Contain("includeInactive=true");
            httpContext.Request.QueryString.Value.Should().Contain("page=3");
        }

        [Fact]
        public void SetQueryString_ShouldNormalizeMissingLeadingQuestionMark()
        {
            var httpContext = new DefaultHttpContext();

            httpContext.SetQueryString("page=5&filter=active");

            httpContext.Request.QueryString.Value.Should().Be("?page=5&filter=active");
        }

        [Fact]
        public async Task AddRequestDelegate_ShouldRegisterNoOpDelegate_WhenNoDelegateIsProvided()
        {
            var mocker = new Mocker();
            mocker.AddRequestDelegate();

            var next = mocker.GetObject<RequestDelegate>();
            var context = new DefaultHttpContext();

            next.Should().NotBeNull();
            await next!(context);
        }

        [Fact]
        public async Task AddRequestDelegate_ShouldRegisterProvidedDelegate()
        {
            var mocker = new Mocker();
            var nextCalled = false;
            mocker.AddRequestDelegate(ctx =>
            {
                nextCalled = true;
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            });

            var next = mocker.GetObject<RequestDelegate>();
            var context = new DefaultHttpContext();

            await next!(context);

            nextCalled.Should().BeTrue();
            context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        }

        [Fact]
        public void GetObjectResultContent_ShouldReturnTypedPayload_FromIActionResult()
        {
            IActionResult result = new OkObjectResult(new SamplePayload("test"));

            var payload = result.GetObjectResultContent<SamplePayload>();

            payload.Value.Should().Be("test");
        }

        [Fact]
        public void GetTypedActionResults_ShouldReturnExpectedConcreteResult()
        {
            IActionResult ok = new OkObjectResult("ok");
            IActionResult badRequest = new BadRequestObjectResult("bad");
            IActionResult conflict = new ConflictObjectResult("conflict");
            IActionResult noContent = new NoContentResult();

            ok.GetOkObjectResult().Value.Should().Be("ok");
            badRequest.GetBadRequestObjectResult().Value.Should().Be("bad");
            conflict.GetConflictObjectResult().Value.Should().Be("conflict");
            noContent.GetNoContentResult().Should().NotBeNull();
        }

        [Fact]
        public void GetObjectResult_ShouldThrow_WhenResultTypeDoesNotMatch()
        {
            IActionResult result = new NoContentResult();

            var act = () => result.GetObjectResult();

            act.Should().Throw<InvalidOperationException>();
        }

        private sealed record SamplePayload(string Value);
    }
}
