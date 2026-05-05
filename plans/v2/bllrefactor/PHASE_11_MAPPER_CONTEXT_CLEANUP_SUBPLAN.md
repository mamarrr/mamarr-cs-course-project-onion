# Phase 11: Mapper Context Cleanup

Goal: remove authorization claim parsing from MVC mappers and make Portal
controllers pass explicit app-user ids from the WebApp context boundary.

Scope:
- Update MVC mappers used by Portal controllers to accept `Guid appUserId`
  instead of `ClaimsPrincipal`.
- Update the Customer workspace mapper used by Portal MVC pages similarly.
- Resolve app-user id in Portal controllers through
  `ICurrentPortalContextResolver`.
- Keep API controllers and API mapper claim parsing out of scope for this phase.

Out of scope:
- No BLL, DAL, Base, schema, migration, Admin, API controller, or test changes.

