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
    public class CustomerRepresentativeController : Controller
    {
        private readonly AppDbContext _context;

        public CustomerRepresentativeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: CustomerRepresentative
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.CustomerRepresentatives.Include(c => c.Customer).Include(c => c.CustomerRepresentativeRole).Include(c => c.Resident);
            return View(await appDbContext.ToListAsync());
        }

        // GET: CustomerRepresentative/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customerRepresentative = await _context.CustomerRepresentatives
                .Include(c => c.Customer)
                .Include(c => c.CustomerRepresentativeRole)
                .Include(c => c.Resident)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customerRepresentative == null)
            {
                return NotFound();
            }

            return View(customerRepresentative);
        }

        // GET: CustomerRepresentative/Create
        public IActionResult Create()
        {
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "RegistryCode");
            ViewData["CustomerRepresentativeRoleId"] = new SelectList(_context.CustomerRepresentativeRoles, "Id", "Code");
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName");
            return View();
        }

        // POST: CustomerRepresentative/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ValidFrom,ValidTo,IsActive,CreatedAt,CustomerRepresentativeRoleId,CustomerId,ResidentId,Id")] CustomerRepresentative customerRepresentative)
        {
            if (ModelState.IsValid)
            {
                var notes = Request.Form[nameof(CustomerRepresentative.Notes)].ToString();

                customerRepresentative.Id = Guid.NewGuid();
                if (string.IsNullOrWhiteSpace(notes))
                {
                    customerRepresentative.Notes = null;
                }
                else
                {
                    customerRepresentative.Notes = notes;
                }
                _context.Add(customerRepresentative);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "RegistryCode", customerRepresentative.CustomerId);
            ViewData["CustomerRepresentativeRoleId"] = new SelectList(_context.CustomerRepresentativeRoles, "Id", "Code", customerRepresentative.CustomerRepresentativeRoleId);
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName", customerRepresentative.ResidentId);
            return View(customerRepresentative);
        }

        // GET: CustomerRepresentative/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customerRepresentative = await _context.CustomerRepresentatives.FindAsync(id);
            if (customerRepresentative == null)
            {
                return NotFound();
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "RegistryCode", customerRepresentative.CustomerId);
            ViewData["CustomerRepresentativeRoleId"] = new SelectList(_context.CustomerRepresentativeRoles, "Id", "Code", customerRepresentative.CustomerRepresentativeRoleId);
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName", customerRepresentative.ResidentId);
            return View(customerRepresentative);
        }

        // POST: CustomerRepresentative/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("ValidFrom,ValidTo,IsActive,CreatedAt,CustomerRepresentativeRoleId,CustomerId,ResidentId,Id")] CustomerRepresentative customerRepresentative)
        {
            if (id != customerRepresentative.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var entity = await _context.CustomerRepresentatives.FindAsync(id);
                if (entity == null) return NotFound();

                var notes = Request.Form[nameof(CustomerRepresentative.Notes)].ToString();

                entity.ValidFrom = customerRepresentative.ValidFrom;
                entity.ValidTo = customerRepresentative.ValidTo;
                entity.IsActive = customerRepresentative.IsActive;
                entity.CreatedAt = customerRepresentative.CreatedAt;
                entity.CustomerRepresentativeRoleId = customerRepresentative.CustomerRepresentativeRoleId;
                entity.CustomerId = customerRepresentative.CustomerId;
                entity.ResidentId = customerRepresentative.ResidentId;

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
                    if (!CustomerRepresentativeExists(id))
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
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "RegistryCode", customerRepresentative.CustomerId);
            ViewData["CustomerRepresentativeRoleId"] = new SelectList(_context.CustomerRepresentativeRoles, "Id", "Code", customerRepresentative.CustomerRepresentativeRoleId);
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName", customerRepresentative.ResidentId);
            return View(customerRepresentative);
        }

        // GET: CustomerRepresentative/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customerRepresentative = await _context.CustomerRepresentatives
                .Include(c => c.Customer)
                .Include(c => c.CustomerRepresentativeRole)
                .Include(c => c.Resident)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customerRepresentative == null)
            {
                return NotFound();
            }

            return View(customerRepresentative);
        }

        // POST: CustomerRepresentative/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var customerRepresentative = await _context.CustomerRepresentatives.FindAsync(id);
            if (customerRepresentative != null)
            {
                _context.CustomerRepresentatives.Remove(customerRepresentative);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CustomerRepresentativeExists(Guid id)
        {
            return _context.CustomerRepresentatives.Any(e => e.Id == id);
        }
    }
}
