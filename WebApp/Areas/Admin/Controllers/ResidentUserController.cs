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
    public class ResidentUserController : Controller
    {
        private readonly AppDbContext _context;

        public ResidentUserController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ResidentUser
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.ResidentUsers.Include(r => r.AppUser).Include(r => r.Resident);
            return View(await appDbContext.ToListAsync());
        }

        // GET: ResidentUser/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var residentUser = await _context.ResidentUsers
                .Include(r => r.AppUser)
                .Include(r => r.Resident)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (residentUser == null)
            {
                return NotFound();
            }

            return View(residentUser);
        }

        // GET: ResidentUser/Create
        public IActionResult Create()
        {
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "FirstName");
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName");
            return View();
        }

        // POST: ResidentUser/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CreatedAt,IsActive,ValidFrom,ValidTo,AppUserId,ResidentId,Id")] ResidentUser residentUser)
        {
            if (ModelState.IsValid)
            {
                residentUser.Id = Guid.NewGuid();
                _context.Add(residentUser);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "FirstName", residentUser.AppUserId);
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName", residentUser.ResidentId);
            return View(residentUser);
        }

        // GET: ResidentUser/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var residentUser = await _context.ResidentUsers.FindAsync(id);
            if (residentUser == null)
            {
                return NotFound();
            }
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "FirstName", residentUser.AppUserId);
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName", residentUser.ResidentId);
            return View(residentUser);
        }

        // POST: ResidentUser/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("CreatedAt,IsActive,ValidFrom,ValidTo,AppUserId,ResidentId,Id")] ResidentUser residentUser)
        {
            if (id != residentUser.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(residentUser);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResidentUserExists(residentUser.Id))
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
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "FirstName", residentUser.AppUserId);
            ViewData["ResidentId"] = new SelectList(_context.Residents, "Id", "FirstName", residentUser.ResidentId);
            return View(residentUser);
        }

        // GET: ResidentUser/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var residentUser = await _context.ResidentUsers
                .Include(r => r.AppUser)
                .Include(r => r.Resident)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (residentUser == null)
            {
                return NotFound();
            }

            return View(residentUser);
        }

        // POST: ResidentUser/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var residentUser = await _context.ResidentUsers.FindAsync(id);
            if (residentUser != null)
            {
                _context.ResidentUsers.Remove(residentUser);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ResidentUserExists(Guid id)
        {
            return _context.ResidentUsers.Any(e => e.Id == id);
        }
    }
}
