# Workflow Implementation Plans — Unique Index Alignment Changelog

## Why this update exists

The latest `dev` branch replaced natural-key alternate keys with unique indexes.

The workflow plans now align with that direction.

## Main decision

For resident ID code:

```csharp
builder.Entity<Resident>()
    .HasIndex(e => new { e.ManagementCompanyId, e.IdCode })
    .IsUnique()
    .HasDatabaseName("ux_resident_company_id_code");
```

Do not reintroduce:

```csharp
builder.Entity<Resident>()
    .HasAlternateKey(e => new { e.ManagementCompanyId, e.IdCode })
    .HasName("uq_resident_mcompany_idcode");
```

## Rule

```text
Use unique indexes for schema/business uniqueness.
Use EF alternate keys only when another entity targets that natural key through HasPrincipalKey.
```

The resident self-link workflow needs lookup and uniqueness. It does not need an EF alternate key because relationships use `ResidentId`.

## Files updated

- `00-architecture-guidance.md`
- `07-resident-user-link-workflow.md`
- `CHANGELOG-final-facade-resident-idcode-alignment.md`
- `CHANGELOG-architecture-alignment.md`
- `workflow-implementation-plans-combined.md`
