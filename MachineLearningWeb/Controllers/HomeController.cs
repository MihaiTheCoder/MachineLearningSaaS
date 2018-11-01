using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MachineLearningWeb.Models;
using System.IO;
using System.ComponentModel.DataAnnotations;

namespace MachineLearningWeb.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        Dictionary<string, string> IMAGE_EXTENSIONS = new Dictionary<string, string> { { ".jpg", "image/jpeg" }, { ".jpeg", "image/jpeg" }, { ".png", "image/png" } };

        [HttpGet("staticfiles/{fileName}")]
        public IActionResult StaticFile([Required]string fileName)
        {
            var secureFileName = GetSecureFileName(fileName);
            var file = Path.Combine(Directory.GetCurrentDirectory(), "StaticFiles", secureFileName);
            string extension = Path.GetExtension(secureFileName)?.ToLower();

            if (extension == null || !IMAGE_EXTENSIONS.ContainsKey(extension))
                return BadRequest("Invalid image extension");

            return PhysicalFile(file, IMAGE_EXTENSIONS[extension]);
        }

        private string GetSecureFileName(string fileName)
        {
            var invalids = System.IO.Path.GetInvalidFileNameChars();
            var newName = String.Join("_", fileName.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            return newName;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
