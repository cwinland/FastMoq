# FastMoq Sample Applications

This directory contains complete sample applications demonstrating FastMoq's capabilities in real-world scenarios, particularly focusing on modern .NET and Azure integration patterns.

## Sample Applications

1. **[E-Commerce Order Processing](./ecommerce-orders/)** - Complete order processing system with Azure integration
2. **[Microservices Communication](./microservices/)** - Service-to-service communication patterns
3. **[Background Processing](./background-services/)** - Queue processing and background jobs
4. **[Blazor Web Application](./blazor-webapp/)** - Full-stack Blazor application with testing

## Common Patterns Demonstrated

- **Azure Service Bus** integration and testing
- **Azure Blob Storage** operations
- **Azure Key Vault** configuration
- **Entity Framework Core** with Azure SQL
- **HttpClient** and external API integration
- **Background Services** and hosted services
- **Blazor Components** testing
- **API Controllers** with complex dependencies
- **Configuration and Options** patterns
- **Logging and Monitoring** integration

## Getting Started

Each sample application includes:
- Complete source code
- Comprehensive test suite using FastMoq
- Docker configuration for local development
- README with setup instructions
- Azure deployment templates

Choose a sample based on your use case and follow the individual README files for setup instructions.

## Prerequisites

- .NET 8.0 or later
- Azure subscription (for cloud features)
- Docker Desktop (optional, for containerized development)
- Visual Studio 2022 or VS Code

## Quick Start

1. Clone the repository
2. Navigate to a sample directory
3. Follow the sample-specific README
4. Run the tests to see FastMoq in action

```bash
cd docs/samples/ecommerce-orders
dotnet restore
dotnet test
```

## Learning Objectives

After exploring these samples, you'll understand how to:

- Structure tests for complex, real-world applications
- Mock Azure services and external dependencies
- Test asynchronous and background operations
- Verify logging and monitoring behavior
- Handle configuration and secrets in tests
- Test web applications and APIs comprehensively
- Implement integration testing strategies

## Support

If you have questions about the samples or need help adapting them to your specific use case, please:

1. Check the individual sample README files
2. Review the [main documentation](../../README.md)
3. Open an issue on the [FastMoq repository](https://github.com/cwinland/FastMoq/issues)