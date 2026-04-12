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
        public async Task<IActionResult> Create([Bind("Notes,IsActive,VendorId,TicketCategoryId,Id")] VendorTicketCategory vendorTicketCategory)
        {
            if (ModelState.IsValid)
            {
                vendorTicketCategory.Id = Guid.NewGuid();
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
        public async Task<IActionResult> Edit(Guid id, [Bind("Notes,IsActive,VendorId,TicketCategoryId,Id")] VendorTicketCategory vendorTicketCategory)
        {
            if (id != vendorTicketCategory.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vendorTicketCategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VendorTicketCategoryExists(vendorTicketCategory.Id))
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
