# FastMoq Roadmap Notes

This document captures the current provider-era direction for FastMoq. It is not a marketing roadmap. It is a working backlog note for contributors and maintainers.

## Current Direction

FastMoq is moving toward a provider-based architecture where:

1. FastMoq core owns creation, injection, lifecycle, and portable helpers.
2. Provider-native arrangement and advanced mocking behavior stay with the active mocking library.
3. Moq-specific backward compatibility is preserved where needed, but it should live in Moq-oriented layers rather than defining all provider behavior.

## Recently Completed Foundation

- Native provider objects are now surfaced through tracked mocks.
- Instance creation has an options-based API that unifies the older split entry points.
- Known framework-style types now have a per-`Mocker` extension point through `AddKnownType(...)`.
- A repo-native testing guide now documents the current recommended patterns.

## Active Work

### Provider boundary cleanup

The main remaining architectural work is continuing to isolate shared FastMoq behavior from Moq-specific compatibility behavior.

Areas still in motion:

- Reducing reliance on legacy Moq-oriented surfaces in core paths.
- Keeping provider-native interactions available without forcing all providers to emulate Moq.
- Preserving compatibility where it is useful while keeping the long-term API shape provider-neutral.

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

`MockOptional` is still available, but it is a coarse global switch and does not feel like the right long-term API shape.

The likely direction is to replace or reduce it in favor of clearer, more explicit creation/resolution controls rather than a broad process-style toggle.

Future work should evaluate:

- whether optional-parameter behavior belongs on instance-creation options instead of the ambient `Mocker`
- whether the remaining `MockOptional` scenarios are better expressed through explicit setup
- whether the member should eventually move to compatibility-only status in the next major-version cleanup

## Documentation Follow-Ups

Documentation now covers the testing decision points and known-type behavior, but more sample updates are still desirable later:

- Provider-native examples once additional provider work lands.
- Focused migration notes for older Moq-heavy test suites.
- Expanded web samples if broader web support moves out of the deferred bucket.

## Decision Rules

When choosing what to do next, use these rules:

1. Prefer provider-boundary work over adding new provider-specific surface area.
2. Prefer per-`Mocker` extension points over global mutable registries.
3. Prefer compatibility shims in provider-specific layers over pushing Moq behavior into all providers.
4. Defer new web-framework breadth until the core/provider split is clearer.
