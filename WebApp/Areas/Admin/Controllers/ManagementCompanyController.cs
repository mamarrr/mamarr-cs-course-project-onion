using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.DAL.EF;
using App.Domain;

namespace WebApp.Areas_Admin_Controllers
{
    [Area("Admin")]
    public class ManagementCompanyController : Controller
    {
        private readonly AppDbContext _context;

        public ManagementCompanyController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ManagementCompany
        public async Task<IActionResult> Index()
        {
            return View(await _context.ManagementCompanies.ToListAsync());
        }

        // GET: ManagementCompany/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var managementCompany = await _context.ManagementCompanies
                .FirstOrDefaultAsync(m => m.Id == id);
            if (managementCompany == null)
            {
                return NotFound();
            }

            return View(managementCompany);
        }

        // GET: ManagementCompany/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ManagementCompany/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,RegistryCode,VatNumber,Email,Phone,Address,CreatedAt,IsActive,Id")] ManagementCompany managementCompany)
        {
            if (ModelState.IsValid)
            {
                managementCompany.Id = Guid.NewGuid();
                _context.Add(managementCompany);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(managementCompany);
        }

        // GET: ManagementCompany/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var managementCompany = await _context.ManagementCompanies.FindAsync(id);
            if (managementCompany == null)
            {
                return NotFound();
            }
            return View(managementCompany);
        }

        // POST: ManagementCompany/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Name,RegistryCode,VatNumber,Email,Phone,Address,CreatedAt,IsActive,Id")] ManagementCompany managementCompany)
        {
            if (id != managementCompany.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(managementCompany);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ManagementCompanyExists(managementCompany.Id))
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
            return View(managementCompany);
        }

        // GET: ManagementCompany/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var managementCompany = await _context.ManagementCompanies
                .FirstOrDefaultAsync(m => m.Id == id);
            if (managementCompany == null)
            {
                return NotFound();
            }

            return View(managementCompany);
        }

        // POST: ManagementCompany/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var managementCompany = await _context.ManagementCompanies.FindAsync(id);
            if (managementCompany != null)
            {
                _context.ManagementCompanies.Remove(managementCompany);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ManagementCompanyExists(Guid id)
        {
            return _context.ManagementCompanies.Any(e => e.Id == id);
        }
    }
}
