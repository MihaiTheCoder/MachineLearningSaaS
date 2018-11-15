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
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;
using System.Drawing;

namespace MachineLearningWeb.Controllers
{
    [Authorize]
    public class MLProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _memoryCache;

        public MLProjectsController(ApplicationDbContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
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

        [HttpPost("MLProjects/Tags/{projectId}")]
        public async Task<IActionResult> CreateTag(int projectId, string tagName)
        {
            var project = _context.MLProject.Find(projectId);
            var tag = new Tag { ProjectId = project.ID, Name = tagName };
            if (project == null || project.OwnerId != GetUserID())
            {
                return NotFound();
            }
            if (string.IsNullOrWhiteSpace(tagName))
                return BadRequest();

            _context.Add(tag);
            await _context.SaveChangesAsync();

            var tags = _context.Tags.Where(t => t.ProjectId == project.ID);
            var tagShortcut = tags.Count(t => t.ID < tag.ID) + 1;
            tag.TagShortcut = tagShortcut;

            await _context.SaveChangesAsync();

            return PartialView("_Tags", tags);
        }

        [HttpPost("MLProjects/ImageTags")]
        public async Task<IActionResult> AddImageTag(ImageTag imageTag)
        {
            var project = _context.ImageModel.Include(i => i.Project).FirstOrDefault(i => i.ID == imageTag.ImageId)?.Project;

            if (project == null || project.OwnerId != GetUserID())
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var imageTags = _context.ImageTags.Where(i => i.ImageId == imageTag.ImageId).ToList();
                var imageTagSameRegion = imageTags.FirstOrDefault(i => i.RelativeCoords.Equals(imageTag.RelativeCoords));
                if (imageTagSameRegion == null)
                    _context.Add(imageTag);
                else
                    imageTagSameRegion.TagId = imageTag.TagId;

                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpPut("MLProjects/ImageTags")]
        public async Task<IActionResult> UpdateImageTag(ImageTag imageTag)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var imageTagDb =_context.ImageTags.Include(it => it.Image.Project).FirstOrDefault(it => it.ID == imageTag.ID);
            var project = imageTagDb?.Image.Project;

            if (project == null || project.OwnerId != GetUserID())
            {
                return NotFound();
            }
            _context.Entry(imageTagDb).State = EntityState.Detached;
            _context.Update(imageTag);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("MLProjects/ImageTags/{imageTagID}")]
        public async Task<IActionResult> DeleteImageTag(int imageTagID)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var imageTag = _context.ImageTags.Include(it => it.Image.Project).FirstOrDefault(it => it.ID == imageTagID);
            var project = imageTag?.Image.Project;
            if (project == null || project.OwnerId != GetUserID())
            {
                return NotFound();
            }

            _context.ImageTags.Remove(imageTag);
            await _context.SaveChangesAsync();
            return Ok();
        }
            

        // GET: MLProjects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mLProjectTask = _context.MLProject.FindAsync(id);
            var mLProjectImages = _context.ImageModel.Include(i=>i.ImageTags).Where(i => i.ProjectId == id).ToListAsync();
            var mLProjectTags =_context.Tags.Where(t => t.ProjectId == id).ToListAsync();
            await Task.WhenAll(new Task[] {mLProjectTask, mLProjectImages, mLProjectTags });
            var mlProject = mLProjectTask.Result;
            mlProject.Images = mLProjectImages.Result;
            mlProject.Tags = mLProjectTags.Result;
            
            _memoryCache.Set($"images_${mlProject.ID}", mlProject.Images);

            if (mlProject == null || mlProject.OwnerId != GetUserID())
            {
                return NotFound();
            }
            return View(mlProject);
        }

        private void CacheImageModels(ICollection<ImageModel> images)
        {
            _memoryCache.Set(GetImagesKey(), images, DateTimeOffset.FromUnixTimeSeconds(3));
        }

        private string GetImagesKey()
        {
            return $"images_{GetUserID()}";
        }

        private string GetFileName(int imageModelId)
        {
            var images = _memoryCache.GetOrCreate(GetImagesKey(), cacheEntry => GetImagesFromSameProject(imageModelId));
            ImageModel image;
            images.TryGetValue(imageModelId, out image);
            return image?.FileName;
        }

        private Dictionary<int, ImageModel> GetImagesFromSameProject(int imageModelId)
        {
            var imageModel = _context.ImageModel.Include(i => i.Project).FirstOrDefault(i => i.ID == imageModelId);
            if (imageModel == null || imageModel.Project.OwnerId != GetUserID())
                return default(Dictionary<int, ImageModel>);
            else
                return _context.ImageModel.Where(i => i.ProjectId == imageModel.Project.ID).ToDictionary(i => i.ID, i => i);
        }

        Dictionary<string, string> IMAGE_EXTENSIONS = new Dictionary<string, string> { { ".jpg", "image/jpeg" }, { ".jpeg", "image/jpeg" }, { ".png", "image/png" } };

        public IActionResult Images(int imageModelId)
        {            
            var fileName = GetFileName(imageModelId);

            if (fileName == null)
                return BadRequest("File does not exist");

            var file = Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles", fileName);
            string extension = Path.GetExtension(fileName)?.ToLower();

            return PhysicalFile(file, IMAGE_EXTENSIONS[extension]);
        }

        public IActionResult Thumb(int imageModelId)
        {
            var fileName = GetFileName(imageModelId);
            string extension = Path.GetExtension(fileName)?.ToLower();

            if (fileName == null)
                return BadRequest("File does not exist");

            var directory = Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles", "Thumbs", "120");
            var thumbPath = Path.Combine(directory, fileName);
            if(!System.IO.File.Exists(thumbPath))
            {
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var originalFilePath = Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles", fileName);
                Image image = Image.FromFile(originalFilePath);
                Image thumb = image.GetThumbnailImage(120, 120, () => false, IntPtr.Zero);
                thumb.Save(thumbPath);
            }


            return PhysicalFile(thumbPath, IMAGE_EXTENSIONS[extension]);
        }

        [HttpGet("MLProjects/CreateImageModels/{id}")]
        public ActionResult CreateImageModels(int id)
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

            string extension = Path.GetExtension(file.FileName)?.ToLower();

            if (extension == null || !IMAGE_EXTENSIONS.ContainsKey(extension))
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
