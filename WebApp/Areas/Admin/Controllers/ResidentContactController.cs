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
    public class ResidentContactController : Controller
    {
        private readonly AppDbContext _context;

        public ResidentContactController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ResidentContact
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.ResidentContacts.Include(r => r.Contact).Include(r => r.Resident);
            return View(await appDbContext.ToListAsync());
        }

        // GET: ResidentContact/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var residentContact = await _context.ResidentContacts
                .Include(r => r.Contact)
                .Include(r => r.Resident)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (residentContact == null)
            {
                return NotFound();
            }

            return View(residentContact);
        }

        // GET: ResidentContact/Create
        public IActionResult Create()
        {
            ViewData["ContactId"] = new SelectList(_context.Contacts, "Id", "ContactValue");
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName");
            return View();
        }

        // POST: ResidentContact/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ValidFrom,ValidTo,Confirmed,IsPrimary,ResidentId,ContactId,Id")] ResidentContact residentContact)
        {
            if (ModelState.IsValid)
            {
                residentContact.Id = Guid.NewGuid();
                _context.Add(residentContact);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ContactId"] = new SelectList(_context.Contacts, "Id", "ContactValue", residentContact.ContactId);
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName", residentContact.ResidentId);
            return View(residentContact);
        }

        // GET: ResidentContact/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var residentContact = await _context.ResidentContacts.FindAsync(id);
            if (residentContact == null)
            {
                return NotFound();
            }
            ViewData["ContactId"] = new SelectList(_context.Contacts, "Id", "ContactValue", residentContact.ContactId);
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName", residentContact.ResidentId);
            return View(residentContact);
        }

        // POST: ResidentContact/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("ValidFrom,ValidTo,Confirmed,IsPrimary,ResidentId,ContactId,Id")] ResidentContact residentContact)
        {
            if (id != residentContact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(residentContact);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResidentContactExists(residentContact.Id))
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
            ViewData["ContactId"] = new SelectList(_context.Contacts, "Id", "ContactValue", residentContact.ContactId);
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName", residentContact.ResidentId);
            return View(residentContact);
        }

        // GET: ResidentContact/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var residentContact = await _context.ResidentContacts
                .Include(r => r.Contact)
                .Include(r => r.Resident)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (residentContact == null)
            {
                return NotFound();
            }

            return View(residentContact);
        }

        // POST: ResidentContact/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var residentContact = await _context.ResidentContacts.FindAsync(id);
            if (residentContact != null)
            {
                _context.ResidentContacts.Remove(residentContact);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ResidentContactExists(Guid id)
        {
            return _context.ResidentContacts.Any(e => e.Id == id);
        }
    }
}
