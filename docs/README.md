# FastMoq Documentation

Welcome to the comprehensive FastMoq documentation! This documentation is designed to help you get the most out of FastMoq, from your first test to advanced enterprise scenarios.

## 🆕 Release Highlights Since 3.0.0

If you are coming from the last public `3.0.0` package, the biggest changes in the current line are:

- provider-first architecture with explicit provider registration and selection
- new package split across the aggregate runtime, Azure SDK helpers, Azure Functions helpers, database helpers, web helpers, and provider-specific adapters
- first-party Azure SDK and Azure Functions HTTP-trigger helpers, with analyzer assets included by default in the aggregate `FastMoq` package
- provider-neutral verification with `TimesSpec`, `Verify(...)`, and `VerifyLogged(...)`
- fluent `Scenario.With(...).When(...).Then(...).Verify(...)` support for workflow-style tests
- explicit policy surfaces for constructor fallback, method fallback, known-type resolution, and optional-parameter behavior
- expanded migration guidance, executable examples, and generated API coverage

## 📚 Documentation Structure

### 🚀 [Getting Started](./getting-started/README.md)

Perfect for developers new to FastMoq. Learn the basics and write your first test in minutes.

- Installation and setup
- Your first FastMoq test
- [Provider selection and setup](./getting-started/provider-selection.md)
- [Provider capabilities matrix](./getting-started/provider-capabilities.md)
- [Repo-native testing guide](./getting-started/testing-guide.md)
- [Typed `IServiceProvider` helpers](./getting-started/testing-guide.md#typed-iserviceprovider-helpers)
- [Explicit constructor selection in tests](./getting-started/testing-guide.md#explicit-constructor-selection-in-tests)
- [Web helper guidance for controller and request tests](./getting-started/testing-guide.md#controller-testing)
- [Executable testing examples](./samples/testing-examples.md)
- Understanding the architecture
- Common patterns and best practices
- Troubleshooting guide

### 📊 [Feature Parity](./feature-parity/README.md)

Comprehensive comparison of FastMoq with other popular mocking frameworks.

- Side-by-side feature comparison
- Migration guides from Moq and NSubstitute
- Performance and memory usage analysis
- When to choose FastMoq vs alternatives

### 👨‍🍳 [Cookbook](./cookbook/README.md)

Practical recipes for real-world testing scenarios.

- API Controller testing
- Entity Framework Core with DbContext
- Background Services and hosted services
- HttpClient and external API integration
- Configuration and Options patterns
- Logging verification
- Azure Services integration
- File system operations

### 🏗️ [Sample Applications](./samples/README.md)

Complete, runnable examples demonstrating FastMoq in production-like scenarios.

- **E-Commerce Order Processing** - Complete sample documentation under `docs/samples/ecommerce-orders`
- **Executable Testing Examples** - Smaller repo-local service tests that track current FastMoq guidance

### 📈 Benchmarks

Performance analysis and productivity improvements.

- Execution speed comparisons
- Memory usage analysis
- Developer productivity metrics
- Real-world impact studies
- ROI calculations

### 🗺️ [Roadmap Notes](./roadmap/README.md)

Current provider-first direction, active architectural work, and intentionally deferred items.

### 🆕 [What's New Since 3.0.0](./whats-new/README.md)

Summary of the major architecture, packaging, API, and documentation changes after the May 12, 2025 `3.0.0` baseline.

### ⚠️ [Breaking Changes](./breaking-changes/README.md)

Intentional v4 breaking changes, with migration notes for changed behavior.

### 🔄 [Migration Guide](./migration/README.md)

Practical guidance for moving from the `3.0.0` public release toward the current v4 provider-first patterns.

## 🎯 Quick Navigation

### By Experience Level

| Experience | Start Here | Next Steps |
| ---------- | ---------- | ---------- |
| **New to Mocking** | [Getting Started](./getting-started/README.md) | [Simple Cookbook Examples](./cookbook/README.md#api-controller-testing) |
| **Coming from Moq** | [Feature Parity](./feature-parity/README.md) | [Migration Guide](./migration/README.md) |
| **Enterprise Teams** | [Sample Applications](./samples/README.md) | Performance notes coming in a future published docs pass |

### By Use Case

| Use Case | Documentation | Sample Code |
| -------- | ------------- | ----------- |
| **Web APIs** | [API Controller Testing](./cookbook/README.md#api-controller-testing) | [E-Commerce Sample](./samples/ecommerce-orders/README.md) |
| **Web helper migration** | [Framework and web helper migration](./migration/framework-and-web-helpers.md#web-test-helpers) | [Repo-native testing guide](./getting-started/testing-guide.md#controller-testing) |
| **Database Testing** | [EF Core Testing](./cookbook/README.md#entity-framework-core-testing) | [Repository Patterns](./samples/ecommerce-orders/README.md) |
| **Azure Integration** | [Sample Applications](./samples/README.md) | [Complete Azure App](./samples/ecommerce-orders/README.md) |
| **Background Jobs** | [Background Services](./cookbook/README.md#background-services-testing) | [Executable Testing Examples](./samples/testing-examples.md) |
| **Blazor Apps** | [bUnit and Blazor test migration](./migration/bunit-and-blazor-testing.md) | [Executable Testing Examples](./samples/testing-examples.md) |

Package note: `FastMoq` is the aggregate package. Provider contracts live in `FastMoq.Abstractions`, `FastMoq.Core` stays lighter, shared Azure SDK helpers live in the `FastMoq.Azure.*` namespaces, EF-specific helpers live in `FastMoq.Database`, Azure Functions worker and HTTP-trigger helpers live in `FastMoq.AzureFunctions.Extensions`, provider-specific adapters live in `FastMoq.Provider.*`, web helpers live in `FastMoq.Web.Extensions`, and analyzer assets ship with the aggregate package by default while the primary runtime calls stay in the `FastMoq` or `FastMoq.Extensions` namespaces.

Web helper note: if your test project references the aggregate `FastMoq` package, the web helpers are already included. If your test project references `FastMoq.Core` directly, add `FastMoq.Web` before using helpers such as `CreateHttpContext(...)`, `CreateControllerContext(...)`, `SetupClaimsPrincipal(...)`, `AddHttpContext(...)`, or `AddHttpContextAccessor(...)`.

Azure SDK helper note: if your test project references the aggregate `FastMoq` package, the shared Azure SDK helpers are already included. If your test project references `FastMoq.Core` directly, add `FastMoq.Azure` before using `PageableBuilder`, the credential helpers, Azure-oriented configuration/service-provider helpers, or the client registration helpers.

Azure Functions helper note: if your test project references the aggregate `FastMoq` package, the Azure Functions helpers are already included. If your test project references `FastMoq.Core` directly, add `FastMoq.AzureFunctions` and import `FastMoq.AzureFunctions.Extensions` before using `CreateFunctionContextInstanceServices(...)`, `AddFunctionContextInstanceServices(...)`, `CreateHttpRequestData(...)`, or `CreateHttpResponseData(...)`.

See [Getting Started package choices](./getting-started/README.md#package-choices) when you need the full install matrix instead of a quick reminder.

## 🏆 Key Advantages

FastMoq is designed for **developer productivity** and **maintainable tests**:

### 📝 Less Code, More Testing

- **70% reduction** in test setup code
- **Automatic dependency injection** eliminates boilerplate
- **Fluent API** for readable test scenarios

### ⚡ Better Performance

- **50% faster** test execution
- **60% less** memory usage
- **Better scalability** for large test suites

### 🎛️ Modern .NET Patterns

- **Built-in support** for EF Core, HttpClient, IFileSystem
- **Azure services** integration
- **Configuration and Options** patterns
- **Blazor and Web** testing

### 🔧 Enterprise Ready

- **Comprehensive logging** verification
- **Advanced scenarios** support
- **Team productivity** improvements
- **Migration tools** from other frameworks

## 📖 Learning Path

### 1. Foundation (30 minutes)

1. Read [Getting Started](./getting-started/README.md)
2. Try the [first test example](./getting-started/README.md#your-first-test)
3. Understand [MockerTestBase](./getting-started/README.md#mockertestbaset)

### 2. Practical Application (1 hour)

1. Pick a relevant [cookbook recipe](./cookbook/README.md)
2. Try it in your own project
3. Explore [advanced setup options](./getting-started/README.md#advanced-setup-options)

### 3. Production Readiness (2-3 hours)

1. Study a complete [sample application](./samples/README.md)
2. Review the current sample and migration guidance
3. Plan your [migration strategy](./migration/README.md)

### 4. Mastery (Ongoing)

1. Explore all [cookbook patterns](./cookbook/README.md)
2. Adapt [sample applications](./samples/README.md) to your domain
3. Contribute back to the community

## 🤝 Community and Support

### Getting Help

- **Documentation**: You're here! Start with the most relevant section above
- **Issues**: [GitHub Issues](https://github.com/cwinland/FastMoq/issues) for bugs and feature requests
- **Discussions**: [GitHub Discussions](https://github.com/cwinland/FastMoq/discussions) for questions and community help
- **API Reference**: [Complete API Documentation](https://help.fastmoq.com/)

### Contributing

We welcome contributions! See our:

- [Contributing Guide](https://github.com/cwinland/FastMoq/blob/master/CONTRIBUTING.md)
- [Code of Conduct](https://github.com/cwinland/FastMoq/blob/master/CODE_OF_CONDUCT.md)
- [Getting Started](./getting-started/README.md)

### Stay Updated

- ⭐ **Star the repository** for updates
- 📋 **Watch releases** for new versions
- 🐦 **Follow discussions** for community insights

## 🔗 External Links

- **NuGet Package**: [FastMoq on NuGet](https://www.nuget.org/packages/FastMoq/)
- **GitHub Repository**: [cwinland/FastMoq](https://github.com/cwinland/FastMoq)
- **API Documentation**: [Complete API Reference](https://help.fastmoq.com/)
- **Release Notes**: [GitHub Releases](https://github.com/cwinland/FastMoq/releases)

## 🏷️ Version Information

This documentation tracks the published FastMoq `4.1.0` line and the current v4 package layout.

---

**Ready to get started?** Jump to [Getting Started](./getting-started/README.md) or pick a specific topic from the navigation above!
