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
    public class TicketPriorityController : Controller
    {
        private readonly AppDbContext _context;

        public TicketPriorityController(AppDbContext context)
        {
            _context = context;
        }

        // GET: TicketPriority
        public async Task<IActionResult> Index()
        {
            return View(await _context.TicketPriorities.ToListAsync());
        }

        // GET: TicketPriority/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticketPriority = await _context.TicketPriorities
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticketPriority == null)
            {
                return NotFound();
            }

            return View(ticketPriority);
        }

        // GET: TicketPriority/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TicketPriority/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Label,Id")] TicketPriority ticketPriority)
        {
            if (ModelState.IsValid)
            {
                ticketPriority.Id = Guid.NewGuid();
                _context.Add(ticketPriority);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ticketPriority);
        }

        // GET: TicketPriority/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticketPriority = await _context.TicketPriorities.FindAsync(id);
            if (ticketPriority == null)
            {
                return NotFound();
            }
            return View(ticketPriority);
        }

        // POST: TicketPriority/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Code,Label,Id")] TicketPriority ticketPriority)
        {
            if (id != ticketPriority.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ticketPriority);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketPriorityExists(ticketPriority.Id))
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
            return View(ticketPriority);
        }

        // GET: TicketPriority/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticketPriority = await _context.TicketPriorities
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticketPriority == null)
            {
                return NotFound();
            }

            return View(ticketPriority);
        }

        // POST: TicketPriority/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var ticketPriority = await _context.TicketPriorities.FindAsync(id);
            if (ticketPriority != null)
            {
                _context.TicketPriorities.Remove(ticketPriority);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TicketPriorityExists(Guid id)
        {
            return _context.TicketPriorities.Any(e => e.Id == id);
        }
    }
}
