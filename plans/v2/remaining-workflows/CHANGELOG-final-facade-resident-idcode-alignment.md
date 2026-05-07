# Workflow Implementation Plans — Final Facade and Resident ID Code Alignment Changelog

## Main updates

Removed remaining standalone public subworkflow service-contract wording.

Use parent facades:

```text
Vendor category assignments -> IVendorService / IAppBLL.Vendors
Customer representatives -> ICustomerService / IAppBLL.Customers
Resident user links -> IResidentService / IAppBLL.Residents
Scheduled work -> ITicketService / IAppBLL.Tickets
Work logs -> ITicketService / IAppBLL.Tickets
```

Do not create public:

```text
IVendorCategoryService
ICustomerRepresentativeService
IResidentUserService
IScheduledWorkService
IWorkLogService
```

Internal helper classes are allowed if needed, but they should remain internal implementation details.

## Resident ID code update

Resident ID code is unique by management company:

```csharp
builder.Entity<Resident>()
    .HasIndex(e => new { e.ManagementCompanyId, e.IdCode })
    .IsUnique()
    .HasDatabaseName("ux_resident_company_id_code");
```

Public self-link by ID code has no management-company route, so the BLL should query active residents by ID code across management companies.

Current policy:

```text
0 matches -> return validation/not-found error
1 match -> create/reuse one ResidentUser link
multiple matches across management companies -> create/reuse links for all active matches
```

No management-company approval is required. The links grant resident context only.

## Alternate key / unique index decision

The codebase has replaced natural-key alternate keys with unique indexes.

Use:

```csharp
builder.Entity<Resident>()
    .HasIndex(e => new { e.ManagementCompanyId, e.IdCode })
    .IsUnique()
    .HasDatabaseName("ux_resident_company_id_code");
```

Do not use:

```csharp
builder.Entity<Resident>()
    .HasAlternateKey(e => new { e.ManagementCompanyId, e.IdCode })
    .HasName("uq_resident_mcompany_idcode");
```

Reason:

```text
ResidentUser and other relationships use ResidentId.
The self-link workflow needs uniqueness and lookup, not an EF principal key.
Use alternate keys only when another entity explicitly targets a natural key via HasPrincipalKey.
```

## Files updated

- `00-architecture-guidance.md`
- `03-vendor-category-workflow.md`
- `06-customer-representative-workflow.md`
- `07-resident-user-link-workflow.md`
- `08-scheduled-work-workflow.md`
- `09-work-log-workflow.md`
- `CHANGELOG-architecture-alignment.md`
- `workflow-implementation-plans-combined.md`
