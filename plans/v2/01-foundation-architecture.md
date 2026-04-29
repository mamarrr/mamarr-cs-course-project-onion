# Mamarr CS Course Project Refactor Agent Plan

This file is one implementation slice of the larger refactor plan. It is written to be handed to an AI coding agent as a focused task.

Global constraints for every slice:

- Preserve existing user-facing behavior.
- Do not add new features.
- Do not implement ticket, vendor, scheduled work, or work-log functionality.
- Do not change API routes unless explicitly required and documented.
- Do not intentionally change JSON response shapes.
- Do not intentionally change MVC page behavior.
- Do not introduce FluentResults into DAL repository interfaces or UOW methods.
- Repositories return DAL DTOs, nullable DAL DTOs, booleans, lists, IDs, or throw unexpected infrastructure/programming exceptions.
- Use the existing `BaseRepository` for custom EF repositories whenever possible, because it already contains regular CRUD operations and built-in IDOR/parent-scope restrictions for entities that have a parent management company or customer.
- Custom repositories should inherit from `BaseRepository` when the entity can use the generic CRUD/mapping behavior. Add only query-specific or workflow-specific methods on top of it.
- Do not reimplement regular CRUD methods such as add, update, remove, find, or list in custom repositories unless the current `BaseRepository` cannot support that entity/use case.
- BLL service boundaries return `FluentResults.Result` or `FluentResults.Result<T>`.
- `App.DTO` remains API-only and must not be referenced by BLL.
- BLL must not reference `App.DAL.EF` or `AppDbContext`.
- Controllers must not use repositories or `AppDbContext`.
- Dependency registration helper methods live in `WebApp/Helpers`, not in `App.BLL` or `App.DAL.EF`.
- Build the solution after the slice changes and fix compile errors caused by the slice.

# Slice 0: Foundation Architecture

## Goal

Prepare the shared architecture pieces used by all later vertical slices.

This phase must not change user-facing behavior. No business controller or feature service should be refactored in this phase, except for harmless namespace/import changes needed to keep the application compiling.

## Scope

Implement:

- `App.BLL.Contracts` project.
- FluentResults package usage at the BLL service boundary.
- Shared FluentResults error classes.
- Correct `Base.DAL.Contracts` namespaces.
- `IBaseUOW.SaveChangesAsync(CancellationToken)` returning `int`.
- Transaction methods directly on `IAppUOW`.
- `AppUOW` transaction implementation that hides EF Core transaction objects.
- WebApp result-to-HTTP mapper for FluentResults.
- Mapper base interfaces.
- WebApp DI helper methods under `WebApp/Helpers`.

Do not implement any feature-specific repositories or service refactors yet.


## Repository inheritance rule

Use the existing `BaseRepository` wherever possible when implementing custom EF repositories.

`BaseRepository` already contains the regular CRUD operations and the existing IDOR/parent-scope restrictions for entities that have a parent management company or customer. Custom repositories should therefore inherit from `BaseRepository` and add only the slice-specific queries or workflow methods that are not already covered by the base implementation.

Do not duplicate generic CRUD behavior in custom repositories unless the current `BaseRepository` cannot support the entity or use case. When a repository interface needs normal CRUD operations, it should inherit from the existing `IBaseRepository` using the project's current generic type signature. Match the actual generic parameters and mapper pattern already used in the codebase.

## Files to inspect first

- `*.sln`
- `Base.DAL.Contracts/IBaseUOW.cs`
- `Base.DAL.Contracts/IBaseRepository.cs`
- `App.Contracts/IAppUow.cs` or `App.Contracts/IAppUOW.cs`
- `App.DAL.EF/AppUOW.cs`
- `App.DAL.EF/AppDbContext.cs`
- `App.BLL/App.BLL.csproj`
- `App.Contracts/App.Contracts.csproj`
- `WebApp/WebApp.csproj`
- `WebApp/Program.cs`
- Existing BLL service registration code in `Program.cs`

## Allowed files/folders to create or modify

- `App.BLL.Contracts/**`
- `App.BLL.Contracts/App.BLL.Contracts.csproj`
- `Base.DAL.Contracts/IBaseUOW.cs`
- `Base.DAL.Contracts/IBaseRepository.cs`, only if namespace fixes require it
- `App.Contracts/IAppUow.cs` or equivalent UOW contract file
- `App.Contracts/Common/**`, if the UOW contract is moved there
- `App.DAL.EF/AppUOW.cs`
- `WebApp/Infrastructure/Results/**`
- `WebApp/Helpers/DependencyInjectionHelpers.cs`
- Project files needed for references/packages
- Solution file
- `WebApp/Program.cs`, only to use the new helper methods while preserving existing registrations

Do not create:

- `App.DAL.EF/DependencyInjection.cs`
- `App.BLL/DependencyInjection.cs`
- A transaction abstraction interface/class outside `IAppUOW`
- Feature-specific repositories

## Package and project setup

Create the BLL contracts project if it does not exist.

Suggested commands:

```bash
dotnet new classlib -n App.BLL.Contracts
dotnet sln add App.BLL.Contracts/App.BLL.Contracts.csproj
dotnet add App.BLL.Contracts package FluentResults
dotnet add App.BLL package FluentResults
dotnet add App.BLL reference App.BLL.Contracts/App.BLL.Contracts.csproj
dotnet add WebApp reference App.BLL.Contracts/App.BLL.Contracts.csproj
```

Add additional project references only if needed by compilation and dependency direction.

Target dependency direction after this slice:

```text
WebApp
  -> App.DTO
  -> App.BLL.Contracts
  -> App.BLL
  -> App.Contracts

App.BLL
  -> App.BLL.Contracts
  -> App.Contracts

App.BLL.Contracts
  -> FluentResults
  -> Base.Contracts, if needed

App.DAL.EF
  -> App.Contracts
  -> App.Domain
  -> Base.DAL.EF

App.Contracts
  -> Base.DAL.Contracts
  -> Base.Contracts
```

## Task 1: Fix Base.DAL.Contracts namespaces

Ensure files under `Base.DAL.Contracts` use the contracts namespace, not the EF namespace.

Target:

```csharp
namespace Base.DAL.Contracts;
```

Update all usages caused by this change.

## Task 2: Update IBaseUOW

Target shape:

```csharp
namespace Base.DAL.Contracts;

public interface IBaseUOW
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

Update `BaseUOW` and `AppUOW` implementations accordingly.

## Task 3: Add transaction methods directly to IAppUOW

Do not create a separate transaction abstraction.

Target shape:

```csharp
public interface IAppUOW : IBaseUOW
{
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

Repository properties will be added slice by slice. Do not add all final repository properties unless their interfaces already exist.

## Task 4: Implement transactions inside AppUOW

`AppUOW` should privately hold the EF Core `IDbContextTransaction`.

Expected behavior:

- `BeginTransactionAsync` starts an EF Core transaction and stores it privately.
- `CommitTransactionAsync` commits the active transaction and clears/disposes it.
- `RollbackTransactionAsync` rolls back the active transaction and clears/disposes it.
- `IDbContextTransaction` must never be exposed outside `App.DAL.EF`.
- Nested transactions are not supported.
- Calling `BeginTransactionAsync` while a transaction is already active should throw `InvalidOperationException`.
- Calling commit/rollback with no active transaction should either no-op or throw; choose one and keep it consistent. Prefer throwing for misuse.

Example implementation idea:

```csharp
private IDbContextTransaction? _transaction;

public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
{
    if (_transaction is not null)
    {
        throw new InvalidOperationException("A transaction is already active.");
    }

    _transaction = await UowDbContext.Database.BeginTransactionAsync(cancellationToken);
}
```

Use the actual DbContext field/property names already present in the project.

## Task 5: Create App.BLL.Contracts common result errors

Create:

```text
App.BLL.Contracts/
  Common/
    Errors/
      NotFoundError.cs
      ForbiddenError.cs
      UnauthorizedError.cs
      ConflictError.cs
      ValidationAppError.cs
      BusinessRuleError.cs
      UnexpectedAppError.cs
    ValidationFailureModel.cs
```

Each error class should derive from `FluentResults.Error`.

Example:

```csharp
using FluentResults;

namespace App.BLL.Contracts.Common.Errors;

public sealed class NotFoundError : Error
{
    public NotFoundError(string message) : base(message)
    {
        Metadata["ErrorType"] = "NotFound";
    }
}
```

Validation failure model:

```csharp
namespace App.BLL.Contracts.Common;

public sealed class ValidationFailureModel
{
    public string PropertyName { get; init; } = default!;
    public string ErrorMessage { get; init; } = default!;
}
```

Validation error:

```csharp
using App.BLL.Contracts.Common;
using FluentResults;

namespace App.BLL.Contracts.Common.Errors;

public sealed class ValidationAppError : Error
{
    public IReadOnlyList<ValidationFailureModel> Failures { get; }

    public ValidationAppError(
        string message,
        IReadOnlyList<ValidationFailureModel> failures) : base(message)
    {
        Failures = failures;
        Metadata["ErrorType"] = "Validation";
    }
}
```

## Task 6: Create WebApp FluentResults-to-HTTP mapping

Create:

```text
WebApp/Infrastructure/Results/FluentResultHttpExtensions.cs
```

Target behavior:

- Success with `Result<T>` returns `OkObjectResult` after mapping model to response DTO.
- Success with `Result` returns `NoContentResult`.
- `NotFoundError` maps to 404.
- `ForbiddenError` maps to 403.
- `UnauthorizedError` maps to 401.
- `ConflictError` maps to 409.
- `ValidationAppError` maps to 400.
- `BusinessRuleError` maps to 400.
- Unknown error maps to 500.

Keep response shape as close as possible to existing error response behavior. If current project already has an error response DTO/helper, reuse that shape.

Example extension signatures:

```csharp
public static ActionResult<TResponse> ToActionResult<TModel, TResponse>(
    this Result<TModel> result,
    Func<TModel, TResponse> map)

public static IActionResult ToActionResult(this Result result)
```

## Task 7: Add mapper base interfaces

Create base mapper interfaces in appropriate projects without forcing one universal mapper pattern if existing mapper interfaces already exist.

Suggested:

```text
Base.Contracts/IMapper.cs
```

or layer-specific interfaces:

```text
App.Contracts/Common/IDalMapper.cs
App.BLL.Contracts/Common/IBllMapper.cs
WebApp/Infrastructure/Mapping/IWebMapper.cs
```

Keep this simple. Do not over-engineer.

Minimal generic shape:

```csharp
public interface IMapper<TLeft, TRight>
{
    TRight? Map(TLeft? source);
    TLeft? Map(TRight? source);
}
```

If existing base mapper classes already exist, prefer adapting to them rather than duplicating.

## Task 8: Add WebApp dependency injection helpers

Create:

```text
WebApp/Helpers/DependencyInjectionHelpers.cs
```

Do not create DI extension files inside `App.BLL` or `App.DAL.EF`.

Required methods:

```csharp
public static IServiceCollection AddAppDalEf(
    this IServiceCollection services,
    string connectionString)

public static IServiceCollection AddAppBll(
    this IServiceCollection services)

public static IServiceCollection AddWebAppMappers(
    this IServiceCollection services)
```

`AddAppDalEf` should register:

- `AppDbContext`
- `IAppUOW` -> `AppUOW`
- Existing DAL services/repositories currently registered in `Program.cs`, if any

`AddAppBll` should register existing BLL services currently registered in `Program.cs`.

`AddWebAppMappers` can be empty initially or register existing WebApp mappers if any exist.

`Program.cs` target shape:

```csharp
builder.Services.AddAppDalEf(connectionString);
builder.Services.AddAppBll();
builder.Services.AddWebAppMappers();
```

Do not remove registrations unless they are moved into the helper methods.

## Task 9: Clarify FluentResults scope in code comments or docs

Add a short comment in the foundation docs or near BLL contracts:

```text
FluentResults is used at BLL service boundaries and inside BLL application flow.
Repositories and UOW methods do not return FluentResults by default.
```

## Build verification

Run:

```bash
dotnet build
```

Fix compile errors caused by this slice.

## Stop condition

Stop after the foundation builds and existing services/controllers still compile.

Do not refactor Customer, Property, Unit, Resident, Lease, Management, or Onboarding behavior in this slice.
