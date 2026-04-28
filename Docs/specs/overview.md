# Project Overview Specification

## Purpose

This project is a multi-tenant property maintenance CRM for organizations that manage properties, customers, units, residents, and maintenance activity across multiple portfolios. Its core purpose is to give each management company a structured workspace for handling maintenance operations from onboarding through issue resolution.

At a product level, the platform combines CRM-style relationship management with operational maintenance workflows. It supports the creation and management of company relationships, property structures, occupants, and maintenance tickets in one shared system while keeping each tenant isolated from others.

## Product Scope

The current repository structure indicates a platform organized around several connected product areas:

- onboarding and workspace access
- management company administration
- customer and resident workspaces
- property and unit management
- lease and occupancy relationships
- maintenance ticket lifecycle handling
- vendor and work tracking
- web UI and versioned API access

## High-Level Feature Areas

### Onboarding and workspace entry

The product supports account onboarding, company creation, company join requests, and workspace selection or redirection. This suggests the platform is designed for users who may belong to multiple business contexts and need guided entry into the correct workspace.

### Management company operations

Management company functionality appears to be a central product area. It includes company profile management, membership administration, role handling, and company-scoped operations for users responsible for overseeing property portfolios and service processes.

### Customer, resident, property, and unit management

The repository indicates structured support for:

- customer organizations
- residents
- properties
- units
- lease assignments and occupancy relationships

Together, these capabilities position the product as a property operations system rather than a ticketing tool alone.

### Maintenance lifecycle management

The domain model shows a full maintenance workflow built around tickets, categories, priorities, statuses, scheduled work, work logs, and vendors. At a high level, the product is intended to help track issues from initial reporting through assignment, scheduling, execution, completion, and closure.

### Shared dashboards and workspace navigation

The presence of dashboard DTOs and workspace-specific services suggests role-based dashboard experiences and context-aware navigation across management, customer, resident, property, and unit views.

## Product Direction Note

These specifications are intentionally concise and high level. They describe current inferred product intent and major functional areas based on the repository structure. They will be refined and expanded in later iterations as business rules, user journeys, and platform requirements are clarified.
