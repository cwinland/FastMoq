using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FastMoq.Web;
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
            principal.FindFirst(ClaimTypes.Email)!.Value.Should().Be("test.user@microsoft.com");
            principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be("11111111-1111-1111-1111-111111111111");
        }

        [Fact]
        public void SetupClaimsPrincipal_ShouldRespectConfiguredOptions()
        {
            var mocker = new Mocker();
            var options = new TestClaimsPrincipalOptions
            {
                AuthenticationType = "CustomAuth",
                Name = "Adele Vance",
                DisplayName = "Adele Vance",
                Email = "adele.vance@microsoft.com",
                PreferredUserName = "adevance@microsoft.com",
                ObjectId = "22222222-2222-2222-2222-222222222222",
                TenantId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            };
            options.AdditionalClaims.Add(new Claim("custom", "value"));

            var principal = mocker.SetupClaimsPrincipal(options, "Admin");

            principal.Identity.Should().NotBeNull();
            principal.Identity!.AuthenticationType.Should().Be("CustomAuth");
            principal.Identity.Name.Should().Be("Adele Vance");
            principal.FindFirst(ClaimTypes.Email)!.Value.Should().Be("adele.vance@microsoft.com");
            principal.FindFirst("preferred_username")!.Value.Should().Be("adevance@microsoft.com");
            principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")!.Value.Should().Be("22222222-2222-2222-2222-222222222222");
            principal.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")!.Value.Should().Be("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            principal.FindFirst("custom")!.Value.Should().Be("value");
        }

        [Fact]
        public void SetupClaimsPrincipal_WithCustomClaims_ShouldPreserveExistingClaims_AndBackfillCompatibilityDefaults()
        {
            var mocker = new Mocker();
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Name, "Custom User"),
                new Claim(ClaimTypes.Email, "custom.user@microsoft.com"),
            };

            var principal = mocker.SetupClaimsPrincipal(claims);

            principal.Identity.Should().NotBeNull();
            principal.Identity!.Name.Should().Be("Custom User");
            principal.IsInRole("Admin").Should().BeTrue();
            principal.FindFirst(ClaimTypes.Email)!.Value.Should().Be("custom.user@microsoft.com");
            principal.FindFirst("preferred_username")!.Value.Should().Be("test.user@microsoft.com");
            principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be("11111111-1111-1111-1111-111111111111");
        }

        [Fact]
        public void SetupClaimsPrincipal_WithCustomClaimsAndOptions_ShouldBackfillOnlyMissingValues()
        {
            var mocker = new Mocker();
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("preferred_username", "custom.alias@microsoft.com"),
            };
            var options = new TestClaimsPrincipalOptions
            {
                Name = "Configured User",
                Email = "configured.user@microsoft.com",
            };

            var principal = mocker.SetupClaimsPrincipal(claims, options);

            principal.Identity.Should().NotBeNull();
            principal.Identity!.Name.Should().Be("Configured User");
            principal.FindFirst("preferred_username")!.Value.Should().Be("custom.alias@microsoft.com");
            principal.FindFirst(ClaimTypes.Email)!.Value.Should().Be("configured.user@microsoft.com");
        }

        [Fact]
        public void SetupClaimsPrincipal_ShouldAllowDisablingCompatibilityClaims()
        {
            var mocker = new Mocker();
            var options = new TestClaimsPrincipalOptions
            {
                IncludeDefaultIdentityClaims = false,
            };
            options.AdditionalClaims.Add(new Claim("custom", "value"));

            var principal = mocker.SetupClaimsPrincipal(options, "Admin");

            principal.Identity.Should().NotBeNull();
            principal.Identity!.Name.Should().BeNull();
            principal.IsInRole("Admin").Should().BeTrue();
            principal.FindFirst(ClaimTypes.Email).Should().BeNull();
            principal.FindFirst("custom")!.Value.Should().Be("value");
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
        public void CreateControllerContext_WithClaims_ShouldAvoidSeparateUserAssignment()
        {
            var mocker = new Mocker();
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Name, "jdoe@microsoft.com"),
            };

            var controllerContext = mocker.CreateControllerContext(claims);

            controllerContext.HttpContext.Should().NotBeNull();
            controllerContext.HttpContext.User.Identity.Should().NotBeNull();
            controllerContext.HttpContext.User.Identity!.Name.Should().Be("jdoe@microsoft.com");
            controllerContext.HttpContext.User.IsInRole("Admin").Should().BeTrue();
        }

        [Fact]
        public void GetObject_ControllerContext_ShouldShareTrackedHttpContext()
        {
            var mocker = new Mocker();
            var controllerContext = mocker.GetObject<ControllerContext>();
            var secondControllerContext = mocker.GetObject<ControllerContext>();
            var httpContext = mocker.GetObject<HttpContext>();
            var principal = mocker.SetupClaimsPrincipal("Admin");

            controllerContext.Should().NotBeNull();
            secondControllerContext.Should().NotBeNull();
            controllerContext!.HttpContext.Should().NotBeNull();
            secondControllerContext.Should().BeSameAs(controllerContext);
            controllerContext.HttpContext.Should().BeSameAs(httpContext);

            controllerContext.HttpContext.User = principal;

            secondControllerContext!.HttpContext.User.Should().BeSameAs(principal);
            mocker.GetObject<HttpContext>()!.User.Should().BeSameAs(principal);
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
            httpContext.User.FindFirst(ClaimTypes.Email)!.Value.Should().Be("test.user@microsoft.com");
        }

        [Fact]
        public void CreateHttpContext_ShouldUseConfiguredPrincipalOptions()
        {
            var mocker = new Mocker();
            var options = new TestClaimsPrincipalOptions
            {
                Name = "Configured User",
                Email = "configured.user@microsoft.com",
            };

            var httpContext = mocker.CreateHttpContext(options, "Admin");

            httpContext.User.Identity.Should().NotBeNull();
            httpContext.User.Identity!.Name.Should().Be("Configured User");
            httpContext.User.FindFirst(ClaimTypes.Email)!.Value.Should().Be("configured.user@microsoft.com");
        }

        [Fact]
        public void CreateHttpContext_WithClaims_ShouldAvoidSeparateUserAssignment()
        {
            var mocker = new Mocker();
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Name, "jdoe@microsoft.com"),
            };

            var httpContext = mocker.CreateHttpContext(claims);

            httpContext.User.Identity.Should().NotBeNull();
            httpContext.User.Identity!.Name.Should().Be("jdoe@microsoft.com");
            httpContext.User.IsInRole("Admin").Should().BeTrue();
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
