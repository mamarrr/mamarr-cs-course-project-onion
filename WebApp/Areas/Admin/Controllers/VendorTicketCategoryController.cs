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
    public class VendorTicketCategoryController : Controller
    {
        private readonly AppDbContext _context;

        public VendorTicketCategoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: VendorTicketCategory
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.VendorTicketCategories.Include(v => v.TicketCategory).Include(v => v.Vendor);
            return View(await appDbContext.ToListAsync());
        }

        // GET: VendorTicketCategory/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vendorTicketCategory = await _context.VendorTicketCategories
                .Include(v => v.TicketCategory)
                .Include(v => v.Vendor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vendorTicketCategory == null)
            {
                return NotFound();
            }

            return View(vendorTicketCategory);
        }

        // GET: VendorTicketCategory/Create
        public IActionResult Create()
        {
            ViewData["TicketCategoryId"] = new SelectList(_context.TicketCategories, "Id", "Code");
            ViewData["VendorId"] = new SelectList(_context.Vendors, "Id", "Name");
            return View();
        }

        // POST: VendorTicketCategory/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IsActive,VendorId,TicketCategoryId,Id")] VendorTicketCategory vendorTicketCategory)
        {
            if (ModelState.IsValid)
            {
                var notes = Request.Form[nameof(VendorTicketCategory.Notes)].ToString();

                vendorTicketCategory.Id = Guid.NewGuid();
                if (string.IsNullOrWhiteSpace(notes))
                {
                    vendorTicketCategory.Notes = null;
                }
                else
                {
                    vendorTicketCategory.Notes = notes;
                }
                _context.Add(vendorTicketCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["TicketCategoryId"] = new SelectList(_context.TicketCategories, "Id", "Code", vendorTicketCategory.TicketCategoryId);
            ViewData["VendorId"] = new SelectList(_context.Vendors, "Id", "Name", vendorTicketCategory.VendorId);
            return View(vendorTicketCategory);
        }

        // GET: VendorTicketCategory/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vendorTicketCategory = await _context.VendorTicketCategories.FindAsync(id);
            if (vendorTicketCategory == null)
            {
                return NotFound();
            }
            ViewData["TicketCategoryId"] = new SelectList(_context.TicketCategories, "Id", "Code", vendorTicketCategory.TicketCategoryId);
            ViewData["VendorId"] = new SelectList(_context.Vendors, "Id", "Name", vendorTicketCategory.VendorId);
            return View(vendorTicketCategory);
        }

        // POST: VendorTicketCategory/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("IsActive,VendorId,TicketCategoryId,Id")] VendorTicketCategory vendorTicketCategory)
        {
            if (id != vendorTicketCategory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var entity = await _context.VendorTicketCategories.FindAsync(id);
                if (entity == null) return NotFound();

                var notes = Request.Form[nameof(VendorTicketCategory.Notes)].ToString();

                entity.IsActive = vendorTicketCategory.IsActive;
                entity.VendorId = vendorTicketCategory.VendorId;
                entity.TicketCategoryId = vendorTicketCategory.TicketCategoryId;

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
                    if (!VendorTicketCategoryExists(id))
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
            ViewData["TicketCategoryId"] = new SelectList(_context.TicketCategories, "Id", "Code", vendorTicketCategory.TicketCategoryId);
            ViewData["VendorId"] = new SelectList(_context.Vendors, "Id", "Name", vendorTicketCategory.VendorId);
            return View(vendorTicketCategory);
        }

        // GET: VendorTicketCategory/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vendorTicketCategory = await _context.VendorTicketCategories
                .Include(v => v.TicketCategory)
                .Include(v => v.Vendor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vendorTicketCategory == null)
            {
                return NotFound();
            }

            return View(vendorTicketCategory);
        }

        // POST: VendorTicketCategory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var vendorTicketCategory = await _context.VendorTicketCategories.FindAsync(id);
            if (vendorTicketCategory != null)
            {
                _context.VendorTicketCategories.Remove(vendorTicketCategory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VendorTicketCategoryExists(Guid id)
        {
            return _context.VendorTicketCategories.Any(e => e.Id == id);
        }
    }
}
