using System;
using AzureStorage.BLL;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AzureStorage.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using AzureStorage.Data;
using Microsoft.WindowsAzure.Storage.File;
using System.IO;

namespace AzureStorage.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDBContext _context;

        public HomeController(IConfiguration configuration, ApplicationDBContext context)
        {
            _configuration = configuration;
            _context = context;
        }
        public  IActionResult Index()
        {            
            return View();
        }
        
        #region ADD FILE
        [HttpGet]
        public IActionResult AddFile()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddFile(IFormFile file)
        {
            // VAlidation check IFormFile before this

            if (ModelState.IsValid)
            {
                AzureStorageHelper azureFileStorage = new AzureStorageHelper(_configuration["StorageConString"]);

                Document document = await azureFileStorage.CheckFolderAndCreateAsync("Pictures", "Dog", file);
                if (document != null)
                {
                    _context.Document.Add(document);
                    await _context.SaveChangesAsync();
                }

                return View(nameof(Index));
            }
            return View();
        }
        #endregion

        #region VIEW FILE
        [HttpGet]
        public IActionResult ViewFile()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ViewFile(int? id)
        {
            Document document = _context.Document
                .SingleOrDefault(s => s.DocumentId == id);

            if (document == null)
            {
                return NotFound();
            }

            AzureStorageHelper azureStorageHelper = new AzureStorageHelper(_configuration["StorageConString"]);

            CloudFile file = azureStorageHelper.GetFileAsync(document);

            if (!await file.ExistsAsync())
            {
                return NotFound();
            }

            try
            {
                Stream stream = await file.OpenReadAsync();
                Response.Headers.Add("Content-Disposition", string.Format("inline; filename={0}", document.Name));

                return new FileStreamResult(stream, "image/jpeg");
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region DELETE FILE
        [HttpGet]
        public IActionResult DeleteFile()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFile(int id)
        {
            Document document = _context.Document
               .SingleOrDefault(s => s.DocumentId == id);

            if (document == null)
            {
                return NotFound();
            }

            AzureStorageHelper azureStorageHelper = new AzureStorageHelper(_configuration["StorageConString"]);

            await azureStorageHelper.DeleteDocumentAsync(document);

            return View(nameof(Index));
        } 
        #endregion
        
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
