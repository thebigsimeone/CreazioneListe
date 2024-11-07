using CreazioneListe.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace CreazioneListe.Controllers
{
    public class FileController : Controller
    {
        private readonly IFileService _fileService;

        public FileController(IFileService fileService)
        {
            _fileService = fileService;
        }

        public IActionResult ListaFile(string tenant)
        {
            try
            {
                var files = _fileService.GetFilesList(tenant);
                return View(files);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public IActionResult Download(string tenant, string fileName)
        {
            try
            {
                var file = _fileService.GetFile(tenant, fileName);
                var memory = new MemoryStream();
                using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                {
                    stream.CopyTo(memory);
                }
                memory.Position = 0;

                return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public IActionResult DownloadAll(string tenant)
        {
            try
            {
                var zipFile = _fileService.CreateZipFile(tenant);
                var memory = new MemoryStream();
                using (var stream = new FileStream(zipFile.FullName, FileMode.Open, FileAccess.Read))
                {
                    stream.CopyTo(memory);
                }
                memory.Position = 0;

                return File(memory, "application/zip", zipFile.Name);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
