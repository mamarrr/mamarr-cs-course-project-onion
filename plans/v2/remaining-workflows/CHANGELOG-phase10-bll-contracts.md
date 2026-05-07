# Changelog: Phase 10 BLL Contracts for Extracted Ticket Subworkflows

## Updated files

- `10-ticket-lifecycle-integration-workflow.md`
- `00-architecture-guidance.md`

## Main correction

The plans now explicitly allow and require BLL contracts for the extracted ticket subworkflow services:

```text
IScheduledWorkService / ScheduledWorkService
IWorkLogService / WorkLogService
```

These contracts are for BLL composition and DI behind `TicketService`.

## Architecture rule retained

The extracted services are still not AppBLL facades:

```text
Do not add IAppBLL.ScheduledWorks.
Do not add IAppBLL.WorkLogs.
Controllers still call only _bll.Tickets.
```

## DI shape

```csharp
services.AddScoped<IScheduledWorkService, ScheduledWorkService>();
services.AddScoped<IWorkLogService, WorkLogService>();
services.AddScoped<ITicketService, TicketService>();
```
