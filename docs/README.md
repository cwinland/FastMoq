# FastMoq Documentation

Welcome to the comprehensive FastMoq documentation! This documentation is designed to help you get the most out of FastMoq, from your first test to advanced enterprise scenarios.

## 📚 Documentation Structure

### 🚀 [Getting Started](./getting-started)

Perfect for developers new to FastMoq. Learn the basics and write your first test in minutes.

- Installation and setup
- Your first FastMoq test
- [Provider selection and setup](./getting-started/provider-selection.md)
- [Repo-native testing guide](./getting-started/testing-guide.md)
- [Executable testing examples](./samples/testing-examples.md)
- Understanding the architecture
- Common patterns and best practices
- Troubleshooting guide

### 📊 [Feature Parity](./feature-parity)

Comprehensive comparison of FastMoq with other popular mocking frameworks.

- Side-by-side feature comparison
- Migration guides from Moq and NSubstitute
- Performance and memory usage analysis
- When to choose FastMoq vs alternatives

### 👨‍🍳 [Cookbook](./cookbook)

Practical recipes for real-world testing scenarios.

- API Controller testing
- Entity Framework Core with DbContext
- Background Services and hosted services
- HttpClient and external API integration
- Configuration and Options patterns
- Logging verification
- Azure Services integration
- File system operations

### 🏗️ [Sample Applications](./samples)

Complete, runnable examples demonstrating FastMoq in production-like scenarios.

- **E-Commerce Order Processing** - Complete sample documentation under `docs/samples/ecommerce-orders`
- **Executable Testing Examples** - Smaller repo-local service tests that track current FastMoq guidance

### 📈 [Benchmarks](./benchmarks)

Performance analysis and productivity improvements.

- Execution speed comparisons
- Memory usage analysis
- Developer productivity metrics
- Real-world impact studies
- ROI calculations

### 🗺️ [Roadmap Notes](./roadmap)

Current provider-first direction, active architectural work, and intentionally deferred items.

### 🆕 [What's New Since 3.0.0](./whats-new)

Summary of the v4 release line relative to the May 12, 2025 `3.0.0` release baseline.

### ⚠️ [Breaking Changes](./breaking-changes)

Intentional v4 breaking changes, with migration notes for changed behavior.

### 🔄 [Migration Guide](./migration)

Practical guidance for moving from the `3.0.0` public release toward the current v4 provider-first patterns.

## 🎯 Quick Navigation

### By Experience Level

| Experience | Start Here | Next Steps |
| ---------- | ---------- | ---------- |
| **New to Mocking** | [Getting Started](./getting-started) | [Simple Cookbook Examples](./cookbook#api-controller-testing) |
| **Coming from Moq** | [Feature Parity](./feature-parity) | [Migration Guide](./feature-parity#migration-guide) |
| **Enterprise Teams** | [Sample Applications](./samples) | [Benchmarks](./benchmarks) |

### By Use Case

| Use Case | Documentation | Sample Code |
| -------- | ------------- | ----------- |
| **Web APIs** | [API Controller Testing](./cookbook#api-controller-testing) | [E-Commerce Sample](./samples/ecommerce-orders/) |
| **Database Testing** | [EF Core Testing](./cookbook#entity-framework-core-testing) | [Repository Patterns](./samples/ecommerce-orders/) |
| **Azure Integration** | [Azure Services](./cookbook#azure-services-testing) | [Complete Azure App](./samples/ecommerce-orders/) |
| **Background Jobs** | [Background Services](./cookbook#background-services-testing) | [Executable Testing Examples](./samples/testing-examples.md) |
| **Blazor Apps** | [Getting Started](./getting-started) | [Executable Testing Examples](./samples/testing-examples.md) |

Package note: `FastMoq` is the aggregate package. `FastMoq.Core` is intentionally lighter, EF-specific helpers live in `FastMoq.Database`, and web helpers live in `FastMoq.Web` while the primary calls stay in the `FastMoq` namespace.

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

1. Read [Getting Started](./getting-started)
2. Try the [first test example](./getting-started#your-first-test)
3. Understand [MockerTestBase](./getting-started#mockertestbase)

### 2. Practical Application (1 hour)

1. Pick a relevant [cookbook recipe](./cookbook)
2. Try it in your own project
3. Explore [advanced setup options](./getting-started#advanced-setup-options)

### 3. Production Readiness (2-3 hours)

1. Study a complete [sample application](./samples)
2. Review [performance benchmarks](./benchmarks)
3. Plan your [migration strategy](./feature-parity#migration-guide)

### 4. Mastery (Ongoing)

1. Explore all [cookbook patterns](./cookbook)
2. Adapt [sample applications](./samples) to your domain
3. Contribute back to the community

## 🤝 Community and Support

### Getting Help

- **Documentation**: You're here! Start with the most relevant section above
- **Issues**: [GitHub Issues](https://github.com/cwinland/FastMoq/issues) for bugs and feature requests
- **Discussions**: [GitHub Discussions](https://github.com/cwinland/FastMoq/discussions) for questions and community help
- **API Reference**: [Complete API Documentation](https://help.fastmoq.com/)

### Contributing

We welcome contributions! See our:

- [Contributing Guide](../CONTRIBUTING.md)
- [Code of Conduct](../CODE_OF_CONDUCT.md)
- [Development Setup](./contributing)

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

This documentation tracks the current repository direction after **FastMoq 3.0.0**. For older versions:

- [Version 2.x Documentation](./legacy/v2/)
- [Migration from 2.x to 3.x](./migration/v2-to-v3.md)

---

**Ready to get started?** Jump to [Getting Started](./getting-started) or pick a specific topic from the navigation above!
