using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MachineLearningWeb.Data;
using MachineLearningWeb.Models;
using Microsoft.AspNetCore.Authorization;

namespace MachineLearningWeb.Controllers
{
    [Authorize]
    public class MLProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MLProjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: MLProjects
        public async Task<IActionResult> Index()
        {
            return View(await _context.MLProject.ToListAsync());
        }

        // GET: MLProjects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mLProject = await _context.MLProject
                .FirstOrDefaultAsync(m => m.ID == id);
            if (mLProject == null)
            {
                return NotFound();
            }

            return View(mLProject);
        }

        // GET: MLProjects/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MLProjects/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,ProjectType")] MLProject mLProject)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mLProject);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(mLProject);
        }

        // GET: MLProjects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mLProject = await _context.MLProject.FindAsync(id);
            if (mLProject == null)
            {
                return NotFound();
            }
            return View(mLProject);
        }

        // POST: MLProjects/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,ProjectType")] MLProject mLProject)
        {
            if (id != mLProject.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mLProject);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MLProjectExists(mLProject.ID))
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
            return View(mLProject);
        }

        // GET: MLProjects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mLProject = await _context.MLProject
                .FirstOrDefaultAsync(m => m.ID == id);
            if (mLProject == null)
            {
                return NotFound();
            }

            return View(mLProject);
        }

        // POST: MLProjects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mLProject = await _context.MLProject.FindAsync(id);
            _context.MLProject.Remove(mLProject);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MLProjectExists(int id)
        {
            return _context.MLProject.Any(e => e.ID == id);
        }
    }
}
