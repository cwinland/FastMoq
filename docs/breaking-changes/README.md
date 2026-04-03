# Breaking Changes

This page tracks intentional breaking changes in the current unreleased repository line relative to the last public `3.0.0` package.

- Public release baseline: `3.0.0`
- Release date: May 12, 2025
- This document describes repo behavior that is intended for the next major version line.

## Current breaking changes

### `Strict` no longer implies the old full strict profile

In `3.0.0`, `Strict` was documented as a broader switch that affected multiple behaviors at once, including preconfigured object substitution, non-public fallback behavior, and strict Moq behavior.

In the current repo, `Strict` is primarily a compatibility property layered over `MockFeatures.FailOnUnconfigured` rather than the single entry point for the full strict profile.

What changed:

- `Strict = true` should no longer be treated as shorthand for the whole current strict behavior profile.
- If you want the broader repo-era strict preset, use `UseStrictPreset()`.
- If you only want fail-on-unconfigured compatibility behavior, `Strict` still works for that path.

What did not disappear:

- `Strict` still affects strict mock creation behavior.
- `Strict` still affects some non-public fallback behavior because constructor and method fallback logic is tied to `FailOnUnconfigured`.

Migration guidance:

```csharp
// Compatibility-style strict behavior
Mocks.Strict = true;

// Full repo-era strict preset
Mocks.UseStrictPreset();
```

Use `UseStrictPreset()` when the test intends to opt into the full strict behavior profile. Use `Strict` only when the test intentionally depends on the narrower compatibility path.

### Strict `IFileSystem` no longer guarantees a raw mock

In `3.0.0`, strict `IFileSystem` behavior was documented and demonstrated as using a raw Moq mock rather than the built-in `MockFileSystem` defaults.

In the current repo, tracked `IFileSystem` mocks are still preconfigured against FastMoq's built-in in-memory file system even when `Strict` or `MockFeatures.FailOnUnconfigured` is enabled.

What changed:

- `GetMock<IFileSystem>()` no longer implies an "empty" `IFileSystem` mock under strict behavior.
- Auto-created tracked `IFileSystem` mocks can expose non-null members such as `File`, `Directory`, and `Path` even when fail-on-unconfigured behavior is enabled.
- Tests that previously asserted null members on strict `IFileSystem` mocks must now explicitly override those members if null behavior is required.

Why this changed:

- The repo now treats known framework-heavy types as useful built-ins by default.
- Tracked mocks are preconfigured through the known-type pipeline so they behave consistently with the built-in in-memory file system model.
- This keeps the provider-era model simpler than having a separate strict-only `IFileSystem` branch.

Previous expectation:

```csharp
Mocks.Strict = true;
var fileSystem = Mocks.GetMock<IFileSystem>().Object;

fileSystem.Directory.Should().BeNull();
```

Current repo behavior:

```csharp
Mocks.Behavior.Enabled |= MockFeatures.FailOnUnconfigured;
var fileSystem = Mocks.GetMock<IFileSystem>().Object;

fileSystem.Directory.Should().NotBeNull();
```

If you need a null or otherwise custom member value, set it explicitly:

```csharp
Mocks.Behavior.Enabled |= MockFeatures.FailOnUnconfigured;
Mocks.GetMock<IFileSystem>()
    .Setup(x => x.Directory)
    .Returns((IDirectory)null!);
```

### What did not change in the same way

This breaking change is currently specific to strict `IFileSystem` mock enrichment.

- strict `HttpClient` still avoids the prebuilt direct `Mocker.HttpClient` instance during normal object resolution
- `DbContext` continues to use the existing built-in managed-instance path and is not part of this strict `IFileSystem` break
- `Strict` still participates in non-public constructor and method fallback decisions; the break here is about `IFileSystem` mock enrichment, not removal of all strict semantics

In other words, the repo did not broadly change every built-in type to ignore strict-mode behavior. The compatibility break identified here is the tracked `IFileSystem` mock path.

## Related docs

- [What's New Since 3.0.0](../whats-new/README.md)
- [Migration Guide: 3.0.0 To Current Repo](../migration/README.md)
- [Executable Testing Examples](../samples/testing-examples.md)
