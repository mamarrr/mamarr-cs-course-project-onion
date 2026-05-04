# DAL Refactor Phase 2: Define Canonical DTO Rules and Clean Risky Mappings

> Scope file only. Use this together with `DAL_REFACTOR_MASTER_GUIDANCE.md`.
> Do not implement other phases unless explicitly instructed.

## Goals

- Make canonical DAL DTOs safe for `BaseRepository` mapping.
- Avoid relying on unloaded navigation properties in canonical mapper logic.
- Keep projection DTOs for cross-entity data.
- Avoid changing repository ownership or delete behavior in this phase.

## Tasks

1. For each `XxxDalDto`, verify it contains mostly scalar properties from the same domain entity/table.

2. Remove or avoid navigation-derived fields from canonical DTOs unless the repository always loads those navigations before mapping.

3. Move navigation-derived data to projection DTOs such as:
   - profile DTOs
   - dashboard DTOs
   - list item DTOs
   - search item DTOs
   - workspace DTOs

4. Review all DAL mappers.
   - Mapper from domain to canonical DAL DTO must be safe if navigation properties are not loaded.
   - Mapper from canonical DAL DTO to domain entity must not accidentally wipe unrelated fields.
   - For entities with `LangStr`, avoid generic update unless the mapper/update is known to preserve translations correctly.

5. Identify DTOs that should remain specialized.
   - Do not remove `Create`, `Update`, `Profile`, `Dashboard`, `ListItem`, or `SearchItem` DTOs just to force one DTO everywhere.

## Example concern to look for

If `UnitDalDto` includes `CustomerId` or `ManagementCompanyId` but `Unit` only reaches those through `Property -> Customer -> ManagementCompany`, the canonical mapper can produce empty ids when navigation properties are not loaded.

Move those fields to one of:

- `UnitProfileDalDto`
- `UnitDashboardDalDto`
- another projection DTO

## Out of scope

- Do not move repository methods between repositories.
- Do not create delete guards.
- Do not rewrite BLL services.
- Do not change controller behavior.
- Do not implement cascade or blocked delete changes yet.

## Acceptance criteria

- Canonical DTOs are safe for base CRUD.
- Projection DTOs carry cross-entity display data.
- No mapper depends silently on unloaded navigation properties.
- Existing repository methods still compile after DTO cleanup.
