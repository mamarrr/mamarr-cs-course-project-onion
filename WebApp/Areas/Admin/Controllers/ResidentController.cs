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
    public class ResidentController : Controller
    {
        private readonly AppDbContext _context;

        public ResidentController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Resident
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Residents.Include(r => r.ManagementCompany);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Resident/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resident = await _context.Residents
                .Include(r => r.ManagementCompany)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (resident == null)
            {
                return NotFound();
            }

            return View(resident);
        }

        // GET: Resident/Create
        public IActionResult Create()
        {
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address");
            return View();
        }

        // POST: Resident/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,IdCode,PreferredLanguage,IsActive,CreatedAt,ManagementCompanyId,Id")] Resident resident)
        {
            if (ModelState.IsValid)
            {
                resident.Id = Guid.NewGuid();
                _context.Add(resident);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address", resident.ManagementCompanyId);
            return View(resident);
        }

        // GET: Resident/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resident = await _context.Residents.FindAsync(id);
            if (resident == null)
            {
                return NotFound();
            }
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address", resident.ManagementCompanyId);
            return View(resident);
        }

        // POST: Resident/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("FirstName,LastName,IdCode,PreferredLanguage,IsActive,CreatedAt,ManagementCompanyId,Id")] Resident resident)
        {
            if (id != resident.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(resident);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResidentExists(resident.Id))
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
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address", resident.ManagementCompanyId);
            return View(resident);
        }

        // GET: Resident/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resident = await _context.Residents
                .Include(r => r.ManagementCompany)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (resident == null)
            {
                return NotFound();
            }

            return View(resident);
        }

        // POST: Resident/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var resident = await _context.Residents.FindAsync(id);
            if (resident != null)
            {
                _context.Residents.Remove(resident);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ResidentExists(Guid id)
        {
            return _context.Residents.Any(e => e.Id == id);
        }
    }
}
