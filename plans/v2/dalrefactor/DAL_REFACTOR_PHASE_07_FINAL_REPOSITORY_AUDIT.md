# DAL Refactor Phase 7 — Final Repository and Architecture Audit

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

## Purpose

Phase 7 is the final audit after the DAL refactor, canonical DAL DTO cleanup, blocked-delete guard work, and BLL service cleanup.

This phase should not introduce a new architecture. It should verify that the architecture produced by the previous phases is consistent, understandable, and maintainable.

The audit should answer:

```text
Are repository boundaries clean?
Is BaseRepository used where it should be?
Are canonical DAL DTOs used consistently?
Are delete workflows intentionally designed?
Are BLL services using repositories correctly?
Are stale methods and duplicate semantics removed?
Does the solution build?
```

---

# Current assumptions

This Phase 7 plan assumes Phase 6 is complete.

That means:

```text
Simple create flows use repository Add return ids where practical.
Remaining Guid.NewGuid() usages in BLL are intentional workflow exceptions.
Converted delete flows use IAppDeleteGuard and then owning repository RemoveAsync.
ManagementCompany.DeleteCascadeAsync remains intentionally unchanged.
Lease scoped deletes remain allowed where they delete only lease rows after scoped lookup.
BLL validation and business decisions stay in BLL.
```

If any of these assumptions are false during implementation, record the mismatch and fix it before closing Phase 7.

---

# Important intentional exception — ManagementCompany DeleteCascadeAsync

`ManagementCompanyRepository.DeleteCascadeAsync` is intentionally kept.

It may remain user-facing and UI-accessible for now.

Do not fail the audit merely because this method exists.

Do not remove it in Phase 7.

Do not replace it with delete guard in Phase 7.

Do not convert it to `RemoveAsync` in Phase 7.

Audit it only as an intentional exception:

```text
ManagementCompany.DeleteCascadeAsync is allowed to remain.
It should be clearly understood as an exceptional management-company delete workflow.
It should not be confused with the normal blocked-delete policy for Unit, Property, Customer, Resident, or Ticket.
```

This exception means that some general audit rules must be read as:

```text
No large cascade delete workflows remain,
except the intentionally retained ManagementCompany.DeleteCascadeAsync.
```

---

# Goals

- Verify repository boundaries after the DAL refactor.
- Verify BaseRepository CRUD reuse.
- Verify canonical DAL DTO usage.
- Verify boolean blocked-delete policy for converted entities.
- Verify `ManagementCompany.DeleteCascadeAsync` is the only intentional large cascade exception.
- Remove stale methods and duplicate semantics.
- Confirm BLL uses the cleaned repository boundaries.
- Confirm the final architecture is understandable and maintainable.
- Confirm the solution builds.

---

# Checklist per repository

For every repository, verify:

```text
[ ] It inherits from BaseRepository where applicable.
[ ] It uses the canonical DAL DTO for base CRUD.
[ ] It does not duplicate base CRUD without reason.
[ ] Add/Find/All/Remove use BaseRepository where practical.
[ ] UpdateAsync uses BaseRepository only when generic update is safe.
[ ] LangStr entities use custom tracked update methods.
[ ] Custom tracked update methods use AsTracking().
[ ] Read methods rely on global no-tracking unless explicit tracking is needed.
[ ] It does not query unrelated DbSets except for necessary parent-scope checks, projections, or natural aggregate queries.
[ ] It exposes existence predicates only for its own entity or natural aggregate.
[ ] It exposes HasDeleteDependenciesAsync only for its own entity or natural aggregate.
[ ] It uses clear method names.
[ ] It does not contain duplicated logic that belongs in another repository.
```

The management-company repository has wider membership/profile/context responsibilities by design. Audit it for clarity, not for forced minimalism.

---

# BaseRepository and canonical DTO audit

Verify:

```text
[ ] Repository contracts inherit IBaseRepository<TCanonicalDalDto> where appropriate.
[ ] Repository implementations inherit BaseRepository<TCanonicalDalDto, TDomainEntity, AppDbContext> where appropriate.
[ ] Canonical DAL DTOs inherit/use the base entity shape and carry an Id.
[ ] Canonical DAL DTOs do not carry CreatedAt unless there is a specific read/display reason.
[ ] Canonical DAL DTOs do not contain LangStr properties directly.
[ ] Projection/list/detail DTOs are separate from canonical CRUD DTOs.
[ ] Removed CreateDalDto/UpdateDalDto types were not reintroduced just to support simple CRUD.
[ ] BaseRepository.Add return id is used in simple create flows where practical.
[ ] Remaining manually generated ids are documented or clearly needed before related object creation.
```

---

# Checklist for delete behavior

For converted delete workflows, verify:

```text
[ ] Important business deletes are blocked when dependent records exist.
[ ] Converted delete methods do not silently delete related business records.
[ ] BLL delete guard returns allowed/blocked only.
[ ] BLL returns generic BusinessRuleError when deletion is blocked.
[ ] No dependency counts are required for UI.
[ ] No dependency-specific UI messages are required.
[ ] Repository HasDeleteDependenciesAsync methods return bool.
[ ] Repository dependency predicates use AnyAsync where practical.
[ ] Repository delete methods delete only the requested entity, except true owned child rows if explicitly allowed.
[ ] FK constraints remain as a safety net.
[ ] Raw FK constraint names are not exposed as normal user-facing messages.
```

Converted delete workflows to verify:

```text
UnitProfileService.DeleteAsync
PropertyProfileService.DeleteAsync
CustomerProfileService.DeleteAsync
ResidentProfileService.DeleteAsync
ManagementTicketService.DeleteAsync
```

Expected converted pattern:

```text
resolve scope/access
validate confirmation
check authorization
IAppDeleteGuard.CanDeleteXAsync(...)
if blocked -> generic BusinessRuleError
if allowed -> repository RemoveAsync(...)
SaveChangesAsync
```

## Delete exceptions

Allowed exceptions:

```text
ManagementCompanyProfileService.DeleteProfileAsync may continue using ManagementCompanyRepository.DeleteCascadeAsync.
LeaseAssignmentService may continue using DeleteForResidentAsync/DeleteForUnitAsync if those methods only delete lease rows after scoped lookup.
```

Do not treat these as audit failures.

---

# Checklist for BLL

Verify:

```text
[ ] Validation decisions are in BLL services.
[ ] Business-rule decisions are in BLL services.
[ ] Permission/authorization decisions are in BLL services or BLL access services.
[ ] Repositories return facts/persistence results, not FluentResults.
[ ] BLL uses repositories through IAppUOW.
[ ] BLL does not use EF Core or AppDbContext directly.
[ ] BLL does not depend on WebApp.
[ ] BLL does not depend directly on ASP.NET Identity infrastructure except through intentionally designed identity/account services.
[ ] BLL command/model DTOs stay in BLL DTO/contract layers.
[ ] BLL uses canonical DAL DTOs for simple repository persistence.
```

---

# Checklist for repository contracts

Verify:

```text
[ ] Repository contracts expose persistence/query capabilities, not business workflows.
[ ] Repository contracts expose factual predicates, not validation decisions.
[ ] Repository contracts do not expose FluentResults.
[ ] Repository contracts do not expose WebApp/API models.
[ ] Repository contracts do not expose BLL command/model DTOs.
[ ] No obsolete methods remain in contracts.
[ ] No old cascade-delete methods remain in repository contracts except intentional exceptions.
```

Intentional contract exception:

```text
IManagementCompanyRepository.DeleteCascadeAsync remains intentionally.
```

Allowed scoped delete methods:

```text
ILeaseRepository.DeleteForResidentAsync
ILeaseRepository.DeleteForUnitAsync
```

These are allowed if they are scoped lease-row deletes and not broad cascade orchestration.

---

# Checklist for BLL contracts

Verify:

```text
[ ] BLL contracts expose business use cases.
[ ] BLL contracts use BLL command/query/model DTOs.
[ ] BLL contracts do not expose DAL DTOs.
[ ] BLL contracts do not expose EF/domain entities.
[ ] BLL contracts are grouped by feature/workflow.
[ ] BLL contracts are not forced into generic CRUD when the use case is workflow-heavy.
```

---

# Duplicate semantic audit

Search for duplicate semantics across repositories:

```text
ExistsInCompanyAsync
ExistsInCustomerAsync
ExistsInPropertyAsync
ExistsInUnitAsync
IsLinkedToUnitAsync
HasDeleteDependenciesAsync
SlugExists...
RegistryCodeExists...
AllSlugs...
Search...
List...Options...
OptionsFor...
Delete...
DeleteCascadeAsync
Update...
FindProfile...
FindDetails...
FindForEdit...
```

For each duplicate:

1. Confirm whether duplication is intentional.
2. If not intentional, keep the method in the owning repository.
3. Update callers.
4. Remove stale contract methods.
5. Build.

## Expected duplicate patterns

Some duplicate names are acceptable when each repository checks its own entity.

Examples:

```text
CustomerRepository.ExistsInCompanyAsync(customerId, managementCompanyId)
PropertyRepository.ExistsInCompanyAsync(propertyId, managementCompanyId)
UnitRepository.ExistsInCompanyAsync(unitId, managementCompanyId)
ResidentRepository.ExistsInCompanyAsync(residentId, managementCompanyId)
VendorRepository.ExistsInCompanyAsync(vendorId, managementCompanyId)
```

This is acceptable because each repository owns its own entity.

Unacceptable pattern:

```text
LeaseRepository.ResidentExistsInCompanyAsync(...)
LeaseRepository.PropertyExistsInCompanyAsync(...)
TicketRepository.CustomerExistsInCompanyAsync(...)
```

Those checks should live in the owning repositories.

---

# Repository-specific audit notes

## ContactRepository

Verify:

```text
[ ] Uses BaseRepository for simple CRUD.
[ ] Custom update exists only because Contact has LangStr Notes.
[ ] Custom update uses AsTracking and marks LangStr property modified when needed.
```

## CustomerRepository

Verify:

```text
[ ] Simple CRUD uses BaseRepository where practical.
[ ] Customer-specific predicates remain here.
[ ] HasDeleteDependenciesAsync checks customer-owned dependencies.
[ ] No broad cascade delete remains.
```

## PropertyRepository

Verify:

```text
[ ] Simple CRUD uses BaseRepository where practical.
[ ] Property-specific predicates remain here.
[ ] HasDeleteDependenciesAsync checks property dependencies.
[ ] No broad cascade delete remains.
```

## UnitRepository

Verify:

```text
[ ] Simple CRUD uses BaseRepository where practical.
[ ] Unit-specific predicates remain here.
[ ] HasDeleteDependenciesAsync checks unit dependencies.
[ ] No broad cascade delete remains.
```

## ResidentRepository

Verify:

```text
[ ] Simple CRUD uses BaseRepository where practical.
[ ] Resident-specific predicates remain here.
[ ] HasDeleteDependenciesAsync checks resident dependencies.
[ ] No broad cascade delete remains.
```

## LeaseRepository

Verify:

```text
[ ] Lease overlap logic remains here.
[ ] Resident/property/unit existence checks are not duplicated here.
[ ] Scoped list/find/update/delete methods are intentional.
[ ] DeleteForResidentAsync/DeleteForUnitAsync delete only the lease row after scoped lookup.
```

## TicketRepository

Verify:

```text
[ ] Ticket workflow queries remain here.
[ ] Ticket number checks remain here.
[ ] Ticket status update remains custom.
[ ] HasDeleteDependenciesAsync checks scheduled work/work log dependencies.
[ ] No broad cascade delete remains for normal ticket delete.
[ ] LangStr Title/Description update marks JSON/LangStr properties modified.
```

## ManagementCompanyRepository

Verify:

```text
[ ] Management-company profile/context/membership methods are clear.
[ ] Membership methods are intentional specialized methods.
[ ] DeleteCascadeAsync remains intentionally.
[ ] DeleteCascadeAsync is not accidentally used by unrelated delete flows.
[ ] DeleteCascadeAsync is understood as the explicit management-company delete workflow.
```

Do not remove `DeleteCascadeAsync` in this phase.

## ManagementCompanyJoinRequestRepository

Verify:

```text
[ ] Join-request workflow/status methods remain here.
[ ] Simple CRUD uses BaseRepository where practical.
[ ] Status transition methods are not forced into generic update.
```

## VendorRepository

Verify:

```text
[ ] Vendor predicates and ticket option methods are still owned correctly.
[ ] No unrelated entity checks are hidden here.
```

## LookupRepository

Verify:

```text
[ ] Lookup repository remains lookup-focused.
[ ] It owns lookup existence and option queries.
[ ] No business workflow logic is added here.
```

---

# Controller/API caller audit

Phase 7 is not a controller rewrite.

Only verify:

```text
[ ] Controllers compile against the current BLL contracts.
[ ] Controllers do not call DAL repositories directly.
[ ] Controllers do not bypass BLL delete guard for converted delete flows.
[ ] Controllers do not contain business validation that belongs in BLL.
[ ] Controllers may map request/view models to BLL commands.
```

Management-company delete UI may continue to call the BLL workflow that uses `DeleteCascadeAsync`.

---

# BaseService audit

Do not force services into BaseService.

Verify only:

```text
[ ] No workflow-heavy service was forced into BaseService.
[ ] BaseService is considered only for future simple CRUD use cases.
```

Do not convert these in Phase 7:

```text
onboarding services
lease assignment workflows
ticket workflows
profile services with access checks
membership administration
delete guard or delete policy services
workspace/access services
management-company profile/delete cascade workflows
```

---

# Out of scope

Do not start a new architectural refactor.

Do not convert every BLL service to `BaseService`.

Do not rewrite controllers unless stale contract usage is discovered.

Do not change database schema unless audit finds a real FK/cascade mismatch.

Do not remove or redesign `ManagementCompany.DeleteCascadeAsync`.

Do not replace management-company cascade delete with delete guard in this phase.

Do not add dependency counts or detailed blocked-delete UI messages.

Do not reintroduce DAL create/update DTOs for simple CRUD.

---

# Final audit procedure

Suggested execution:

```text
1. Build the solution.
2. Search for obsolete DAL create/update DTO names.
3. Search for old cascade delete methods.
4. Search for direct AppDbContext/EF usage in BLL.
5. Search for FluentResults or BLL errors in DAL projects.
6. Search for duplicate repository predicate semantics.
7. Review each repository contract.
8. Review each repository implementation.
9. Review converted delete BLL flows.
10. Review ManagementCompany.DeleteCascadeAsync as an intentional exception.
11. Review lease scoped delete methods as intentional.
12. Build again.
```

Useful searches:

```text
CreateDalDto
UpdateDalDto
GetDeleteDependencySummaryAsync
CountAsync(          // inspect only; counts may be valid elsewhere
DeleteCascadeAsync
ExecuteDeleteAsync
AppDbContext         // in BLL projects
FluentResults        // in DAL projects
BusinessRuleError    // in DAL projects
ValidationAppError   // in DAL projects
Guid.NewGuid()       // in BLL services, confirm intentional or replaced by Add return id
```

---

# Acceptance criteria

Phase 7 is complete when:

```text
[ ] Repositories are focused.
[ ] Base CRUD is reused where practical.
[ ] Canonical DAL DTOs are used for simple persistence.
[ ] LangStr update methods are custom and tracked.
[ ] Delete behavior is blocked-delete by default for converted entities.
[ ] Boolean HasDeleteDependenciesAsync predicates are used for converted entities.
[ ] No dependency counts/details are required for blocked-delete UI.
[ ] BLL delete guard is the central place for dependency-based delete decisions for converted entities.
[ ] Unit/Property/Customer/Resident/Ticket deletes do not cascade related business data.
[ ] ManagementCompany.DeleteCascadeAsync remains intentionally unchanged.
[ ] Lease scoped delete methods remain intentional and limited.
[ ] BLL uses repositories through IAppUOW.
[ ] BLL does not use EF Core/AppDbContext directly.
[ ] DAL does not construct BLL FluentResults/errors.
[ ] Repository contracts expose persistence/query capabilities, not BLL workflows.
[ ] BLL contracts expose business use cases.
[ ] No stale contract methods remain except documented intentional exceptions.
[ ] The solution builds.
```

---

# Final reminder

Phase 7 is an audit and cleanup phase.

It should close the refactor by confirming that the architecture is consistent.

Do not use Phase 7 to start a new refactor.

Do not change the intentional management-company cascade delete behavior.
