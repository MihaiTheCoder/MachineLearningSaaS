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
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MachineLearningWeb.Helpers;
using System.IO;
using System.Threading;
using System.Diagnostics;

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

        private string GetUserID()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // GET: MLProjects
        public async Task<IActionResult> Index()
        {            
            return View(await _context.MLProject.Where(p => p.OwnerId == GetUserID()).ToListAsync());
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
            if (mLProject == null || mLProject.OwnerId != GetUserID())
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
        public async Task<IActionResult> Create([Bind("ID,ProjectType,ProjectName")] MLProject mLProject)
        {
            if (ModelState.IsValid)
            {
                mLProject.OwnerId = GetUserID();
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
            if (mLProject == null || mLProject.OwnerId != GetUserID())
            {
                return NotFound();
            }
            return View(mLProject);
        }

        [HttpGet("MLProjects/CreateImageModels/{id}")]
        public async Task<ActionResult> CreateImageModels(int id)
        {
            return View(new ImageModel { ProjectId = id });
        }

        [HttpPost]
        public async Task UploadImage(int projectId, IFormFile file, CancellationToken cancellationToken)
        {
            Debug.Write("UploadImage Started");
            if (file == null)
                return;

            if (cancellationToken.IsCancellationRequested)
                return;

            var secureFileName = FileHelper.GetSecureFileName(file.FileName);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles", secureFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream, cancellationToken);
            }
            ImageModel imageModel = new ImageModel { ProjectId = projectId, FileName = secureFileName };

            _context.Add(imageModel);
            await _context.SaveChangesAsync();
            Debug.Write("UploadImage Ended");
        }

        // POST: MLProjects/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,ProjectType,ProjectName")] MLProject mLProject)
        {
            if (id != mLProject.ID)
            {
                return NotFound();
            }

            if (!_context.MLProject.Any(proj => proj.ID == id && proj.OwnerId == GetUserID()))
                return NotFound();

            mLProject.OwnerId = GetUserID();

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
            if (mLProject == null || mLProject.OwnerId != GetUserID())
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
            if (mLProject.OwnerId != GetUserID())
                return NotFound();

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
