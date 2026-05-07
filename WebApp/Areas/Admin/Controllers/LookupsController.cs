using App.BLL.Contracts;
using App.BLL.DTO.Admin.Lookups;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels.Admin.Lookups;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SystemAdmin")]
[Route("Admin/Lookups")]
public class LookupsController : Controller
{
    private readonly IAppBLL _bll;

    public LookupsController(IAppBLL bll)
    {
        _bll = bll;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(List), new { lookupType = AdminLookupType.PropertyType });
    }

    [HttpGet("{lookupType}")]
    public async Task<IActionResult> List(AdminLookupType lookupType, CancellationToken cancellationToken)
    {
        return View(MapList(await _bll.AdminLookups.GetLookupItemsAsync(lookupType, cancellationToken)));
    }

    [HttpGet("{lookupType}/create")]
    public IActionResult Create(AdminLookupType lookupType)
    {
        return View("Edit", new AdminLookupEditViewModel
        {
            PageTitle = AdminText.CreateLookup,
            ActiveSection = "Lookups",
            Type = lookupType,
            LookupTitle = lookupType.ToString()
        });
    }

    [HttpPost("{lookupType}/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminLookupType lookupType, AdminLookupEditViewModel vm, CancellationToken cancellationToken)
    {
        vm.PageTitle = AdminText.CreateLookup;
        vm.ActiveSection = "Lookups";
        vm.Type = lookupType;
        if (!ModelState.IsValid) return View("Edit", vm);

        var result = await _bll.AdminLookups.CreateLookupItemAsync(lookupType, new AdminLookupEditDto
        {
            Code = vm.Code,
            Label = vm.Label
        }, cancellationToken);

        if (result.IsFailed)
        {
            ModelState.AddModelError(string.Empty, result.Errors.FirstOrDefault()?.Message ?? AdminText.UnableToCreateLookup);
            return View("Edit", vm);
        }

        var list = MapList(await _bll.AdminLookups.GetLookupItemsAsync(lookupType, cancellationToken));
        list.SuccessMessage = AdminText.LookupItemCreatedSuccessfully;
        return View("List", list);
    }

    [HttpGet("{lookupType}/{id:guid}/edit")]
    public async Task<IActionResult> Edit(AdminLookupType lookupType, Guid id, CancellationToken cancellationToken)
    {
        var dto = await _bll.AdminLookups.GetLookupItemForEditAsync(lookupType, id, cancellationToken);
        if (dto is null) return NotFound();

        return View(new AdminLookupEditViewModel
        {
            PageTitle = AdminText.EditLookup,
            ActiveSection = "Lookups",
            Type = lookupType,
            Id = id,
            LookupTitle = lookupType.ToString(),
            Code = dto.Code,
            Label = dto.Label,
            IsProtected = dto.IsProtected
        });
    }

    [HttpPost("{lookupType}/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminLookupType lookupType, Guid id, AdminLookupEditViewModel vm, CancellationToken cancellationToken)
    {
        vm.PageTitle = AdminText.EditLookup;
        vm.ActiveSection = "Lookups";
        vm.Type = lookupType;
        vm.Id = id;
        if (!ModelState.IsValid) return View(vm);

        var result = await _bll.AdminLookups.UpdateLookupItemAsync(lookupType, id, new AdminLookupEditDto
        {
            Id = id,
            Code = vm.Code,
            Label = vm.Label
        }, cancellationToken);

        if (result.IsFailed)
        {
            ModelState.AddModelError(string.Empty, result.Errors.FirstOrDefault()?.Message ?? AdminText.UnableToUpdateLookup);
            return View(vm);
        }

        var list = MapList(await _bll.AdminLookups.GetLookupItemsAsync(lookupType, cancellationToken));
        list.SuccessMessage = AdminText.LookupItemUpdatedSuccessfully;
        return View("List", list);
    }

    [HttpGet("{lookupType}/{id:guid}/delete")]
    public async Task<IActionResult> Delete(AdminLookupType lookupType, Guid id, CancellationToken cancellationToken)
    {
        var dto = await _bll.AdminLookups.GetDeleteCheckAsync(lookupType, id, cancellationToken);
        return View(new AdminLookupDeleteViewModel
        {
            PageTitle = AdminText.DeleteLookup,
            ActiveSection = "Lookups",
            Type = lookupType,
            Id = id,
            Code = dto.Code,
            Label = dto.Label,
            IsProtected = dto.IsProtected,
            IsInUse = dto.IsInUse,
            BlockReason = dto.BlockReason
        });
    }

    [HttpPost("{lookupType}/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(AdminLookupType lookupType, Guid id, CancellationToken cancellationToken)
    {
        var result = await _bll.AdminLookups.DeleteLookupItemAsync(lookupType, id, cancellationToken);
        var list = MapList(await _bll.AdminLookups.GetLookupItemsAsync(lookupType, cancellationToken));
        if (result.IsFailed)
        {
            list.ErrorMessage = result.Errors.FirstOrDefault()?.Message ?? AdminText.UnableToDeleteLookup;
        }
        else
        {
            list.SuccessMessage = AdminText.LookupItemDeletedSuccessfully;
        }

        return View("List", list);
    }

    private static AdminLookupListViewModel MapList(AdminLookupListDto dto) => new()
    {
        PageTitle = dto.Title,
        ActiveSection = "Lookups",
        Type = dto.Type,
        LookupTitle = dto.Title,
        LookupTypes = dto.LookupTypes.Select(type => new AdminLookupTypeOptionViewModel
        {
            Type = type.Type,
            Title = type.Title
        }).ToList(),
        Items = dto.Items.Select(item => new AdminLookupItemViewModel
        {
            Id = item.Id,
            Code = item.Code,
            Label = item.Label,
            IsProtected = item.IsProtected
        }).ToList()
    };
}
