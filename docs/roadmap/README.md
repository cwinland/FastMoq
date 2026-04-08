# FastMoq Roadmap

This page summarizes the planned FastMoq priorities for upcoming releases. It focuses on future work rather than repeating completed migration steps.

## Planned for v4

### Provider-neutral release hardening

v4 work will continue to sharpen the provider boundary so shared FastMoq behavior stays portable and provider-specific behavior stays explicit.

Planned focus areas:

- Further reduce reliance on Moq-shaped compatibility paths in shared core flows.
- Keep provider-native access available without requiring every provider to emulate Moq.
- Harden packaging and provider-selection guidance for the built-in Moq, NSubstitute, and reflection providers.
- Tighten migration guidance for compatibility surfaces that remain in v4 but are not intended to define the long-term API shape.

### Analyzer expansion

The remaining non-web analyzer work is planned for v4 where the guidance is stable enough to stay low-noise and useful in everyday test authoring.

Planned analyzer work includes:

- Distinguishing between tests that should use `GetOrCreateMock<T>()` and tests that should intentionally replace resolution with `AddType(...)` or `AddKnownType(...)`.
- Guiding suites away from one-object-for-all-types service-provider shims once the typed `IServiceProvider` helper is available.
- Adding keyed-service diagnostics when same-type constructor dependencies should not collapse into one unkeyed test double.

### Test helpers and migration support

v4 also includes helper and migration work where common testing scenarios still need a first-party answer rather than documentation alone.

Planned work includes:

- A typed `IServiceProvider` helper for framework-heavy suites that need type-aware service resolution during tests.
- An Azure Functions worker helper for `FunctionContext.InstanceServices` built around typed service resolution.
- A clearer first-party path for common `SetupSet(...)`-heavy tests where the real need is setter observation or value capture.
- Focused migration guidance and examples for compatibility-only APIs that remain temporary rather than long-term patterns.

### Documentation and examples

v4 documentation work will focus on the features and migration paths that still need stronger release-facing guidance.

Planned documentation updates include:

- More provider-native examples beyond the current capability matrix.
- Focused migration notes for older Moq-heavy suites.
- Keyed-service guidance tied to new diagnostics and helper APIs.
- Examples for new typed service-provider and Azure testing helpers.
- Additional DbContext examples as those surfaces continue to harden.

## Later or decision pending

### Web and UI expansion

Some web-focused items remain intentionally outside the committed v4 scope because their timing depends on how narrow and stable the public web surface should be.

Potential follow-up areas include:

- Additional ASP.NET integration helpers beyond the current `HttpContext` and `HttpClient` patterns.
- MVC, minimal API, and non-Blazor convenience layers.
- More public web abstractions where they can be added without hard-coding framework assumptions too early.
- Richer provider-specific convenience layers once the shared provider contract is stable enough to support them cleanly.

### Blazor migration analyzers

Targeted analyzer guidance for older `FastMoq.Web` helper patterns is still planned, but its release target remains open.

Planned follow-up includes:

- Flagging older parameter setup patterns that should move toward `RenderParameter`.
- Flagging legacy nested-component targeting assumptions when the current rendered-component path is clearer.
- Pointing older helper usage toward the current `MockerBlazorTestBase<T>` guidance when that recommendation can be made precisely.

## Planned v5 Cleanup

### Obsolete and compatibility surface cleanup

A future major-version cleanup will continue reducing Moq-oriented compatibility members that remain only to ease migration.

The goal is a smaller, clearer public surface where provider-first APIs are the default path and compatibility shims are no longer carrying day-to-day guidance.

### `MockOptional` retirement

FastMoq will continue moving optional-parameter guidance toward explicit controls such as `Mocker.OptionalParameterResolution`, `InvocationOptions`, and focused `MockerTestBase<TComponent>` construction overrides.

Planned follow-up includes:

- Rewriting any remaining compatibility-only `MockOptional` examples.
- Exposing the explicit options model more directly where targeted helper APIs would benefit from it.
- Removing the `MockOptional` compatibility alias in `v5` once the migration path is complete.
