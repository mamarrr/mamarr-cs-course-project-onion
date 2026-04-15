# Continued onboarding workflows

Note: this is a school project no big security hardening is needed.
Note: If a user doesnt have any contexts and goes to home page, it is displayed that no available contexts.

## New Management company flow
 - Register app user
 - Choose new management company flow
 - Register Company
 - Add Customer
 - Add Resident
### Option 1 (Add ResidentUser)
 - Add ResidentUser entry with AppUser email. Works only if AppUser exists, no approval by AppUser needed.

### Option 2 (Accept ResidentUser requests)
 - AppUser requests access with idcode and customer registry code.
 - System finds customer via registry code and resident with idcode.
 - Notification is created to accept/refuse
 - On accept link is created.

### Other
 - If resident is customer representative, then management company can under that resident page make CustomerRepresentative table entry with role.
   This is the only way of making CustomerRepresentative for now.
 - Management company owner or manager can add ManagerCompanyUsers directly with email, no approval needed from Appuser side.

## Management company new user flow
 - Register app User
### Option 1
 - Choose employee of existing management company flow
 - Enter registry code of management company and role to be requested.
 - Management company has to accept and then ManagementCompanyUser entry is created.
### Option 2
 - After registering user can be asked to go to home page.
 - ManagementCompany Owner or Manager can add user to ManagementCompanyUser directly with email, upon which context appears.
 



## Customer representative flow
 - Register app user
### Option 1
 - Req access to resident with customer registry code and id code
 - Management company accepts request.
 - ResidentUser entry is created

### Option 2
 - ManagementCompany Owner or Manager adds ResidentUser entry after User account creation

- Management company adds / has added CustomerRepresentative entry for Customer and Resident.

## Resident flow

- Register app user

### Option 1
- Req access to resident with customer registry code and id code
- Management company accepts request.
- ResidentUser entry is created

### Option 2
- ManagementCompany adds ResidentUser entry after account creation



## Notes

 - need some notification system, probably new database entity/entities?
 - Auditing