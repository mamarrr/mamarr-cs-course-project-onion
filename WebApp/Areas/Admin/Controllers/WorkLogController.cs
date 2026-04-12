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
    public class WorkLogController : Controller
    {
        private readonly AppDbContext _context;

        public WorkLogController(AppDbContext context)
        {
            _context = context;
        }

        // GET: WorkLog
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.WorkLogs.Include(w => w.AppUser).Include(w => w.ScheduledWork);
            return View(await appDbContext.ToListAsync());
        }

        // GET: WorkLog/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workLog = await _context.WorkLogs
                .Include(w => w.AppUser)
                .Include(w => w.ScheduledWork)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (workLog == null)
            {
                return NotFound();
            }

            return View(workLog);
        }

        // GET: WorkLog/Create
        public IActionResult Create()
        {
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "FirstName");
            ViewData["ScheduledWorkId"] = new SelectList(_context.ScheduledWorks, "Id", "Id");
            return View();
        }

        // POST: WorkLog/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CreatedAt,WorkStart,WorkEnd,Hours,MaterialCost,LaborCost,Description,AppUserId,ScheduledWorkId,Id")] WorkLog workLog)
        {
            if (ModelState.IsValid)
            {
                workLog.Id = Guid.NewGuid();
                _context.Add(workLog);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "FirstName", workLog.AppUserId);
            ViewData["ScheduledWorkId"] = new SelectList(_context.ScheduledWorks, "Id", "Id", workLog.ScheduledWorkId);
            return View(workLog);
        }

        // GET: WorkLog/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workLog = await _context.WorkLogs.FindAsync(id);
            if (workLog == null)
            {
                return NotFound();
            }
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "FirstName", workLog.AppUserId);
            ViewData["ScheduledWorkId"] = new SelectList(_context.ScheduledWorks, "Id", "Id", workLog.ScheduledWorkId);
            return View(workLog);
        }

        // POST: WorkLog/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("CreatedAt,WorkStart,WorkEnd,Hours,MaterialCost,LaborCost,Description,AppUserId,ScheduledWorkId,Id")] WorkLog workLog)
        {
            if (id != workLog.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(workLog);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WorkLogExists(workLog.Id))
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
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "FirstName", workLog.AppUserId);
            ViewData["ScheduledWorkId"] = new SelectList(_context.ScheduledWorks, "Id", "Id", workLog.ScheduledWorkId);
            return View(workLog);
        }

        // GET: WorkLog/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workLog = await _context.WorkLogs
                .Include(w => w.AppUser)
                .Include(w => w.ScheduledWork)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (workLog == null)
            {
                return NotFound();
            }

            return View(workLog);
        }

        // POST: WorkLog/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var workLog = await _context.WorkLogs.FindAsync(id);
            if (workLog != null)
            {
                _context.WorkLogs.Remove(workLog);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool WorkLogExists(Guid id)
        {
            return _context.WorkLogs.Any(e => e.Id == id);
        }
    }
}
