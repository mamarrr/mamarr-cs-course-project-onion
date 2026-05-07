# Workflow Implementation Plans — Controller Alignment Changelog

## Main update

The plans now explicitly distinguish embedded UX from controller placement.

## Decisions added

```text
Vendor category assignments:
  Keep actions in VendorsController initially.
  Do not create VendorCategoriesController initially.
  Embed category assignments in Vendor Details.
  No standalone Index.

Vendor contacts:
  Use VendorContactsController as a subresource controller.
  Entry point is Vendor Details -> Contacts.
  No standalone Index.
  Redirect back to Vendor Details after successful POST.

Resident contacts:
  Use ResidentContactsController as a subresource controller.
  Entry point is Resident Details -> Contacts.
  No standalone Index.
  Redirect back to Resident Details after successful POST.
```

## Files updated

- `00-architecture-guidance.md`
- `03-vendor-category-workflow.md`
- `04-vendor-contact-workflow.md`
- `05-resident-contact-workflow.md`
- `workflow-implementation-plans-combined.md`

All other files are included unchanged from the previous architecture-aligned package.
