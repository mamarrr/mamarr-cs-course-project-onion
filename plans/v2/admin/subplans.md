# Admin UI Subplans

## Phase A: backend architecture

- Add Admin DAL DTOs, BLL DTOs, repository contracts, service contracts, and mappers.
- Wire Admin repositories through `IAppUOW` / `AppUOW`.
- Wire Admin services through `IAppBLL` / `AppBLL`.
- Extend the existing lookup repository for generic lookup CRUD.

## Phase B: Admin shell and dashboard

- Add protected `WebApp/Areas/Admin` MVC shell.
- Add admin layout, sidebar, header, and CSS.
- Implement dashboard counts and recent activity through `IAppBLL.AdminDashboard`.

## Phase C: operational Admin pages

- Implement users search/details and POST-only lock/unlock.
- Implement companies search/details/edit.
- Implement generic lookup management.
- Implement read-only global ticket monitor.

## Phase D: verification and polish

- Add empty states, validation summaries, and strongly typed success/error messages.
- Run build and fix compile issues.
- Do not add tests while the project testing override remains active.
