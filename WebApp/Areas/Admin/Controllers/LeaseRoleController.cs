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
    public class LeaseRoleController : Controller
    {
        private readonly AppDbContext _context;

        public LeaseRoleController(AppDbContext context)
        {
            _context = context;
        }

        // GET: LeaseRole
        public async Task<IActionResult> Index()
        {
            return View(await _context.LeaseRoles.ToListAsync());
        }

        // GET: LeaseRole/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leaseRole = await _context.LeaseRoles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (leaseRole == null)
            {
                return NotFound();
            }

            return View(leaseRole);
        }

        // GET: LeaseRole/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: LeaseRole/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Label,Id")] LeaseRole leaseRole)
        {
            if (ModelState.IsValid)
            {
                leaseRole.Id = Guid.NewGuid();
                _context.Add(leaseRole);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(leaseRole);
        }

        // GET: LeaseRole/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leaseRole = await _context.LeaseRoles.FindAsync(id);
            if (leaseRole == null)
            {
                return NotFound();
            }
            return View(leaseRole);
        }

        // POST: LeaseRole/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Code,Label,Id")] LeaseRole leaseRole)
        {
            if (id != leaseRole.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(leaseRole);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LeaseRoleExists(leaseRole.Id))
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
            return View(leaseRole);
        }

        // GET: LeaseRole/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leaseRole = await _context.LeaseRoles
                .FirstOrDefaultAsync(m => m.Id == id);
            if (leaseRole == null)
            {
                return NotFound();
            }

            return View(leaseRole);
        }

        // POST: LeaseRole/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var leaseRole = await _context.LeaseRoles.FindAsync(id);
            if (leaseRole != null)
            {
                _context.LeaseRoles.Remove(leaseRole);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LeaseRoleExists(Guid id)
        {
            return _context.LeaseRoles.Any(e => e.Id == id);
        }
    }
}
