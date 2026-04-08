# Copilot Migration Prompts

This page is an appendix to [Migration Guide: 3.0.0 To Current Repo](./README.md).

The main migration guide defines the migration boundary, package choices, provider-selection rules, obsolete-surface expectations, and web-helper guidance. This page collects reusable prompt text that follows those rules.

## When to use which prompt

| Prompt | Use it when | Avoid it when |
| --- | --- | --- |
| Project-level migration prompt | You are migrating a suite in small batches and still need to decide which tests should stabilize first versus modernize now. | You want every touched file to remove obsolete APIs aggressively. |
| Strict obsolete-cleanup prompt | The suite is already stable and you want touched files fully converted away from obsolete FastMoq entry points wherever possible. | You are still in the earliest "just get the suite green" phase. |

## Project-level migration prompt

Use this prompt when you want Copilot to help with a staged migration without skipping package, provider, or obsolete-API decisions.

```text
I am migrating this test project from FastMoq 3.0.0-style usage to the current v4 provider-first APIs.

Use this migration guide as the source of truth for package, provider, and obsolete-surface rules when needed:
https://github.com/cwinland/FastMoq/blob/master/docs/migration/README.md

Work only on a small batch of tests at a time and run tests frequently after each batch.

Before editing:
- inspect the current test project package references
- determine whether the project is using the aggregate FastMoq package or split packages such as FastMoq.Core, FastMoq.Web, FastMoq.Database, FastMoq.Provider.Moq, or FastMoq.Provider.NSubstitute
- inspect shared test helpers and base classes before leaf tests, especially for `.Object`, `.Reset()`, `Func<Times>` helper signatures, and framework service-provider setup such as `FunctionContext.InstanceServices`
- identify whether the current tests depend on Moq-specific behavior such as GetMock<T>(), direct Mock<T>, Setup(...), SetupSet(...), SetupAllProperties(), Protected(), VerifyLogger(...), or out/ref verification patterns
- identify whether the tests use web helpers, DbContext helpers, or HTTP compatibility helpers that require specific packages

Migration rules:
- prefer GetOrCreateMock<T>(), GetObject<T>(), Verify(...), VerifyNoOtherCalls(...), VerifyLogged(...), and TimesSpec when that makes the test clearer
- treat obsolete APIs such as GetMock<T>(), VerifyLogger(...), Initialize<T>(...), and MockOptional as migration targets, not as the preferred end state
- replace GetMock<T>() with GetOrCreateMock<T>() plus provider-package extensions such as AsMoq() unless the test still truly requires raw Mock<T>-shaped compatibility
- keep Moq-specific behavior on Moq when the test still depends on Moq-only semantics
- for NSubstitute migration, replace Moq Setup(...) syntax with native NSubstitute arrangement syntax instead of trying to keep Moq-shaped setup chains
- for controller or HttpContext tests, prefer FastMoq.Web helpers such as CreateHttpContext(...), CreateControllerContext(...), SetupClaimsPrincipal(...), AddHttpContext(...), and AddHttpContextAccessor(...)
- if exact custom claims matter, use TestClaimsPrincipalOptions with IncludeDefaultIdentityClaims = false
- if local repo helpers overlap with FastMoq.Web helpers, re-point the local wrappers first and simplify later only if it improves clarity
- do not rewrite every test to provider-neutral form if the suite still needs Moq compatibility in specific pockets, but still remove obsolete entry points where a provider-specific v4 replacement exists

For this batch:
- explain which package and provider assumptions you found
- explain which tests should stay on Moq versus which can move toward provider-neutral APIs
- convert obsolete APIs to their v4 replacements wherever the test intent stays clear
- make the smallest safe edits needed
- summarize any obsolete usages that remain and explain exactly why they were not converted in this batch
```

Why this prompt shape works:

- it tells Copilot to inspect package boundaries before changing code
- it keeps the migration incremental instead of turning it into one risky rewrite
- it makes full conversion away from obsolete entry points the default instead of leaving them behind by accident
- it preserves Moq-only pockets intentionally instead of treating them as mistakes
- it pushes web-helper and claims-principal migrations toward the documented FastMoq.Web path

Recommended expectation:

- use stabilization only as the first pass when the suite is not green yet
- once the suite is stable, treat full conversion away from obsolete APIs as the recommended default for touched tests
- leave obsolete usage behind only when the test still depends on a compatibility-only shape that does not yet have a clean v4 replacement

## Strict obsolete-cleanup prompt

Use this version when the goal is not only to keep the suite passing, but also to remove obsolete FastMoq entry points from every touched file unless a concrete blocker remains.

```text
I am migrating this FastMoq test file to the current v4 APIs.

Use this migration guide as the source of truth for package, provider, and obsolete-surface rules when needed:
https://github.com/cwinland/FastMoq/blob/master/docs/migration/README.md

Treat obsolete FastMoq APIs as migration defects to remove from this file unless there is a documented blocker.

Before editing:
- inspect package references and active provider assumptions
- if the file depends on shared helper wrappers or base classes, inspect those first before patching repeated leaf-test churn
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

Use the strict variant after the project has already completed its first stabilization pass. It is a better fit for touched-file cleanup than for the earliest "just get the suite green" phase.
