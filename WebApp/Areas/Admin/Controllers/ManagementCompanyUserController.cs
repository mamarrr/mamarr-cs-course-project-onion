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
    public class ManagementCompanyUserController : Controller
    {
        private readonly AppDbContext _context;

        public ManagementCompanyUserController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ManagementCompanyUser
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.ManagementCompanyUsers.Include(m => m.AppUser).Include(m => m.ManagementCompany).Include(m => m.ManagementCompanyRole);
            return View(await appDbContext.ToListAsync());
        }

        // GET: ManagementCompanyUser/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var managementCompanyUser = await _context.ManagementCompanyUsers
                .Include(m => m.AppUser)
                .Include(m => m.ManagementCompany)
                .Include(m => m.ManagementCompanyRole)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (managementCompanyUser == null)
            {
                return NotFound();
            }

            return View(managementCompanyUser);
        }

        // GET: ManagementCompanyUser/Create
        public IActionResult Create()
        {
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "FirstName");
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address");
            ViewData["ManagementCompanyRoleId"] = new SelectList(_context.ManagementCompanyRoles, "Id", "Code");
            return View();
        }

        // POST: ManagementCompanyUser/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ValidFrom,ValidTo,JobTitle,IsActive,CreatedAt,ManagementCompanyId,AppUserId,ManagementCompanyRoleId,Id")] ManagementCompanyUser managementCompanyUser)
        {
            if (ModelState.IsValid)
            {
                managementCompanyUser.Id = Guid.NewGuid();
                _context.Add(managementCompanyUser);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "FirstName", managementCompanyUser.AppUserId);
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address", managementCompanyUser.ManagementCompanyId);
            ViewData["ManagementCompanyRoleId"] = new SelectList(_context.ManagementCompanyRoles, "Id", "Code", managementCompanyUser.ManagementCompanyRoleId);
            return View(managementCompanyUser);
        }

        // GET: ManagementCompanyUser/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var managementCompanyUser = await _context.ManagementCompanyUsers.FindAsync(id);
            if (managementCompanyUser == null)
            {
                return NotFound();
            }
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "FirstName", managementCompanyUser.AppUserId);
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address", managementCompanyUser.ManagementCompanyId);
            ViewData["ManagementCompanyRoleId"] = new SelectList(_context.ManagementCompanyRoles, "Id", "Code", managementCompanyUser.ManagementCompanyRoleId);
            return View(managementCompanyUser);
        }

        // POST: ManagementCompanyUser/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("ValidFrom,ValidTo,JobTitle,IsActive,CreatedAt,ManagementCompanyId,AppUserId,ManagementCompanyRoleId,Id")] ManagementCompanyUser managementCompanyUser)
        {
            if (id != managementCompanyUser.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(managementCompanyUser);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ManagementCompanyUserExists(managementCompanyUser.Id))
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
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "FirstName", managementCompanyUser.AppUserId);
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address", managementCompanyUser.ManagementCompanyId);
            ViewData["ManagementCompanyRoleId"] = new SelectList(_context.ManagementCompanyRoles, "Id", "Code", managementCompanyUser.ManagementCompanyRoleId);
            return View(managementCompanyUser);
        }

        // GET: ManagementCompanyUser/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var managementCompanyUser = await _context.ManagementCompanyUsers
                .Include(m => m.AppUser)
                .Include(m => m.ManagementCompany)
                .Include(m => m.ManagementCompanyRole)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (managementCompanyUser == null)
            {
                return NotFound();
            }

            return View(managementCompanyUser);
        }

        // POST: ManagementCompanyUser/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var managementCompanyUser = await _context.ManagementCompanyUsers.FindAsync(id);
            if (managementCompanyUser != null)
            {
                _context.ManagementCompanyUsers.Remove(managementCompanyUser);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ManagementCompanyUserExists(Guid id)
        {
            return _context.ManagementCompanyUsers.Any(e => e.Id == id);
        }
    }
}
