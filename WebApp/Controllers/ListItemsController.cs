using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.DAL.EF;
using App.Domain;
using Microsoft.AspNetCore.Authorization;
using WebApp.ViewModels;

namespace WebApp.Controllers
{
    [Authorize]
    public class ListItemsController : Controller
    {
        private readonly AppDbContext _context;

        public ListItemsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: ListItems
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var res = await _context
                .ListItems
                .Include(l => l.AppUser)
                .Where(l => l.AppUserId == userId)
                .ToListAsync();

            return View(res);
        }

        // GET: ListItems/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetUserId();
            var listItem = await _context.ListItems
                .Include(l => l.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id &&   m.AppUserId == userId);
            if (listItem == null)
            {
                return NotFound();
            }

            return View(listItem);
        }

        // GET: ListItems/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ListItems/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ListItemCreateViewModel vm)
        {
            if (ModelState.IsValid)
            {
                var listItem = new ListItem();
                listItem.AppUserId = GetUserId();
                listItem.ItemDescription = vm.ItemDescription;
                listItem.IsDone = vm.IsDone;
                listItem.Summary = vm.Summary;
                
                _context.Add(listItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(vm);
        }

        // GET: ListItems/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetUserId();

            var listItem = await _context.ListItems
                .FirstOrDefaultAsync(l => l.Id == id && l.AppUserId == userId);
            if (listItem == null)
            {
                return NotFound();
            }

            var vm = new ListItemEditViewModel()
            {
                Id = listItem.Id,
                ItemDescription = listItem.ItemDescription,
                Summary = listItem.Summary,
                IsDone = listItem.IsDone,
            };

            return View(vm);
        }

        // POST: ListItems/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id,
            ListItemEditViewModel vm)
        {
            if (id != vm.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var entity = await _context.ListItems.FindAsync(id);    
                if (entity == null) return NotFound();
                entity.ItemDescription = vm.ItemDescription;
                entity.IsDone = vm.IsDone;
                entity.Summary.SetTranslation(vm.Summary);
                _context.Update(entity);
                
                
                try
                {
                    _context.Update(entity);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ListItemExists(entity.Id))
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

            return View(vm);
        }

        // GET: ListItems/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = GetUserId();
            var listItem = await _context.ListItems
                .Include(l => l.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id &&  m.AppUserId == userId);
            if (listItem == null)
            {
                return NotFound();
            }

            return View(listItem);
        }

        // POST: ListItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var userId = GetUserId();
            var listItem = await _context.ListItems
                .FirstOrDefaultAsync(l  => l.Id == id && l.AppUserId == userId);
            if (listItem != null)
            {
                _context.ListItems.Remove(listItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ListItemExists(Guid id)
        {
            return _context.ListItems.Any(e => e.Id == id);
        }

        private Guid GetUserId()
        {
            var userIdString = User.Claims.First(c =>
                c.Type == ClaimTypes.NameIdentifier).Value;
            var userId = Guid.Parse(userIdString);
            return userId;
        }
    }
}