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
    public class CustomerRepresentativeRoleController : Controller
    {
        private readonly AppDbContext _context;

        public CustomerRepresentativeRoleController(AppDbContext context)
        {
            _context = context;
        }

        // GET: CustomerRepresentativeRole
        public async Task<IActionResult> Index()
        {
            return View(await _context.CustomerRepresentativeRoles.ToListAsync());
        }

        // GET: CustomerRepresentativeRole/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customerRepresentativeRole = await _context.CustomerRepresentativeRoles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customerRepresentativeRole == null)
            {
                return NotFound();
            }

            return View(customerRepresentativeRole);
        }

        // GET: CustomerRepresentativeRole/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CustomerRepresentativeRole/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Label,Id")] CustomerRepresentativeRole customerRepresentativeRole)
        {
            if (ModelState.IsValid)
            {
                customerRepresentativeRole.Id = Guid.NewGuid();
                _context.Add(customerRepresentativeRole);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(customerRepresentativeRole);
        }

        // GET: CustomerRepresentativeRole/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customerRepresentativeRole = await _context.CustomerRepresentativeRoles.FindAsync(id);
            if (customerRepresentativeRole == null)
            {
                return NotFound();
            }
            return View(customerRepresentativeRole);
        }

        // POST: CustomerRepresentativeRole/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Code,Label,Id")] CustomerRepresentativeRole customerRepresentativeRole)
        {
            if (id != customerRepresentativeRole.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(customerRepresentativeRole);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerRepresentativeRoleExists(customerRepresentativeRole.Id))
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
            return View(customerRepresentativeRole);
        }

        // GET: CustomerRepresentativeRole/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customerRepresentativeRole = await _context.CustomerRepresentativeRoles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customerRepresentativeRole == null)
            {
                return NotFound();
            }

            return View(customerRepresentativeRole);
        }

        // POST: CustomerRepresentativeRole/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var customerRepresentativeRole = await _context.CustomerRepresentativeRoles.FindAsync(id);
            if (customerRepresentativeRole != null)
            {
                _context.CustomerRepresentativeRoles.Remove(customerRepresentativeRole);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CustomerRepresentativeRoleExists(Guid id)
        {
            return _context.CustomerRepresentativeRoles.Any(e => e.Id == id);
        }
    }
}
