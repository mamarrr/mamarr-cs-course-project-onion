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
    public class TicketStatusController : Controller
    {
        private readonly AppDbContext _context;

        public TicketStatusController(AppDbContext context)
        {
            _context = context;
        }

        // GET: TicketStatus
        public async Task<IActionResult> Index()
        {
            return View(await _context.TicketStatuses.ToListAsync());
        }

        // GET: TicketStatus/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticketStatus = await _context.TicketStatuses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticketStatus == null)
            {
                return NotFound();
            }

            return View(ticketStatus);
        }

        // GET: TicketStatus/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TicketStatus/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Label,Id")] TicketStatus ticketStatus)
        {
            if (ModelState.IsValid)
            {
                ticketStatus.Id = Guid.NewGuid();
                _context.Add(ticketStatus);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ticketStatus);
        }

        // GET: TicketStatus/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticketStatus = await _context.TicketStatuses.FindAsync(id);
            if (ticketStatus == null)
            {
                return NotFound();
            }
            return View(ticketStatus);
        }

        // POST: TicketStatus/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Code,Label,Id")] TicketStatus ticketStatus)
        {
            if (id != ticketStatus.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ticketStatus);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketStatusExists(ticketStatus.Id))
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
            return View(ticketStatus);
        }

        // GET: TicketStatus/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ticketStatus = await _context.TicketStatuses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticketStatus == null)
            {
                return NotFound();
            }

            return View(ticketStatus);
        }

        // POST: TicketStatus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var ticketStatus = await _context.TicketStatuses.FindAsync(id);
            if (ticketStatus != null)
            {
                _context.TicketStatuses.Remove(ticketStatus);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TicketStatusExists(Guid id)
        {
            return _context.TicketStatuses.Any(e => e.Id == id);
        }
    }
}
