# Platform Expectations Specification

## Multi-Tenant and Security Expectations

The platform is intended to operate as a multi-tenant system centered on management company boundaries. At a high level, this means each management company should work within its own scoped business context, with users only able to access data and actions that belong to their authorized tenant and role.

Key expectations include:

- tenant isolation between management companies
- role-based access across platform areas
- restricted visibility to workspace-relevant data only
- safe handling of cross-workspace routing and context selection
- protection against unauthorized access to records outside the active tenant scope

Security is therefore not only an authentication concern, but also a core product behavior that shapes how users navigate, view, and act within the system.

## Localization Expectations

The repository indicates that the product is intended to support English and Estonian as first-class languages. Localization expectations are high level but clear:

- the UI should be available in both supported languages
- user-facing labels and messages should be localized consistently
- localizable business data should support multilingual display
- language selection should remain coherent across onboarding and workspace flows

Localization is part of the expected product experience rather than a later add-on.

## API and Platform Characteristics

At a platform level, the product appears to provide both server-rendered web experiences and versioned REST API access.

High-level characteristics include:

- ASP.NET-based web application structure
- versioned API surface for external or client integration
- DTO-oriented API contracts
- Swagger or similar API discoverability support
- shared route context for workspace-aware client behavior
- support for authenticated browser and token-based API access

This suggests a platform that can serve both interactive human workflows and integration-friendly application workflows.

## Operational Characteristics

The repository also suggests several broader platform expectations:

- dashboard-oriented workspace experiences
- support for structured lookup data such as statuses, priorities, roles, and categories
- stable core domain areas spanning companies, customers, residents, properties, units, and maintenance operations
- room for continued expansion as additional workflows are specified

## Iteration Note

This document captures only high-level platform expectations inferred from the current repository. More detailed specifications for permissions, lifecycle rules, workspace boundaries, and API behavior should be added in future iterations as the product definition matures.
