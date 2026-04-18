# Copilot Migration Prompts

Use [Migration Guide: 3.0.0 To The Current v4 Line](./README.md) and its linked detail pages as the source of truth.

This page only collects reusable AI prompt templates that point back to those migration docs. If you copy one of these prompts into a custom prompt, keep the doc links in the prompt body; the model does not need this page at runtime.

## When to use which prompt

| Prompt | Use it when | Avoid it when |
| --- | --- | --- |
| Project-level migration prompt | You are migrating a suite in small batches and still need to decide which tests should stabilize first versus modernize now. | You want every touched file to remove obsolete APIs aggressively. |
| Strict obsolete-cleanup prompt | The suite is already stable and you want touched files fully converted away from obsolete FastMoq entry points wherever possible. | You are still in the earliest "just get the suite green" phase. |

## Project-level migration prompt

Use this prompt for staged migration work where package, provider, and obsolete-API decisions still matter.

```text
I am migrating this test project from FastMoq 3.0.0-style usage to the current v4 provider-first APIs.

Use these docs, in order, as the source of truth:
- Main migration guide and routing: https://github.com/cwinland/FastMoq/blob/master/docs/migration/README.md
- API replacements and migration exceptions: https://github.com/cwinland/FastMoq/blob/master/docs/migration/api-replacements-and-exceptions.md
- Provider bootstrap, package boundaries, and Moq-to-NSubstitute translation: https://github.com/cwinland/FastMoq/blob/master/docs/migration/provider-and-compatibility.md
- Framework helpers, FastMoq.Web migration, claims-principal guidance, keyed DI, and FunctionContext.InstanceServices: https://github.com/cwinland/FastMoq/blob/master/docs/migration/framework-and-web-helpers.md

If only one doc can be provided, use the main migration guide. If two can be provided, use the main migration guide and API replacements doc.

Work only on a small batch of tests at a time and run tests frequently after each batch.

Before editing:
- inspect package references and determine whether the project uses the aggregate FastMoq package or split packages such as FastMoq.Core, FastMoq.Web, FastMoq.Database, FastMoq.Provider.Moq, or FastMoq.Provider.NSubstitute
- inspect shared helpers and base classes before leaf tests, especially `.Object`, `.Reset()`, `Func<Times>`, and framework service-provider setup such as `FunctionContext.InstanceServices`
- identify Moq-specific dependencies such as GetMock<T>(), direct Mock<T>, Setup(...), SetupSet(...), SetupAllProperties(), Protected(), VerifyLogger(...), and out/ref verification
- identify web helpers, DbContext helpers, and HTTP compatibility helpers that imply package boundaries

Migration rules:
- prefer GetOrCreateMock<T>(), GetObject<T>(), Verify(...), VerifyNoOtherCalls(...), VerifyLogged(...), and TimesSpec when that makes the test clearer
- treat obsolete APIs such as GetMock<T>(), VerifyLogger(...), Initialize<T>(...), and MockOptional as migration targets, not the preferred end state
- replace GetMock<T>() with GetOrCreateMock<T>() plus provider-package extensions such as AsMoq() unless the test still truly requires raw Mock<T>-shaped compatibility
- keep Moq-specific behavior on Moq when the test still depends on Moq-only semantics
- for NSubstitute migration, replace Moq Setup(...) syntax with native NSubstitute arrangement syntax
- for controller or HttpContext tests, prefer FastMoq.Web helpers such as CreateHttpContext(...), CreateControllerContext(...), SetupClaimsPrincipal(...), AddHttpContext(...), and AddHttpContextAccessor(...)
- if exact custom claims matter, use TestClaimsPrincipalOptions with IncludeDefaultIdentityClaims = false
- if local repo helpers overlap with FastMoq.Web helpers, re-point the local wrappers first and simplify later only if it improves clarity
- do not force provider-neutral rewrites where Moq compatibility is still the clearer path, but still remove obsolete entry points where a v4 replacement exists

For this batch:
- explain the package and provider assumptions you found
- explain which tests should stay on Moq and which can move toward provider-neutral APIs
- convert obsolete APIs to their v4 replacements wherever the test intent stays clear
- make the smallest safe edits needed
- list any obsolete usages that remain and the exact blocker for each one
```

Use this version for incremental migration. Stabilize first if the suite is still red. Once it is stable, treat obsolete removal as the default for touched tests and leave compatibility APIs only where a real blocker remains.

## Strict obsolete-cleanup prompt

Use this version when the goal is to keep the suite passing and remove obsolete FastMoq entry points from every touched file unless a concrete blocker remains.

```text
I am migrating this FastMoq test file to the current v4 APIs.

Use these docs, in order, as the source of truth:
- Main migration guide and routing: https://github.com/cwinland/FastMoq/blob/master/docs/migration/README.md
- API replacements and migration exceptions: https://github.com/cwinland/FastMoq/blob/master/docs/migration/api-replacements-and-exceptions.md
- Provider bootstrap, package boundaries, and Moq-to-NSubstitute translation: https://github.com/cwinland/FastMoq/blob/master/docs/migration/provider-and-compatibility.md
- Framework helpers, FastMoq.Web migration, claims-principal guidance, keyed DI, and FunctionContext.InstanceServices: https://github.com/cwinland/FastMoq/blob/master/docs/migration/framework-and-web-helpers.md

If only one doc can be provided, use the main migration guide. If two can be provided, use the main migration guide and API replacements doc.

Treat obsolete FastMoq APIs as migration defects to remove from this file unless there is a documented blocker.

Before editing:
- inspect package references and active provider assumptions
- inspect shared helpers or base classes first if the file depends on them
- identify every use of GetMock<T>(), VerifyLogger(...), Initialize<T>(...), MockOptional, and other obsolete or compatibility-only FastMoq APIs in this file
- identify any Moq-only behavior that still requires provider-specific handling

Editing rules:
- replace obsolete FastMoq APIs with current v4 equivalents wherever possible
- prefer GetOrCreateMock<T>(), GetObject<T>(), Verify(...), VerifyNoOtherCalls(...), VerifyLogged(...), TimesSpec, AddType(...), and FastMoq.Web helpers where appropriate
- use GetOrCreateMock<T>().AsMoq() or direct provider-package extensions instead of leaving GetMock<T>() behind when the test still needs Moq semantics
- only keep an obsolete call if the file still depends on a compatibility-only shape with no clean v4 replacement in that test

Output expectations:
- make the smallest safe edits needed
- list every obsolete usage that was replaced
- list every obsolete usage that remains and explain the specific blocker for each one
```

Use the strict variant after the first stabilization pass. It is for touched-file cleanup, not the earliest "just get the suite green" phase.
