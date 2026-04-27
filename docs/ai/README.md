# AI Prompt Templates For FastMoq

Use the checked-in repo docs under `docs/` as the source of truth for current-branch work.

This page collects reusable AI prompt templates for three common jobs:

- migrating tests from FastMoq `3.0.0`-style usage to the current v4 line
- writing new tests directly against the current FastMoq APIs
- modernizing or reviewing existing tests that are already on v4-era packages

The hosted `help.fastmoq.com` site is a useful published mirror, but it can lag the current branch. If you are working in a checked-out repo, keep the repo-relative doc paths in the prompt body. If you only have browser access, substitute the matching GitHub pages for those files.

## When to use which prompt

| Prompt | Use it when | Avoid it when |
| --- | --- | --- |
| Project-level migration prompt | You are migrating a suite in small batches and still need to decide which tests should stabilize first versus modernize now. | You want every touched file to remove obsolete APIs aggressively. |
| Strict obsolete-cleanup prompt | The suite is already stable and you want touched files fully converted away from obsolete FastMoq entry points wherever possible. | You are still in the earliest "just get the suite green" phase. |
| New test authoring prompt | You are writing a new FastMoq test and want the current preferred APIs instead of migration compatibility paths. | You are primarily rewriting existing legacy tests with minimal churn. |
| Existing test modernization prompt | The project is already on current packages and you want to improve clarity, provider-first usage, or helper choice without treating the work as a v3-to-v4 migration. | You are doing the first stabilization pass of a legacy migration. |

## Shared rules for every prompt

Use these rules in every prompt below. They exist to stop the model from inventing FastMoq APIs or suggesting helpers that are not actually available in the current project.

- Before editing, inspect package references to confirm which FastMoq packages are installed, including provider packages such as `FastMoq.Provider.Moq` and `FastMoq.Provider.NSubstitute`, plus helper packages such as `FastMoq.Web`, `FastMoq.Database`, and `FastMoq.AzureFunctions`.
- Inspect `using` directives, `GlobalUsings.cs`, and nearby tests or shared helpers before assuming extension methods such as `Setup(...)`, `AsMoq()`, `AsNSubstitute()`, `VerifyLogged(...)`, `WhenHttpRequest(...)`, or typed service-provider helpers are available in the current project.
- Verify each FastMoq API you plan to introduce against the local codebase and the referenced docs before using it.
- If you cannot confirm that an API exists in the current project or docs, do not guess. Report the uncertainty, name what is missing, and use the nearest documented alternative instead.
- Prefer the narrowest current FastMoq API that matches the test intent. Do not rewrite to a broader or more compatibility-heavy surface unless the test still needs it.

## Project-level migration prompt

Use this prompt for staged migration work where package, provider, and obsolete-API decisions still matter.

```text
I am migrating this test project from FastMoq 3.0.0-style usage to the current v4 provider-first APIs.

Use these repo docs, in order, as the source of truth:
- docs/migration/README.md
- docs/migration/api-replacements-and-exceptions.md
- docs/migration/provider-and-compatibility.md
- docs/migration/framework-and-web-helpers.md
- docs/getting-started/provider-selection.md
- docs/getting-started/provider-capabilities.md
- docs/samples/testing-examples.md

If only one doc can be provided, use docs/migration/README.md.
If only two docs can be provided, use docs/migration/README.md and docs/migration/api-replacements-and-exceptions.md.
If I am working from hosted docs instead of a repo checkout, use the matching GitHub pages for those files and treat the checked-in repo docs as authoritative for current-branch behavior.

Before editing:
- inspect package references and determine whether the project uses the aggregate FastMoq package or split packages such as FastMoq.Core, FastMoq.Web, FastMoq.Database, FastMoq.Provider.Moq, or FastMoq.Provider.NSubstitute
- inspect using directives, global usings, shared helpers, and base classes before leaf tests, especially around `.Object`, `.Reset()`, `Func<Times>`, logger verification helpers, and framework service-provider setup such as `FunctionContext.InstanceServices`
- inspect nearby tests to confirm which FastMoq APIs are actually available in this project before proposing replacements
- identify Moq-specific dependencies such as GetMock<T>(), direct Mock<T>, Setup(...), SetupSet(...), SetupAllProperties(), Protected(), VerifyLogger(...), and out/ref verification
- identify NSubstitute-specific usage, web helpers, DbContext helpers, and HTTP compatibility helpers that imply package or provider boundaries

Migration rules:
- prefer GetOrCreateMock<T>(), GetObject<T>(), Verify(...), VerifyNoOtherCalls(...), VerifyLogged(...), and TimesSpec when that makes the test clearer
- treat obsolete APIs such as GetMock<T>(), VerifyLogger(...), Initialize<T>(...), and MockOptional as migration targets, not the preferred end state
- replace GetMock<T>() with GetOrCreateMock<T>() plus provider-package extensions such as AsMoq() only when those extensions are confirmed to exist in the current project
- for Moq-specific setup on IFastMock<T>, confirm the project references FastMoq.Provider.Moq and imports the needed provider extensions before using Setup(...), SetupGet(...), SetupSequence(...), or Protected()
- for NSubstitute migration, replace Moq Setup(...) syntax with native NSubstitute arrangement syntax only after confirming the project is actually using the NSubstitute provider
- for controller or HttpContext tests, prefer FastMoq.Web helpers such as CreateHttpContext(...), CreateControllerContext(...), SetupClaimsPrincipal(...), AddHttpContext(...), and AddHttpContextAccessor(...) only when FastMoq.Web is present
- if exact custom claims matter, use TestClaimsPrincipalOptions with IncludeDefaultIdentityClaims = false
- if local repo helpers overlap with FastMoq.Web helpers, re-point the local wrappers first and simplify later only if it improves clarity
- do not force provider-neutral rewrites where Moq compatibility is still the clearer path, but still remove obsolete entry points where a documented v4 replacement exists
- when a replacement is blocked, cite the concrete blocker such as a missing package, missing namespace import, provider capability difference, or a documented compatibility exception from the migration docs

For this batch:
- explain the package and provider assumptions you found
- explain which tests should stay on Moq and which can move toward provider-neutral APIs
- convert obsolete APIs to their v4 replacements wherever the test intent stays clear and the replacement is confirmed locally
- make the smallest safe edits needed
- list any obsolete usages that remain and the exact blocker for each one
- if you cannot confirm a proposed FastMoq API, stop and say which symbol could not be validated instead of inventing a replacement
```

Use this version for incremental migration. Stabilize first if the suite is still red. Once it is stable, treat obsolete removal as the default for touched tests and leave compatibility APIs only where a real blocker remains.

## Strict obsolete-cleanup prompt

Use this version when the goal is to keep the suite passing and remove obsolete FastMoq entry points from every touched file unless a concrete blocker remains.

```text
I am migrating this FastMoq test file to the current v4 APIs.

Use these repo docs, in order, as the source of truth:
- docs/migration/README.md
- docs/migration/api-replacements-and-exceptions.md
- docs/migration/provider-and-compatibility.md
- docs/migration/framework-and-web-helpers.md
- docs/getting-started/provider-selection.md
- docs/getting-started/provider-capabilities.md

If only one doc can be provided, use docs/migration/README.md.
If only two docs can be provided, use docs/migration/README.md and docs/migration/api-replacements-and-exceptions.md.
If I am working from hosted docs instead of a repo checkout, use the matching GitHub pages for those files and treat the checked-in repo docs as authoritative for current-branch behavior.

Before editing:
- inspect package references, active provider assumptions, using directives, and nearby test patterns
- inspect shared helpers or base classes first if the file depends on them
- identify every use of GetMock<T>(), VerifyLogger(...), Initialize<T>(...), MockOptional, and other obsolete or compatibility-only FastMoq APIs in this file
- confirm that each planned replacement API exists in the current project and referenced docs before using it
- identify any Moq-only or NSubstitute-only behavior that still requires provider-specific handling

Editing rules:
- replace obsolete FastMoq APIs with current documented v4 equivalents wherever possible
- prefer GetOrCreateMock<T>(), GetObject<T>(), Verify(...), VerifyNoOtherCalls(...), VerifyLogged(...), TimesSpec, AddType(...), and FastMoq-owned helper packages when appropriate and confirmed locally
- use GetOrCreateMock<T>().AsMoq() or direct provider-package extensions instead of leaving GetMock<T>() behind when the test still needs Moq semantics and the provider package is present
- only keep an obsolete call if the file still depends on a compatibility-only shape with no clean documented v4 replacement in that test
- if a replacement API cannot be confirmed in the current project or docs, stop and report the gap instead of guessing

Output expectations:
- make the smallest safe edits needed
- list every obsolete usage that was replaced
- list every obsolete usage that remains and explain the specific blocker for each one
```

Use the strict variant after the first stabilization pass. It is for touched-file cleanup, not the earliest "just get the suite green" phase.

## New test authoring prompt

Use this prompt when you are writing a new FastMoq test and want the current preferred APIs from the start instead of migration compatibility paths.

```text
I am writing a new test that should use the current FastMoq APIs and patterns.

Use these repo docs, in order, as the source of truth:
- docs/getting-started/testing-guide.md
- docs/getting-started/provider-selection.md
- docs/getting-started/provider-capabilities.md
- docs/samples/testing-examples.md
- docs/cookbook/README.md
- docs/migration/api-replacements-and-exceptions.md only if a compatibility or legacy question comes up

If only one doc can be provided, use docs/getting-started/testing-guide.md.
If only two docs can be provided, use docs/getting-started/testing-guide.md and docs/samples/testing-examples.md.
If I am working from hosted docs instead of a repo checkout, use the matching GitHub pages for those files and treat the checked-in repo docs as authoritative for current-branch behavior.

Before writing the test:
- inspect package references, provider selection, using directives, and nearby tests to confirm the available FastMoq APIs in this project
- inspect the component or service under test and choose the narrowest harness that matches the behavior under test
- confirm whether this test should use direct construction, direct Mocker usage, MockerTestBase<T>, FastMoq.Web helpers, DbContext helpers, or typed service-provider helpers
- confirm whether the arrange side should stay provider-specific for the selected provider or whether the test can stay fully provider-neutral

Authoring rules:
- prefer current provider-first APIs such as GetOrCreateMock<T>(), GetObject<T>(), Verify(...), VerifyNoOtherCalls(...), VerifyLogged(...), TimesSpec, AddType(...), CreateStandaloneFastMock<T>(), and FastMoq-owned helpers when they fit the test intent
- do not introduce obsolete or compatibility-only APIs such as GetMock<T>(), VerifyLogger(...), Initialize<T>(...), MockOptional, or Strict unless the test is explicitly documenting a compatibility scenario
- if provider-specific setup is needed, use the provider-specific syntax that matches the confirmed provider and package references
- prefer repo-backed example patterns from docs/samples/testing-examples.md and nearby tests over invented helper shapes
- if you cannot confirm that a FastMoq API exists in this project or docs, stop and say what could not be validated instead of inventing a symbol

For the response:
- explain the harness and provider choice briefly
- write the smallest clear test that matches current FastMoq guidance
- call out any package or import required for the APIs you used
```

Use this version for new code, examples, and fresh tests in already-upgraded projects.

## Existing test modernization prompt

Use this prompt when a test already runs on current packages but you want to improve clarity, reduce compatibility carryover, or move toward clearer provider-first patterns without treating the work as a full migration.

```text
I am modernizing an existing FastMoq test that already runs on current packages. The goal is clearer current-style FastMoq usage, not a broad migration rewrite.

Use these repo docs, in order, as the source of truth:
- docs/getting-started/testing-guide.md
- docs/samples/testing-examples.md
- docs/getting-started/provider-capabilities.md
- docs/migration/api-replacements-and-exceptions.md when compatibility carryover or obsolete APIs appear
- docs/migration/framework-and-web-helpers.md when the churn is in FastMoq.Web, service-provider helpers, or framework-specific helpers

If only one doc can be provided, use docs/getting-started/testing-guide.md.
If only two docs can be provided, use docs/getting-started/testing-guide.md and docs/samples/testing-examples.md.
If I am working from hosted docs instead of a repo checkout, use the matching GitHub pages for those files and treat the checked-in repo docs as authoritative for current-branch behavior.

Before editing:
- inspect package references, using directives, nearby tests, and shared helpers to confirm the current FastMoq surface in this project
- identify whether the test is carrying forward compatibility-only patterns, overly broad harness choices, provider-specific verification that could be provider-first, or helper indirection that no longer adds value
- confirm which replacement APIs are both documented and locally available before suggesting them

Modernization rules:
- prefer clearer current APIs when the rewrite is mechanical and low risk
- keep provider-specific setup only where the test still depends on provider-specific semantics
- prefer Mocker.Verify(...), VerifyNoOtherCalls(...), VerifyLogged(...), TimesSpec, GetObject<T>(), AddType(...), and FastMoq-owned helpers where they make the intent clearer
- do not expand local wrapper layers that only forward to verification APIs
- if a proposed cleanup depends on an API that cannot be confirmed in the local project or docs, report the gap instead of guessing

For the response:
- explain the modernization choices briefly
- make the smallest safe clarity improvements
- list any compatibility-heavy patterns that remain and why they were kept
```

Use this version for cleanup passes, authoring reviews, and small refactors in already-upgraded test suites.
