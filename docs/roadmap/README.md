# FastMoq Roadmap Notes

This document captures the current provider-first direction for FastMoq. It is not a marketing roadmap. It is a working backlog note for contributors and maintainers.

It serves as the repo-local place to track v4.x hardening work and candidate v5 enhancements.

For tracker-ready backlog text derived from recent migration feedback, see [Migration Feedback Issue Drafts](./migration-feedback-issue-drafts.md).

## Current Direction

FastMoq is moving toward a provider-based architecture where:

1. FastMoq core owns creation, injection, lifecycle, and portable helpers.
2. Provider-native arrangement and advanced mocking behavior stay with the active mocking library.
3. Moq-specific backward compatibility is preserved where needed, but it should live in Moq-oriented layers rather than defining all provider behavior.

## Recently Completed Foundation

- Native provider objects are now surfaced through tracked mocks.
- Instance creation now uses focused APIs with policy-driven defaults plus explicit public-only and constructor-fallback entry points.
- Known framework-style types now have a per-`Mocker` extension point through `AddKnownType(...)`.
- A repo-native testing guide now documents the current recommended patterns.
- DbContext helpers now expose explicit mocked-sets versus real in-memory modes through `GetDbContextHandle(...)` and `DbContextTestMode`.

## Active Work

### Provider boundary cleanup

The main remaining architectural work is continuing to isolate shared FastMoq behavior from Moq-specific compatibility behavior.

Areas still in motion:

- Reducing reliance on legacy Moq-oriented surfaces in core paths.
- Keeping provider-native interactions available without forcing all providers to emulate Moq.
- Preserving compatibility where it is useful while keeping the long-term API shape provider-neutral.

### Release hardening and packaging

The main remaining release-facing work is no longer about inventing large new features. It is about freezing the transition contract cleanly for the next major line.

Current release-hardening focus:

- Continue hardening and validating the packaging and provider-selection story for the built-in Moq, NSubstitute, and reflection providers.
- Keep the remaining Moq compatibility shims explicit so they do not look like provider-neutral core behavior.
- Tighten migration notes for obsolete or compatibility-only surfaces that will move in `v5`.
- Validate docs and executable examples against the release candidate behavior.

The DbContext mode split itself is now in place. The remaining work there is release hardening, test coverage, and documentation accuracy rather than surface design.

## Deferred Work

These items are intentionally deferred until the provider boundary is more settled.

### Broader web support beyond Blazor

FastMoq currently has meaningful web support, especially around Blazor and common ASP.NET abstractions. Broader web-framework coverage beyond the current Blazor-centered surface is deferred until the core/provider contracts stabilize.

Examples of deferred expansion work:

- Larger ASP.NET integration helpers beyond the current HttpContext and HttpClient patterns.
- Broader MVC, minimal API, and non-Blazor web testing convenience layers.
- Additional public web abstractions that would otherwise hard-code current framework assumptions too early.

### Provider-specific convenience layers

Non-Moq providers may eventually gain richer convenience layers, but that should happen after the shared provider contract is stable enough to support them cleanly.

### Obsolete-surface cleanup

Several Moq-oriented compatibility members remain intentionally available. Removing or reshaping them is deferred to a future major-version cleanup once provider migration guidance is ready.

### `MockOptional` replacement

The first replacement pass is now in place.

`MockOptional` is obsolete and retained only as a compatibility alias. The runtime now prefers explicit optional-parameter controls through `Mocker.OptionalParameterResolution`, `InvocationOptions`, and focused `MockerTestBase<TComponent>` component-construction overrides.

The remaining cleanup direction is to continue reducing older `MockOptional` examples and remove the alias in `v5`, after `v4` ships with the explicit migration path.

Future work should evaluate:

- whether any remaining compatibility-only `MockOptional` examples should be rewritten immediately
- whether more helper APIs should expose the explicit options model directly
- whether any `v4` release notes need stronger migration guidance before the planned `v5` removal

## Documentation Follow-Ups

Documentation now covers the testing decision points and known-type behavior, but more sample updates are still desirable later:

- Expanded provider-native examples beyond the current capability matrix and provider-style samples.
- Focused migration notes for older Moq-heavy test suites.
- Expanded web samples if broader web support moves out of the deferred bucket.
- Additional DbContext examples now that mock-mode versus real-mode options are explicit.

## Decision Rules

The current prioritization rules are:

1. Prefer provider-boundary work over adding new provider-specific surface area.
2. Prefer per-`Mocker` extension points over global mutable registries.
3. Prefer compatibility shims in provider-specific layers over pushing Moq behavior into all providers.
4. Defer new web-framework breadth until the core/provider split is clearer.
