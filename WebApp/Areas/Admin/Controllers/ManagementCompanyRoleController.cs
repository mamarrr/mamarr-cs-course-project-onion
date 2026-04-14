using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.DAL.EF;
using App.Domain;
using Microsoft.AspNetCore.Authorization;

namespace WebApp.Areas_Admin_Controllers
{
    [Area("Admin")]
    [Authorize (Roles = "SystemAdmin")]
    public class ManagementCompanyRoleController : Controller
    {
        private readonly AppDbContext _context;

        public ManagementCompanyRoleController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ManagementCompanyRole
        public async Task<IActionResult> Index()
        {
            return View(await _context.ManagementCompanyRoles.ToListAsync());
        }

        // GET: ManagementCompanyRole/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var managementCompanyRole = await _context.ManagementCompanyRoles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (managementCompanyRole == null)
            {
                return NotFound();
            }

            return View(managementCompanyRole);
        }

        // GET: ManagementCompanyRole/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ManagementCompanyRole/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Label,Id")] ManagementCompanyRole managementCompanyRole)
        {
            if (ModelState.IsValid)
            {
                managementCompanyRole.Id = Guid.NewGuid();
                _context.Add(managementCompanyRole);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(managementCompanyRole);
        }

        // GET: ManagementCompanyRole/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var managementCompanyRole = await _context.ManagementCompanyRoles.FindAsync(id);
            if (managementCompanyRole == null)
            {
                return NotFound();
            }
            return View(managementCompanyRole);
        }

        // POST: ManagementCompanyRole/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Code,Label,Id")] ManagementCompanyRole managementCompanyRole)
        {
            if (id != managementCompanyRole.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(managementCompanyRole);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ManagementCompanyRoleExists(managementCompanyRole.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(managementCompanyRole);
        }

        // GET: ManagementCompanyRole/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var managementCompanyRole = await _context.ManagementCompanyRoles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (managementCompanyRole == null)
            {
                return NotFound();
            }

            return View(managementCompanyRole);
        }

        // POST: ManagementCompanyRole/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var managementCompanyRole = await _context.ManagementCompanyRoles.FindAsync(id);
            if (managementCompanyRole != null)
            {
                _context.ManagementCompanyRoles.Remove(managementCompanyRole);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ManagementCompanyRoleExists(Guid id)
        {
            return _context.ManagementCompanyRoles.Any(e => e.Id == id);
        }
    }
}
