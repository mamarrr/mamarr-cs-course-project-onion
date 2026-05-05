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
    public class LeaseController : Controller
    {
        private readonly AppDbContext _context;

        public LeaseController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Lease
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Leases.Include(l => l.LeaseRole).Include(l => l.Resident).Include(l => l.Unit);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Lease/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lease = await _context.Leases
                .Include(l => l.LeaseRole)
                .Include(l => l.Resident)
                .Include(l => l.Unit)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (lease == null)
            {
                return NotFound();
            }

            return View(lease);
        }

        // GET: Lease/Create
        public IActionResult Create()
        {
            ViewData["LeaseRoleId"] = new SelectList(_context.LeaseRoles, "Id", "Code");
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName");
            ViewData["UnitId"] = new SelectList(_context.Units, "Id", "UnitNr");
            return View();
        }

        // POST: Lease/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StartDate,EndDate,IsActive,LeaseRoleId,UnitId,ResidentId,Id")] Lease lease)
        {
            if (ModelState.IsValid)
            {
                var notes = Request.Form[nameof(Lease.Notes)].ToString();

                lease.Id = Guid.NewGuid();
                if (string.IsNullOrWhiteSpace(notes))
                {
                    lease.Notes = null;
                }
                else
                {
                    lease.Notes = notes;
                }
                _context.Add(lease);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["LeaseRoleId"] = new SelectList(_context.LeaseRoles, "Id", "Code", lease.LeaseRoleId);
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName", lease.ResidentId);
            ViewData["UnitId"] = new SelectList(_context.Units, "Id", "UnitNr", lease.UnitId);
            return View(lease);
        }

        // GET: Lease/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lease = await _context.Leases.FindAsync(id);
            if (lease == null)
            {
                return NotFound();
            }
            ViewData["LeaseRoleId"] = new SelectList(_context.LeaseRoles, "Id", "Code", lease.LeaseRoleId);
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName", lease.ResidentId);
            ViewData["UnitId"] = new SelectList(_context.Units, "Id", "UnitNr", lease.UnitId);
            return View(lease);
        }

        // POST: Lease/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("StartDate,EndDate,IsActive,LeaseRoleId,UnitId,ResidentId,Id")] Lease lease)
        {
            if (id != lease.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var entity = await _context.Leases.FindAsync(id);
                if (entity == null) return NotFound();

                var notes = Request.Form[nameof(Lease.Notes)].ToString();

                entity.StartDate = lease.StartDate;
                entity.EndDate = lease.EndDate;
                entity.LeaseRoleId = lease.LeaseRoleId;
                entity.UnitId = lease.UnitId;
                entity.ResidentId = lease.ResidentId;

                if (string.IsNullOrWhiteSpace(notes))
                {
                    entity.Notes = null;
                }
                else if (entity.Notes == null)
                {
                    entity.Notes = notes;
                }
                else
                {
                    entity.Notes.SetTranslation(notes);
                }

                try
                {
                    _context.Update(entity);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LeaseExists(id))
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
            ViewData["LeaseRoleId"] = new SelectList(_context.LeaseRoles, "Id", "Code", lease.LeaseRoleId);
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName", lease.ResidentId);
            ViewData["UnitId"] = new SelectList(_context.Units, "Id", "UnitNr", lease.UnitId);
            return View(lease);
        }

        // GET: Lease/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lease = await _context.Leases
                .Include(l => l.LeaseRole)
                .Include(l => l.Resident)
                .Include(l => l.Unit)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (lease == null)
            {
                return NotFound();
            }

            return View(lease);
        }

        // POST: Lease/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var lease = await _context.Leases.FindAsync(id);
            if (lease != null)
            {
                _context.Leases.Remove(lease);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LeaseExists(Guid id)
        {
            return _context.Leases.Any(e => e.Id == id);
        }
    }
}
