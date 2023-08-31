using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using wave_application.Datas;
using wave_application.Models;

namespace wave_application.Controllers
{
    public class ImprimanteController : Controller
    {
        private readonly DefaultContext _context;

        public ImprimanteController(DefaultContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
              return View(await _context.Imprimantes.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,NomPc,NomImprimante")] Imprimante imprimante)
        {
            if (ModelState.IsValid)
            {
                _context.Add(imprimante);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(imprimante);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Imprimantes == null)
            {
                return NotFound();
            }

            var imprimante = await _context.Imprimantes.FindAsync(id);
            if (imprimante == null)
            {
                return NotFound();
            }
            return View(imprimante);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NomPc,NomImprimante")] Imprimante imprimante)
        {
            if (id != imprimante.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(imprimante);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ImprimanteExists(imprimante.Id))
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
            return View(imprimante);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Imprimantes == null)
            {
                return NotFound();
            }

            var imprimante = await _context.Imprimantes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (imprimante == null)
            {
                return NotFound();
            }

            return View(imprimante);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Imprimantes == null)
            {
                return Problem("Entity set 'DefaultContext.Imprimantes'  is null.");
            }
            var imprimante = await _context.Imprimantes.FindAsync(id);
            if (imprimante != null)
            {
                _context.Imprimantes.Remove(imprimante);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ImprimanteExists(int id)
        {
          return _context.Imprimantes.Any(e => e.Id == id);
        }
    }
}
