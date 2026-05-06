# Follow-up Note — WebApp Mapper Cleanup

This is not part of the BLL refactor implementation phases unless explicitly assigned.

---

## Goal

Remove `WebApp/Mappers` where they only hide simple ViewModel -> BLL DTO/command construction.

---

## Recommended rule

```text
Remove WebApp/Mappers where they only map ViewModel -> BLL DTO/command.
Controllers may construct BLL DTOs/commands directly from ViewModels.
Shared FluentResults-to-ModelState logic should move to MVC extensions/helpers.
Keep mapper-like helpers only when mapping is genuinely complex and reused in several controllers.
```

---

## Example

Preferred:

```csharp
var dto = new CustomerBllDto
{
    Id = vm.Id,
    Name = vm.Name,
    RegistryCode = vm.RegistryCode
};
```

Avoid mapper classes that only do this:

```csharp
_mapper.Map(vm)
```

unless the mapping is complex, reused, or includes non-trivial conversion logic.

---

## Suggested MVC helper

Move repeated error mapping to something like:

```csharp
public static class ModelStateFluentResultExtensions
{
    public static void AddResultErrors(
        this ModelStateDictionary modelState,
        Result result)
    {
        foreach (var error in result.Errors)
        {
            modelState.AddModelError(string.Empty, error.Message);
        }
    }
}
```

---

## Out of scope for BLL agents

BLL agents should not implement this unless explicitly assigned.

This note exists so later WebApp agents do not reintroduce unnecessary mapper indirection.
