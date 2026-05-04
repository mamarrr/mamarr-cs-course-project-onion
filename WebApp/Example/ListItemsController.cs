using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using App.BLL.Contracts;
using App.DAL.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.DAL.EF;
using App.Domain;
using Base.Helpers;
using Microsoft.AspNetCore.Authorization;
using WebApp.ViewModels;

namespace WebApp.Controllers;

[Authorize]
public class ListItemsController : Controller
{
    private readonly IAppBLL _bll;

    public ListItemsController(IAppBLL bll)
    {
        _bll = bll;
    }

    // GET: ListItems
    public async Task<IActionResult> Index()
    {
        var res = await _bll.ListItems.AllAsync(appUserId: User.GetUserId());
        return View(res);
    }

    // GET: ListItems/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var res = await _bll.ListItems.FindAsync(id.Value, User.GetUserId());

        if (res == null)
        {
            return NotFound();
        }

        return View(res);
    }

    // GET: ListItems/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: ListItems/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ListItemCreateViewModel vm)
    {
        if (ModelState.IsValid)
        {
            var listItem = new App.BLL.DTO.ListItem();
            listItem.ItemDescription = vm.ItemDescription;
            listItem.IsDone = vm.IsDone;
            listItem.Summary = vm.Summary;

            _bll.ListItems.Add(listItem, User.GetUserId());

            await _bll.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(vm);
    }

    // GET: ListItems/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var listItem = await _bll.ListItems.FindAsync(id.Value, User.GetUserId());

        if (listItem == null)
        {
            return NotFound();
        }

        var vm = new ListItemEditViewModel()
        {
            Id = listItem.Id,
            ItemDescription = listItem.ItemDescription,
            Summary = listItem.Summary,
            IsDone = listItem.IsDone,
        };

        return View(vm);
    }

    // POST: ListItems/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id,
        ListItemEditViewModel vm)
    {
        if (id != vm.Id)
        {
            return NotFound();
        }


        if (ModelState.IsValid)
        {
            var listItem = new App.BLL.DTO.ListItem
            {
                Id = vm.Id,
                ItemDescription = vm.ItemDescription,
                IsDone = vm.IsDone,
                Summary = vm.Summary
            };

            await _bll.ListItems.UpdateAsync(listItem, User.GetUserId());

            await _bll.SaveChangesAsync();


            return RedirectToAction(nameof(Index));
        }

        return View(vm);
    }

    // GET: ListItems/Delete/5
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var res = await _bll.ListItems.FindAsync(id.Value, User.GetUserId());

        if (res == null)
        {
            return NotFound();
        }

        return View(res);
    }

    // POST: ListItems/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _bll.ListItems.RemoveAsync(id, User.GetUserId());
        await _bll.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}