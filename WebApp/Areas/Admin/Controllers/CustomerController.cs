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
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;

        public CustomerController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Customer
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Customers.Include(c => c.ManagementCompany);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Customer/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(c => c.ManagementCompany)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address");
            return View();
        }

        // POST: Customer/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,RegistryCode,BillingEmail,BillingAddress,Phone,IsActive,CreatedAt,ManagementCompanyId,Id")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                var notes = Request.Form[nameof(Customer.Notes)].ToString();

                customer.Id = Guid.NewGuid();
                if (string.IsNullOrWhiteSpace(notes))
                {
                    customer.Notes = null;
                }
                else
                {
                    customer.Notes = notes;
                }
                _context.Add(customer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address", customer.ManagementCompanyId);
            return View(customer);
        }

        // GET: Customer/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address", customer.ManagementCompanyId);
            return View(customer);
        }

        // POST: Customer/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Name,RegistryCode,BillingEmail,BillingAddress,Phone,IsActive,CreatedAt,ManagementCompanyId,Id")] Customer customer)
        {
            if (id != customer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var entity = await _context.Customers.FindAsync(id);
                if (entity == null) return NotFound();

                var notes = Request.Form[nameof(Customer.Notes)].ToString();

                entity.Name = customer.Name;
                entity.RegistryCode = customer.RegistryCode;
                entity.BillingEmail = customer.BillingEmail;
                entity.BillingAddress = customer.BillingAddress;
                entity.Phone = customer.Phone;
                entity.IsActive = customer.IsActive;
                entity.CreatedAt = customer.CreatedAt;
                entity.ManagementCompanyId = customer.ManagementCompanyId;

                if (string.IsNullOrWhiteSpace(notes))
                {
                    entity.Notes = null;
                }
                else if (entity.Notes == null)
                {
                    entity.Notes = notes;
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
                    if (!CustomerExists(id))
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
            ViewData["ManagementCompanyId"] = new SelectList(_context.ManagementCompanies, "Id", "Address", customer.ManagementCompanyId);
            return View(customer);
        }

        // GET: Customer/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(c => c.ManagementCompany)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CustomerExists(Guid id)
        {
            return _context.Customers.Any(e => e.Id == id);
        }
    }
}
