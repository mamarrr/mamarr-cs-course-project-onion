using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.DAL.EF;
using App.Domain;
using WebApp.ViewModels;

namespace WebApp.Areas_Admin_Controllers
{
    [Area("Admin")]
    public class ContactTypeController : Controller
    {
        private readonly AppDbContext _context;

        public ContactTypeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ContactType
        public async Task<IActionResult> Index()
        {
            return View(await _context.ContactTypes.ToListAsync());
        }

        // GET: ContactType/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contactType = await _context.ContactTypes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contactType == null)
            {
                return NotFound();
            }

            return View(contactType);
        }

        // GET: ContactType/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ContactType/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContactTypeCreateViewModel vm)
        {
            if (ModelState.IsValid)
            {
                ContactType contactType = new ContactType
                {
                    Id = Guid.NewGuid(),
                    Code = vm.Code,
                    Label = vm.Label
                };
                _context.Add(contactType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vm);
        }

        // GET: ContactType/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contactType = await _context.ContactTypes.FindAsync(id);
            if (contactType == null)
            {
                return NotFound();
            }
            return View(contactType);
        }

        // POST: ContactType/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Code,Label,Id")] ContactType contactType)
        {
            if (id != contactType.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(contactType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactTypeExists(contactType.Id))
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
            return View(contactType);
        }

        // GET: ContactType/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contactType = await _context.ContactTypes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contactType == null)
            {
                return NotFound();
            }

            return View(contactType);
        }

        // POST: ContactType/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var contactType = await _context.ContactTypes.FindAsync(id);
            if (contactType != null)
            {
                _context.ContactTypes.Remove(contactType);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactTypeExists(Guid id)
        {
            return _context.ContactTypes.Any(e => e.Id == id);
        }
    }
}
