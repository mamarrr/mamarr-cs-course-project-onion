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
    public class WorkStatusController : Controller
    {
        private readonly AppDbContext _context;

        public WorkStatusController(AppDbContext context)
        {
            _context = context;
        }

        // GET: WorkStatus
        public async Task<IActionResult> Index()
        {
            return View(await _context.WorkStatuses.ToListAsync());
        }

        // GET: WorkStatus/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workStatus = await _context.WorkStatuses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (workStatus == null)
            {
                return NotFound();
            }

            return View(workStatus);
        }

        // GET: WorkStatus/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: WorkStatus/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Label,Id")] WorkStatus workStatus)
        {
            if (ModelState.IsValid)
            {
                workStatus.Id = Guid.NewGuid();
                _context.Add(workStatus);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(workStatus);
        }

        // GET: WorkStatus/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workStatus = await _context.WorkStatuses.FindAsync(id);
            if (workStatus == null)
            {
                return NotFound();
            }
            return View(workStatus);
        }

        // POST: WorkStatus/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Code,Label,Id")] WorkStatus workStatus)
        {
            if (id != workStatus.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(workStatus);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WorkStatusExists(workStatus.Id))
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
            return View(workStatus);
        }

        // GET: WorkStatus/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workStatus = await _context.WorkStatuses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (workStatus == null)
            {
                return NotFound();
            }

            return View(workStatus);
        }

        // POST: WorkStatus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var workStatus = await _context.WorkStatuses.FindAsync(id);
            if (workStatus != null)
            {
                _context.WorkStatuses.Remove(workStatus);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool WorkStatusExists(Guid id)
        {
            return _context.WorkStatuses.Any(e => e.Id == id);
        }
    }
}
