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
    public class VendorContactController : Controller
    {
        private readonly AppDbContext _context;

        public VendorContactController(AppDbContext context)
        {
            _context = context;
        }

        // GET: VendorContact
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.VendorContacts.Include(v => v.Contact).Include(v => v.Vendor);
            return View(await appDbContext.ToListAsync());
        }

        // GET: VendorContact/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vendorContact = await _context.VendorContacts
                .Include(v => v.Contact)
                .Include(v => v.Vendor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vendorContact == null)
            {
                return NotFound();
            }

            return View(vendorContact);
        }

        // GET: VendorContact/Create
        public IActionResult Create()
        {
            ViewData["ContactId"] = new SelectList(_context.Contacts, "Id", "ContactValue");
            ViewData["VendorId"] = new SelectList(_context.Vendors, "Id", "Name");
            return View();
        }

        // POST: VendorContact/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ValidFrom,ValidTo,Confirmed,IsPrimary,FullName,RoleTitle,ContactId,VendorId,Id")] VendorContact vendorContact)
        {
            if (ModelState.IsValid)
            {
                vendorContact.Id = Guid.NewGuid();
                _context.Add(vendorContact);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ContactId"] = new SelectList(_context.Contacts, "Id", "ContactValue", vendorContact.ContactId);
            ViewData["VendorId"] = new SelectList(_context.Vendors, "Id", "Name", vendorContact.VendorId);
            return View(vendorContact);
        }

        // GET: VendorContact/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vendorContact = await _context.VendorContacts.FindAsync(id);
            if (vendorContact == null)
            {
                return NotFound();
            }
            ViewData["ContactId"] = new SelectList(_context.Contacts, "Id", "ContactValue", vendorContact.ContactId);
            ViewData["VendorId"] = new SelectList(_context.Vendors, "Id", "Name", vendorContact.VendorId);
            return View(vendorContact);
        }

        // POST: VendorContact/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("ValidFrom,ValidTo,Confirmed,IsPrimary,FullName,RoleTitle,ContactId,VendorId,Id")] VendorContact vendorContact)
        {
            if (id != vendorContact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vendorContact);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VendorContactExists(vendorContact.Id))
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
            ViewData["ContactId"] = new SelectList(_context.Contacts, "Id", "ContactValue", vendorContact.ContactId);
            ViewData["VendorId"] = new SelectList(_context.Vendors, "Id", "Name", vendorContact.VendorId);
            return View(vendorContact);
        }

        // GET: VendorContact/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vendorContact = await _context.VendorContacts
                .Include(v => v.Contact)
                .Include(v => v.Vendor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vendorContact == null)
            {
                return NotFound();
            }

            return View(vendorContact);
        }

        // POST: VendorContact/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var vendorContact = await _context.VendorContacts.FindAsync(id);
            if (vendorContact != null)
            {
                _context.VendorContacts.Remove(vendorContact);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VendorContactExists(Guid id)
        {
            return _context.VendorContacts.Any(e => e.Id == id);
        }
    }
}
