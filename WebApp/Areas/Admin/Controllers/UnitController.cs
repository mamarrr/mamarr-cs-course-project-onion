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
    public class UnitController : Controller
    {
        private readonly AppDbContext _context;

        public UnitController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Unit
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Units.Include(u => u.Property);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Unit/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var unit = await _context.Units
                .Include(u => u.Property)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (unit == null)
            {
                return NotFound();
            }

            return View(unit);
        }

        // GET: Unit/Create
        public IActionResult Create()
        {
            ViewData["PropertyId"] = new SelectList(_context.Properties, "Id", "AddressLine");
            return View();
        }

        // POST: Unit/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UnitNr,FloorNr,SizeM2,IsActive,CreatedAt,PropertyId,Id")] Unit unit)
        {
            if (ModelState.IsValid)
            {
                var notes = Request.Form[nameof(Unit.Notes)].ToString();

                unit.Id = Guid.NewGuid();
                if (string.IsNullOrWhiteSpace(notes))
                {
                    unit.Notes = null;
                }
                else
                {
                    unit.Notes = notes;
                }
                _context.Add(unit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PropertyId"] = new SelectList(_context.Properties, "Id", "AddressLine", unit.PropertyId);
            return View(unit);
        }

        // GET: Unit/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var unit = await _context.Units.FindAsync(id);
            if (unit == null)
            {
                return NotFound();
            }
            ViewData["PropertyId"] = new SelectList(_context.Properties, "Id", "AddressLine", unit.PropertyId);
            return View(unit);
        }

        // POST: Unit/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("UnitNr,FloorNr,SizeM2,IsActive,CreatedAt,PropertyId,Id")] Unit unit)
        {
            if (id != unit.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var entity = await _context.Units.FindAsync(id);
                if (entity == null) return NotFound();

                var notes = Request.Form[nameof(Unit.Notes)].ToString();

                entity.UnitNr = unit.UnitNr;
                entity.FloorNr = unit.FloorNr;
                entity.SizeM2 = unit.SizeM2;
                entity.IsActive = unit.IsActive;
                entity.CreatedAt = unit.CreatedAt;
                entity.PropertyId = unit.PropertyId;

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
                    if (!UnitExists(id))
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
            ViewData["PropertyId"] = new SelectList(_context.Properties, "Id", "AddressLine", unit.PropertyId);
            return View(unit);
        }

        // GET: Unit/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var unit = await _context.Units
                .Include(u => u.Property)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (unit == null)
            {
                return NotFound();
            }

            return View(unit);
        }

        // POST: Unit/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var unit = await _context.Units.FindAsync(id);
            if (unit != null)
            {
                _context.Units.Remove(unit);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UnitExists(Guid id)
        {
            return _context.Units.Any(e => e.Id == id);
        }
    }
}
