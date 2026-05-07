# Workflow Implementation Plans — Architecture Alignment Changelog

## Main updates

- Kept `IAppBLL` small and domain-first.
- Added rule: only first-class domains get new `IAppBLL` properties.
- Added `Vendors` as the only clearly justified new first-class domain facade.
- Changed subworkflow plans so vendor contacts/categories are exposed through Vendors.
- Changed resident contacts and resident-user links to be exposed through Residents.
- Changed customer representatives to be exposed through Customers.
- Changed scheduled work and work logs to be exposed through Tickets, with optional future Work/Operations facade only if needed.
- Removed public service-contract inheritance from `IBaseService<TDto>` in the plan text.
- Kept implementation-service inheritance from `BaseService<TBllDto, TDalDto, TRepository, IAppUOW>`.
- Changed vague `Task<Result>` method examples to concrete `Result<T>` examples.
- Strengthened embedded-first MVC page guidance.
- Marked customer representatives and resident-user links as onboarding/access workflows.
- Marked scheduled work as ticket subworkflow and work logs as scheduled-work subworkflow.
- Rebuilt `workflow-implementation-plans-combined.md` from the updated individual files.

## Page strategy

- First-class pages: Vendors.
- Embedded in Vendor Details: contacts and category assignments.
- Embedded in Resident Details: contacts and resident portal access / linked users.
- Embedded in Customer Details: representatives / customer access.
- Embedded in Ticket Details: scheduled work summary and lifecycle availability.
- Separate details page justified: Scheduled Work Details.
- Embedded in Scheduled Work Details: work logs.

## Final parent-facade cleanup update

- Replaced remaining standalone subworkflow service-contract wording:
  - `IVendorCategoryService` -> `IVendorService`
  - `ICustomerRepresentativeService` -> `ICustomerService`
  - `IResidentUserService` -> `IResidentService`
  - `IScheduledWorkService` -> `ITicketService`
  - `IWorkLogService` -> `ITicketService`
- Internal helper classes are still allowed, but they must not be public `IAppBLL` facades.
- Added resident ID code rule: `IdCode` is unique per management company, not globally.
- Public resident self-link by ID code should link all active matching residents across management companies under the current no-approval policy.
- Documented that the Resident alternate key is probably unnecessary unless used as a foreign-key principal; the unique index is enough for uniqueness.

## Unique-index alignment update

- Aligned plans with latest `dev` commit that replaced alternate keys with unique indexes.
- Resident ID-code uniqueness is now documented as a unique index on `{ ManagementCompanyId, IdCode }`.
- Plans now explicitly say not to reintroduce the resident alternate key unless a future relationship targets the natural key via `HasPrincipalKey`.
