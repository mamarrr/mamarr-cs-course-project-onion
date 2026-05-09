# Dashboard Content Plan

## Summary

Build all five planned Portal dashboards as action-focused pages with compact context cards, top-5 lists, and operational metrics. Keep full editing and detail workflows on existing Profile, Contacts, Representatives, Units, Tickets, and Scheduled Work pages.

Default definitions:

- Open tickets: any status except `CLOSED`
- Overdue tickets: `DueAt < now` and status is not `CLOSED`
- High-priority tickets: priority `HIGH` or `URGENT`
- Delayed work: scheduled work past planned start or end and work status is not `DONE` or `CANCELLED`
- Recent activity: last 30 days, top 5 newest items
- Near-term work: today and the next 7 days
- Resident portal access and linked accounts: excluded

## Dashboard Content

### Management Company Dashboard

- Compact company context: company name and current role, with links to profile and users.
- Summary cards: customers, properties, units, residents, vendors, open tickets.
- Ticket command area: overdue tickets, high/urgent tickets, due in the next 7 days, tickets by status, top 5 recently created tickets.
- Work command area: scheduled today, scheduled in the next 7 days, delayed work, recently completed work.
- Join requests: pending count and top 5 pending requests; approved/rejected are shown only as recent activity counts for last 30 days.
- Team overview: active users count and role distribution. Exclude expiring access unless `ValidTo` exists and is within 30 days.
- Recent activity: top 5 new customers, properties, units, residents, and tickets.

### Customer Dashboard

- Compact customer card: name, registry code, billing email/phone, link to profile.
- Portfolio cards: properties, units, active leases, connected residents.
- Ticket area: open, overdue, high/urgent, tickets by property, top 5 recent tickets.
- Representatives: active representative count and top 5 active representatives with role and validity dates.
- Recent activity: top 5 added properties, units, leases, and tickets.

### Resident Dashboard

- Compact resident card: full name, ID code, preferred language, link to profile.
- Active leases: top 5 current leases with property, unit, role, start/end dates.
- Tickets: open, overdue, recently closed, and top 5 recent tickets linked to this resident.
- Contacts: primary contact plus contact-method counts by type; full list remains on Contacts page.
- Representations: active customer representations with role and validity dates.
- Exclude linked user accounts and resident login access status.

### Property Dashboard

- Compact property card: label, type, address/city/postal code, link to profile.
- Unit overview: total, occupied, vacant, units by floor, total known square meters.
- Ticket area: open, overdue, high/urgent, by status, by priority, by category, top 5 recent tickets.
- Residents/leases: current resident count, active lease count, top 5 current residents/leases.
- Scheduled work: upcoming top 5, delayed top 5, recently completed top 5.
- Unit list preview: top 5 units with unit number, floor, size, lease status, current resident, open ticket count.

### Unit Dashboard

- Compact unit card: unit number, floor, size, property, link to details/profile.
- Occupancy: current lease/resident, role, start/end date; show vacant state when no active lease exists.
- Ticket area: open, overdue, recently closed, by status, by priority, top 5 recent tickets.
- Work area: upcoming scheduled work, delayed work, recently completed work.
- Timeline: top 5 newest events from lease starts/ends, ticket creation/closure, and scheduled work completion.

## Interface And Data Shape

- Add dashboard-specific BLL models rather than reusing list-page ViewModels directly.
- Each dashboard service method must resolve tenant/workspace context first, then query scoped aggregates and top-5 previews.
- Dashboard preview rows should carry enough route data for links, but not expose domain entities.
- Keep labels localized through existing resource and `LangStr` patterns.

## Verification

- Run `dotnet build App.Domain\App.Domain.csproj -nologo` or the closest existing build gate available.
- Do not add tests while the project testing override is active.
- Manually verify that each dashboard query is tenant-scoped and does not fetch by ID without company/workspace constraints.

## Assumptions

- Full plan means every dashboard category remains represented, but in compact/action-oriented form.
- Top lists use 5 items.
- Recent means last 30 days.
- Resident account/login access remains out of scope.
