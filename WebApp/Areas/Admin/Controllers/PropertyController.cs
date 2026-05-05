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
    public class PropertyController : Controller
    {
        private readonly AppDbContext _context;

        public PropertyController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Property
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Properties.Include(p => p.Customer).Include(p => p.PropertyType);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Property/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Include(p => p.Customer)
                .Include(p => p.PropertyType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }

        // GET: Property/Create
        public IActionResult Create()
        {
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "RegistryCode");
            ViewData["PropertyTypeId"] = new SelectList(_context.PropertyTypes, "Id", "Code");
            return View();
        }

        // POST: Property/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AddressLine,City,PostalCode,IsActive,CreatedAt,PropertyTypeId,CustomerId,Id")] Property property)
        {
            if (ModelState.IsValid)
            {
                var label = Request.Form[nameof(Property.Label)].ToString();
                var notes = Request.Form[nameof(Property.Notes)].ToString();

                property.Id = Guid.NewGuid();
                property.Label = label;
                if (string.IsNullOrWhiteSpace(notes))
                {
                    property.Notes = null;
                }
                else
                {
                    property.Notes = notes;
                }
                _context.Add(property);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "RegistryCode", property.CustomerId);
            ViewData["PropertyTypeId"] = new SelectList(_context.PropertyTypes, "Id", "Code", property.PropertyTypeId);
            return View(property);
        }

        // GET: Property/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties.FindAsync(id);
            if (property == null)
            {
                return NotFound();
            }
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "RegistryCode", property.CustomerId);
            ViewData["PropertyTypeId"] = new SelectList(_context.PropertyTypes, "Id", "Code", property.PropertyTypeId);
            return View(property);
        }

        // POST: Property/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("AddressLine,City,PostalCode,IsActive,CreatedAt,PropertyTypeId,CustomerId,Id")] Property property)
        {
            if (id != property.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var entity = await _context.Properties.FindAsync(id);
                if (entity == null) return NotFound();

                var label = Request.Form[nameof(Property.Label)].ToString();
                var notes = Request.Form[nameof(Property.Notes)].ToString();

                entity.AddressLine = property.AddressLine;
                entity.City = property.City;
                entity.PostalCode = property.PostalCode;
                entity.CreatedAt = property.CreatedAt;
                entity.PropertyTypeId = property.PropertyTypeId;
                entity.CustomerId = property.CustomerId;

                entity.Label.SetTranslation(label);
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
                    if (!PropertyExists(id))
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
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "RegistryCode", property.CustomerId);
            ViewData["PropertyTypeId"] = new SelectList(_context.PropertyTypes, "Id", "Code", property.PropertyTypeId);
            return View(property);
        }

        // GET: Property/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Include(p => p.Customer)
                .Include(p => p.PropertyType)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }

        // POST: Property/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property != null)
            {
                _context.Properties.Remove(property);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PropertyExists(Guid id)
        {
            return _context.Properties.Any(e => e.Id == id);
        }
    }
}
