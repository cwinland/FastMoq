---
name: "FastMoq.Web"
description: "Use when working in FastMoq.Web or related Blazor and web-helper code, including HttpContext helpers, service registration, and component-testing flows."
applyTo: "FastMoq.Web/**"
---

# FastMoq.Web - Copilot Instructions

## 🌐 Web & Blazor Development Guidelines

When working in `FastMoq.Web`, you are extending FastMoq's capabilities for **web applications and Blazor components**. This includes both server-side and client-side scenarios.

### 🧭 Web-Specific Architecture
- **`FastMoq.Web`**: Core web extensions and helpers
- **`FastMoq.Web.Blazor`**: Blazor component testing framework
- **`MockerBlazorTestBase<T>`**: Primary base class for Blazor component tests

### 🎨 Blazor Testing Patterns
```csharp
public class MyBlazorComponentTests : MockerBlazorTestBase<MyBlazorComponent>
{
    [Fact]
    public void Component_ShouldRenderCorrectly_WhenPropsAreSet()
    {
        // Arrange
        var expectedText = "Hello World";
        
        // Act - Render the component with parameters
        var component = RenderComponent<MyBlazorComponent>(parameters => parameters
            .Add(p => p.Title, expectedText));

        // Assert
        component.Find("h1").TextContent.Should().Be(expectedText);
    }
}
```

### 🔧 Web Extensions Focus
- **Dependency Injection**: Web-specific DI patterns and service registration
- **HTTP Context**: Mocking HttpContext, HttpRequest, HttpResponse
- **Authentication**: Testing auth scenarios and user contexts
- **Configuration**: Web.config, appsettings.json testing helpers

### 🎯 Blazor Component Testing
- **Render testing**: Verify component output and markup
- **Parameter binding**: Test component parameters and cascading parameters
- **Event handling**: Test click, input, and custom events
- **State management**: Test component state changes and lifecycle
- **Service injection**: Mock injected services in components

### 📋 Web-Specific Patterns
```csharp
// HTTP Context mocking
public class WebControllerTests : MockerTestBase<MyController>
{
    public WebControllerTests() : base(mocks =>
    {
        var httpContext = new DefaultHttpContext();
        mocks.GetMock<IHttpContextAccessor>()
            .Setup(x => x.HttpContext)
            .Returns(httpContext);
    });
}

// Blazor service injection
public class BlazorComponentTests : MockerBlazorTestBase<MyComponent>
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Mocks.GetMock<IMyService>().Object);
        base.ConfigureServices(services);
    }
}
```

### 🌍 Web Environment Considerations
- **Client vs Server**: Be aware of Blazor Server vs WebAssembly differences
- **JavaScript Interop**: Mock IJSRuntime for JS interactions
- **Routing**: Test navigation and route parameters
- **Forms**: Test form validation and submission
- **Authentication**: Mock user identity and authorization

### 🚫 Web-Specific Restrictions
- **No core dependencies**: Web projects depend on Core, not vice versa
- **Browser compatibility**: Consider different browser environments
- **Security testing**: Don't skip auth/authz testing scenarios
- **Performance awareness**: Web components have different performance profiles

### 📦 Testing Web Components
- Use `RenderTree` assertions for complex markup
- Test responsive behavior where applicable
- Mock browser APIs appropriately
- Test error boundaries and fallback UI
- Verify accessibility where relevant