using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using FastMoq.Web.Extensions;

namespace FastMoq.Tests.Web
{
    public interface IOrderReader
    {
        Task<IReadOnlyList<OrderDto>> GetOrdersAsync(bool includeInactive, string correlationId, string userName, CancellationToken cancellationToken);
    }

    public sealed class OrdersController(IOrderReader orderReader, IHttpContextAccessor httpContextAccessor) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var includeInactive = string.Equals(HttpContext.Request.Query["includeInactive"], "true", StringComparison.OrdinalIgnoreCase);
            var correlationId = httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-Id"].ToString() ?? string.Empty;
            var userName = User.Identity?.Name ?? string.Empty;

            var orders = await orderReader.GetOrdersAsync(includeInactive, correlationId, userName, cancellationToken);
            return Ok(new OrdersResponse(userName, correlationId, includeInactive, orders));
        }
    }

    public sealed record OrderDto(int Id, string Status);

    public sealed record OrdersResponse(string UserName, string CorrelationId, bool IncludeInactive, IReadOnlyList<OrderDto> Orders);

    public class ControllerHttpContextIntegrationTests : MockerTestBase<OrdersController>
    {
        private HttpContext requestContext = default!;

        protected override Action<Mocker>? SetupMocksAction => mocker =>
        {
            requestContext = mocker
                .CreateHttpContext("Admin")
                .SetRequestHeader("X-Correlation-Id", "corr-123")
                .SetQueryParameter("includeInactive", "true");

            mocker.AddHttpContextAccessor(requestContext);
        };

        protected override Action<OrdersController> CreatedComponentAction => controller =>
        {
            controller.ControllerContext = Mocks.CreateControllerContext(requestContext);
        };

        [Fact]
        public async Task Get_ShouldUseConfiguredHttpContext_AcrossControllerAndAccessor()
        {
            Mocks.GetMock<IOrderReader>()
                .Setup(x => x.GetOrdersAsync(true, "corr-123", "Test User", It.IsAny<CancellationToken>()))
                .ReturnsAsync([new OrderDto(42, "open")]);

            var result = await Component.Get(CancellationToken.None);
            var payload = result.GetObjectResultContent<OrdersResponse>();

            payload.UserName.Should().Be("Test User");
            payload.CorrelationId.Should().Be("corr-123");
            payload.IncludeInactive.Should().BeTrue();
            payload.Orders.Should().ContainSingle(order => order.Id == 42 && order.Status == "open");
        }
    }
}