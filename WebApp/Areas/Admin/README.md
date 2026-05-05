# Admin Area Technical Debt

The Admin area is intentionally isolated from the Portal/Public refactor work.
Its scaffolded CRUD controllers may still use `AppDbContext` or direct EF Core
patterns until the owner explicitly schedules Admin cleanup.

Guardrails while this exception exists:
- Keep direct EF usage contained inside `WebApp/Areas/Admin`.
- Do not copy Admin controller patterns into Public, Portal, or API surfaces.
- Do not add business workflow, tenant authorization policy, or lifecycle rules
to Admin controllers; move that behavior into `App.BLL` when touching it.
- Public API endpoints must continue to use versioned DTO contracts, not Admin
view models or domain entities.

