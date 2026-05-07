# Workflow Implementation Plans — Contact Link Service Alignment Changelog

## Main decision

Vendor/resident contact workflows should not introduce public BLL service facades.

Use:

```text
IAppBLL.Vendors / IVendorService
  vendor contact workflow methods

IAppBLL.Residents / IResidentService
  resident contact workflow methods
```

Do not use:

```text
IAppBLL.VendorContacts
IAppBLL.ResidentContacts
public IVendorContactService
public IResidentContactService
```

## What remains separate

Repositories remain separate:

```text
IVendorContactRepository
IResidentContactRepository
```

Reason: `VendorContact` and `ResidentContact` are persisted link entities with their own metadata, primary/confirmed state, validity windows, and tenant-safe link-row queries.

## ContactService boundary

`ContactService` / `ContactRepository` should stay focused on:

```text
contact value/type
company-scoped duplicate checks
contact options/selectors
contact create/update/delete for the contact row itself
```

Do not move vendor/resident contact link metadata, primary/confirmed state, validity windows, or attach/remove rules into `ContactService`.

## Files updated

- `00-architecture-guidance.md`
- `01-contact-base-workflow.md`
- `02-vendor-workflow.md`
- `04-vendor-contact-workflow.md`
- `05-resident-contact-workflow.md`
- `CHANGELOG-architecture-alignment.md`
- `CHANGELOG-controller-alignment.md`
- `workflow-implementation-plans-combined.md`
