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
    public class ContactController : Controller
    {
        private readonly AppDbContext _context;

        public ContactController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Contact
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Contacts.Include(c => c.ContactType).Include(c => c.ManagementCompany);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Contact/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.ContactType)
                .Include(c => c.ManagementCompany)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // GET: Contact/Create
        public IActionResult Create()
        {
            ViewData["ContactTypeId"] = new SelectList(_context.ContactTypes, "Id", "Code");
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address");
            return View();
        }

        // POST: Contact/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ContactValue,CreatedAt,ContactTypeId,ManagementCompanyId,Id")] Contact contact)
        {
            if (ModelState.IsValid)
            {
                var notes = Request.Form[nameof(Contact.Notes)].ToString();

                contact.Id = Guid.NewGuid();
                if (string.IsNullOrWhiteSpace(notes))
                {
                    contact.Notes = null;
                }
                else
                {
                    contact.Notes = notes;
                }
                _context.Add(contact);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ContactTypeId"] = new SelectList(_context.ContactTypes, "Id", "Code", contact.ContactTypeId);
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address", contact.ManagementCompanyId);
            return View(contact);
        }

        // GET: Contact/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts.FindAsync(id);
            if (contact == null)
            {
                return NotFound();
            }
            ViewData["ContactTypeId"] = new SelectList(_context.ContactTypes, "Id", "Code", contact.ContactTypeId);
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address", contact.ManagementCompanyId);
            return View(contact);
        }

        // POST: Contact/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("ContactValue,CreatedAt,ContactTypeId,ManagementCompanyId,Id")] Contact contact)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var entity = await _context.Contacts.FindAsync(id);
                if (entity == null) return NotFound();

                var notes = Request.Form[nameof(Contact.Notes)].ToString();

                entity.ContactValue = contact.ContactValue;
                entity.CreatedAt = contact.CreatedAt;
                entity.ContactTypeId = contact.ContactTypeId;
                entity.ManagementCompanyId = contact.ManagementCompanyId;

                if (string.IsNullOrWhiteSpace(notes))
                {
                    entity.Notes = null;
                }
                else if (entity.Notes == null)
                {
                    entity.Notes = notes!;
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
                    if (!ContactExists(id))
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
            ViewData["ContactTypeId"] = new SelectList(_context.ContactTypes, "Id", "Code", contact.ContactTypeId);
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address", contact.ManagementCompanyId);
            return View(contact);
        }

        // GET: Contact/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.ContactType)
                .Include(c => c.ManagementCompany)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contact/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(Guid id)
        {
            return _context.Contacts.Any(e => e.Id == id);
        }
    }
}
