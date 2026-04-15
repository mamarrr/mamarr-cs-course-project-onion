# Urls that need localization fixes, if issue is fixes, mark it as fixed as this is a document that is iteratevely updates

## Fixed

- http://localhost:5065/Onboarding/Register doesnt have form fields translations, Email, password, firstname, lastname

- http://localhost:5065/Onboarding/Login doesnt have form fields translations, Email, password, rememberme.

- upon logging in and going to http://localhost:5065/Onboarding, cannot change language from the dropdown menu, can click on dropdown entities, but language does not change

- http://localhost:5065/Onboarding/NewManagementCompany Doesnt have form fields translations: Name, RegistryCode, VatNumber, Email, Phone, Aadress. Also field validation errors are in english while culture is estonia.

- http://localhost:5065/Onboarding/JoinManagementCompany On trying to join a management company, the error message "Management company was not found." is in english while selected culture is estonia, also "A pending request for this company already exists." error message.

- http://localhost:5065/m/test/users Only field labels in Add Company user subsection has estonian localization, everything else lacks localization, In current users section Job Title display LangStr dictionary not current culture option. Check Admin/ContactTypeController for solving this correctly.

- http://localhost:5065/m/test/users Pending access requests subsection Join request Message shows LangStr dictionary not current culture version of message.

- http://localhost:5065/m/asd/users Basically whole page static ui lacks estonian localizaiton. In add company subsection role selection field "Select Role" entry does not have estonian localization.

- http://localhost:5065/m/asd/users Changing JobTitle of an user in for example estonian also changes it in english. Because JobTitle is LangStr the wanted behaviour is that if i change it in estonian the LangStr has dict entry et: Töötaja, while LangStr entry en: Employee remains and vice versa. Correct example in Admin/ContactTypeController.
- http://localhost:5065/Onboarding/Register Validation errors do not have estonian localization
- http://localhost:5065/Onboarding/Login Validation errors do not have estonian localization
- http://localhost:5065/m/asd/users/6b31eafe-3024-4519-9a39-6a200d0d4d39/edit changing jobtitle in whatever language gives error "No changes were saved. Please retry." 2 problems: error message is not in estonian when in estonian culture and cannot change jobtitle at all.

## Buggy 



