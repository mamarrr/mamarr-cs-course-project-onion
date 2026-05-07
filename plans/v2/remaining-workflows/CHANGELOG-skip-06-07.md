# Workflow Plan Package Update — Skip 06 and 07

## Decision

Plans 06 and 07 are intentionally skipped/deferred:

- 06 Customer Representative Workflow
- 07 Resident User Link Workflow

Plans 08, 09, and 10 remain valid and should be implemented next, but as management-company operational workflows only.

## Main edits

- Marked plans 06 and 07 as skipped/deferred in architecture guidance.
- Removed customer-representative and resident-user-link route/method recommendations from the active roadmap.
- Kept scheduled work and work logs under `ITicketService / IAppBLL.Tickets`.
- Removed incorrect instructions to add new `IAppBLL.ScheduledWorks` or `IAppBLL.WorkLogs` facades.
- Replaced `active user role or resident/customer context` with management-company role-only authorization in plans 08–10.
- Clarified that customer/resident contexts must not access scheduled work, work logs, or ticket lifecycle transitions.

## Recommended implementation order

1. Fix/finish plan 05.
2. Skip plan 06.
3. Skip plan 07.
4. Implement plan 08 Scheduled Work.
5. Implement plan 09 Work Logs.
6. Implement plan 10 Ticket Lifecycle Integration.
