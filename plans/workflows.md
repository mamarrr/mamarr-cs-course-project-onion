# Management Area main workflows

## Global rules for all management workflows

- Every read/write query is tenant-scoped by management company
- Every details/edit/delete route must prevent IDOR by combining id plus tenant filter plus role check
- Never trust client-provided tenant identifiers
- Return not found or forbidden without leaking cross-tenant existence
- All management pages must enforce role-based action visibility and server-side authorization

## Tickets

- List tickets across current management company portfolio with filtering
  - Filters: status, priority, category, property, unit, customer, assigned vendor, date range, overdue SLA
  - List links: ticket details, unit details, property details, customer details, assigned vendor details
- Add new ticket
  - Validate referenced property, unit, customer belong to current tenant
  - Validate category, priority, status come from allowed lookups
- Edit ticket
  - Allow updates by role policy and current lifecycle state
- Delete ticket
  - Restrict by role policy and only when business rules allow
- Ticket workflow pages
  - Ticket details
  - Ticket activity and work log timeline
  - Assignment and scheduling panel
- Lifecycle transition workflow
  - Created to Assigned
  - Assigned to Scheduled
  - Scheduled to In Progress
  - In Progress to Completed
  - Completed to Closed
  - Guardrail: no skipping intermediate statuses unless explicitly approved and audited
- Assign ticket to vendor
  - Vendor must belong to current tenant scope rules
  - Vendor must have matching VendorTicketCategory
  - Vendor availability and active workload capacity check
- Scheduling workflow
  - Create and update planned visit slot
  - Track scheduled work status
  - Link scheduled work back to ticket, unit, property
- Closure workflow
  - Ensure required work logs and outcome details exist before Completed
  - Ensure verification step before Closed

### Ticket role split inside management area

- Manager: full lifecycle progression, reassignment, closure decision
- Support specialist: triage, assignment preparation, scheduling, activity updates
- Finance: billing-relevant view access and finance-related updates only

## Customers

- List customers under current management company with filtering
- Add new customer
- Edit customer
- Delete customer according to role/business restrictions
- Customer details workflow
  - Customer profile and representatives overview
  - Customer portfolio metrics
  - Links to related properties, units, tickets
- Cross-area link to customer-facing context pages where applicable

## Properties

- List properties under current management company with filtering
- Add new property
- Edit property
- Delete property according to role/business restrictions
- Property details workflow
  - Property profile
  - Operational indicators and open ticket summary
  - Quick links to units and related tickets
- Property units workflow
  - Unit list under selected property
  - Unit drill-down link
  - Unit-to-ticket navigation
- Cross-area link to property-facing context pages where applicable

## Units

- Unit details workflow in management area
  - Unit occupancy and resident context
  - Unit open ticket list
  - Unit scheduled works list
  - Quick actions to create ticket and open related ticket details

## Vendors

- List vendors with filtering by category capability, availability, workload, performance indicators
- Add new vendor
- Edit vendor
- Delete vendor according to role/business restrictions
- Vendor assignment safeguards
  - Only assign vendors with matching VendorTicketCategory
  - Block assignment when vendor is inactive or unavailable by policy

### Vendor details page

- Vendor details
- Add VendorTicketCategory entry
- Edit or remove VendorTicketCategory entry by policy
- Vendor contacts
  - Add vendor contact
  - Edit vendor contact
  - Remove vendor contact
- Vendor tickets list
  - Open assigned ticket details
  - Open assignment and scheduling view
- Vendor scheduled works list
  - Open linked ticket, property, unit
  - Track work status progression

## Localization and display consistency

- Management create/edit flows that target localizable domain fields must map form string to LangStr
- Management edit flows for localizable fields must preserve existing translations and update current culture translation only
- List/details pages render localized labels via LangStr display conversion behavior

## Cross-area navigation requirements

- From ticket list and ticket details, always provide links to unit, property, customer, vendor details
- From customer details, provide links to related properties, units, tickets
- From property details and units list, provide links to unit details and related tickets
- From vendor details, provide links to assigned tickets, scheduling page, related property and unit
