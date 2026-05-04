You are working in the `mamarr-cs-course-project` repository on the `dev` branch.

Goal:
Refactor the account/onboarding architecture so that ASP.NET Identity logic stays entirely in the WebApp layer, while business onboarding logic stays in the BLL layer.

Architectural direction:
- Keep one WebApp-level identity service responsible only for ASP.NET Identity operations.
- Remove the extra WebApp orchestration account service if it duplicates identity and BLL responsibilities.
- Controllers should coordinate between WebApp identity services and BLL services.
- BLL must not depend on ASP.NET Identity, UserManager, SignInManager, HttpContext, ClaimsPrincipal, cookies, or WebApp services.
- Creating a management company is business logic and should be done through the BLL onboarding service.
- Register, login, logout, user lookup, and user existence checks are identity logic and should be done through the WebApp identity service.

Expected final dependency direction:
WebApp controller:
- calls the WebApp identity service for register, login, logout, and user validation.
- calls `IAppBLL.AccountOnboarding` for management-company creation, onboarding state, workspace redirect, and other business onboarding actions.

WebApp identity service:
- wraps ASP.NET Identity.
- may use UserManager and SignInManager.
- should not inject or call IAppBLL.
- should not create management companies or perform business onboarding.

BLL:
- exposes `IAppBLL.AccountOnboarding`.
- `AccountOnboardingService` handles business onboarding only.
- it should accept the authenticated user id through command/query objects.
- it may validate that the user id is not empty.
- it should not verify users through ASP.NET Identity.

Implementation tasks:
1. Delete or stop using the duplicate WebApp account orchestration service if it overlaps with the identity service.
2. Keep a single WebApp identity service and interface for identity-only operations.
3. Ensure the WebApp identity service is registered in dependency injection.
4. Ensure controllers inject both the WebApp identity service and `IAppBLL` when they need both identity and business onboarding.
5. Update account/onboarding controllers:
    - Register/login/logout should use the WebApp identity service.
    - Management-company creation should call `IAppBLL.AccountOnboarding.CreateManagementCompanyAsync`.
    - The controller should pass the authenticated user id into the create-management-company command.
6. Ensure `IAppBLL` does not expose identity-specific services.
7. Ensure `AccountOnboardingService` does not depend on any identity service.
8. Remove any constructor parameters, fields, properties, or DI registrations that cause BLL to depend on WebApp identity infrastructure.
9. Keep command/query/model contracts in the existing BLL contract/DTO structure.
10. Run a solution build and fix resulting compile errors according to the same dependency direction.

Do not introduce a WebApp-to-BLL circular dependency.
Do not move ASP.NET Identity types into App.BLL or App.BLL.Contracts.
Do not make the BLL responsible for sign-in, sign-out, password validation, or user creation.
Do not make the identity service responsible for management-company creation.

The final architecture should be:
- IdentityAccountService: identity-only adapter around ASP.NET Identity.
- AccountOnboardingService: business onboarding service in BLL.
- Controller: coordinates request flow by calling identity service for identity work and BLL for business work.