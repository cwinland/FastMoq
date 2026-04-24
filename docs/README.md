# FastMoq Documentation

Welcome to the comprehensive FastMoq documentation! This documentation is designed to help you get the most out of FastMoq, from your first test to advanced enterprise scenarios.

## 🆕 Release Highlights Since 3.0.0

If you are coming from the last public `3.0.0` package, the biggest changes in the current line are:

- provider-first architecture with automatic effective-provider discovery plus explicit provider registration and selection when needed
- new package split across the aggregate runtime, Azure SDK helpers, Azure Functions helpers, database helpers, web helpers, and provider-specific adapters
- first-party Azure SDK and Azure Functions HTTP-trigger helpers, with analyzer assets included by default in both `FastMoq` and `FastMoq.Core`
- provider-neutral verification with `TimesSpec`, `Verify(...)`, and `VerifyLogged(...)`
- fluent `Scenario.With(...).When(...).Then(...).Verify(...)` support for workflow-style tests
- explicit policy surfaces for constructor fallback, method fallback, known-type resolution, and optional-parameter behavior
- expanded migration guidance, executable examples, and generated API coverage

## Provider-First Authoring Ladder

Use this order when you are deciding which example or helper shape to copy into a new or actively edited test:

1. Start with provider-neutral helpers such as `GetOrCreateMock(...)`, `Verify(...)`, `VerifyLogged(...)`, `VerifyNoOtherCalls(...)`, `WhenHttpRequest(...)`, and `AddType(...)`.
2. Use tracked `IFastMock<T>` provider extensions such as `Setup(...)`, `SetupGet(...)`, `SetupSequence(...)`, or `AsNSubstitute()` when the selected provider package exposes them and the arrange step still needs provider-specific syntax.
3. Use explicit `AsMoq()`, raw provider-native APIs, or compatibility wrappers only for the remaining gaps such as protected members, `out` or `ref` verification, or other provider-specific pockets.

When a first-party FastMoq helper already exists for the dependency or framework primitive, prefer that helper over handwritten setup even when the handwritten version would still work.

## 📚 Documentation Structure

### 🚀 [Getting Started](./getting-started/README.md)

Perfect for developers new to FastMoq. Learn the basics and write your first test in minutes.

- Installation and setup
- Your first FastMoq test
- [Provider selection and setup](./getting-started/provider-selection.md)
- [Provider capabilities matrix](./getting-started/provider-capabilities.md)
- [Repo-native testing guide](./getting-started/testing-guide.md)
- [Prefer FastMoq-owned setup when a first-party helper exists](./getting-started/testing-guide.md#prefer-fastmoq-owned-setup)
- [Choose the narrowest harness for the test](./getting-started/testing-guide.md#choose-the-narrowest-harness)
- [Local wrapper boundary for shared helpers](./getting-started/testing-guide.md#local-wrapper-boundary)
- [Tracked vs standalone provider-first mocks](./getting-started/testing-guide.md#tracked-vs-standalone-fast-mocks)
- [Typed `IServiceProvider` helpers](./getting-started/testing-guide.md#typed-iserviceprovider-helpers)
- [Explicit constructor selection in tests](./getting-started/testing-guide.md#explicit-constructor-selection-in-tests)
- [Web helper guidance for controller and request tests](./getting-started/testing-guide.md#controller-testing)
- [Executable testing examples](./samples/testing-examples.md)
- Understanding the architecture
- Common patterns and best practices
- Troubleshooting guide

### 🔄 [Migration Guide](./migration/README.md)

Practical guidance for moving from the public `3.0.0` release toward the current `4.3.0` provider-first patterns.

- Recommended API ladder
- Migration decision table
- Package and provider bootstrap guidance
- Old-to-new API replacements and compatibility exceptions

### 🔎 [API Reference](./api/index.md)

Use the docs-side API overview when you want example-first entry points and curated type routes. Use [Complete API Documentation](https://help.fastmoq.com/) when you want the full published namespace, type, and member reference.

- Example-first API routes
- Quick reference for common types
- Provider contract entry points
- Generated namespace and type reference

### 📊 [Feature Parity](./feature-parity/README.md)

Comprehensive comparison of FastMoq with other popular mocking frameworks.

- Side-by-side feature comparison
- Migration guides from Moq and NSubstitute
- Links to runnable benchmarks when raw overhead comparisons matter
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

### 📈 [Benchmarks](./benchmarks/README.md)

Runnable BenchmarkDotNet coverage for current provider-first FastMoq flows.

- `FastMoq.Benchmarks` compares direct Moq against current FastMoq usage
- Includes the exact run command and benchmark scope for this branch
- Links to the latest checked-in short-run results summary

### 🗺️ [Roadmap Notes](./roadmap/README.md)

Current provider-first direction, active architectural work, and intentionally deferred items.

### 🆕 [What's New Since 3.0.0](./whats-new/README.md)

Summary of the major architecture, packaging, API, and documentation changes after the May 12, 2025 `3.0.0` baseline.

### ⚠️ [Breaking Changes](./breaking-changes/README.md)

Intentional v4 breaking changes, with migration notes for changed behavior.

## 🎯 Quick Navigation

### By Experience Level

| Experience | Start Here | Next Steps |
| ---------- | ---------- | ---------- |
| **New to Mocking** | [Getting Started](./getting-started/README.md) | [Simple Cookbook Examples](./cookbook/README.md#api-controller-testing) |
| **Coming from Moq** | [Feature Parity](./feature-parity/README.md) | [Migration Guide](./migration/README.md) |
| **Enterprise Teams** | [Sample Applications](./samples/README.md) | [Testing Guide](./getting-started/testing-guide.md) |

### By Use Case

| Use Case | Documentation | Sample Code |
| -------- | ------------- | ----------- |
| **Web APIs** | [API Controller Testing](./cookbook/README.md#api-controller-testing) | [E-Commerce Sample](./samples/ecommerce-orders/README.md) |
| **Web helper migration** | [Framework and web helper migration](./migration/framework-and-web-helpers.md#web-test-helpers) | [Repo-native testing guide](./getting-started/testing-guide.md#controller-testing) |
| **Database Testing** | [EF Core Testing](./cookbook/README.md#entity-framework-core-testing) | [Repository Patterns](./samples/ecommerce-orders/README.md) |
| **Azure Integration** | [Sample Applications](./samples/README.md) | [Complete Azure App](./samples/ecommerce-orders/README.md) |
| **Background Jobs** | [Background Services](./cookbook/README.md#background-services-testing) | [Executable Testing Examples](./samples/testing-examples.md) |
| **Blazor Apps** | [bUnit and Blazor test migration](./migration/bunit-and-blazor-testing.md) | [Executable Testing Examples](./samples/testing-examples.md) |

Direct routes:

- Provider-first authoring: [Getting Started](./getting-started/README.md), [Testing Guide](./getting-started/testing-guide.md), and [API quick reference](./api/quick-reference.md)
- Harness and wrapper decisions: [Choose The Narrowest Harness](./getting-started/testing-guide.md#choose-the-narrowest-harness) and [Local Wrapper Boundary](./getting-started/testing-guide.md#local-wrapper-boundary)
- Migration cleanup: [Migration Guide](./migration/README.md), [Provider and compatibility guidance](./migration/provider-and-compatibility.md), and [API replacements and migration exceptions](./migration/api-replacements-and-exceptions.md)
- Troubleshooting provider or package mismatches: [Provider selection](./getting-started/provider-selection.md), [Provider capabilities](./getting-started/provider-capabilities.md), and [Getting Started package choices](./getting-started/README.md#package-choices)

Package note: `FastMoq` is the aggregate package. Provider contracts for custom providers and advanced extensions live in `FastMoq.Abstractions`, `FastMoq.Core` keeps the provider-neutral runtime, shared Azure SDK helpers live in the `FastMoq.Azure.*` namespaces, EF-specific helpers live in `FastMoq.Database`, Azure Functions worker and HTTP-trigger helpers live in `FastMoq.AzureFunctions.Extensions`, provider-specific adapters live in `FastMoq.Provider.*`, web helpers live in `FastMoq.Web.Extensions`, and analyzer assets ship with both `FastMoq` and `FastMoq.Core` by default while the primary runtime calls stay in the `FastMoq` or `FastMoq.Extensions` namespaces.

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
2. Read the [Testing Guide](./getting-started/testing-guide.md#start-here)
3. Keep the [API quick reference](./api/quick-reference.md) nearby for type lookups while you write tests

### 2. Raw Mock Cleanup (1 hour)

1. Read the [Recommended API ladder](./migration/README.md#recommended-api-ladder)
2. Use [API replacements and migration exceptions](./migration/api-replacements-and-exceptions.md) for the high-churn old-to-new rewrites
3. Keep migration-only Moq pockets explicit instead of letting them leak back into general-purpose helpers

### 3. Ambiguity And Multi-Instance Cases (30 minutes)

1. Read [Keyed services and same-type dependencies](./getting-started/testing-guide.md#keyed-services-and-same-type-dependencies)
2. Read [Provider and compatibility guidance](./migration/provider-and-compatibility.md)
3. Use the detached and tracked decision tables before introducing a fresh `Mocker` just to get a second mock

### 4. Equality And Verification Semantics (30 minutes)

1. Read [MockModel equality semantics](./getting-started/testing-guide.md#mockmodel-equality-semantics)
2. Read the reflection-provider caveats in [Provider capabilities](./getting-started/provider-capabilities.md#reflection-provider)
3. Use [Executable testing examples](./samples/testing-examples.md) when you want detached and tracked verification examples backed by repository tests

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

This documentation tracks the FastMoq `4.3.0` line and the current v4 package layout.

---

**Ready to get started?** Jump to [Getting Started](./getting-started/README.md) or pick a specific topic from the navigation above!
