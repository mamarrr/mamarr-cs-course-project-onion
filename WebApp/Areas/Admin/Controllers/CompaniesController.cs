using App.BLL.Contracts;
using App.BLL.DTO.Admin.Companies;
using App.Resources.Views;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.ViewModels.Admin.Companies;

namespace WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SystemAdmin")]
[Route("Admin/Companies")]
public class CompaniesController : Controller
{
    private readonly IAppBLL _bll;

    public CompaniesController(IAppBLL bll)
    {
        _bll = bll;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(AdminCompanySearchViewModel search, CancellationToken cancellationToken)
    {
        var dto = await _bll.AdminCompanies.SearchCompaniesAsync(new AdminCompanySearchDto
        {
            SearchText = search.SearchText,
            Name = search.Name,
            RegistryCode = search.RegistryCode,
            Slug = search.Slug
        }, cancellationToken);

        return View(new AdminCompanyListViewModel
        {
            PageTitle = AdminText.Companies,
            ActiveSection = "Companies",
            Search = search,
            Companies = dto.Companies.Select(company => new AdminCompanyListItemViewModel
            {
                Id = company.Id,
                Name = company.Name,
                RegistryCode = company.RegistryCode,
                Slug = company.Slug,
                Email = company.Email,
                UsersCount = company.UsersCount,
                OpenTicketsCount = company.OpenTicketsCount
            }).ToList()
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _bll.AdminCompanies.GetCompanyDetailsAsync(id, cancellationToken);
        return dto is null ? NotFound() : View(ToDetailsVm(dto));
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _bll.AdminCompanies.GetCompanyForEditAsync(id, cancellationToken);
        return dto is null ? NotFound() : View(ToEditVm(dto));
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AdminCompanyEditViewModel vm, CancellationToken cancellationToken)
    {
        if (id != vm.Id) return NotFound();

        vm.PageTitle = AdminText.EditCompany;
        vm.ActiveSection = "Companies";
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var result = await _bll.AdminCompanies.UpdateCompanyAsync(id, new AdminCompanyUpdateDto
        {
            Name = vm.Name,
            RegistryCode = vm.RegistryCode,
            VatNumber = vm.VatNumber,
            Email = vm.Email,
            Phone = vm.Phone,
            Address = vm.Address,
            Slug = vm.Slug
        }, cancellationToken);

        if (result.IsFailed)
        {
            ModelState.AddModelError(string.Empty, result.Errors.FirstOrDefault()?.Message ?? AdminText.UnableToUpdateCompany);
            return View(vm);
        }

        var detailsVm = ToDetailsVm(result.Value);
        detailsVm.SuccessMessage = AdminText.CompanyUpdatedSuccessfully;
        return View("Details", detailsVm);
    }

    private static AdminCompanyDetailsViewModel ToDetailsVm(AdminCompanyDetailsDto dto) => new()
    {
        PageTitle = dto.Name,
        ActiveSection = "Companies",
        Id = dto.Id,
        Name = dto.Name,
        RegistryCode = dto.RegistryCode,
        VatNumber = dto.VatNumber,
        Email = dto.Email,
        Phone = dto.Phone,
        Address = dto.Address,
        Slug = dto.Slug,
        UsersCount = dto.UsersCount,
        CustomersCount = dto.CustomersCount,
        PropertiesCount = dto.PropertiesCount,
        UnitsCount = dto.UnitsCount,
        ResidentsCount = dto.ResidentsCount,
        TicketsCount = dto.TicketsCount,
        OpenTicketsCount = dto.OpenTicketsCount,
        VendorsCount = dto.VendorsCount,
        PendingJoinRequestsCount = dto.PendingJoinRequestsCount
    };

    private static AdminCompanyEditViewModel ToEditVm(AdminCompanyEditDto dto) => new()
    {
        PageTitle = AdminText.EditCompany,
        ActiveSection = "Companies",
        Id = dto.Id,
        Name = dto.Name,
        RegistryCode = dto.RegistryCode,
        VatNumber = dto.VatNumber,
        Email = dto.Email,
        Phone = dto.Phone,
        Address = dto.Address,
        Slug = dto.Slug
    };
}
