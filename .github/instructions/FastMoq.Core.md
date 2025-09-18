# FastMoq.Core - Copilot Instructions

## 🏗 Core Architecture Rules

When working in `FastMoq.Core`, you are touching the **foundational framework** that all other projects depend on. Be extra careful about:

### 📐 Design Principles
- **Backward compatibility**: Never break existing public APIs without major version bump
- **Provider abstraction**: Keep Moq-specific code isolated, prepare for `IMockingProvider` pattern
- **Constructor resolution**: Always use `MockerConstructionHelper` for object creation
- **Dependency injection**: Follow constructor injection patterns consistently

### 🔧 Core Components
- **`Mocker.cs`**: Central mock registry and factory. Add new methods carefully, ensure thread safety.
- **`MockerTestBase*.cs`**: Base test classes. Changes here affect all consuming tests.
- **`Extensions/`**: Pure extension methods. Keep them stateless and focused.
- **`Models/`**: Data structures. Prefer immutable designs when possible.

### ✅ When Adding Features
1. **Start with interfaces**: Define contracts before implementations
2. **Add extension methods**: Don't bloat core classes, extend functionality via extensions
3. **Support all target frameworks**: Test against .NET 6, 8, and 9
4. **Maintain XML docs**: All public APIs must have comprehensive documentation
5. **Follow SOLID principles**: Single responsibility, dependency inversion

### 🚫 Core Restrictions
- **No UI dependencies**: Core should have no knowledge of Web/Blazor
- **No test-specific logic**: Keep test helpers in `TestClassExtensions`
- **No direct Moq coupling**: Prepare for provider abstraction
- **No breaking changes**: Maintain compatibility with existing consumers

### 🧪 Testing Requirements
- Add tests to `FastMoq.Tests` for all new Core functionality
- Test constructor resolution edge cases thoroughly
- Verify mock lifecycle management
- Test all target framework combinations