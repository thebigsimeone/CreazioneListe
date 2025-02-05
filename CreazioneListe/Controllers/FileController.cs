using CreazioneListe.Interfaces;
using CreazioneListe.Services;
using Microsoft.AspNetCore.Mvc;
using Renci.SshNet;
using System.IO;

namespace CreazioneListe.Controllers
{
    public class FileController : Controller
    {
        private readonly IFileService _fileService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileController> _logger;

        public FileController(IFileService fileService, IConfiguration configuration, ILogger<FileController> logger)
        {
            _fileService = fileService;
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult ListaFile(string tenant)
        {
            if (string.IsNullOrEmpty(tenant))
            {
                return BadRequest("Tenant non specificato.");
            }

            try
            {
                var files = _fileService.GetFilesList(tenant);
                ViewBag.Tenant = tenant;
                return View(files);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public IActionResult Download(string tenant, string fileName)
        {
            if (string.IsNullOrEmpty(tenant))
            {
                return BadRequest("Tenant non specificato.");
            }

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
            if (string.IsNullOrEmpty(tenant))
            {
                return BadRequest("Tenant non specificato.");
            }

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

        [HttpPost]
        public async Task<IActionResult> SendFile([FromForm] string fileName, [FromForm] string tenant, [FromForm] string selectedDirectory)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(tenant) || string.IsNullOrEmpty(selectedDirectory))
            {
                return BadRequest("Parametri non validi.");
            }

            try
            {
                // Recupera il file dal server
                var file = _fileService.GetFile(tenant, fileName);

                using (var fileStream = file.OpenRead())
                {
                    // **Passaggio 1: Caricare il file nell'area SFTP**
                    var sftpService = new SftpService("access854988094.webspace-data.io", 22, "acc30641284", "5zgeHOyDnC");
                    using (var client = new SftpClient("access854988094.webspace-data.io", 22, "acc30641284", "5zgeHOyDnC"))
                    {
                        client.Connect();
                        client.UploadFile(fileStream, $"{selectedDirectory}/{file.Name}");
                        client.Disconnect();
                    }

                    // **Passaggio 2: Inviare il file all'API remota**
                    using (var client = new HttpClient())
                    {
                        using (var multipartFormDataContent = new MultipartFormDataContent())
                        {
                            fileStream.Position = 0; // Reset del file stream per il secondo utilizzo
                            var fileContent = new StreamContent(fileStream);
                            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                            multipartFormDataContent.Add(fileContent, "file", file.Name);

                            var response = await client.PostAsync("http://192.168.50.207:5280/api/Create/upload", multipartFormDataContent);

                            if (!response.IsSuccessStatusCode)
                            {
                                return StatusCode((int)response.StatusCode, $"Errore nell'invio all'API: {await response.Content.ReadAsStringAsync()}");
                            }
                        }
                    }
                }

                return Ok($"File {fileName} inviato con successo a {selectedDirectory} e all'API esterna.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'invio del file {FileName} alla directory {Directory} e all'API.", fileName, selectedDirectory);
                return StatusCode(500, $"Errore durante l'invio del file: {ex.Message}");
            }
        }
    }
}
